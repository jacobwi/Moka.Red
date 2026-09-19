using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Feedback.CommandPalette;
using Moka.Red.Feedback.Extensions;

namespace Moka.Red.Feedback.Tests.Components;

// A command's Href went to NavigationManager.NavigateTo as it was. A URL outside the app goes to the
// browser, so a javascript: Href ran as soon as the command was picked.
public class MokaCommandPaletteHrefTests : BunitContext
{
	private const string Module = "./_content/Moka.Red.Feedback/moka-command-palette.js";

	public MokaCommandPaletteHrefTests()
	{
		Services.AddMokaFeedback();
		BunitJSModuleInterop module = JSInterop.SetupModule(Module);
		module.Setup<int>("registerShortcut", _ => true).SetResult(1);
		module.SetupVoid("updateShortcut", _ => true).SetVoidResult();
		module.SetupVoid("focusInput", _ => true).SetVoidResult();
	}

	[Theory]
	[InlineData("javascript:alert(document.domain)")]
	[InlineData("  JavaScript:alert(1)")]
	[InlineData("data:text/html,<script>alert(1)</script>")]
	public async Task AScriptHref_IsNotNavigatedTo_ButTheCommandStillRuns(string href)
	{
		bool ran = await RunCommand(href);

		Assert.True(ran);
		Assert.Empty(Services.GetRequiredService<BunitNavigationManager>().History);
	}

	[Fact]
	public async Task AnAppHref_IsNavigatedTo()
	{
		await RunCommand("/settings");

		Assert.Equal("http://localhost/settings", Services.GetRequiredService<NavigationManager>().Uri);
	}

	private async Task<bool> RunCommand(string href)
	{
		bool ran = false;
		IMokaCommandPaletteService palette = Services.GetRequiredService<IMokaCommandPaletteService>();
		palette.Register(new MokaCommand
		{
			Id = "go",
			Title = "Go",
			Href = href,
			OnExecuteSync = () => ran = true
		});

		IRenderedComponent<MokaCommandPalette> cut = Render<MokaCommandPalette>();
		await cut.InvokeAsync(palette.Open);
		await cut.Find(".moka-command-palette").KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });
		return ran;
	}
}
