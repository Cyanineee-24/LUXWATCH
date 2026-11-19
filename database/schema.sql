-- =============================================================================
-- LuxWatch: Supabase schema, Tier 1 tables
-- Run in the Supabase SQL Editor BEFORE seed.sql. Safe to re-run (see reset).
--
-- Conventions
--   * Column names = C# property names in snake_case (Models/*.cs).
--   * Ids are uuid (gen_random_uuid(), built into Postgres 13+) to match the
--     C# Guid ids. Seeds never hardcode ids; they look FKs up by natural key.
--   * C# enums are stored as text + CHECK listing the exact enum member names
--     (no native Postgres enums, so adding a member is a one-line CHECK change).
--   * Money = numeric(12,2) >= 0. Timestamps = timestamptz.
--   * Every FK column is indexed explicitly (Postgres does not do this for you),
--     unless a UNIQUE constraint on that column already provides the index.
-- =============================================================================


-- -----------------------------------------------------------------------------
-- DEV-ONLY RESET: drops all Tier 1 tables (and their data!) so this file can be
-- re-run while we iterate. REMOVE THIS BLOCK once real data lives in the DB.
-- Reverse dependency order.
-- -----------------------------------------------------------------------------
drop table if exists public.reviews      cascade;
drop table if exists public.orders       cascade;
drop table if exists public.watch_images cascade;
drop table if exists public.watch_specs  cascade;
drop table if exists public.watches      cascade;
drop table if exists public.dealers      cascade;
drop table if exists public.brands       cascade;
drop table if exists public.users        cascade;
-- ------------------------------ end reset ------------------------------------


-- users ← User
-- No password column: login moves to Supabase Auth (auth.users) later.
create table public.users (
    id          uuid        primary key default gen_random_uuid(),
    username    text        not null unique,
    full_name   text        not null,
    email       text        not null unique,
    country     text,                                   -- ISO-2 code, e.g. 'PH'
    created_at  timestamptz not null default now()
);


-- brands ← Brand
-- slug is indexed by its UNIQUE constraint.
create table public.brands (
    id             uuid    primary key default gen_random_uuid(),
    name           text    not null unique,
    slug           text    not null unique,
    logo_url       text,
    description    text,
    is_featured    boolean not null default false,
    display_order  int     not null default 0
);


-- dealers ← Dealer
-- User 1 ── 0..1 Dealer: user_id is UNIQUE (which also indexes it).
-- WatchesSold / ActiveListings / rating averages are derived, NOT stored.
create table public.dealers (
    id               uuid    primary key default gen_random_uuid(),
    user_id          uuid    not null unique references public.users (id) on delete restrict,
    name             text    not null unique,
    country          text    not null,
    since            date    not null,                  -- C# DateOnly
    ships_within     text,
    responds_within  text,
    is_verified      boolean not null default false,
    created_at       timestamptz not null default now()
);


-- watches ← Watch (the listing)
-- slug is indexed by its UNIQUE constraint. IsPopular is derived
-- (views_in_48h > 1000), NOT stored.
create table public.watches (
    id                     uuid          primary key default gen_random_uuid(),
    slug                   text          not null unique,
    brand_id               uuid          not null references public.brands  (id) on delete restrict,
    dealer_id              uuid          not null references public.dealers (id) on delete restrict,
    model                  text          not null,
    reference              text,
    subtitle               text,
    description            text,
    condition              text          not null
                           check (condition in ('PreOwned', 'BrandNew')),
    -- C# Watch.Year is a display string ('1998 (approx.)'). Stored as a real
    -- year + flag; the app rebuilds the string (Phase 3).
    year                   int           check (year between 1900 and 2100),
    year_is_approximate    boolean       not null default false,
    has_box                boolean       not null default false,
    has_papers             boolean       not null default false,
    price_php              numeric(12,2) not null check (price_php >= 0),
    shipping_cost_php      numeric(12,2) not null default 0 check (shipping_cost_php >= 0),
    location_country_code  char(2),
    views_in_48h           int           not null default 0 check (views_in_48h >= 0),
    listed_date            date          not null default current_date,   -- C# DateOnly
    delivery_window        text,
    status                 text          not null default 'Active'
                           check (status in ('Draft', 'PendingReview', 'Active', 'Reserved', 'Sold'))
);

create index watches_brand_id_idx  on public.watches (brand_id);
create index watches_dealer_id_idx on public.watches (dealer_id);
-- Bestsellers grid: active listings, newest first.
create index watches_status_listed_date_idx on public.watches (status, listed_date desc);


-- watch_specs ← WatchSpec
-- The C# model is a key/value list (Watch 1 ── 0..* WatchSpec, with
-- DisplayOrder), so this is one row per spec line, owned by the watch.
create table public.watch_specs (
    id             uuid primary key default gen_random_uuid(),
    watch_id       uuid not null references public.watches (id) on delete cascade,
    key            text not null,
    value          text not null,
    display_order  int  not null default 0,
    unique (watch_id, display_order)        -- leading watch_id → also serves as the FK index
);


