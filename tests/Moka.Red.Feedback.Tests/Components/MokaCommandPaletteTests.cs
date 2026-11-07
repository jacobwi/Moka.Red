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

	[Fact]
	public async Task RowsHaveNoHoverListener_AndCarryTheirIndex()
	{
		SetupModule();
		IMokaCommandPaletteService palette = Services.GetRequiredService<IMokaCommandPaletteService>();
		palette.RegisterMany([new MokaCommand { Id = "a", Title = "A" }, new MokaCommand { Id = "b", Title = "B" }]);

		IRenderedComponent<MokaCommandPalette> cut = Render<MokaCommandPalette>();
		await cut.InvokeAsync(palette.Open);

		var rows = cut.FindAll(".moka-command-palette__item");
		Assert.All(rows, row => Assert.False(row.HasAttribute("blazor:onmouseenter")));
		Assert.Equal(["0", "1"], rows.Select(row => row.GetAttribute("data-moka-index")));
	}

	[Fact]
	public async Task Enter_RunsTheHoveredCommand()
	{
		SetupModule();
		var ran = new List<string>();
		IMokaCommandPaletteService palette = Services.GetRequiredService<IMokaCommandPaletteService>();
		palette.RegisterMany(
		[
			new MokaCommand { Id = "a", Title = "A", OnExecuteSync = () => ran.Add("a") },
			new MokaCommand { Id = "b", Title = "B", OnExecuteSync = () => ran.Add("b") }
		]);

		IRenderedComponent<MokaCommandPalette> cut = Render<MokaCommandPalette>();
		await cut.InvokeAsync(palette.Open);

		await cut.InvokeAsync(() => cut.Instance.OnItemHovered(1));
		await cut.Find(".moka-command-palette").KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

		Assert.Equal(["b"], ran);
	}

	[Fact]
	public async Task AnArrowKey_CarriesOnFromTheHoveredCommand()
	{
		SetupModule();
		IMokaCommandPaletteService palette = Services.GetRequiredService<IMokaCommandPaletteService>();
		palette.RegisterMany(
		[
			new MokaCommand { Id = "a", Title = "A" },
			new MokaCommand { Id = "b", Title = "B" },
			new MokaCommand { Id = "c", Title = "C" }
		]);

		IRenderedComponent<MokaCommandPalette> cut = Render<MokaCommandPalette>();
		await cut.InvokeAsync(palette.Open);

		await cut.InvokeAsync(() => cut.Instance.OnItemHovered(1));
		await cut.Find(".moka-command-palette").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown" });

		Assert.Equal("C", cut.Find(".moka-command-palette__item--focused .moka-command-palette__item-title").TextContent);
	}

	// Enter ran the command in a task nothing awaited, so a command that failed from the keyboard vanished,
	// where the same command failing from a click reached the error boundary.
	[Fact]
	public async Task Enter_ACommandThatFails_ReachesTheRenderer()
	{
		SetupModule();
		IMokaCommandPaletteService palette = Services.GetRequiredService<IMokaCommandPaletteService>();
		palette.RegisterMany(
		[
			new MokaCommand
			{
				Id = "save",
				Title = "Save",
				OnExecute = async () =>
				{
					await Task.Yield();
					throw new InvalidOperationException("Command failed");
				}
			}
		]);

		IRenderedComponent<MokaCommandPalette> cut = Render<MokaCommandPalette>();
		await cut.InvokeAsync(palette.Open);
		await cut.Find(".moka-command-palette").KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

		Exception error = await Renderer.UnhandledException.WaitAsync(TimeSpan.FromSeconds(5),
			Xunit.TestContext.Current.CancellationToken);
		Assert.Equal("Command failed", error.Message);
	}

	private BunitJSModuleInterop SetupModule()
	{
		JSInterop.SetupModule("./_content/Moka.Red.Core/moka-hover.js").Mode = JSRuntimeMode.Loose;
		BunitJSModuleInterop module = JSInterop.SetupModule(Module);
		module.Setup<int>("registerShortcut", _ => true).SetResult(1);
		module.SetupVoid("updateShortcut", _ => true).SetVoidResult();
		module.SetupVoid("focusInput", _ => true).SetVoidResult();
		return module;
	}
}
