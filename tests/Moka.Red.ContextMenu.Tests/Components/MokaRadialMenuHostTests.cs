using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.ContextMenu.Extensions;
using static Moka.Red.ContextMenu.Tests.RadialDom;

namespace Moka.Red.ContextMenu.Tests.Components;

public class MokaRadialMenuHostTests : BunitContext
{
	private readonly List<string> _picked = [];

	public MokaRadialMenuHostTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		JSInterop.SetupModule(Module);
		JSInterop.SetupModule("./_content/Moka.Red.ContextMenu/MokaContextMenu.razor.js");
		Services.AddMokaContextMenu();
	}

	private IMokaContextMenuService Menu => Services.GetRequiredService<IMokaContextMenuService>();

	[Fact]
	public void DrawsNoRing_UntilOneIsShown()
	{
		IRenderedComponent<MokaContextMenuHost> cut = Render<MokaContextMenuHost>();

		Assert.Empty(cut.FindAll(".moka-radial-menu"));
	}

	[Fact]
	public async Task ShowRadial_DrawsTheRingAtThePoint()
	{
		IRenderedComponent<MokaContextMenuHost> cut = Render<MokaContextMenuHost>();

		await cut.InvokeAsync(() => Menu.ShowRadial(150, 220, Device()));

		Assert.Equal("--moka-radial-x: 150px; --moka-radial-y: 220px", Ring(cut).GetAttribute("style"));
		Assert.Empty(cut.FindAll(".moka-ctx-menu"));
	}

	[Fact]
	public async Task APick_RunsTheSpec_AndClosesTheSharedRing()
	{
		IRenderedComponent<MokaContextMenuHost> cut = Render<MokaContextMenuHost>();
		await cut.InvokeAsync(() => Menu.ShowRadial(150, 220, Device(Record)));

		await Button(cut, "Allow").ClickAsync(new MouseEventArgs());

		Assert.Equal(["allow"], _picked);
		Assert.Null(Menu.Radial);
		Assert.Empty(cut.FindAll(".moka-radial-menu"));
	}

	[Fact]
	public async Task Escape_ClosesTheSharedRing()
	{
		IRenderedComponent<MokaContextMenuHost> cut = Render<MokaContextMenuHost>();
		await cut.InvokeAsync(() => Menu.ShowRadial(150, 220, Device()));

		await Ring(cut).KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

		Assert.Null(Menu.Radial);
		Assert.Empty(cut.FindAll(".moka-radial-menu"));
	}

	[Fact]
	public async Task ShowingAListMenu_ClosesTheRing()
	{
		IRenderedComponent<MokaContextMenuHost> cut = Render<MokaContextMenuHost>();
		await cut.InvokeAsync(() => Menu.ShowRadial(150, 220, Device()));

		await cut.InvokeAsync(() => Menu.Show(10, 10, [new MokaContextMenuItem { Text = "Copy" }]));

		Assert.Empty(cut.FindAll(".moka-radial-menu"));
		Assert.Single(cut.FindAll(".moka-ctx-menu"));
	}

	[Fact]
	public async Task TheSameRing_OpensAgainAfterClosing()
	{
		IRenderedComponent<MokaContextMenuHost> cut = Render<MokaContextMenuHost>();
		MokaRadialMenuSpec spec = Device();
		await cut.InvokeAsync(() => Menu.ShowRadial(150, 220, spec));
		await Ring(cut).KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

		await cut.InvokeAsync(() => Menu.ShowRadial(150, 220, spec));

		Assert.Single(cut.FindAll(".moka-radial-menu"));
	}

	private Task Record(MokaRadialPick pick)
	{
		_picked.Add(pick.Answer);
		return Task.CompletedTask;
	}
}