-- watch_images ← WatchImage (C# property is SortOrder)
create table public.watch_images (
    id          uuid primary key default gen_random_uuid(),
    watch_id    uuid not null references public.watches (id) on delete cascade,
    url         text not null,
    alt_text    text,
    sort_order  int  not null default 0,
    unique (watch_id, sort_order)           -- leading watch_id → also serves as the FK index
);


-- orders ← Order (minimal: exists so reviews have something to attach to)
-- price_php / shipping_cost_php are snapshots taken at purchase time.
create table public.orders (
    id                 uuid          primary key default gen_random_uuid(),
    order_number       text          not null unique,
    buyer_id           uuid          not null references public.users   (id) on delete restrict,
    watch_id           uuid          not null references public.watches (id) on delete restrict,
    price_php          numeric(12,2) not null check (price_php >= 0),
    shipping_cost_php  numeric(12,2) not null default 0 check (shipping_cost_php >= 0),
    placed_at          timestamptz   not null default now(),
    status             text          not null default 'PendingPayment'
                       check (status in ('PendingPayment', 'InEscrow', 'Shipped', 'Delivered', 'Completed', 'Cancelled'))
);

create index orders_buyer_id_idx on public.orders (buyer_id);
create index orders_watch_id_idx on public.orders (watch_id);
-- UML note: at most one non-cancelled order per watch.
create unique index orders_one_active_per_watch on public.orders (watch_id) where status <> 'Cancelled';


-- reviews ← Review   (SOFT DELETE table: deleted_at is null = visible)
-- Order 1 ── 0..1 Review: order_id is UNIQUE (which also indexes it).
create table public.reviews (
    id                 uuid     primary key default gen_random_uuid(),
    order_id           uuid     not null unique references public.orders (id) on delete restrict,
    overall            smallint not null check (overall           between 1 and 5),
    shipping           smallint not null check (shipping          between 1 and 5),
    item_as_described  smallint not null check (item_as_described between 1 and 5),
    communication      smallint not null check (communication     between 1 and 5),
    recommends         boolean  not null,
    -- Same rule as Review.IsValid() in C#: 10–500 characters.
    comment            text     not null check (char_length(comment) between 10 and 500),
    -- TEMPORARY: replaces the UML User 0..* ── 0..* Review "marked helpful"
    -- association while there is no login to tell users apart. Becomes a
    -- review_helpful (user_id, review_id) table once auth exists.
    helpful_count      int      not null default 0 check (helpful_count >= 0),
    created_at         timestamptz not null default now(),
    deleted_at         timestamptz                       -- null = visible
);

comment on column public.reviews.helpful_count is
    'TEMPORARY: stands in for the UML User<->Review "marked helpful" association until auth exists; then becomes a review_helpful (user_id, review_id) table.';

-- Visible reviews, newest first.
create index reviews_visible_created_at_idx on public.reviews (created_at desc) where deleted_at is null;


-- =============================================================================
-- SECURITY: the two locks (GRANTs + Row Level Security)
--
-- CLASS POLICY: open to anyone with the publishable key. Replace with
-- auth.uid()-based policies once login exists.
--
-- Supabase gives anon/authenticated broad default privileges on new tables in
-- public, so we REVOKE everything first and grant back only what the UI needs.
-- =============================================================================

-- Read access: all 8 tables.
do $$
declare
    t text;
begin
    foreach t in array array['users', 'brands', 'dealers', 'watches', 'watch_specs',
                             'watch_images', 'orders', 'reviews']
    loop
        execute format('revoke all on public.%I from anon, authenticated', t);
        execute format('grant select on public.%I to anon, authenticated', t);
        execute format('alter table public.%I enable row level security', t);
        execute format(
            'create policy %I on public.%I for select to anon, authenticated using (true)',
            t || '_read_all', t);
    end loop;
end
$$;

-- Write access: reviews only (the only table the UI writes this milestone).
--
-- INSERT: whole row (the RateDealer form).
-- UPDATE: column-level, only helpful_count ("Helpful" button) and deleted_at
--   (soft delete). Ratings and comment can't be edited through the API.
--   ⚠ Phase 5: updates must send ONLY these columns, i.e. use
--   .Set(x => x.HelpfulCount, …) / .Set(x => x.DeletedAt, …).Update().
--   Calling .Update(model) on a whole object sends every column and is
--   refused with "permission denied for table reviews".
-- DELETE: not granted. Removing a review is a soft delete
--   (UPDATE … SET deleted_at = now()), so a real DELETE is refused.
--
-- The select policy deliberately does NOT filter deleted_at: if it did, the
-- soft-delete UPDATE would fail RLS (the new row would no longer be visible).
-- Hiding soft-deleted rows is the app query's job (deleted_at=is.null).
grant insert on public.reviews to anon, authenticated;
grant update (helpful_count, deleted_at) on public.reviews to anon, authenticated;

create policy "reviews_insert_all" on public.reviews
    for insert to anon, authenticated
    with check (true);

create policy "reviews_update_all" on public.reviews
    for update to anon, authenticated
    using (true)
    with check (true);
