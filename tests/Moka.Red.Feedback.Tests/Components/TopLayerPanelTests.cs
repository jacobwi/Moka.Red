using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Feedback.Dropdown;
using Moka.Red.Feedback.Extensions;
using Moka.Red.Feedback.HoverCard;
using Moka.Red.Feedback.Notification;
using Moka.Red.Feedback.NotificationBell;
using Moka.Red.Feedback.Popover;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Feedback.Tests.Components;

// A panel drawn inside its trigger's wrapper was cut off by a card's overflow and painted under any later element
// with a stacking context of its own, such as the boot screen below the bell on the Feedback page. The panels now
// go to the top layer through Core's moka-popup.js; what that does in the browser can't run here, so these tests
// cover what the components render for it and when they call it.
public class TopLayerPanelTests : BunitContext
{
	private readonly BunitJSModuleInterop _popup;

	public TopLayerPanelTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_popup = TopLayerPopup.Setup(JSInterop);
		Services.AddMokaFeedback();
	}

	[Fact]
	public async Task Popover_OpensItsPopupAgainstItsTrigger_WithTheBackdropUnderIt()
	{
		IRenderedComponent<MokaPopover> cut = RenderPopover();
		string? triggerId = cut.Find(".moka-popover-trigger").GetAttribute("blazor:elementReference");

		await cut.Find(".moka-popover-trigger").ClickAsync(new MouseEventArgs());

		IReadOnlyDictionary<string, object?> options =
			TopLayerPopup.AssertOpened(_popup, cut.Find(".moka-popover-popup"), triggerId, "bottom");
		Assert.Null(options["width"]);
		Assert.Equal(true, options["flip"]);

		IElement backdrop = cut.Find(".moka-popover-backdrop");
		Assert.Equal("manual", backdrop.GetAttribute("popover"));
		Assert.Equal(backdrop.GetAttribute("blazor:elementReference"),
			Assert.IsType<ElementReference>(_popup.Invocations["open"][^1].Arguments[3]).Id);
	}

	[Fact]
	public async Task Popover_ClosesItsPopup_WhenTheBackdropIsClicked()
	{
		IRenderedComponent<MokaPopover> cut = RenderPopover();
		await cut.Find(".moka-popover-trigger").ClickAsync(new MouseEventArgs());

		await cut.Find(".moka-popover-backdrop").ClickAsync(new MouseEventArgs());

		Assert.Empty(cut.FindAll(".moka-popover-popup"));
		TopLayerPopup.AssertClosed(_popup);
	}

	[Fact]
	public async Task Popover_ClosesItsPopup_WhenDisposedOpen()
	{
		IRenderedComponent<MokaPopover> cut = RenderPopover();
		await cut.Find(".moka-popover-trigger").ClickAsync(new MouseEventArgs());

		await DisposeComponentsAsync();

		TopLayerPopup.AssertClosed(_popup);
	}

	[Fact]
	public void Popover_ThatNeverOpens_CallsNothing()
	{
		IRenderedComponent<MokaPopover> cut = RenderPopover();
		cut.Render(p => p.Add(x => x.Position, MokaPopoverPosition.Top));

		Assert.Equal(0, TopLayerPopup.Count(_popup, "open"));
		Assert.Equal(0, TopLayerPopup.Count(_popup, "close"));
	}

	[Fact]
	public void Popover_SendsItsPlacementAgain_OnlyWhenItChanges()
	{
		IRenderedComponent<MokaPopover> cut = RenderPopover(p => p.Add(x => x.Open, true));
		cut.Render(p => p.Add(x => x.PopoverContent, "Other details"));
		Assert.Equal(1, TopLayerPopup.Count(_popup, "open"));

		cut.Render(p => p.Add(x => x.Position, MokaPopoverPosition.TopEnd));

		Assert.Equal(2, TopLayerPopup.Count(_popup, "open"));
		Assert.Equal("top-end", Options()["placement"]);
	}

	[Fact]
	public void Popover_PassesItsWidthShiftAndArrow()
	{
		RenderPopover(p => p
			.Add(x => x.Open, true)
			.Add(x => x.MatchWidth, true)
			.Add(x => x.OffsetX, 6)
			.Add(x => x.Arrow, true));

		IReadOnlyDictionary<string, object?> options = Options();
		Assert.Equal("min", options["width"]);
		Assert.Equal(6, options["shift"]);
		Assert.Equal(false, options["flip"]);
	}

	[Fact]
	public void Popover_WithAHoverTrigger_HasNoBackdrop()
	{
		IRenderedComponent<MokaPopover> cut = RenderPopover(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Trigger, MokaPopoverTrigger.Hover));

		Assert.Empty(cut.FindAll(".moka-popover-backdrop"));
		Assert.Null(_popup.Invocations["open"][^1].Arguments[3]);
	}

	[Fact]
	public async Task Dropdown_OpensItsMenuThroughThePopover()
	{
		IRenderedComponent<MokaDropdown> cut = Render<MokaDropdown>(p => p
			.Add(x => x.ChildContent, (RenderFragment)(b => b.AddMarkupContent(0, "<button type=\"button\">Actions</button>")))
			.Add(x => x.Items, (RenderFragment)(b =>
			{
				b.OpenComponent<MokaDropdownItem>(0);
				b.AddAttribute(1, nameof(MokaDropdownItem.Text), "Rename");
				b.CloseComponent();
			})));
		string? triggerId = cut.Find(".moka-popover-trigger").GetAttribute("blazor:elementReference");

		await cut.Find(".moka-popover-trigger").ClickAsync(new MouseEventArgs());

		TopLayerPopup.AssertOpened(_popup, cut.Find(".moka-popover-popup"), triggerId, "bottom-start");
	}

	[Fact]
	public async Task NotificationBell_OpensItsPanelAgainstTheBell_AndClosesItOnEscape()
	{
		IRenderedComponent<MokaNotificationBell> cut = Render<MokaNotificationBell>();
		string? rootId = cut.Find(".moka-notification-bell").GetAttribute("blazor:elementReference");

		await cut.Find(".moka-notification-bell__trigger").ClickAsync(new MouseEventArgs());
		TopLayerPopup.AssertOpened(_popup, cut.Find(".moka-notification-bell__dropdown"), rootId, "bottom-end");
		Assert.Null(_popup.Invocations["open"][^1].Arguments[3]);

		await cut.Find(".moka-notification-bell").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

		Assert.Empty(cut.FindAll(".moka-notification-bell__dropdown"));
		TopLayerPopup.AssertClosed(_popup);
	}

	[Fact]
	public async Task NotificationBell_ClosesItsPanel_WhenDisposedOpen()
	{
		IRenderedComponent<MokaNotificationBell> cut = Render<MokaNotificationBell>(p => p
			.Add(x => x.Position, MokaPopoverPosition.TopStart));
		await cut.Find(".moka-notification-bell__trigger").ClickAsync(new MouseEventArgs());
		Assert.Equal("top-start", Options()["placement"]);

		await DisposeComponentsAsync();

		TopLayerPopup.AssertClosed(_popup);
	}

	[Fact]
	public async Task NotificationCenter_OpensItsPanelAgainstTheBell_AndClosesIt()
	{
		IRenderedComponent<MokaNotificationCenter> cut = Render<MokaNotificationCenter>();
		string? rootId = cut.Find(".moka-notification-center").GetAttribute("blazor:elementReference");

		await cut.Find("button.moka-notification-bell").ClickAsync(new MouseEventArgs());
		TopLayerPopup.AssertOpened(_popup, cut.Find(".moka-notification-panel"), rootId, "bottom-end");

		await cut.Find("button.moka-notification-bell").ClickAsync(new MouseEventArgs());

		TopLayerPopup.AssertClosed(_popup);
	}

	[Fact]
	public async Task NotificationCenter_ClosesItsPanel_WhenDisposedOpen()
	{
		IRenderedComponent<MokaNotificationCenter> cut = Render<MokaNotificationCenter>();
		await cut.Find("button.moka-notification-bell").ClickAsync(new MouseEventArgs());

		await DisposeComponentsAsync();

		TopLayerPopup.AssertClosed(_popup);
	}

	[Fact]
	public async Task HoverCard_OpensItsCardAgainstTheTrigger_AndClosesItWhenTheMouseLeaves()
	{
		IRenderedComponent<MokaHoverCard> cut = Render<MokaHoverCard>(p => p
			.Add(x => x.Delay, 0)
			.Add(x => x.Position, MokaPopoverPosition.Right)
			.Add(x => x.ChildContent, "Ada")
			.Add(x => x.CardContent, "Ada Lovelace"));
		string? triggerId = cut.Find(".moka-hover-card__trigger").GetAttribute("blazor:elementReference");

		await cut.Find(".moka-hover-card__trigger").TriggerEventAsync("onmouseenter", new MouseEventArgs());
		TopLayerPopup.AssertOpened(_popup, cut.Find(".moka-hover-card__card"), triggerId, "right");

		await cut.Find(".moka-hover-card__trigger").TriggerEventAsync("onmouseleave", new MouseEventArgs());
		cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".moka-hover-card__card")), TimeSpan.FromSeconds(10));

		TopLayerPopup.AssertClosed(_popup);
	}

	private IRenderedComponent<MokaPopover> RenderPopover(
		Action<ComponentParameterCollectionBuilder<MokaPopover>>? configure = null) =>
		Render<MokaPopover>(p =>
		{
			p.Add(x => x.PopoverContent, "Details");
			p.AddChildContent("<button type=\"button\">Info</button>");
			configure?.Invoke(p);
		});

	private IReadOnlyDictionary<string, object?> Options() =>
		Assert.IsAssignableFrom<IReadOnlyDictionary<string, object?>>(_popup.Invocations["open"][^1].Arguments[4]);
}
