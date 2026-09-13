using MokaRedServer.Components;
using Moka.Red.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .__MOKA_SERVICE_METHOD__();

builder.Services.AddMokaRed();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
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
app.MapRazorComponents<App>()
    .__MOKA_RENDERMODE_METHOD__();

app.Run();
