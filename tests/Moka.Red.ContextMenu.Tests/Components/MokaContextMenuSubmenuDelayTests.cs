using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Tests.Shared;
using static Moka.Red.ContextMenu.Tests.MenuDom;

namespace Moka.Red.ContextMenu.Tests.Components;

// A sub-menu opens once the pointer has rested on its item, and closes a moment after the pointer leaves the menu. The
// pointer routinely moves on before either wait runs out, and a wait cut short changes nothing.
public class MokaContextMenuSubmenuDelayTests : BunitContext
{
	private static readonly TimeSpan OpenDelay = TimeSpan.FromMilliseconds(140);
	private static readonly TimeSpan CloseDelay = TimeSpan.FromMilliseconds(250);
	private static readonly TimeSpan OneMillisecond = TimeSpan.FromMilliseconds(1);

	private readonly FakeClock _clock = new();

	public MokaContextMenuSubmenuDelayTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddSingleton<TimeProvider>(_clock);
	}

	[Fact]
	public async Task RestingOnAnItem_OpensItsSubmenuOnceTheDelayRunsOut()
	{
		IRenderedComponent<MokaContextMenu> cut = RenderMenu();

		await EnterRowAsync(cut, "Share");
		await AdvanceAsync(cut, OpenDelay - OneMillisecond);
		Assert.Single(Menus(cut));

		await AdvanceAsync(cut, OneMillisecond);
		Assert.Equal(2, Menus(cut).Count);
	}

	[Fact]
	public async Task MovingOnBeforeTheDelayRunsOut_OpensNothing()
	{
		IRenderedComponent<MokaContextMenu> cut = RenderMenu();

		await EnterRowAsync(cut, "Share");
		await AdvanceAsync(cut, OpenDelay - OneMillisecond);
		await EnterRowAsync(cut, "Open");
		await AdvanceAsync(cut, TimeSpan.FromSeconds(5));

		Assert.Single(Menus(cut));
	}

	[Fact]
	public async Task AnOpenWaitThatAlreadyFired_DoesNothingOnceTheItemIsLeft()
	{
		IRenderedComponent<MokaContextMenu> cut = RenderMenu();

		await EnterRowAsync(cut, "Share");
		FakeTimer waiting = _clock.Timers.Single();
		await EnterRowAsync(cut, "Open");
		await cut.InvokeAsync(waiting.InvokeCallback);

		Assert.Single(Menus(cut));
	}

	[Fact]
	public async Task LeavingTheMenu_ClosesTheSubmenuOnceTheGracePeriodRunsOut()
	{
		IRenderedComponent<MokaContextMenu> cut = await OpenShareAsync();

		await Root(cut).MouseLeaveAsync(new MouseEventArgs());
		await AdvanceAsync(cut, CloseDelay - OneMillisecond);
		Assert.Equal(2, Menus(cut).Count);

		await AdvanceAsync(cut, OneMillisecond);
		Assert.Single(Menus(cut));
	}

	[Fact]
	public async Task ComingBackBeforeTheGracePeriodRunsOut_KeepsTheSubmenuOpen()
	{
		IRenderedComponent<MokaContextMenu> cut = await OpenShareAsync();

		await Root(cut).MouseLeaveAsync(new MouseEventArgs());
		await AdvanceAsync(cut, CloseDelay - OneMillisecond);
		await Root(cut).MouseEnterAsync(new MouseEventArgs());
		await AdvanceAsync(cut, TimeSpan.FromSeconds(5));

		Assert.Equal(2, Menus(cut).Count);
	}

	[Fact]
	public async Task Disposing_StopsTheWait()
	{
		IRenderedComponent<MokaContextMenu> cut = RenderMenu();

		await EnterRowAsync(cut, "Share");
		await cut.Instance.DisposeAsync();

		Assert.True(_clock.Timers.Single().Disposed);
	}

	private IRenderedComponent<MokaContextMenu> RenderMenu() =>
		Render<MokaContextMenu>(p => p
			.Add(x => x.Items, (IReadOnlyList<MokaContextMenuItem>)
			[
				new MokaContextMenuItem { Text = "Open" },
				new MokaContextMenuItem
				{
					Text = "Share",
					Children = [new MokaContextMenuItem { Text = "Copy link" }, new MokaContextMenuItem { Text = "Email" }]
				}
			])
			.Add(x => x.Visible, true)
			.Add(x => x.X, 100)
			.Add(x => x.Y, 100));

	private async Task<IRenderedComponent<MokaContextMenu>> OpenShareAsync()
	{
		IRenderedComponent<MokaContextMenu> cut = RenderMenu();
		await EnterRowAsync(cut, "Share");
		await AdvanceAsync(cut, OpenDelay);
		Assert.Equal(2, Menus(cut).Count);
		return cut;
	}

	private static Task EnterRowAsync(IRenderedComponent<MokaContextMenu> cut, string text) =>
		Row(Root(cut), text).MouseEnterAsync(new MouseEventArgs());

	private Task AdvanceAsync(IRenderedComponent<MokaContextMenu> cut, TimeSpan by) =>
		cut.InvokeAsync(() => _clock.Advance(by));

	private static IElement Root(IRenderedComponent<MokaContextMenu> cut) => Menus(cut)[0];
}
