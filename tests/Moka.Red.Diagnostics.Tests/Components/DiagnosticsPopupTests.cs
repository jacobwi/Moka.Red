using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Diagnostics.Components;
using Moka.Red.Diagnostics.Extensions;
using Moka.Red.Diagnostics.Pages;
using Moka.Red.Diagnostics.Services;

namespace Moka.Red.Diagnostics.Tests.Components;

// The overlay's new-window button opened /moka-diagnostics, and on Blazor Server that page runs a
// circuit of its own with its own scoped service, so its Render and Log tabs never showed the data of
// the window that opened it.
public class DiagnosticsPopupTests : BunitContext
{
	private const string ModulePath = "./_content/Moka.Red.Diagnostics/moka-diagnostics.js";
	private MokaDiagnosticsService? _openerService;

	public DiagnosticsPopupTests()
	{
		Services.AddMokaDiagnostics(options => options.StartExpanded = true);

		// A test renders everything in one scope. Handing the first request (the overlay's) one service
		// and every later request a new one stands in for the scope each circuit gets.
		Services.AddTransient<IMokaDiagnosticsService>(sp =>
		{
			var service = new MokaDiagnosticsService(sp.GetRequiredService<DiagnosticsOptions>());
			_openerService ??= service;
			return service;
		});

		JSInterop.Mode = JSRuntimeMode.Loose;
	}

	[Fact]
	public async Task PopupPage_ShowsTheWindowThatOpenedIt()
	{
		JSRuntimeInvocationHandler openPage = SetupOverlayModule();
		IRenderedComponent<MokaDiagnosticsOverlay> overlay = Render<MokaDiagnosticsOverlay>();
		Assert.NotNull(_openerService);
		_openerService.RecordRender("OpenerOnlyComponent", "opener-1", TimeSpan.FromMilliseconds(2));

		await overlay.Find("button[title='Open in new window']").ClickAsync(new MouseEventArgs());
		JSRuntimeInvocation invocation = Assert.Single(openPage.Invocations);
		string? sessionKey = invocation.Arguments.Count > 0 ? invocation.Arguments[0] as string : null;
		Assert.False(string.IsNullOrEmpty(sessionKey));

		Services.GetRequiredService<NavigationManager>().NavigateTo($"moka-diagnostics?session={sessionKey}");
		IRenderedComponent<MokaDiagnosticsPage> page = Render<MokaDiagnosticsPage>();
		await page.FindAll(".moka-diag-page-tab").Single(tab => tab.TextContent.Trim() == "Render")
			.ClickAsync(new MouseEventArgs());

		Assert.Contains("OpenerOnlyComponent", page.Markup, StringComparison.Ordinal);
	}

	[Fact]
	public void PageWithoutAKnownSession_ShowsItsOwnData()
	{
		Services.GetRequiredService<NavigationManager>().NavigateTo("moka-diagnostics?session=unknown");

		IRenderedComponent<MokaDiagnosticsPage> page = Render<MokaDiagnosticsPage>();

		Assert.Contains("own data", page.Find(".moka-diag-page-notice").TextContent, StringComparison.Ordinal);
	}

	private JSRuntimeInvocationHandler SetupOverlayModule()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(ModulePath);
		module.Mode = JSRuntimeMode.Loose;
		return module.SetupVoid("openDiagnosticsPage", _ => true);
	}
}
