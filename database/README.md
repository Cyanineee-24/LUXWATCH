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
5. **Patches (`patches/NNN_*.sql`)** are one-off fixes for a project that was **already** set up with an older `schema.sql`. Run any you haven't run yet, in number order. A fresh `schema.sql` already includes them.
   - `001_users_column_grant.sql`: hides `users.email` from the publishable key.
   - `002_ordering.sql`: the order number sequence and the `place_order`, `cancel_order` and `confirm_delivery` functions (see "Ordering" below). After running it, **re-run `views.sql`** too, because `dealer_reviews` gained the order's own price columns.
   - **Full reset order:** `schema.sql` → `seed.sql` → `views.sql`. The patches are already in `schema.sql`.
6. Test the API with **`test.http`** (VS Code REST Client extension). Fill in the URL and key locally, and don't commit them.

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

`schema.sql` first revokes Supabase's default privileges. Then every table gets RLS, a `SELECT` grant and a read policy for `anon` and `authenticated`. On `users` the grant is **column-level**: `id, username, full_name, country, created_at`. **`email` is not readable** with the publishable key. Only `reviews` can be written:
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

**How the app does it:**
- **Delete** on your own review sends `PATCH reviews?id=eq.… {"deleted_at": "<now>"}`. Every read filters `deleted_at=is.null`.
- **Undo** sends the same PATCH with `{"deleted_at": null}`, and the review comes back.
- The database has **no DELETE grant**, so a row can't really be removed from the app.

The code is in `Data/Repositories/ReviewWriteRepository.cs`.

## Reset demo data

Run these three files in the SQL Editor, in order: **`schema.sql` → `seed.sql` → `views.sql`**. `schema.sql` already includes every patch.

You need a reset after testing Create a couple of times. Marcus (the demo user) has only **2 reviewable orders** (`LW-2026-0401`, `LW-2026-0402`), and a soft-deleted review still holds its order (`unique(order_id)`). So after two test reviews there's nothing left to review until you reset. Resetting also clears Helpful counts and soft deletes back to the seed values.

## Ordering (functions, not write grants)

The publishable key is public. If `anon` could `INSERT` into `orders` or `UPDATE` `watches`, anyone could create an order at any price or mark any watch Sold. So **`anon` has no write grant on either table.** Instead, three `security definer` functions each do one whole operation in a single transaction: they validate every input, lock the rows involved, and copy the price from the database. `anon` may only **execute** them, through `POST /rest/v1/rpc/<name>`.

| Function | From order status | Order becomes | Watch becomes | Error keys (errcode `P0001`) |
|---|---|---|---|---|
| `place_order(p_watch_id, p_buyer_id)` | (new) | `PendingPayment`; price and shipping copied from the watch | `Active` → `Reserved` | `WATCH_NOT_FOUND`, `WATCH_NOT_AVAILABLE`, `BUYER_NOT_FOUND`, `CANNOT_BUY_OWN_WATCH` |
| `cancel_order(p_order_id, p_buyer_id)` | `PendingPayment`, `InEscrow` | `Cancelled` | `Reserved` → `Active` (back on sale) | `ORDER_NOT_FOUND`, `ORDER_NOT_CANCELLABLE` |
| `confirm_delivery(p_order_id, p_buyer_id)` | `PendingPayment`, `InEscrow`, `Shipped`, `Delivered` | `Completed` (now reviewable) | `Sold` | `ORDER_NOT_FOUND`, `ORDER_NOT_CONFIRMABLE` |

- **Two buyers at once:** `place_order` locks the watch row (`for update`), so the second buyer sees `WATCH_NOT_AVAILABLE`. The partial unique index "one non-cancelled order per watch" is the last line of defence (`23505`).
- **Not your order:** a missing order and someone else's order give the same `ORDER_NOT_FOUND`, so other people's orders don't leak.
- **Demo shortcut:** `confirm_delivery` stands in for pay → ship → deliver until the payment and shipment tables exist.
- **TODO (auth):** `p_buyer_id` is passed by the app (the demo user). Under the class policy any caller can act as any buyer. With Supabase Auth this becomes `auth.uid()`.
