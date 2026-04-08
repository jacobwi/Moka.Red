using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Moka.Blazor.Repl.Host;

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

// ── Customize: register your library's services here ─────────────
// Anything you add can be @inject-ed by docs preview snippets.
// Examples:
//   builder.Services.AddYourThing();
//   builder.Services.AddSingleton<IMyService, MyService>();
// ──────────────────────────────────────────────────────────────────

await builder.Build().RunAsync();
