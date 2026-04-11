using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Moka.Blazor.Repl.Host;
using Moka.Red.Extensions;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Static-root mount of the shared App component from Moka.Blazor.Repl.Host.
// App owns the [JSInvokable] LoadAssembly method that wasmPreview.js calls
// to dynamically render compiled docs preview snippets.
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(_ => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});

// ── Moka.Red service registrations for docs previews ────────────
builder.Services.AddMokaRed();

await builder.Build().RunAsync();
