using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Feedback.Alert;
using Moka.Red.Feedback.Cheatsheet;
using Moka.Red.Feedback.Dialog;
using Moka.Red.Feedback.Drawer;
using Moka.Red.Feedback.Extensions;
using Moka.Red.Feedback.Notification;
using Moka.Red.Feedback.NotificationBell;
using Moka.Red.Feedback.Onboarding;
using Moka.Red.Feedback.Popover;
using Moka.Red.Feedback.SlashMenu;
using Moka.Red.Feedback.Toast;
using Moka.Red.Feedback.Tooltip;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Feedback.Tests.Components;

// The close buttons and bells show only an icon and had no hover name at all, so they sat beside an app's own header
// buttons with Moka tooltips and said nothing. Each is named by aria-label and by a MokaTooltip; a close button ends
// its header's row, so its name opens below and lines up with the edge.
public class IconTooltipTests : BunitContext
{
	public IconTooltipTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddMokaFeedback();
	}

	[Fact]
	public void Alert_NamesItsCloseButton()
	{
		IRenderedComponent<MokaAlert> cut = Render<MokaAlert>(p => p
			.Add(x => x.Closable, true)
			.AddChildContent("Saved"));

		IElement close = cut.Find(".moka-alert-close");
		IconTooltip.AssertNamed(close, "Close", MokaTooltipPosition.BottomEnd);
		Assert.Equal("button", close.GetAttribute("type"));
	}

	[Fact]
	public void Dialog_NamesItsCloseButton()
	{
		IRenderedComponent<MokaDialog> cut = Render<MokaDialog>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Title, "Settings"));

		IElement close = cut.Find(".moka-dialog-close");
		IconTooltip.AssertNamed(close, "Close", MokaTooltipPosition.BottomEnd);
		Assert.Equal("button", close.GetAttribute("type"));
	}

	[Fact]
	public void Drawer_NamesItsCloseButton()
	{
		IRenderedComponent<MokaDrawer> cut = Render<MokaDrawer>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Title, "Filters"));

		IconTooltip.AssertNamed(cut.Find(".moka-drawer-close"), "Close drawer", MokaTooltipPosition.BottomEnd);
	}

	[Fact]
	public void Cheatsheet_NamesItsCloseButton()
	{
		IRenderedComponent<MokaCheatsheet> cut = Render<MokaCheatsheet>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Title, "Shortcuts"));

		IconTooltip.AssertNamed(cut.Find(".moka-cheatsheet-close"), "Close cheatsheet", MokaTooltipPosition.BottomEnd);
	}

	[Fact]
	public void Onboarding_NamesItsSkipButton()
	{
		IRenderedComponent<MokaOnboarding> cut = Render<MokaOnboarding>(p => p
			.Add(x => x.Steps, [new MokaOnboardingStep { Title = "Welcome", Description = "Start here.", TargetSelector = "#one" }])
			.Add(x => x.Active, true));

		IconTooltip.AssertNamed(cut.Find(".moka-onboarding-skip"), "Skip tour", MokaTooltipPosition.BottomEnd);
	}

	// Toasts along the bottom of the window open the close button's name above it.
	[Theory]
	[InlineData(MokaToastPosition.TopRight, MokaTooltipPosition.BottomEnd)]
	[InlineData(MokaToastPosition.TopCenter, MokaTooltipPosition.BottomEnd)]
	[InlineData(MokaToastPosition.BottomRight, MokaTooltipPosition.TopEnd)]
	[InlineData(MokaToastPosition.BottomCenter, MokaTooltipPosition.TopEnd)]
	public void Toast_NamesItsCloseButtonAwayFromTheWindowsEdge(MokaToastPosition place, MokaTooltipPosition position)
	{
		IRenderedComponent<MokaToastHost> cut = Render<MokaToastHost>(p => p.Add(x => x.Position, place));

		Services.GetRequiredService<IMokaToastService>().ShowSuccess("Saved");

		IElement close = cut.WaitForElement(".moka-toast-close");
		IconTooltip.AssertNamed(close, "Close", position);
		Assert.Equal("button", close.GetAttribute("type"));
	}

	// The bell opens its panel where the tooltip would, so it shows none while the panel is open; its name stays.
	[Fact]
	public async Task NotificationCenter_NamesItsBellAndDismissButtons()
	{
		Services.GetRequiredService<IMokaNotificationService>().Push("Build succeeded", "Pipeline #42 completed.");
		IRenderedComponent<MokaNotificationCenter> cut = Render<MokaNotificationCenter>();

		IconTooltip.AssertNamed(cut.Find(".moka-notification-bell"), "Notifications, 1 unread", MokaTooltipPosition.BottomEnd);

		await cut.Find(".moka-notification-bell").ClickAsync(new MouseEventArgs());

		Assert.Null(cut.Find(".moka-notification-bell").ParentElement!.QuerySelector(":scope > .moka-tooltip-popup"));
		// The dismiss buttons make a column down the panel, so each name opens to the left, over its own notification.
		IconTooltip.AssertNamed(cut.Find(".moka-notification-item__dismiss"), "Dismiss Build succeeded", MokaTooltipPosition.Left);
	}

	// The bell's name opens on the side its panel does, the side with room.
	[Theory]
	[InlineData(MokaPopoverPosition.BottomEnd, MokaTooltipPosition.BottomEnd)]
	[InlineData(MokaPopoverPosition.BottomStart, MokaTooltipPosition.BottomStart)]
	[InlineData(MokaPopoverPosition.TopEnd, MokaTooltipPosition.TopEnd)]
	[InlineData(MokaPopoverPosition.Left, MokaTooltipPosition.Left)]
	public void NotificationBell_NamesItsTriggerOnThePanelsSide(MokaPopoverPosition panel, MokaTooltipPosition position)
	{
		IRenderedComponent<MokaNotificationBell> cut = Render<MokaNotificationBell>(p => p
			.Add(x => x.UnreadCount, 0)
			.Add(x => x.Position, panel));

		IconTooltip.AssertNamed(cut.Find(".moka-notification-bell__trigger"), "Notifications", position);
	}

	// Two of the hint caps are arrows, so each says what its key does in a tooltip below the header's row.
	[Fact]
	public void SlashMenu_NamesItsKeyHints()
	{
		IRenderedComponent<MokaSlashMenu> cut = Render<MokaSlashMenu>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Header, "Insert")
			.Add(x => x.Items, [new MokaSlashMenuItem { Title = "Heading" }]));

		IReadOnlyList<IElement> caps = cut.FindAll(".moka-slash-menu-hints kbd");
		Assert.Equal(3, caps.Count);
		(string Name, string Place)[] expected = [("Navigate", "bottom"), ("Select", "bottom"), ("Close", "bottom-end")];
		for (int i = 0; i < caps.Count; i++)
		{
			Assert.False(caps[i].HasAttribute("title"));
			IElement popup = caps[i].ParentElement!.QuerySelector(":scope > .moka-tooltip-popup")!;
			Assert.Equal(expected[i].Name, popup.TextContent.Trim());
			Assert.Contains($"moka-tooltip-popup--{expected[i].Place}", popup.ClassList);
		}
	}
}
