-- =============================================================================
-- LuxWatch: read views (read models, not UML classes)
-- Run AFTER seed.sql, and again after any re-run of schema.sql (its reset drops
-- the tables with CASCADE, which drops these views too). Safe to re-run.
--
-- security_invoker = true: the view runs with the CALLER's rights, so the anon
-- grants and RLS policies on the base tables still apply (Postgres 15+).
-- =============================================================================

-- Re-runnable: drop + create (rather than CREATE OR REPLACE) so a column can be
-- renamed or reordered later without "cannot change name of view column".
drop view if exists public.dealer_reviews;
drop view if exists public.dealer_stats;


-- -----------------------------------------------------------------------------
-- dealer_reviews: one row per review, with everything the review card shows.
-- Why: a review's author, watch, brand and dealer live across reviews → orders
-- → users / watches → brands. Joining in the database lets the app read a page
-- of reviews for a dealer in ONE request instead of several requests or a
-- fetch inside a loop (N+1).
-- deleted_at is kept and NOT filtered here: the app filters deleted_at is null
-- in its query, so moderation tools can still see soft-deleted rows.
-- -----------------------------------------------------------------------------
create view public.dealer_reviews
with (security_invoker = true) as
select
    r.id,
    r.order_id,
    r.overall,
    r.shipping,
    r.item_as_described,
    r.communication,
    r.recommends,
    r.comment,
    r.helpful_count,
    r.created_at,
    r.deleted_at,
    o.order_number,
    o.status     as order_status,      -- Review requires a Delivered/Completed order
    o.placed_at,
    o.buyer_id,
    u.username   as author_username,   -- User requires a username
    u.full_name  as author_name,
    u.country    as author_country,
    w.id         as watch_id,
    w.slug       as watch_slug,
    w.model      as watch_model,
    w.dealer_id,
    b.name       as brand_name
from public.reviews  r
join public.orders   o on o.id = r.order_id
join public.users    u on u.id = o.buyer_id
join public.watches  w on w.id = o.watch_id
join public.brands   b on b.id = w.brand_id;


-- -----------------------------------------------------------------------------
-- dealer_stats: one row per dealer with listing counts and rating averages.
-- Why: these are derived values (not stored). Computing them in C# would mean
-- downloading every watch and review of the dealer; here it is one small row.
-- Each aggregate runs in its own subquery: joining watches AND reviews in one
-- step would multiply rows and inflate the counts. Dealers with no reviews
-- still appear (left joins) with review_count = 0 and null averages.
-- Only non-deleted reviews count.
-- -----------------------------------------------------------------------------
create view public.dealer_stats
with (security_invoker = true) as
select
    d.id                                   as dealer_id,
    coalesce(ws.active_listings, 0)        as active_listings,
    coalesce(ws.watches_sold, 0)           as watches_sold,
    coalesce(rs.review_count, 0)           as review_count,
    rs.avg_overall,
    rs.avg_shipping,
    rs.avg_item_as_described,
    rs.avg_communication,
    rs.recommend_pct,
    -- Star distribution (overall rating) for the review summary bars.
    coalesce(rs.stars_5, 0)                as stars_5,
    coalesce(rs.stars_4, 0)                as stars_4,
    coalesce(rs.stars_3, 0)                as stars_3,
    coalesce(rs.stars_2, 0)                as stars_2,
    coalesce(rs.stars_1, 0)                as stars_1
from public.dealers d
left join (
    select
        dealer_id,
        (count(*) filter (where status = 'Active'))::int as active_listings,
        (count(*) filter (where status = 'Sold'))::int   as watches_sold
    from public.watches
    group by dealer_id
) ws on ws.dealer_id = d.id
left join (
    select
        w.dealer_id,
        count(*)::int                                              as review_count,
        round(avg(r.overall), 1)                                   as avg_overall,
        round(avg(r.shipping), 1)                                  as avg_shipping,
        round(avg(r.item_as_described), 1)                         as avg_item_as_described,
        round(avg(r.communication), 1)                             as avg_communication,
        round(100.0 * avg(case when r.recommends then 1 else 0 end))::int as recommend_pct,
        (count(*) filter (where r.overall = 5))::int               as stars_5,
        (count(*) filter (where r.overall = 4))::int               as stars_4,
        (count(*) filter (where r.overall = 3))::int               as stars_3,
        (count(*) filter (where r.overall = 2))::int               as stars_2,
        (count(*) filter (where r.overall = 1))::int               as stars_1
    from public.reviews r
    join public.orders  o on o.id = r.order_id
    join public.watches w on w.id = o.watch_id
    where r.deleted_at is null
    group by w.dealer_id
) rs on rs.dealer_id = d.id;


-- -----------------------------------------------------------------------------
-- Grants: same revoke-then-grant pattern as the tables. Read only.
-- -----------------------------------------------------------------------------
revoke all on public.dealer_reviews from anon, authenticated;
revoke all on public.dealer_stats   from anon, authenticated;
grant select on public.dealer_reviews to anon, authenticated;
grant select on public.dealer_stats   to anon, authenticated;
