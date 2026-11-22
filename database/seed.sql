-- =============================================================================
-- LuxWatch: demo seed data, CATALOG ONLY
-- Run AFTER schema.sql. Ids are uuid defaults: never hardcoded; every FK is
-- looked up by its natural key (slug, username, dealer name).
--
-- Catalog vs transactional data: this file seeds what a marketplace has before
-- anyone buys anything (brands, dealers, users, listings). Orders and reviews
-- are TRANSACTIONAL: they start empty and are created only by the app
-- (Buy → Confirm delivery → Rate dealer), so every one of them is real demo data.
-- =============================================================================
begin;

-- ── users ────────────────────────────────────────────────────────────────────
-- marcusvance = the demo buyer (Demo:CurrentUsername); the 4 dealer owners.
insert into public.users (username, full_name, email, country, created_at) values
    ('marcusvance',  'Marcus Vance',         'marcus.vance@horology.com',      'PH', '2026-01-01T00:00:00Z'),
    ('hauszeitwerk', 'Haus Zeitwerk',        'service@zeitwerk.example.de',    'DE', '2015-01-01T00:00:00Z'),
    ('manilachrono', 'Manila Chrono',        'hello@manilachrono.example.ph',  'PH', '2018-03-01T00:00:00Z'),
    ('lioncitytime', 'Lion City Timepieces', 'sales@lioncitytime.example.sg',  'SG', '2012-06-01T00:00:00Z'),
    ('ginzatokei',   'Ginza Tokei',          'info@ginzatokei.example.jp',     'JP', '2009-04-01T00:00:00Z');

-- ── brands (20, landing-page order) ──────────────────────────────────────────
-- slug = Brand.GenerateSlug(name): lower-case, spaces → '-'.
insert into public.brands (name, slug, is_featured, display_order, description) values
    ('Rolex',               'rolex',               true,   0, 'Iconic Swiss luxury watch manufacturer founded in 1905.'),
    ('Patek Philippe',      'patek-philippe',      true,   1, 'Geneva''s premier independent family-owned manufacture.'),
    ('Audemars Piguet',     'audemars-piguet',     true,   2, 'Master of high complications and creator of the Royal Oak.'),
    ('Panerai',             'panerai',             false,  3, 'Italian design combined with Swiss horological excellence.'),
    ('Breitling',           'breitling',           false,  4, 'Specialist in chronographs and technical aviation watches.'),
    ('Omega',               'omega',               true,   5, 'Precision watchmaking, chronometers of the Moon and the Olympics.'),
    ('Seiko',               'seiko',               false,  6, 'Pioneering Japanese watchmaking and craftsmanship.'),
    ('TAG Heuer',           'tag-heuer',           false,  7, 'Avant-garde Swiss chronographs rooted in motor racing.'),
    ('IWC',                 'iwc',                 false,  8, 'Engineered luxury timepieces from Schaffhausen.'),
    ('Tudor',               'tudor',               true,   9, 'Born for daring adventurers, exceptional robust reliability.'),
    ('Cartier',             'cartier',             true,  10, 'The jeweler of kings and king of shaped watchmakers.'),
    ('Vacheron Constantin', 'vacheron-constantin', true,  11, 'The oldest continuously operating watch manufacture since 1755.'),
    ('Jaeger-LeCoultre',    'jaeger-lecoultre',    false, 12, 'The watchmaker''s watchmaker from the Vallée de Joux.'),
    ('Hublot',              'hublot',              false, 13, 'The Art of Fusion in modern luxury horology.'),
    ('Zenith',              'zenith',              false, 14, 'Home of the high-frequency El Primero chronograph caliber.'),
    ('Breguet',             'breguet',             false, 15, 'Inventor of the tourbillon and pioneer of Haute Horlogerie.'),
    ('Blancpain',           'blancpain',           false, 16, 'Creator of the modern diving watch and pure mechanical traditions.'),
    ('Grand Seiko',         'grand-seiko',         false, 17, 'Japanese perfection, Zaratsu polishing, and Spring Drive innovation.'),
    ('Richard Mille',       'richard-mille',       false, 18, 'A racing machine on the wrist with space-age materials.'),
    ('Chopard',             'chopard',             false, 19, 'Artisan watchmaking, Happy Diamonds, and Mille Miglia elegance.');

