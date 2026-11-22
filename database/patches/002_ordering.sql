-- =============================================================================
-- Patch 002: ordering through database functions.
-- For a project that was ALREADY created with an older schema.sql. A fresh
-- schema.sql run already includes this, so there's no need to run it there.
-- Safe to re-run (create or replace / if not exists).
--
-- WHY FUNCTIONS, NOT GRANTS
-- The publishable key is public. If anon could INSERT into orders or UPDATE
-- watches, anyone could create an order at any price or mark any watch Sold.
-- So anon gets NO write grant on orders or watches. Instead these functions
-- do the whole operation in ONE transaction, enforce the rules, and copy the
-- price from the database. anon may only EXECUTE them.
--
-- security definer = the function runs with its OWNER's rights (that's how it
-- can write when anon can't). That is powerful, so every function validates
-- every input, uses fully qualified names and pins search_path.
--
-- Errors use stable message keys (errcode P0001) that the app maps to text:
--   WATCH_NOT_FOUND, WATCH_NOT_AVAILABLE, BUYER_NOT_FOUND, CANNOT_BUY_OWN_WATCH,
--   ORDER_NOT_FOUND, ORDER_NOT_CANCELLABLE, ORDER_NOT_CONFIRMABLE
-- =============================================================================

-- Order numbers: LW-<year>-0001, 0002, … (orders start empty: the seed is catalog only).
-- No grant to anon: only the functions (running as owner) call nextval().
create sequence if not exists public.order_number_seq start 1;


-- -----------------------------------------------------------------------------
-- place_order: Active watch → new PendingPayment order + watch Reserved.
-- -----------------------------------------------------------------------------
create or replace function public.place_order(p_watch_id uuid, p_buyer_id uuid)
returns public.orders
language plpgsql
security definer
set search_path = public
as $$
-- TODO (auth): replace p_buyer_id with auth.uid(). Under the class policy any caller can act as any buyer.
declare
    v_watch    public.watches%rowtype;
    v_owner_id uuid;
    v_seq      bigint;
    v_order    public.orders%rowtype;
begin
    if p_watch_id is null then
        raise exception 'WATCH_NOT_FOUND' using errcode = 'P0001';
    end if;
    if p_buyer_id is null then
        raise exception 'BUYER_NOT_FOUND' using errcode = 'P0001';
    end if;

    -- Lock the watch row: a second buyer waits here, then sees status <> 'Active'.
    select * into v_watch
    from public.watches
    where id = p_watch_id
    for update;

    if not found then
        raise exception 'WATCH_NOT_FOUND' using errcode = 'P0001';
    end if;
    if v_watch.status <> 'Active' then
        raise exception 'WATCH_NOT_AVAILABLE' using errcode = 'P0001';
    end if;

    if not exists (select 1 from public.users where id = p_buyer_id) then
        raise exception 'BUYER_NOT_FOUND' using errcode = 'P0001';
    end if;

    select d.user_id into v_owner_id
    from public.dealers d
    where d.id = v_watch.dealer_id;

    if v_owner_id = p_buyer_id then
        raise exception 'CANNOT_BUY_OWN_WATCH' using errcode = 'P0001';
    end if;

    v_seq := nextval('public.order_number_seq');

    -- Price and shipping are copied from the watch row, NEVER taken from the caller.
    insert into public.orders (order_number, buyer_id, watch_id, price_php, shipping_cost_php, placed_at, status)
    values (
        'LW-' || to_char(now(), 'YYYY') || '-' || lpad(v_seq::text, greatest(4, length(v_seq::text)), '0'),
        p_buyer_id,
        p_watch_id,
        v_watch.price_php,
        v_watch.shipping_cost_php,
        now(),
        'PendingPayment'
    )
    returning * into v_order;
    -- Last line of defence: orders_one_active_per_watch (partial unique index)
    -- raises 23505 if a non-cancelled order already exists for this watch.

    update public.watches
    set status = 'Reserved'
    where id = p_watch_id;

    return v_order;
