# LuxWatch

A luxury watch marketplace built with Blazor WebAssembly (.NET 8) and Tailwind CSS v4. Data lives in Supabase (Postgres plus its auto-generated REST API), and the browser calls it with the **publishable** key.

## Run it

```bash
dotnet run --launch-profile http
```
Then open http://localhost:5230. The Supabase Project URL and publishable key are in [`wwwroot/appsettings.json`](wwwroot/appsettings.json). The publishable key is public by design. **The secret key must never be in this repo.**

Database setup (schema, seed data, views, patches) is in [`database/README.md`](database/README.md).

## Storage pattern: soft delete

Reviews are never physically deleted. **Delete** sets `deleted_at`, every read filters `deleted_at is null`, and **Undo** clears it again. The database has no DELETE grant, so rows can't really be removed from the app. For the reasons and details, see [`database/README.md`](database/README.md#storage-pattern-soft-delete-reviews).

**Reset demo data:** re-run `schema.sql` → `seed.sql` → `views.sql` in the Supabase SQL Editor. This restores the watches to their seeded statuses, removes test orders, and clears Helpful counts and soft deletes. See [`database/README.md`](database/README.md#reset-demo-data).

## Ordering

```
Buy now → order placed (watch Reserved, gone from the grid) → My orders
        → Confirm delivery (demo) → watch Sold, order Completed → Rate dealer
        → Helpful → Delete / Undo
```

**Why database functions instead of write grants:** the publishable key is public. If the browser could `INSERT` into `orders` or `UPDATE` `watches`, anyone could create an order at any price or mark any watch Sold. So the public key has **no write grant on `orders` or `watches`**. Three Postgres functions (`security definer`) each do one whole operation in a single transaction: they validate the input, lock the rows, and **copy the price from the database**. The app calls them through `POST /rest/v1/rpc/<name>`, and that is the only way in. A direct `POST /orders` is refused.

| Action (UI) | Function | Order | Watch |
|---|---|---|---|
| **Buy now → Place order** (watch detail) | `place_order` | new, `PendingPayment` | `Active` → `Reserved` |
| **Cancel order** (My orders) | `cancel_order` | `PendingPayment`/`InEscrow` → `Cancelled` | `Reserved` → `Active` (back on sale) |
| **Confirm delivery (demo)** (My orders) | `confirm_delivery` | `PendingPayment`…`Delivered` → `Completed` | → `Sold` |

- **Demo shortcut:** `confirm_delivery` stands in for pay → ship → deliver until the payment and shipment tables exist.
- **Two buyers at once:** the function locks the watch row. The second buyer gets "Sorry, someone just reserved this watch." The "one non-cancelled order per watch" unique index backs this up.
- **TODO (auth):** the app passes the demo user's id (`p_buyer_id`). Under the class policy any caller could act as any buyer. With Supabase Auth this becomes `auth.uid()`.
- Details and error keys: [`database/README.md`](database/README.md#ordering-functions-not-write-grants), and the SQL itself in [`database/patches/002_ordering.sql`](database/patches/002_ordering.sql).

## Architecture

```
Pages  →  Data/Repositories  →  Data/Rows (Supabase BaseModel)  →  Supabase
              │
              └→ Data/Mapping  →  Models/ (UML domain classes; no Supabase attributes)
```

Pages only see the domain models in `Models/`. Row classes are used only inside the repositories.

## Fast habits (data access)

### 1. Indexes
Defined in [`database/schema.sql`](database/schema.sql):

| Index | Why |
|---|---|
| `watches.slug`, `brands.slug` (UNIQUE) | The detail page looks a watch up by slug. A unique constraint is also an index. |
| Every foreign key: `watches.brand_id`, `watches.dealer_id`, `orders.buyer_id`, `orders.watch_id`, `watch_images (watch_id, sort_order)`, `watch_specs (watch_id, display_order)`, `reviews.order_id` (UNIQUE), `dealers.user_id` (UNIQUE) | Postgres doesn't index foreign keys automatically. Joins and embeds (`watch → images/specs`, `review → order → watch`) use these. |
| `watches (status, listed_date desc)` | The bestsellers grid: `status = 'Active'`, newest first, read straight from the index. |
| `reviews (created_at desc) WHERE deleted_at IS NULL` (partial) | Visible reviews, newest first. Soft-deleted rows aren't in the index at all. |
| `orders (watch_id) WHERE status <> 'Cancelled'` (partial UNIQUE) | Enforces "at most one active order per watch" in the database. |

### 2. Filtering in the database, not in C#
Every filter, sort and page limit is added **before** `.Get()`, so Postgres does the work and only the needed rows travel over the network:
- **Active only:** `status=eq.Active`. Sold and Draft watches never reach the browser.
- **Deleted reviews excluded:** `deleted_at=is.null` in the query. The views keep `deleted_at`, so moderation can still see removed reviews.
- **Paging with `.Range`:** 12 cards per page. We ask for **13 rows** (`pageSize + 1`). If the 13th comes back, there's a next page. That replaces a separate `count` query.
- **Derived numbers in a view:** a dealer's active, sold and review counts, its rating averages and star distribution come from `dealer_stats` as **one row**, instead of downloading every watch and review and adding them up in C#.

### 3. One request, not many (no N+1)
| Page | Requests | What they are |
|---|---|---|
| `/collections/bestsellers` | **1 per page** | `watches` filtered and paged, with brand, dealer, images and specs embedded through `[Reference]` |
| `/watch/{slug}` | **4 per page load, regardless of how many reviews** | 1. the watch with brand, dealer (+ owner), images and specs embedded. Then **in parallel** (`Task.WhenAll`): 2. `dealer_stats` · 3. one page of `dealer_reviews` (a view that joins reviews → orders → users/watches → brands) · 4. **one** batched `watches?id=in.(…)` for the watches on that review page |

There is never a `.Get()` inside a loop.

The **first** detail page of a session makes one extra request: the demo user (`users?username=eq.marcusvance`, needed to show Delete on your own reviews). It runs in parallel with the others and is cached afterwards, so every later detail page makes 4.

**Writes** are one request each: Submit review is 1 POST, Helpful is 1 PATCH, Delete and Undo are each 1 PATCH. After Delete or Undo, the reviews and stats reload in parallel (3 GETs).

**Ordering:**
- **Place order** is 1 POST (`rpc/place_order`), then the watch and dealer stats reload in parallel (2 GETs).
- **`/account/orders`** is **1 GET per page**: orders with the watch, brand, images and review embedded, paged with `pageSize + 1`.
- **Cancel** and **Confirm delivery** are each 1 POST (rpc), then 1 GET to reload the list.

### How to check it yourself
Open the page, then **DevTools → Network**, filter by `supabase.co`, and reload:
- `/collections/bestsellers` shows **1** request (`/rest/v1/watches?…status=eq.Active…`). Click **Next** and exactly 1 more appears.
- `/watch/rolex-submariner-14060` shows **4** requests: `watches?slug=eq.…`, `dealer_stats?…`, `dealer_reviews?…deleted_at=is.null…`, and `watches?id=in.(…)`.

## Known gaps

- **Unbuilt features** (Authentication, Collector Portal, Sell, Favorites, Help, Security, password reset) have real routes that show a styled "Coming soon" page, so no link ends on "Page not found".
- **Search** isn't built: pressing Enter in the search bar opens the listings.
- **No login yet:** the app acts as the demo user `marcusvance` (`Demo:CurrentUsername` in `appsettings.json`).

## Milestone checklist

| Rubric item | Where it lives |
|---|---|
| **Table design** | [`database/schema.sql`](database/schema.sql): 8 Tier 1 tables, uuid keys, CHECKs, FK indexes, RLS, and the ordering functions. Read views are in [`database/views.sql`](database/views.sql). The table → UML map is in [`database/README.md`](database/README.md). |
| **Read** | `/collections/bestsellers` (active listings, paged), `/watch/{slug}` (watch, dealer stats, reviews), `/account/orders` (my orders) |
| **Create** | **Place order** on `/watch/{slug}` (`rpc/place_order`) and **Submit review** on `/watch/{slug}/rate` (`POST /reviews`) |
| **Update** | **Cancel** / **Confirm delivery** on My orders (`rpc/cancel_order`, `rpc/confirm_delivery`) and **Helpful** on a review card (`PATCH helpful_count`) |
| **Delete** | **Delete** / **Undo** on your own review: `PATCH deleted_at` (soft delete) |
| **Fast habits** | [Fast habits](#fast-habits-data-access) above |
| **Storage pattern** | [Storage pattern: soft delete](#storage-pattern-soft-delete) above, with details in [`database/README.md`](database/README.md#storage-pattern-soft-delete-reviews) |

### Demo script

**Start from a reset** (`schema.sql` → `seed.sql` → `views.sql`): 24 Active watches, **0 orders, 0 reviews**. The expected values below assume that. The example watch is the Rolex GMT-Master II "Pepsi" (dealer: Lion City Timepieces).

| # | Do this | Expect |
|---|---|---|
| 0 | **My orders** (navbar) | "No orders yet." |
| 1 | **Bestsellers** (`/collections/bestsellers`) | 12 cards + Next; page 2 has 12. DevTools → Network: 1 request per page. |
| 2 | **Watch detail** `/watch/rolex-gmt-master-ii-126710blro` | ₱1,150,000. Dealer box: 0 watches sold, 6 active listings, **"No reviews yet"**. Reviews section: "No reviews yet. Reviews appear after a completed purchase." 4 requests (5 on the session's first detail page). |
| 3 | **Buy now → Place order** | "Order **LW-2026-0001** placed." The button area says **Reserved**, and active listings drop to 5. 1 POST (`rpc/place_order`). |
| 4 | **Bestsellers** again | The GMT is gone; page 2 now has 11 cards. |
| 5 | **My orders → Confirm delivery (demo)** | LW-2026-0001 is **Completed** (₱1,152,500 incl. shipping), with a **Rate dealer** button. |
| 6 | **Rate dealer** → e.g. 5★ everywhere, Recommend: Yes, a comment | You land on the GMT page (**Sold**): "Thanks, your review is posted." Reviews (1), rating **5.0**, **100%** recommend, star bars 1/0/0/0/0, 1 watch sold. |
| 7 | **Helpful** on your review | Shows **Helpful ✓ 1**, in place (1 PATCH). |
| 8 | **Delete** → confirm, then **Undo**, then **Delete** again | Back to "No reviews yet" → the review returns → gone again. |
| 9 | **Supabase Table Editor** | `orders`: 1 row, `Completed`, `price_php` 1150000.00 copied from the watch. `watches`: the GMT is `Sold`. `reviews`: 1 row, `helpful_count` 1, **`deleted_at` filled in** (soft delete). |

### Screenshots

| The app | Supabase Table Editor |
|---|---|
| ![LuxWatch app](docs/screenshots/app.png) | ![Supabase Table Editor](docs/screenshots/table-editor.png) |

_Placeholders: add `docs/screenshots/app.png` and `docs/screenshots/table-editor.png`._