-- ── dealers (4, each owned by a user) ────────────────────────────────────────
-- Dealer counts (active listings, sold, reviews) are derived by the
-- dealer_stats view from real rows, never stored here.
insert into public.dealers (user_id, name, country, since, ships_within, responds_within, is_verified, created_at)
select u.id, v.name, v.country, v.since, v.ships, v.responds, v.verified, v.created_at
from (values
    ('hauszeitwerk', 'Haus Zeitwerk GmbH',      'Germany',     date '2015-01-01', 'Usually ships in-stock items within 24 hours', 'Replies within 10 hours', true,  timestamptz '2015-01-01T00:00:00Z'),
    ('manilachrono', 'Manila Chrono Exchange',  'Philippines', date '2018-03-01', 'Ships within 2 business days',                 'Replies within 2 hours',  true,  timestamptz '2018-03-01T00:00:00Z'),
    ('lioncitytime', 'Lion City Timepieces',    'Singapore',   date '2012-06-01', 'Usually ships within 24 hours',                'Replies within 4 hours',  true,  timestamptz '2012-06-01T00:00:00Z'),
    ('ginzatokei',   'Ginza Tokei Gallery',     'Japan',       date '2009-04-01', 'Ships within 3 business days',                 'Replies within 12 hours', false, timestamptz '2009-04-01T00:00:00Z')
) as v(username, name, country, since, ships, responds, verified, created_at)
join public.users u on u.username = v.username;


-- ── Active catalog: 24 listings ──────────────────────────────────────────────
-- Staged in a temp table so watches, images and specs come from one list.
-- Prices span ₱22,500 → ₱6.2M; 9 listings have views_in_48h > 1000 (Popular).
create temp table _listing (
    slug text, brand_slug text, dealer text, loc char(2),
    model text, reference text, subtitle text, description text,
    condition text, year int, year_approx boolean, has_box boolean, has_papers boolean,
    price numeric(12,2), shipping numeric(12,2), views int, listed date, delivery text,
    movement text, material text, diameter text, water text
) on commit drop;

insert into _listing values
-- Haus Zeitwerk GmbH (DE) — 7
('rolex-submariner-14060', 'rolex', 'Haus Zeitwerk GmbH', 'DE',
 'Submariner (No Date)', '14060', 'Automatic men''s watch, Ref. 14060',
 'An exceptional neo-vintage Rolex Submariner Ref. 14060 featuring a clean two-line dial and luminous tritium hour markers with subtle patina. Retaining sharp factory lug bevels and the iconic aluminum bezel insert, this reference captures the purest essence of the modern classic dive watch. Fully inspected, authentic throughout, and performing within chronometer standards.',
 'PreOwned', 1998, true, false, false, 561563, 4500, 1148, '2026-09-15', 'Oct 1 – Oct 17',
 null, null, null, null),   -- specs + images for this one are seeded explicitly below
('patek-philippe-calatrava-5196r', 'patek-philippe', 'Haus Zeitwerk GmbH', 'DE',
 'Calatrava', '5196R-001', 'Manual-wind dress watch, rose gold',
 'A purist''s Calatrava in 18k rose gold with a silvery opaline dial and slim dauphine hands. Serviced, unpolished case, complete with Patek Philippe box and certificate.',
 'PreOwned', 2015, false, true, true, 1480000, 6500, 540, '2026-10-28', 'Nov 26 – Dec 6',
 'Manual winding', '18k rose gold', '37 mm', '30 m'),
('omega-aqua-terra-22010412101001', 'omega', 'Haus Zeitwerk GmbH', 'DE',
 'Seamaster Aqua Terra 150M', '220.10.41.21.01.001', 'Co-Axial Master Chronometer',
 'Teak-pattern black dial, Master Chronometer calibre 8900 and a crisp steel bracelet. Light wear only, full set.',
 'PreOwned', 2021, false, true, true, 285000, 3500, 410, '2026-11-02', 'Nov 24 – Dec 3',
 'Automatic', 'Steel', '41 mm', '150 m'),
('iwc-portugieser-chronograph-iw371605', 'iwc', 'Haus Zeitwerk GmbH', 'DE',
 'Portugieser Chronograph', 'IW371605', 'Automatic chronograph, blue hands',
 'The classic Portugieser layout with applied Arabic numerals and blue hands on a silver dial. Includes IWC box and papers.',
 'PreOwned', 2019, false, true, true, 445000, 3500, 720, '2026-10-20', 'Nov 24 – Dec 3',
 'Automatic', 'Steel', '41 mm', '30 m'),
('tag-heuer-monaco-cbl2111', 'tag-heuer', 'Haus Zeitwerk GmbH', 'DE',
 'Monaco Calibre Heuer 02', 'CBL2111.FC6453', 'Square automatic chronograph',
 'The square racing icon with the in-house Heuer 02 movement and a sunray blue dial. Strap recently replaced.',
 'PreOwned', 2020, false, true, false, 298000, 3500, 450, '2026-11-05', 'Nov 24 – Dec 3',
 'Automatic', 'Steel', '39 mm', '100 m'),
