using Microsoft.AspNetCore.Components.Web;

namespace Moka.Red.ContextMenu.Tests.Components;

// Show(MouseEventArgs, ...) noticed a keyboard click but still opened at the event's position, which
// a keyboard click reports as 0,0. Every menu a button opened with Enter or Space landed in the
// corner of the viewport. The menu asks where focus is instead, while it positions itself.
public class MokaContextMenuKeyboardAnchorTests
{
	private static readonly MokaContextMenuItem[] Items = [new() { Text = "Rename" }];

	private static MokaContextMenuService NewService() => new();

	// Enter or Space on a button: no click count, no button, no position.
	[Fact]
	public void AKeyboardClick_AnchorsToTheFocusedElement()
	{
		MokaContextMenuService service = NewService();

		service.Show(new MouseEventArgs { Detail = 0, Button = 0, ClientX = 0, ClientY = 0 }, Items);

		Assert.True(service.OpenedFromKeyboard);
		Assert.True(service.AnchorToFocus);
	}

	// The context-menu key and Shift+F10 report no button either, but the browser does position them
	// on the focused element, so that position is the one to use.
	[Fact]
	public void TheContextMenuKey_KeepsTheBrowsersPosition()
	{
		MokaContextMenuService service = NewService();

		service.Show(new MouseEventArgs { Detail = 0, Button = 0, ClientX = 120, ClientY = 64 }, Items);

		Assert.True(service.OpenedFromKeyboard);
		Assert.False(service.AnchorToFocus);
		Assert.Equal(120, service.X);
		Assert.Equal(64, service.Y);
	}

	[Fact]
	public void ARightClick_IsLeftAlone()
	{
		MokaContextMenuService service = NewService();

		service.Show(new MouseEventArgs { Detail = 0, Button = 2, ClientX = 300, ClientY = 200 }, Items);

		Assert.False(service.OpenedFromKeyboard);
		Assert.False(service.AnchorToFocus);
		Assert.Equal(300, service.X);
	}

	// A right click that happens to land on the origin is still a right click.
	[Fact]
	public void ARightClickAtTheOrigin_IsLeftAlone()
	{
		MokaContextMenuService service = NewService();

		service.Show(new MouseEventArgs { Detail = 0, Button = 2, ClientX = 0, ClientY = 0 }, Items);

		Assert.False(service.AnchorToFocus);
	}

	[Fact]
	public void ClosingClearsTheAnchor()
	{
		MokaContextMenuService service = NewService();
		service.Show(new MouseEventArgs { Detail = 0, Button = 0 }, Items);

		service.Close();

		Assert.False(service.AnchorToFocus);
	}

	// The coordinate overload is for callers that already know where the menu goes.
	[Fact]
	public void TheCoordinateOverload_NeverAnchorsToFocus()
	{
		MokaContextMenuService service = NewService();

		service.Show(10, 20, Items, openedFromKeyboard: true, anchorTop: 4);

		Assert.False(service.AnchorToFocus);
		Assert.Equal(4, service.AnchorTop);
	}

	// One change notification per open, whichever path it takes, or the host renders twice.
	[Fact]
	public void AKeyboardOpen_NotifiesOnce()
	{
		MokaContextMenuService service = NewService();
		int changes = 0;
		service.OnChanged += () => changes++;

		service.Show(new MouseEventArgs { Detail = 0, Button = 0 }, Items);

		Assert.Equal(1, changes);
	}
}
