using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Layout.Card;

namespace Moka.Red.Layout.Tests.Components;

public class MokaCardTests : BunitContext
{
	[Fact]
	public void Renders_RootClass()
	{
		IRenderedComponent<MokaCard> cut = Render<MokaCard>();

		IElement el = cut.Find(".moka-card");
		Assert.NotNull(el);
	}

	[Fact]
	public void DefaultElevation_Is1()
	{
		IRenderedComponent<MokaCard> cut = Render<MokaCard>();

		IElement el = cut.Find(".moka-card");
		Assert.Contains("moka-card--elevation-1", el.ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void Elevation_AppliesClass()
	{
		IRenderedComponent<MokaCard> cut = Render<MokaCard>(p => p
			.Add(x => x.Elevation, 3));

		IElement el = cut.Find(".moka-card");
		Assert.Contains("moka-card--elevation-3", el.ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void Outlined_AppliesClass()
	{
		IRenderedComponent<MokaCard> cut = Render<MokaCard>(p => p
			.Add(x => x.Outlined, true));

		IElement el = cut.Find(".moka-card");
		Assert.Contains("moka-card--outlined", el.ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void Title_RendersInHeader()
	{
		IRenderedComponent<MokaCard> cut = Render<MokaCard>(p => p
			.Add(x => x.Title, "My Card"));

		IElement title = cut.Find(".moka-card-title");
		Assert.Equal("My Card", title.TextContent);
	}

	[Fact]
	public void Subtitle_RendersInHeader()
	{
		IRenderedComponent<MokaCard> cut = Render<MokaCard>(p => p
			.Add(x => x.Title, "Card")
			.Add(x => x.Subtitle, "Subtitle text"));

		IElement subtitle = cut.Find(".moka-card-subtitle");
		Assert.Equal("Subtitle text", subtitle.TextContent);
	}

	[Fact]
	public void Clickable_AddsClass()
	{
		IRenderedComponent<MokaCard> cut = Render<MokaCard>(p => p
			.Add(x => x.Clickable, true));

		IElement el = cut.Find(".moka-card");
		Assert.Contains("moka-card--clickable", el.ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void FullWidth_AddsClass()
	{
		IRenderedComponent<MokaCard> cut = Render<MokaCard>(p => p
			.Add(x => x.FullWidth, true));

		IElement el = cut.Find(".moka-card");
		Assert.Contains("moka-card--full-width", el.ClassName, StringComparison.Ordinal);
	}

	// The card used to be a role="button" div bound to moka-keys.js, so controls inside it were nested in a
	// button. Now the card is a plain element and its one action is a real button or link, which the browser
	// focuses and activates with no script.
	[Fact]
	public async Task ACardWithAnAction_IsAPlainElement_AndItsActionIsTheOneTabStop()
	{
		int clicks = 0;

		IRenderedComponent<MokaCard> clickable = Render<MokaCard>(p => p
			.Add(x => x.Title, "Plan")
			.Add(x => x.Clickable, true)
			.Add(x => x.OnClick, _ => clicks++));
		IRenderedComponent<MokaCard> link = Render<MokaCard>(p => p
			.Add(x => x.Clickable, true)
			.Add(x => x.Href, "/plans")
			.Add(x => x.ActionLabel, "Plans"));
		IRenderedComponent<MokaCard> plain = Render<MokaCard>(p => p.Add(x => x.Title, "Plan"));

		foreach (IElement card in new[] { clickable.Find(".moka-card"), link.Find(".moka-card"), plain.Find(".moka-card") })
		{
			Assert.Equal("DIV", card.TagName);
			Assert.False(card.HasAttribute("role"));
			Assert.False(card.HasAttribute("tabindex"));
		}

		IElement button = Assert.Single(clickable.FindAll("button"));
		Assert.Equal("button", button.GetAttribute("type"));
		Assert.Empty(clickable.FindAll("[tabindex]"));
		Assert.Single(link.FindAll("a[href]"));
		Assert.Empty(plain.FindAll("button, a"));
		Assert.Empty(JSInterop.Invocations);

		// A click anywhere on the card lands on the action's stretched ::after, so this is the path every click takes.
		await button.ClickAsync(new MouseEventArgs());
		Assert.Equal(1, clicks);
	}

	[Fact]
	public void CollapsibleHeader_IsAToggleButton_WithTheActionsOutsideIt()
	{
		BunitJSModuleInterop keys = SetupKeysModule();

		IRenderedComponent<MokaCard> cut = Render<MokaCard>(p => p
			.Add(x => x.Title, "Details")
			.Add(x => x.Collapsible, true)
			.Add(x => x.HeaderActions, "<button type=\"button\" class=\"edit\">Edit</button>")
			.Add(x => x.Footer, "Updated today")
			.AddChildContent("Body text"));

		IElement toggle = cut.Find(".moka-card-header [role=button]");
		Assert.Equal("0", toggle.GetAttribute("tabindex"));
		Assert.Equal("true", toggle.GetAttribute("aria-expanded"));
		Assert.Contains("Details", toggle.TextContent, StringComparison.Ordinal);
		Assert.False(toggle.HasAttribute("aria-label"));
		Assert.Null(cut.Find("button.edit").Closest("[role=button]"));

		string[] controls = toggle.GetAttribute("aria-controls")!.Split(' ');
		Assert.Equal([cut.Find(".moka-card-body").Id, cut.Find(".moka-card-footer").Id], controls);

		JSRuntimeInvocation bind = keys.VerifyInvoke("bindActivation");
		Assert.Equal(toggle.GetAttribute("blazor:elementReference"), Assert.IsType<ElementReference>(bind.Arguments[0]).Id);
	}

	[Fact]
	public async Task CollapsingFromTheHeader_ReportsFalseAndHidesTheBody()
	{
		SetupKeysModule();
		List<bool> changes = [];

		IRenderedComponent<MokaCard> cut = Render<MokaCard>(p => p
			.Add(x => x.Title, "Details")
			.Add(x => x.Collapsible, true)
			.Add(x => x.CollapsedChanged, collapsed => changes.Add(collapsed))
			.AddChildContent("Body text"));

		await cut.Find(".moka-card-header").ClickAsync(new MouseEventArgs());

		IElement toggle = cut.Find(".moka-card-header [role=button]");
		Assert.Equal("false", toggle.GetAttribute("aria-expanded"));
		Assert.False(toggle.HasAttribute("aria-controls"));
		Assert.Empty(cut.FindAll(".moka-card-body"));
		Assert.Equal([true], changes);
	}

	// A HeaderActions button used to toggle the card, and on a clickable card it clicked the card too.
	[Fact]
	public async Task AHeaderAction_NeitherTogglesNorClicksTheCard()
	{
		SetupKeysModule();
		int cardClicks = 0;
		int actionClicks = 0;

		IRenderedComponent<MokaCard> cut = Render<MokaCard>(p => p
			.Add(x => x.Title, "Details")
			.Add(x => x.Collapsible, true)
			.Add(x => x.Clickable, true)
			.Add(x => x.OnClick, _ => cardClicks++)
			.Add(x => x.HeaderActions, (RenderFragment)(b =>
			{
				b.OpenElement(0, "button");
				b.AddAttribute(1, "type", "button");
				b.AddAttribute(2, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, () => actionClicks++));
				b.AddContent(3, "Edit");
				b.CloseElement();
			}))
			.AddChildContent("Body text"));

		await cut.Find(".moka-card-header-actions button").ClickAsync(new MouseEventArgs());

		Assert.Equal(1, actionClicks);
		Assert.Equal(0, cardClicks);
		Assert.Single(cut.FindAll(".moka-card-body"));
	}

	[Fact]
	public async Task TogglingAClickableCard_DoesNotClickIt()
	{
		SetupKeysModule();
		int cardClicks = 0;

		IRenderedComponent<MokaCard> cut = Render<MokaCard>(p => p
			.Add(x => x.Title, "Details")
			.Add(x => x.Collapsible, true)
			.Add(x => x.Clickable, true)
			.Add(x => x.OnClick, _ => cardClicks++)
			.AddChildContent("Body text"));

		await cut.Find(".moka-card-header").ClickAsync(new MouseEventArgs());

		Assert.Empty(cut.FindAll(".moka-card-body"));
		Assert.Equal(0, cardClicks);
	}

	// The link card never raised OnClick although HandleClick was written for it.
	[Fact]
	public async Task ALinkCard_RaisesOnClick()
	{
		int cardClicks = 0;

		IRenderedComponent<MokaCard> cut = Render<MokaCard>(p => p
			.Add(x => x.Href, "/details")
			.Add(x => x.OnClick, _ => cardClicks++)
			.AddChildContent("Body text"));

		await cut.Find("a.moka-card-action").ClickAsync(new MouseEventArgs());

		Assert.Equal(1, cardClicks);
	}

	// With only HeaderActions there is no title, so the toggle names itself.
	[Fact]
	public void ATitlelessToggle_HasAName()
	{
		SetupKeysModule();

		IRenderedComponent<MokaCard> cut = Render<MokaCard>(p => p
			.Add(x => x.Collapsible, true)
			.Add(x => x.HeaderActions, "<button type=\"button\">Edit</button>"));

		Assert.Equal("Toggle section", cut.Find(".moka-card-header [role=button]").GetAttribute("aria-label"));
	}

	// The header used to write the Collapsed parameter, so the parent's next render put its own
	// value back when it did not bind Collapsed.
	[Fact]
	public async Task AParentRerender_WithTheSameCollapsed_KeepsTheUsersToggle()
	{
		SetupKeysModule();

		IRenderedComponent<MokaCard> cut = Render<MokaCard>(p => p
			.Add(x => x.Title, "Details")
			.Add(x => x.Collapsible, true)
			.Add(x => x.Collapsed, false)
			.AddChildContent("Body text"));

		await cut.Find(".moka-card-header").ClickAsync(new MouseEventArgs());
		cut.Render(p => p.Add(x => x.Collapsed, false));

		Assert.Empty(cut.FindAll(".moka-card-body"));

		// A value the parent has not passed before still wins.
		cut.Render(p => p.Add(x => x.Collapsed, true));
		cut.Render(p => p.Add(x => x.Collapsed, false));

		Assert.Single(cut.FindAll(".moka-card-body"));
	}

	private BunitJSModuleInterop SetupKeysModule()
	{
		BunitJSModuleInterop keys = JSInterop.SetupModule("./_content/Moka.Red.Core/moka-keys.js");
		keys.SetupVoid("bindActivation", _ => true).SetVoidResult();
		return keys;
	}
}
