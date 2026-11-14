using Moka.Red.Showcase.Components;
using Moka.Red.Showcase.Components.Layout;
using Moka.Red.Diagnostics.Extensions;
using Moka.Red.Diagnostics.Pages;
using Moka.Red.Extensions;
using Moka.Red.Feedback.Extensions;
using Moka.Red.Layout.DockLayout;
using Moka.Red.Navigation.Extensions;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
	.AddInteractiveServerComponents();

builder.Services.AddMokaRed();
builder.Services.AddMokaSystemNotifier();
builder.Services.AddMokaTabs<string>();
builder.Services.AddMokaDockWindows();
builder.Services.AddMokaDiagnostics();
builder.Services.AddScoped<ShowcaseLook>();

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Error", true);
	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
	.AddInteractiveServerRenderMode()
	// Endpoints for /moka-diagnostics and /moka-dock-window, the page a dock panel opens in another window. Routes.razor
	// lists the assemblies for the router, but without this a direct load of either URL answers 404 with the not-found page.
	.AddAdditionalAssemblies(typeof(MokaDiagnosticsPage).Assembly, typeof(MokaDockWindowPage).Assembly);

app.Run();
