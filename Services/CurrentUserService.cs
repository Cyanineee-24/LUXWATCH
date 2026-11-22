using LuxWatch.Data.Mapping;
using LuxWatch.Data.Rows;
using LuxWatch.Models;
using static Supabase.Postgrest.Constants;

namespace LuxWatch.Services;

/// <summary>The user the app acts as.</summary>
public interface ICurrentUserService
{
    /// <summary>The current user, loaded from public.users once and cached; null if not found.</summary>
    Task<User?> GetAsync();
}

// TEMP until Supabase Auth: replace with the signed-in user (auth.uid()).
// For now the demo user's username comes from wwwroot/appsettings.json (Demo:CurrentUsername).
public sealed class CurrentUserService(Supabase.Client db, string username) : ICurrentUserService
{
    private User? cached;
    private Task<User?>? pending;

    public async Task<User?> GetAsync()
    {
        if (cached is not null) return cached;

        // Share one in-flight request between concurrent callers.
        pending ??= LoadAsync();
        try
        {
            cached = await pending;
            return cached;
        }
        finally
        {
            if (cached is null) pending = null;   // don't cache failures or "not found": allow a retry
        }
    }

    private async Task<User?> LoadAsync()
    {
        // Explicit columns, never select=*: anon can't read users.email (column-level grant).
        var res = await db.From<UserRow>()
            .Select("id,username,full_name,country,created_at")
            .Filter("username", Operator.Equals, username)
            .Limit(1)
            .Get();

        return res.Models.FirstOrDefault()?.ToDomain();
    }
}
