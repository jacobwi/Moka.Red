using AngleSharp.Dom;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Feedback.CommandPalette;
using Moka.Red.Feedback.Extensions;

namespace Moka.Red.Feedback.Tests.Components;

// A command could only navigate in place, so an app that wanted a page in another tab opened it itself through JS
// interop from OnExecute.
public class MokaCommandPaletteTargetTests : BunitContext
{
	private const string Module = "./_content/Moka.Red.Feedback/moka-command-palette.js";
	private const string Address = "https://192.168.1.3:8043/";

	private readonly BunitJSModuleInterop _module;

	public MokaCommandPaletteTargetTests()
	{
		Services.AddMokaFeedback();
		JSInterop.Mode = JSRuntimeMode.Loose;
		_module = JSInterop.SetupModule(Module);
		_module.Setup<int>("registerShortcut", _ => true).SetResult(1);
		_module.SetupVoid("updateShortcut", _ => true).SetVoidResult();
		_module.SetupVoid("focusInput", _ => true).SetVoidResult();
		_module.SetupVoid("openTab", _ => true).SetVoidResult();
	}

	private BunitNavigationManager Navigation => Services.GetRequiredService<BunitNavigationManager>();

	[Fact]
	public async Task ACommandForANewTab_IsALink_ThatSaysSo()
	{
		IRenderedComponent<MokaCommandPalette> cut = await Open(new MokaCommand
		{
			Id = "omada", Title = "Open Omada", Href = Address, Target = "_blank"
		});

		IElement row = cut.Find(".moka-command-palette__item");
		Assert.Equal("A", row.TagName);
		Assert.Equal(Address, row.GetAttribute("href"));
		Assert.Equal("_blank", row.GetAttribute("target"));
		Assert.Equal("noopener noreferrer", row.GetAttribute("rel"));
		Assert.Equal("-1", row.GetAttribute("tabindex"));
		Assert.Equal("0", row.GetAttribute("data-moka-index"));
		Assert.Equal("true", row.QuerySelector(".moka-command-palette__item-external")!.GetAttribute("aria-hidden"));
		Assert.Equal(", opens in a new tab", row.QuerySelector(".moka-visually-hidden")!.TextContent);
	}

	[Fact]
	public async Task ACommandWithoutATarget_IsTheRowItWas()
	{
		IRenderedComponent<MokaCommandPalette> cut = await Open(new MokaCommand { Id = "go", Title = "Settings", Href = "/settings" });

		IElement row = cut.Find(".moka-command-palette__item");
		Assert.Equal("DIV", row.TagName);
		Assert.Empty(row.QuerySelectorAll(".moka-command-palette__item-external, .moka-visually-hidden"));
	}

	[Fact]
	public async Task Enter_OpensTheTab_BeforeTheCommandRuns_AndStaysOnThePage()
	{
		List<string> order = [];
		IRenderedComponent<MokaCommandPalette> cut = await Open(new MokaCommand
		{
			Id = "omada",
			Title = "Open Omada",
			Href = Address,
			Target = "_blank",
			OnExecuteSync = () => order.Add(_module.Invocations["openTab"].Count == 1 ? "ran after the tab" : "ran first")
		});

		await cut.Find(".moka-command-palette").KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

		JSRuntimeInvocation open = Assert.Single(_module.Invocations["openTab"]);
		Assert.Equal([Address, "_blank"], open.Arguments);
		Assert.Equal(["ran after the tab"], order);
		Assert.Empty(Navigation.History);
		Assert.Empty(cut.FindAll(".moka-command-palette"));
	}

	[Fact]
	public async Task AClick_IsTheLinksOwn_SoTheCommandRunsAndNothingElseOpens()
	{
		bool ran = false;
		IRenderedComponent<MokaCommandPalette> cut = await Open(new MokaCommand
		{
			Id = "omada", Title = "Open Omada", Href = Address, Target = "_blank", OnExecuteSync = () => ran = true
		});

		await cut.Find("a.moka-command-palette__item").ClickAsync(new MouseEventArgs());

		Assert.True(ran);
		Assert.Empty(_module.Invocations["openTab"]);
		Assert.Empty(Navigation.History);
		Assert.Empty(cut.FindAll(".moka-command-palette"));
	}

	[Fact]
	public async Task ATargetThatStaysInTheTab_NavigatesAsBefore()
	{
		IRenderedComponent<MokaCommandPalette> cut = await Open(new MokaCommand
		{
			Id = "go", Title = "Settings", Href = "/settings", Target = "_self"
		});

		Assert.Equal("DIV", cut.Find(".moka-command-palette__item").TagName);

		await cut.Find(".moka-command-palette").KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

		Assert.Equal("http://localhost/settings", Navigation.Uri);
		Assert.Empty(_module.Invocations["openTab"]);
	}

	[Theory]
	[InlineData("javascript:alert(document.domain)")]
	[InlineData("data:text/html,<script>alert(1)</script>")]
	public async Task AScriptHref_IsNeverALink_NorOpened(string href)
	{
		bool ran = false;
		IRenderedComponent<MokaCommandPalette> cut = await Open(new MokaCommand
		{
			Id = "x", Title = "X", Href = href, Target = "_blank", OnExecuteSync = () => ran = true
		});

		Assert.Empty(cut.FindAll("a"));

		await cut.Find(".moka-command-palette").KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

		Assert.True(ran);
		Assert.Empty(_module.Invocations["openTab"]);
		Assert.Empty(Navigation.History);
	}

	[Fact]
	public async Task ADisabledCommand_IsALinkToNowhere()
	{
		IRenderedComponent<MokaCommandPalette> cut = await Open(new MokaCommand
		{
			Id = "omada", Title = "Open Omada", Href = Address, Target = "_blank", Disabled = true
		});

		IElement row = cut.Find("a.moka-command-palette__item");
		Assert.False(row.HasAttribute("href"));
		Assert.False(row.HasAttribute("target"));
		Assert.Equal("link", row.GetAttribute("role"));
		Assert.Equal("true", row.GetAttribute("aria-disabled"));

		await cut.Find(".moka-command-palette").KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

		Assert.Empty(_module.Invocations["openTab"]);
	}

	[Fact]
	public async Task TheWords_CanBeReplaced()
	{
		IMokaCommandPaletteService palette = Services.GetRequiredService<IMokaCommandPaletteService>();
		palette.Register(new MokaCommand { Id = "omada", Title = "Omada", Href = Address, Target = "_blank" });
		IRenderedComponent<MokaCommandPalette> cut = Render<MokaCommandPalette>(p => p.Add(x => x.NewTabText, "ny flik"));
		await cut.InvokeAsync(palette.Open);

		Assert.Equal(", ny flik", cut.Find(".moka-visually-hidden").TextContent);
	}

	private async Task<IRenderedComponent<MokaCommandPalette>> Open(MokaCommand command)
	{
		IMokaCommandPaletteService palette = Services.GetRequiredService<IMokaCommandPaletteService>();
		palette.Register(command);
		IRenderedComponent<MokaCommandPalette> cut = Render<MokaCommandPalette>();
		await cut.InvokeAsync(palette.Open);
		return cut;
	}
}