end;
$$;


-- -----------------------------------------------------------------------------
-- cancel_order: PendingPayment/InEscrow → Cancelled, watch Reserved → Active.
-- -----------------------------------------------------------------------------
create or replace function public.cancel_order(p_order_id uuid, p_buyer_id uuid)
returns public.orders
language plpgsql
security definer
set search_path = public
as $$
-- TODO (auth): replace p_buyer_id with auth.uid(). Under the class policy any caller can act as any buyer.
declare
    v_order public.orders%rowtype;
begin
    if p_order_id is null or p_buyer_id is null then
        raise exception 'ORDER_NOT_FOUND' using errcode = 'P0001';
    end if;

    select * into v_order
    from public.orders
    where id = p_order_id
    for update;

    -- Same key for "doesn't exist" and "not yours": don't leak other people's orders.
    if not found or v_order.buyer_id <> p_buyer_id then
        raise exception 'ORDER_NOT_FOUND' using errcode = 'P0001';
    end if;

    if v_order.status not in ('PendingPayment', 'InEscrow') then
        raise exception 'ORDER_NOT_CANCELLABLE' using errcode = 'P0001';
    end if;

    update public.orders
    set status = 'Cancelled'
    where id = p_order_id
    returning * into v_order;

    -- Back on the grid.
    update public.watches
    set status = 'Active'
    where id = v_order.watch_id
      and status = 'Reserved';

    return v_order;
end;
$$;


-- -----------------------------------------------------------------------------
-- confirm_delivery: PendingPayment/InEscrow/Shipped/Delivered → Completed,
-- watch → Sold. The order then becomes reviewable.
-- -----------------------------------------------------------------------------
create or replace function public.confirm_delivery(p_order_id uuid, p_buyer_id uuid)
returns public.orders
language plpgsql
security definer
set search_path = public
as $$
-- DEMO SHORTCUT: stands in for pay → ship → deliver until payment/shipment tables exist.
-- TODO (auth): replace p_buyer_id with auth.uid(). Under the class policy any caller can act as any buyer.
declare
    v_order public.orders%rowtype;
begin
    if p_order_id is null or p_buyer_id is null then
        raise exception 'ORDER_NOT_FOUND' using errcode = 'P0001';
    end if;

    select * into v_order
    from public.orders
    where id = p_order_id
    for update;

    if not found or v_order.buyer_id <> p_buyer_id then
        raise exception 'ORDER_NOT_FOUND' using errcode = 'P0001';
    end if;

    if v_order.status not in ('PendingPayment', 'InEscrow', 'Shipped', 'Delivered') then
        raise exception 'ORDER_NOT_CONFIRMABLE' using errcode = 'P0001';
    end if;

    update public.orders
    set status = 'Completed'
    where id = p_order_id
    returning * into v_order;

    update public.watches
    set status = 'Sold'
    where id = v_order.watch_id;

    return v_order;
end;
$$;


-- -----------------------------------------------------------------------------
-- Execute rights: nobody by default (Postgres grants EXECUTE to PUBLIC, and
-- Supabase adds anon/authenticated via default privileges), then exactly the
-- two API roles. Table grants are UNCHANGED: anon still only SELECTs orders
-- and watches, so a direct POST /orders is refused.
-- -----------------------------------------------------------------------------
revoke execute on function public.place_order(uuid, uuid)      from public, anon, authenticated;
revoke execute on function public.cancel_order(uuid, uuid)     from public, anon, authenticated;
revoke execute on function public.confirm_delivery(uuid, uuid) from public, anon, authenticated;

grant execute on function public.place_order(uuid, uuid)      to anon, authenticated;
grant execute on function public.cancel_order(uuid, uuid)     to anon, authenticated;
grant execute on function public.confirm_delivery(uuid, uuid) to anon, authenticated;
