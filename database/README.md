# LuxWatch database (Supabase)

Postgres schema and demo data for the Tier 1 tables. The Blazor WebAssembly app reads them directly through Supabase's auto-generated REST API (the Data API), using the **publishable** key.

> **Never commit the Secret key (listed under "Secret keys" in API Keys) or the database password.** The app runs in the browser, so anything it ships is public.

## How to run

1. Open the Supabase dashboard, then your project, then **SQL Editor**, then **New query**.
2. Paste and run **`schema.sql`**. It creates the tables, constraints, indexes, grants and RLS policies.
   - It starts with a dev-only reset block that drops the 8 tables, so you can re-run it while iterating. **The reset wipes all data.** Run `seed.sql` again afterwards.
3. Paste and run **`seed.sql`**. It loads the demo data in one transaction.
4. Paste and run **`views.sql`**. It creates the read views `dealer_reviews` and `dealer_stats`.
   - **Run it again after any re-run of `schema.sql`.** The reset drops the tables with `cascade`, which also drops the views.
5. Test the API with **`test.http`** (VS Code REST Client extension). Fill in the URL and key locally, and don't commit them.

**Where to find the URL and key:** dashboard → **Project Settings → API Keys**. Copy the **Project URL** (`https://<ref>.supabase.co`) and the **Publishable key** (`sb_publishable_…`). The project URL is also shown under **Project Settings → Data API**.

## Tables → UML classes

| SQL table | UML class | Intentional differences |
|---|---|---|
| `users` | User | No `PasswordHash`. Login moves to Supabase Auth. |
| `brands` | Brand | None |
| `dealers` | Dealer | `WatchesSold`, `ActiveListings` and the rating average are derived, not stored. The seed baselines (200 / 82) are front-end-only fakes and are dropped. `name` is unique (the seed looks dealers up by it). |
| `watches` | Watch | `IsPopular` is derived (`views_in_48h > 1000`). The C# `Year` string ("1998 (approx.)") is split into `year int` plus `year_is_approximate boolean`. The app rebuilds the display string. Watch types (`Types`) are deferred. |
| `watch_specs` | WatchSpec | Key/value rows (`key`, `value`, `display_order`), many per watch, `on delete cascade`. |
| `watch_images` | WatchImage | `sort_order` (C# `SortOrder`), many per watch, `on delete cascade`. |
| `orders` | Order | Minimal: no payment, shipment or authentication yet. A partial unique index allows at most one non-cancelled order per watch. |
| `reviews` | Review | `helpful_count` replaces the User↔Review "marked helpful" association until auth exists. It will become a `review_helpful (user_id, review_id)` table. Adds `deleted_at` for soft delete. `comment` is required, 10–500 characters, the same rule as `Review.IsValid()`. |

| `dealer_reviews` (view) | Read model, not a UML class | One row per review, joined across reviews → orders → users / watches → brands, so a dealer's reviews load in one request. Includes `deleted_at`; the app filters `deleted_at is null` in its query. |
| `dealer_stats` (view) | Read model, not a UML class | One row per dealer: active and sold counts, plus rating averages and recommend % from non-deleted reviews. Replaces `Dealer.WatchesSold`, `ActiveListings` and `GetAverageRating()`. |

Both views use `security_invoker = true`, so the caller's grants and RLS on the base tables still apply. They are read-only for `anon` and `authenticated`.

**Ids:** every primary key is `uuid default gen_random_uuid()`, matching the C# `Guid` ids. The seed never hardcodes ids. It looks foreign keys up by natural key (slug, username, dealer name, order number).

**Enums:** `Condition`, `WatchStatus` and `OrderStatus` are stored as `text` with a `CHECK` that lists the exact C# member names (e.g. `'PreOwned'`). We don't use native Postgres enums.

**Deferred to Tier 2 (after auth):** Payment, Shipment, AuthenticationReport, Favorite, CollectionItem, WatchType.

## Demo data (`seed.sql`)

| Table | Rows | What's in it |
|---|---|---|
| users | 13 | Marcus Vance (the demo buyer, `SampleData.CurrentUser`), 4 dealer owners, 8 reviewers |
| brands | 20 | The landing-page brands |
| dealers | 4 | Haus Zeitwerk (DE), Manila Chrono Exchange (PH), Lion City Timepieces (SG), Ginza Tokei Gallery (JP) |
| watches | 34 | 24 Active across 13 brands (₱22,500 to ₱6.2M; 9 are Popular, with over 1,000 views in 48h) and 10 Sold |
| watch_specs | 102 | 4 per Active listing, 10 for the featured Submariner |
| watch_images | 61 | 2 per Active listing, 5 for the Submariner, 1 per Sold watch |
| orders | 10 | All Completed, all on Sold watches. `LW-2026-0401` and `0402` are Marcus's and still unreviewed. |
| reviews | 8 | One per order `LW-2026-0301` to `0308` |

## Security (class policy)

`schema.sql` first revokes Supabase's default privileges. Then every table gets RLS, a `SELECT` grant and a read policy for `anon` and `authenticated`. Only `reviews` can be written:
- `INSERT` of a whole row (the RateDealer form).
- `UPDATE` of **only** `helpful_count` and `deleted_at` (a column-level grant). App code must send just those columns, e.g. `.Set(x => x.DeletedAt, …).Update()`, never `.Update(wholeModel)`.

No table grants `DELETE`. These policies are open to anyone with the publishable key. They will be replaced with `auth.uid()`-based policies once login exists.

## Storage pattern: soft delete (reviews)

Reviews are never physically deleted. "Deleting" one is an `UPDATE` that sets `deleted_at`. The app shows only rows where `deleted_at is null`, and the database refuses a real `DELETE` because there is no grant for it.

There are three reasons:
- **Restorable.** Moderators can restore a review that was removed by mistake or during a dispute.
- **Auditable.** Dealer rating averages can be recalculated and checked against the full history.
- **No second review.** The removed row still exists, so `unique(order_id)` stops the same order from being reviewed again.

The `SELECT` policy deliberately does **not** filter `deleted_at`. If it did, the soft-delete `UPDATE` would fail RLS, because the updated row would no longer be visible.
