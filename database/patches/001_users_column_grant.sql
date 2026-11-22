-- =============================================================================
-- Patch 001: stop exposing user emails to the publishable key.
-- For projects that were ALREADY created with an older schema.sql. A fresh
-- schema.sql run already includes this, so there's no need to run it there.
-- Safe to re-run.
-- =============================================================================

-- email is deliberately not readable with the publishable key.
revoke select on public.users from anon, authenticated;
grant select (id, username, full_name, country, created_at) on public.users to anon, authenticated;