('breitling-navitimer-ab0138', 'breitling', 'Haus Zeitwerk GmbH', 'DE',
 'Navitimer B01 Chronograph 43', 'AB0138211B1A1', 'Pilot''s chronograph with slide rule',
 'Manufacture B01 calibre, 70-hour reserve and the circular slide-rule bezel pilots rely on. Excellent condition.',
 'PreOwned', 2022, false, true, true, 375000, 3500, 300, '2026-11-10', 'Nov 27 – Dec 6',
 'Automatic', 'Steel', '43 mm', '30 m'),
('rolex-datejust-1601-1972', 'rolex', 'Haus Zeitwerk GmbH', 'DE',
 'Datejust 36', '1601', 'Vintage pie-pan dial, Jubilee bracelet',
 'A charming vintage Datejust with a champagne pie-pan dial and white-gold fluted bezel. Honest patina, recently serviced.',
 'PreOwned', 1972, true, false, false, 268000, 3500, 830, '2026-10-12', 'Nov 24 – Dec 3',
 'Automatic', 'Steel / white gold', '36 mm', '100 m'),

-- Manila Chrono Exchange (PH) — 6
('rolex-oyster-perpetual-124300', 'rolex', 'Manila Chrono Exchange', 'PH',
 'Oyster Perpetual 41', '124300', 'Bright blue dial',
 'Clean, unpolished Oyster Perpetual 41 with the calibre 3230 movement. Local PH warranty card included.',
 'PreOwned', 2022, false, true, true, 420000, 1500, 860, '2026-11-08', 'Nov 21 – Nov 25',
 'Automatic', 'Oystersteel', '41 mm', '100 m'),
('audemars-piguet-royal-oak-offshore-26470st', 'audemars-piguet', 'Manila Chrono Exchange', 'PH',
 'Royal Oak Offshore Chronograph', '26470ST.OO.A027CA.01', 'Panda dial, rubber strap',
 'Bold 42 mm Offshore with the "Méga Tapisserie" panda dial. Comes with AP box, papers and a spare strap.',
 'PreOwned', 2018, false, true, true, 1390000, 1500, 940, '2026-10-30', 'Nov 21 – Nov 25',
 'Automatic', 'Steel', '42 mm', '100 m'),
('omega-seamaster-diver-300m-21030422001001', 'omega', 'Manila Chrono Exchange', 'PH',
 'Seamaster Diver 300M', '210.30.42.20.01.001', 'Black wave dial, ceramic bezel',
 'Brand-new, unworn Seamaster Diver 300M with stickers and full Omega warranty.',
 'BrandNew', 2026, false, true, true, 330000, 1500, 1260, '2026-11-14', 'Nov 21 – Nov 25',
 'Automatic', 'Steel', '42 mm', '300 m'),
('cartier-tank-must-wsta0041', 'cartier', 'Manila Chrono Exchange', 'PH',
 'Tank Must Large', 'WSTA0041', 'Quartz, black leather strap',
 'The timeless Tank in its modern Must edition. New old stock, full set.',
 'BrandNew', 2025, false, true, true, 165000, 1500, 520, '2026-11-11', 'Nov 21 – Nov 25',
 'Quartz', 'Steel', '33.7 × 25.5 mm', '30 m'),
('seiko-presage-cocktail-srpb43', 'seiko', 'Manila Chrono Exchange', 'PH',
 'Presage Cocktail Time', 'SRPB43J1', 'Sky-blue sunburst dial',
 'The cult "Blue Moon" Cocktail Time with its shimmering textured dial. Unworn.',
 'BrandNew', 2025, false, true, true, 22500, 800, 980, '2026-11-16', 'Nov 21 – Nov 25',
 'Automatic', 'Steel', '40.5 mm', '50 m'),
('tag-heuer-carrera-cbn2a1b', 'tag-heuer', 'Manila Chrono Exchange', 'PH',
 'Carrera Chronograph 44', 'CBN2A1B.BA0643', 'Heuer 02, black ceramic bezel',
 'Sporty Carrera with the Heuer 02 calibre and a ceramic tachymeter bezel. Minor desk-diving marks on the clasp.',
 'PreOwned', 2021, false, true, true, 189000, 1500, 370, '2026-10-25', 'Nov 21 – Nov 25',
 'Automatic', 'Steel', '44 mm', '100 m'),

