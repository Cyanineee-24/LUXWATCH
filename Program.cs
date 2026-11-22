using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using LuxWatch;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Supabase: one client for the whole app, injected with @inject Supabase.Client.
// Config comes from wwwroot/appsettings.json (publishable key only — it is public by design).
var url = builder.Configuration["Supabase:Url"];
var key = builder.Configuration["Supabase:Key"];
if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(key))
    throw new InvalidOperationException(
        "Supabase:Url / Supabase:Key missing. Put them in wwwroot/appsettings.json.");

var supabase = new Supabase.Client(url, key,
    new Supabase.SupabaseOptions { AutoConnectRealtime = false });
await supabase.InitializeAsync();
builder.Services.AddSingleton(supabase);

// Repositories (reads and writes): stateless, wrap the singleton client → singletons.
builder.Services.AddSingleton<LuxWatch.Data.Repositories.IWatchRepository, LuxWatch.Data.Repositories.SupabaseWatchRepository>();
builder.Services.AddSingleton<LuxWatch.Data.Repositories.IDealerRepository, LuxWatch.Data.Repositories.SupabaseDealerRepository>();
builder.Services.AddSingleton<LuxWatch.Data.Repositories.IReviewReadRepository, LuxWatch.Data.Repositories.SupabaseReviewReadRepository>();
builder.Services.AddSingleton<LuxWatch.Data.Repositories.IOrderRepository, LuxWatch.Data.Repositories.SupabaseOrderRepository>();
builder.Services.AddSingleton<LuxWatch.Data.Repositories.IReviewWriteRepository, LuxWatch.Data.Repositories.SupabaseReviewWriteRepository>();
builder.Services.AddSingleton<LuxWatch.Services.HelpfulVoteTracker>();

// TEMP until Supabase Auth: the demo user from wwwroot/appsettings.json (Demo:CurrentUsername).
var demoUsername = builder.Configuration["Demo:CurrentUsername"];
if (string.IsNullOrWhiteSpace(demoUsername))
    throw new InvalidOperationException(
        "Demo:CurrentUsername missing. Put it in wwwroot/appsettings.json.");
builder.Services.AddSingleton<LuxWatch.Services.ICurrentUserService>(
    sp => new LuxWatch.Services.CurrentUserService(sp.GetRequiredService<Supabase.Client>(), demoUsername));

await builder.Build().RunAsync();
