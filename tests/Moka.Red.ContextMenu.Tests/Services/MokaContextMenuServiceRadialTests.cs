using Microsoft.AspNetCore.Components.Web;
using static Moka.Red.ContextMenu.Tests.RadialDom;

namespace Moka.Red.ContextMenu.Tests.Services;

public class MokaContextMenuServiceRadialTests
{
	private static readonly MokaContextMenuItem[] Items = [new() { Text = "Copy" }];

	[Fact]
	public void ShowRadial_HoldsTheRing_AndRaisesOnChangedOnce()
	{
		MokaContextMenuService service = new();
		int changes = 0;
		service.OnChanged += () => changes++;
		MokaRadialMenuSpec spec = Device();

		service.ShowRadial(120, 80, spec);

		Assert.Equal(new MokaRadialMenuRequest(spec, 120, 80), service.Radial);
		Assert.False(service.Visible);
		Assert.Equal(1, changes);
	}

	[Fact]
	public void ShowRadial_FromTheKeyboard_StartsOnTheFirstButton_WhereItWasAsked()
	{
		MokaContextMenuService service = new();

		service.ShowRadial(120, 80, Device(), openedFromKeyboard: true);

		MokaRadialMenuRequest request = service.Radial!;
		Assert.True(request.OpenedFromKeyboard);
		Assert.False(request.AnchorToFocus);
	}

	[Fact]
	public void ShowRadial_ClosesAnOpenListMenu()
	{
		MokaContextMenuService service = new();
		service.Show(10, 10, Items);

		service.ShowRadial(120, 80, Device());

		Assert.False(service.Visible);
		Assert.Empty(service.Items);
		Assert.NotNull(service.Radial);
	}

	[Fact]
	public void Show_ClosesAnOpenRing()
	{
		MokaContextMenuService service = new();
		service.ShowRadial(120, 80, Device());

		service.Show(10, 10, Items);

		Assert.Null(service.Radial);
		Assert.True(service.Visible);
	}

	[Fact]
	public void Close_ClosesTheRing_AndRaisesOnChanged()
	{
		MokaContextMenuService service = new();
		service.ShowRadial(120, 80, Device());
		int changes = 0;
		service.OnChanged += () => changes++;

		service.Close();
		service.Close();

		Assert.Null(service.Radial);
		Assert.Equal(1, changes);
	}

	[Fact]
	public void TheLastHostLeaving_ClosesTheRing()
	{
		MokaContextMenuService service = new();
		service.RegisterHost();
		service.ShowRadial(120, 80, Device());

		service.UnregisterHost();

		Assert.Null(service.Radial);
	}

	[Fact]
	public void AReleasedRightClick_OpensAtThePointer_WithNoFlick()
	{
		MokaContextMenuService service = new();

		service.ShowRadial(new MouseEventArgs { ClientX = 42, ClientY = 17, Button = 2, Detail = 1 }, Device());

		Assert.Equal(new MokaRadialMenuRequest(service.Radial!.Spec, 42, 17), service.Radial);
	}

	[Fact]
	public void APressStillHeld_OpensForAFlick()
	{
		MokaContextMenuService service = new();

		service.ShowRadial(new MouseEventArgs { ClientX = 42, ClientY = 17, Button = 2, Buttons = 2 }, Device());

		Assert.True(service.Radial!.Flick);
		Assert.False(service.Radial.OpenedFromKeyboard);
	}

	[Theory]
	[InlineData(0, 0)]
	[InlineData(300, 200)]
	public void TheKeyboard_CentresTheRingOnTheFocusedElement(double x, double y)
	{
		MokaContextMenuService service = new();

		service.ShowRadial(new MouseEventArgs { ClientX = x, ClientY = y }, Device());

		MokaRadialMenuRequest request = service.Radial!;
		Assert.True(request.OpenedFromKeyboard);
		Assert.True(request.AnchorToFocus);
		Assert.False(request.Flick);
	}

	[Fact]
	public void ShowRadial_WithoutASpec_Throws()
	{
		MokaContextMenuService service = new();

		Assert.Throws<ArgumentNullException>(() => service.ShowRadial(1, 2, null!));
		Assert.Throws<ArgumentNullException>(() => service.ShowRadial(new MouseEventArgs(), null!));
	}

	[Fact]
	public void APick_AnswersTheButton_OrTheButtonAndChoice()
	{
		MokaRadialItem limit = Device().Items[1];

		Assert.Equal("limit", new MokaRadialPick(limit).Answer);
		Assert.Equal("limit:50", new MokaRadialPick(limit, limit.Choices![1]).Answer);
		Assert.True(limit.HasChoices);
		Assert.False(Device().Items[0].HasChoices);
	}
}
