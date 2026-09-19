using Moka.Red.DevApp.Components;
using Moka.Red.Diagnostics.Extensions;
using Moka.Red.Diagnostics.Pages;
using Moka.Red.Extensions;
using Moka.Red.Navigation.Extensions;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
	.AddInteractiveServerComponents();

builder.Services.AddMokaRed();
builder.Services.AddMokaTabs<string>();
builder.Services.AddMokaDiagnostics();

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
	// Endpoints for /moka-diagnostics. Routes.razor lists the assembly for the router, but without
	// this a direct load of the URL answers 404 with the not-found page.
	.AddAdditionalAssemblies(typeof(MokaDiagnosticsPage).Assembly);

app.Run();