-- Lion City Timepieces (SG) — 6
('rolex-gmt-master-ii-126710blro', 'rolex', 'Lion City Timepieces', 'SG',
 'GMT-Master II "Pepsi"', '126710BLRO', 'Jubilee bracelet, red/blue Cerachrom bezel',
 'The sought-after Pepsi GMT on a Jubilee bracelet. Full set with 2023 warranty card.',
 'PreOwned', 2023, false, true, true, 1150000, 2500, 2140, '2026-11-12', 'Nov 23 – Nov 30',
 'Automatic', 'Oystersteel', '40 mm', '100 m'),
('audemars-piguet-royal-oak-15500st', 'audemars-piguet', 'Lion City Timepieces', 'SG',
 'Royal Oak', '15500ST.OO.1220ST.01', 'Blue Grande Tapisserie dial',
 'The 41 mm Royal Oak with the calibre 4302 movement. Unpolished, complete with AP service papers.',
 'PreOwned', 2020, false, true, true, 2950000, 2500, 2780, '2026-11-06', 'Nov 23 – Nov 30',
 'Automatic', 'Steel', '41 mm', '50 m'),
('tudor-black-bay-gmt-m79830rb', 'tudor', 'Lion City Timepieces', 'SG',
 'Black Bay GMT', 'M79830RB-0001', 'Burgundy/blue bezel',
 'A true travel GMT with the manufacture MT5652 movement. Light hairlines only.',
 'PreOwned', 2022, false, true, true, 215000, 2500, 690, '2026-10-18', 'Nov 23 – Nov 30',
 'Automatic', 'Steel', '41 mm', '200 m'),
('cartier-santos-medium-wssa0029', 'cartier', 'Lion City Timepieces', 'SG',
 'Santos de Cartier Medium', 'WSSA0029', 'Silver dial, QuickSwitch bracelet',
 'The medium Santos with QuickSwitch strap system and SmartLink sizing. Full set, 2023.',
 'PreOwned', 2023, false, true, true, 395000, 2500, 1080, '2026-11-09', 'Nov 23 – Nov 30',
 'Automatic', 'Steel', '35.1 mm', '100 m'),
('vacheron-constantin-overseas-4500v', 'vacheron-constantin', 'Lion City Timepieces', 'SG',
 'Overseas', '4500V/110A-B128', 'Blue dial, three straps',
 'Haute horlogerie sports watch with interchangeable bracelet, leather and rubber straps. Complete set.',
 'PreOwned', 2020, false, true, true, 1920000, 2500, 1520, '2026-11-03', 'Nov 23 – Nov 30',
 'Automatic', 'Steel', '41 mm', '150 m'),
('panerai-luminor-pam01312', 'panerai', 'Lion City Timepieces', 'SG',
 'Luminor Marina', 'PAM01312', 'Sandwich dial, P.9010 movement',
 'Classic Luminor with crown-guard bridge and 3-day power reserve. Comes on the original alligator strap.',
 'PreOwned', 2019, false, true, true, 265000, 2500, 250, '2026-10-15', 'Nov 23 – Nov 30',
 'Automatic', 'Steel', '44 mm', '300 m'),

-- Ginza Tokei Gallery (JP) — 5
('rolex-daytona-116500ln', 'rolex', 'Ginza Tokei Gallery', 'JP',
 'Cosmograph Daytona', '116500LN', 'White dial, ceramic bezel',
 'The modern-classic white-dial Daytona. Excellent condition, full set with 2021 card.',
 'PreOwned', 2021, false, true, true, 1650000, 3000, 3010, '2026-11-13', 'Nov 25 – Dec 2',
 'Automatic', 'Oystersteel', '40 mm', '100 m'),
('patek-philippe-nautilus-5711-1a', 'patek-philippe', 'Ginza Tokei Gallery', 'JP',
 'Nautilus', '5711/1A-010', 'Blue gradient dial',
 'The discontinued steel Nautilus 5711. Lightly worn, with Patek Philippe box, certificate and service card.',
 'PreOwned', 2019, false, true, true, 6200000, 3000, 4120, '2026-11-01', 'Nov 25 – Dec 2',
 'Automatic', 'Steel', '40 mm', '120 m'),
('tudor-pelagos-m25600tn', 'tudor', 'Ginza Tokei Gallery', 'JP',
 'Pelagos', 'M25600TN-0001', 'Titanium dive watch',
 'Professional titanium diver with helium valve and self-adjusting clasp. Very good condition.',
 'PreOwned', 2020, false, true, true, 245000, 3000, 380, '2026-10-22', 'Nov 25 – Dec 2',
 'Automatic', 'Titanium', '42 mm', '500 m'),
