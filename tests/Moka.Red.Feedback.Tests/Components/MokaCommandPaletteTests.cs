using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Feedback.CommandPalette;
using Moka.Red.Feedback.Extensions;

namespace Moka.Red.Feedback.Tests.Components;

// The shortcut is matched in moka-command-palette.js. These tests cover what reaches it.
public class MokaCommandPaletteTests : BunitContext
{
	private const string Module = "./_content/Moka.Red.Feedback/moka-command-palette.js";

	public MokaCommandPaletteTests() => Services.AddMokaFeedback();

	[Fact]
	public void RegistersModK_ByDefault()
	{
		BunitJSModuleInterop module = SetupModule();

		Render<MokaCommandPalette>();

		Assert.Equal("Mod+K", module.VerifyInvoke("registerShortcut").Arguments[1]);
	}

	[Fact]
	public void PassesACustomShortcut_AndUpdatesItWhenItChanges()
	{
		BunitJSModuleInterop module = SetupModule();

		IRenderedComponent<MokaCommandPalette> cut = Render<MokaCommandPalette>(p => p
			.Add(x => x.Shortcut, "Ctrl+Shift+P"));
		cut.Render(p => p.Add(x => x.Shortcut, ""));

		Assert.Equal("Ctrl+Shift+P", module.VerifyInvoke("registerShortcut").Arguments[1]);
		Assert.Equal("", module.VerifyInvoke("updateShortcut").Arguments[1]);
	}

	// Grouped, the list shows Edit before Go. Enter used to run the filtered list's first command in
	// registration order (Go to home) while Save was highlighted.
	[Fact]
	public async Task Enter_RunsTheHighlightedCommand_WhenGrouped()
	{
		SetupModule();
		var ran = new List<string>();
		IMokaCommandPaletteService palette = Services.GetRequiredService<IMokaCommandPaletteService>();
		palette.RegisterMany(
		[
			new MokaCommand { Id = "home", Title = "Go to home", Group = "Go", OnExecuteSync = () => ran.Add("home") },
			new MokaCommand { Id = "save", Title = "Save", Group = "Edit", OnExecuteSync = () => ran.Add("save") }
		]);

		IRenderedComponent<MokaCommandPalette> cut = Render<MokaCommandPalette>();
		await cut.InvokeAsync(palette.Open);

		Assert.Equal("Save", cut.Find(".moka-command-palette__item--focused .moka-command-palette__item-title").TextContent);
		await cut.Find(".moka-command-palette").KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

		Assert.Equal(["save"], ran);
	}

	private BunitJSModuleInterop SetupModule()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(Module);
		module.Setup<int>("registerShortcut", _ => true).SetResult(1);
		module.SetupVoid("updateShortcut", _ => true).SetVoidResult();
		module.SetupVoid("focusInput", _ => true).SetVoidResult();
		return module;
	}
}
