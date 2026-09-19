using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.ContextMenu.Extensions;

namespace Moka.Red.ContextMenu.Tests.Services;

public class MokaContextMenuServiceTests
{
	private static readonly MokaContextMenuItem[] Items = [new() { Text = "Copy" }, new() { Text = "Paste" }];

	[Fact]
	public void Show_OpensTheMenuAtThePosition_AndRaisesOnChanged()
	{
		var service = new MokaContextMenuService();
		int changes = 0;
		service.OnChanged += () => changes++;

		service.Show(120, 80, Items);

		Assert.True(service.Visible);
		Assert.Equal(120, service.X);
		Assert.Equal(80, service.Y);
		Assert.Same(Items, service.Items);
		Assert.Equal(1, changes);
	}

	[Fact]
	public void ShowWithAMouseEvent_OpensAtItsViewportPosition()
	{
		var service = new MokaContextMenuService();

		service.Show(new MouseEventArgs { ClientX = 42, ClientY = 17, ScreenX = 900, ScreenY = 700, OffsetX = 3, OffsetY = 4 },
			Items);

		Assert.True(service.Visible);
		Assert.Equal(42, service.X);
		Assert.Equal(17, service.Y);
		Assert.Same(Items, service.Items);
	}

	// A right click in Chromium reports Detail 0 as well, so the button is what tells it apart.
	[Theory]
	[InlineData("contextmenu", 0, 0, true)] // the context-menu key or Shift+F10
	[InlineData("click", 0, 0, true)] // Enter or Space on a button
	[InlineData("contextmenu", 0, 2, false)] // a right click, as Chromium reports it
	[InlineData("contextmenu", 1, 2, false)]
	[InlineData("click", 1, 0, false)] // a left click
	public void ShowWithAMouseEvent_TellsAKeyboardOpenFromAPointerOpen(string type, long detail, long button, bool keyboard)
	{
		var service = new MokaContextMenuService();

		service.Show(new MouseEventArgs { Type = type, Detail = detail, Button = button }, Items);

		Assert.Equal(keyboard, service.OpenedFromKeyboard);
		Assert.Null(service.AnchorTop);
	}

	[Fact]
	public void Show_CarriesHowTheMenuWasOpened_AndCloseForgetsIt()
	{
		var service = new MokaContextMenuService();

		service.Show(40, 534, Items, openedFromKeyboard: true, anchorTop: 496);

		Assert.True(service.Visible);
		Assert.True(service.OpenedFromKeyboard);
		Assert.Equal(496, service.AnchorTop);

		service.Close();

		Assert.False(service.OpenedFromKeyboard);
		Assert.Null(service.AnchorTop);

		service.Show(10, 10, Items);

		Assert.False(service.OpenedFromKeyboard);
		Assert.Null(service.AnchorTop);
	}

	// An implementation written before the overload still compiles, and the overload falls back to Show.
	[Fact]
	public void TheKeyboardOverload_FallsBackToShow_InOlderImplementations()
	{
		var older = new OlderService();
		IMokaContextMenuService service = older;

		service.Show(40, 534, Items, openedFromKeyboard: true, anchorTop: 496);

		Assert.True(service.Visible);
		Assert.Equal((40d, 534d), (service.X, service.Y));
		Assert.Same(Items, service.Items);
		Assert.False(service.OpenedFromKeyboard);
		Assert.Null(service.AnchorTop);
	}

	[Fact]
	public void Show_ReplacesAMenuThatIsAlreadyOpen()
	{
		var service = new MokaContextMenuService();
		MokaContextMenuItem[] other = [new() { Text = "Rename" }];
		int changes = 0;
		service.OnChanged += () => changes++;

		service.Show(10, 10, Items);
		service.Show(50, 60, other);

		Assert.True(service.Visible);
		Assert.Equal(50, service.X);
		Assert.Equal(60, service.Y);
		Assert.Same(other, service.Items);
		Assert.Equal(2, changes);
	}

	[Fact]
	public void Close_HidesTheMenu_DropsItsItems_AndRaisesOnChanged()
	{
		var service = new MokaContextMenuService();
		service.Show(10, 10, Items);
		int changes = 0;
		service.OnChanged += () => changes++;

		service.Close();

		Assert.False(service.Visible);
		Assert.Empty(service.Items);
		Assert.Equal(1, changes);
	}

	[Fact]
	public void Close_WithNothingOpen_RaisesNothing()
	{
		var service = new MokaContextMenuService();
		int changes = 0;
		service.OnChanged += () => changes++;

		service.Close();

		Assert.False(service.Visible);
		Assert.Equal(0, changes);
	}

	[Fact]
	public void Show_RejectsNullArguments()
	{
		var service = new MokaContextMenuService();

		Assert.Throws<ArgumentNullException>(() => service.Show(0, 0, null!));
		Assert.Throws<ArgumentNullException>(() => service.Show(null!, Items));
		Assert.False(service.Visible);
	}

	[Fact]
	public void HasHost_CountsHosts_AndTheLastOneToGoClosesTheMenu()
	{
		var service = new MokaContextMenuService();
		Assert.False(service.HasHost);

		service.RegisterHost();
		service.RegisterHost();
		service.Show(10, 10, Items);

		service.UnregisterHost();
		Assert.True(service.HasHost);
		Assert.True(service.Visible);

		service.UnregisterHost();
		Assert.False(service.HasHost);
		Assert.False(service.Visible);

		// Unregistering more often than registering does not leave the count below zero.
		service.UnregisterHost();
		service.RegisterHost();
		Assert.True(service.HasHost);
	}

	[Fact]
	public void AddMokaContextMenu_RegistersOneServicePerScope()
	{
		using ServiceProvider provider = new ServiceCollection().AddMokaContextMenu().BuildServiceProvider();
		using IServiceScope first = provider.CreateScope();
		using IServiceScope second = provider.CreateScope();

		IMokaContextMenuService service = first.ServiceProvider.GetRequiredService<IMokaContextMenuService>();

		Assert.IsType<MokaContextMenuService>(service);
		Assert.Same(service, first.ServiceProvider.GetRequiredService<IMokaContextMenuService>());
		Assert.NotSame(service, second.ServiceProvider.GetRequiredService<IMokaContextMenuService>());
	}

	/// <summary>Implements only what the interface asked for before the keyboard overload.</summary>
	private sealed class OlderService : IMokaContextMenuService
	{
		public event Action? OnChanged;

		public bool Visible { get; private set; }

		public double X { get; private set; }

		public double Y { get; private set; }

		public IReadOnlyList<MokaContextMenuItem> Items { get; private set; } = [];

		public void Show(double x, double y, IReadOnlyList<MokaContextMenuItem> items)
		{
			(X, Y, Items, Visible) = (x, y, items, true);
			OnChanged?.Invoke();
		}

		public void Show(MouseEventArgs mouseEvent, IReadOnlyList<MokaContextMenuItem> items) =>
			Show(mouseEvent.ClientX, mouseEvent.ClientY, items);

		public void Close() => Visible = false;
	}
}
