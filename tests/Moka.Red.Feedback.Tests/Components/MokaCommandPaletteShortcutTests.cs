using AngleSharp.Dom;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Feedback.CommandPalette;
using Moka.Red.Feedback.Extensions;

namespace Moka.Red.Feedback.Tests.Components;

// A command's shortcut was one run of dim text while the context menu and the cheatsheet drew key caps,
// so the same "Ctrl+S" looked like two different things in one app.
public class MokaCommandPaletteShortcutTests : BunitContext
{
	private const string Module = "./_content/Moka.Red.Feedback/moka-command-palette.js";

	public MokaCommandPaletteShortcutTests()
	{
		Services.AddMokaFeedback();
		BunitJSModuleInterop module = JSInterop.SetupModule(Module);
		module.Setup<int>("registerShortcut", _ => true).SetResult(1);
		module.SetupVoid("updateShortcut", _ => true).SetVoidResult();
		module.SetupVoid("focusInput", _ => true).SetVoidResult();
	}

	[Fact]
	public async Task EachKeyGetsItsOwnCap()
	{
		IRenderedComponent<MokaCommandPalette> cut = await OpenWith("Ctrl+Shift+S");

		Assert.Equal(["Ctrl", "Shift", "S"], ShortcutCaps(cut));
	}

	[Fact]
	public async Task ASingleKeyIsASingleCap()
	{
		IRenderedComponent<MokaCommandPalette> cut = await OpenWith("F2");

		Assert.Equal(["F2"], ShortcutCaps(cut));
	}

	[Fact]
	public async Task ACommandWithoutAShortcutDrawsNoCap()
	{
		IRenderedComponent<MokaCommandPalette> cut = await OpenWith(null);

		Assert.Empty(ShortcutCaps(cut));
	}

	[Fact]
	public async Task ShowShortcutsFalse_DrawsNoCap()
	{
		IRenderedComponent<MokaCommandPalette> cut = await OpenWith("Ctrl+S", shortcuts => shortcuts.Add(p => p.ShowShortcuts, false));

		Assert.Empty(ShortcutCaps(cut));
	}

	// The footer hints name keys too, so they are caps rather than letters run together with the label.
	[Fact]
	public async Task TheFooterHintsAreCaps()
	{
		IRenderedComponent<MokaCommandPalette> cut = await OpenWith(null);

		Assert.Equal(
			["↑↓", "⏎", "Esc"],
			cut.FindAll(".moka-command-palette__footer kbd").Select(cap => cap.TextContent.Trim()));
	}

	private static IEnumerable<string> ShortcutCaps(IRenderedComponent<MokaCommandPalette> cut) =>
		cut.FindAll(".moka-command-palette__item-shortcut kbd").Select(cap => cap.TextContent.Trim());

	private async Task<IRenderedComponent<MokaCommandPalette>> OpenWith(
		string? shortcut,
		Action<ComponentParameterCollectionBuilder<MokaCommandPalette>>? parameters = null)
	{
		IMokaCommandPaletteService palette = Services.GetRequiredService<IMokaCommandPaletteService>();
		palette.Register(new MokaCommand
		{
			Id = "save",
			Title = "Save",
			Shortcut = shortcut,
			OnExecuteSync = () => { }
		});

		IRenderedComponent<MokaCommandPalette> cut = Render<MokaCommandPalette>(parameters ?? (_ => { }));
		await cut.InvokeAsync(palette.Open);
		return cut;
	}
}
