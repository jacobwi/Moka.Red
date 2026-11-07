using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Moka.Red.Extensions;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddMokaRed();

await builder.Build().RunAsync();