('seiko-prospex-spb143', 'seiko', 'Ginza Tokei Gallery', 'JP',
 'Prospex 62MAS Reissue', 'SPB143J1', 'Grey sunburst dial',
 'Modern take on Seiko''s first diver from 1965. Box and papers included.',
 'PreOwned', 2021, false, true, true, 58000, 3000, 1340, '2026-11-15', 'Nov 25 – Dec 2',
 'Automatic', 'Steel', '40.5 mm', '200 m'),
('grand-seiko-sbgw231', 'grand-seiko', 'Ginza Tokei Gallery', 'JP',
 'Elegance SBGW231', 'SBGW231', 'Ivory dial, hand-wound 9S64',
 'Understated Grand Seiko dress watch with Zaratsu-polished case and a 72-hour hand-wound movement.',
 'PreOwned', 2021, false, true, true, 255000, 3000, 610, '2026-10-26', 'Nov 25 – Dec 2',
 'Manual winding', 'Steel', '37.3 mm', '30 m');

insert into public.watches (
    slug, brand_id, dealer_id, model, reference, subtitle, description, condition,
    year, year_is_approximate, has_box, has_papers, price_php, shipping_cost_php,
    location_country_code, views_in_48h, listed_date, delivery_window, status)
select l.slug, b.id, d.id, l.model, l.reference, l.subtitle, l.description, l.condition,
       l.year, l.year_approx, l.has_box, l.has_papers, l.price, l.shipping,
       l.loc, l.views, l.listed, l.delivery, 'Active'
from _listing l
join public.brands  b on b.slug = l.brand_slug
join public.dealers d on d.name = l.dealer;

-- 4 specs per listing (the Submariner gets its full 10-line list below).
insert into public.watch_specs (watch_id, key, value, display_order)
select w.id, s.key, s.value, s.ord
from _listing l
join public.watches w on w.slug = l.slug
cross join lateral (values
    (0, 'Movement',         l.movement),
    (1, 'Case material',    l.material),
    (2, 'Case diameter',    l.diameter),
    (3, 'Water resistance', l.water)
) as s(ord, key, value)
where l.slug <> 'rolex-submariner-14060';

-- 2 images per listing, rotating through the images in wwwroot/images.
insert into public.watch_images (watch_id, url, alt_text, sort_order)
select w.id,
       (array['/images/landing_page_hero_macro.jpg', '/images/landing_page_hero_gold.jpg',
              '/images/landing_page_hero_art.jpg',   '/images/login_hero_art.jpg',
              '/images/register_hero_art.jpg'])[((l.n + i.ord) % 5)::int + 1],
       b.name || ' ' || l.model || case i.ord when 0 then ' — dial' else ' — case profile' end,
       i.ord
from (select *, row_number() over (order by slug) as n from _listing) l
join public.watches w on w.slug = l.slug
join public.brands  b on b.id = w.brand_id
cross join (values (0), (1)) as i(ord)
where l.slug <> 'rolex-submariner-14060';

-- Featured Submariner: the full 10-line spec list and 5 gallery images.
insert into public.watch_specs (watch_id, key, value, display_order)
select w.id, v.key, v.value, v.ord
from public.watches w
cross join (values
    (0, 'Brand',              'Rolex'),
    (1, 'Model',              'Submariner (No Date)'),
    (2, 'Reference',          '14060'),
    (3, 'Movement',           'Automatic'),
    (4, 'Case material',      'Steel'),
    (5, 'Case diameter',      '40 mm'),
    (6, 'Water resistance',   '300 m'),
    (7, 'Year of production', '1998 (approx.)'),
    (8, 'Condition',          'Used (Good)'),
    (9, 'Scope of delivery',  'No original box, no original papers')
) as v(ord, key, value)
where w.slug = 'rolex-submariner-14060';

insert into public.watch_images (watch_id, url, alt_text, sort_order)
select w.id, v.url, v.alt, v.ord
from public.watches w
cross join (values
    (0, '/images/landing_page_hero_macro.jpg', 'Rolex Submariner dial macro'),
    (1, '/images/login_hero_art.jpg',          'Rolex Submariner case profile'),
    (2, '/images/register_hero_art.jpg',       'Rolex Submariner oyster bracelet'),
    (3, '/images/landing_page_hero_macro.jpg', 'Rolex Submariner bezel close-up'),
    (4, '/images/login_hero_art.jpg',          'Rolex Submariner caseback')
) as v(ord, url, alt)
where w.slug = 'rolex-submariner-14060';


commit;
