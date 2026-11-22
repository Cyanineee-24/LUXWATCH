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

**Reset demo data:** re-run `schema.sql` → `seed.sql` → `views.sql` in the Supabase SQL Editor. The demo user has only 2 reviewable orders, so a reset is needed to test Create more than twice. See [`database/README.md`](database/README.md#reset-demo-data).

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

### How to check it yourself
Open the page, then **DevTools → Network**, filter by `supabase.co`, and reload:
- `/collections/bestsellers` shows **1** request (`/rest/v1/watches?…status=eq.Active…`). Click **Next** and exactly 1 more appears.
- `/watch/rolex-submariner-14060` shows **4** requests: `watches?slug=eq.…`, `dealer_stats?…`, `dealer_reviews?…deleted_at=is.null…`, and `watches?id=in.(…)`.

## Known gaps

- **Order price snapshot:** when reviews are loaded, the `Order` object takes its price from the watch's current price instead of the order's stored `price_php` / `shipping_cost_php`. The order price isn't shown anywhere in the UI.

## Milestone checklist

| Rubric item | Where it lives |
|---|---|
| **Table design** | [`database/schema.sql`](database/schema.sql): 8 Tier 1 tables, uuid keys, CHECKs, FK indexes, RLS. Read views are in [`database/views.sql`](database/views.sql). The table → UML map is in [`database/README.md`](database/README.md). |
| **Read** | `/collections/bestsellers` (active listings, paged) and `/watch/{slug}` (watch, dealer stats, reviews) |
| **Create** | **Submit review** on `/watch/{slug}/rate`: `POST /reviews` |
| **Update** | **Helpful** on a review card: `PATCH helpful_count` |
| **Delete** | **Delete** / **Undo** on your own review: `PATCH deleted_at` (soft delete) |
| **Fast habits** | [Fast habits](#fast-habits-data-access) above |
| **Storage pattern** | [Storage pattern: soft delete](#storage-pattern-soft-delete) above, with details in [`database/README.md`](database/README.md#storage-pattern-soft-delete-reviews) |

### Screenshots

| The app | Supabase Table Editor |
|---|---|
| ![LuxWatch app](docs/screenshots/app.png) | ![Supabase Table Editor](docs/screenshots/table-editor.png) |

_Placeholders: add `docs/screenshots/app.png` and `docs/screenshots/table-editor.png`._
