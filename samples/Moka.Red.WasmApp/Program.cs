using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Moka.Red.Extensions;
using Moka.Red.Navigation.Extensions;
using Moka.Red.WasmApp;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddMokaRed();
builder.Services.AddMokaTabs<string>();

await builder.Build().RunAsync();
