using MokaRedServer.Components;
using Moka.Red.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .__MOKA_SERVICE_METHOD__();

builder.Services.AddMokaRed();

var app = builder.Build();

#if (UseWebAssembly)
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
#else
if (!app.Environment.IsDevelopment())
#endif
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
#if (!noHttps)
    app.UseHsts();
#endif
}

#if (!noHttps)
app.UseHttpsRedirection();
#endif
app.UseAntiforgery();
app.MapStaticAssets();
#if (UseWebAssembly)
app.MapRazorComponents<App>()
    .__MOKA_RENDERMODE_METHOD__()
    .AddAdditionalAssemblies(typeof(MokaRedServer.Client._Imports).Assembly);
#else
app.MapRazorComponents<App>()
    .__MOKA_RENDERMODE_METHOD__();
#endif

app.Run();
