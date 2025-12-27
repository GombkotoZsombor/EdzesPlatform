using System;
using System.Net.Http;
using EdzesPlatform;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration; // Ez fontos a config-hoz!
using Microsoft.Extensions.DependencyInjection; // Ez a Services-hez!
using Supabase;
 // Vagy ami a te projekted neve (pl. EdzesPlatform)

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// --- SUPABASE BEKÖTÉSE (Javított) ---
builder.Services.AddScoped<Supabase.Client>(provider =>
{
    var config = provider.GetRequiredService<IConfiguration>();

    // Itt kezeljük a null értéket: ha nincs megadva, dobjon hibát vagy üreset adjon
    var url = config["SupabaseUrl"] ?? throw new Exception("SupabaseUrl hiányzik az appsettings.json-ból!");
    var key = config["SupabaseKey"] ?? throw new Exception("SupabaseKey hiányzik az appsettings.json-ból!");

    var options = new Supabase.SupabaseOptions
    {
        AutoRefreshToken = true,
        AutoConnectRealtime = false,
    };

    var client = new Supabase.Client(url, key, options);
   
    return client;
});
// -------------------------------------
builder.Services.AddScoped<EdzesPlatform.Services.DataService>();
builder.Services.AddScoped<EdzesPlatform.Services.WorkoutExportService>();

await builder.Build().RunAsync();