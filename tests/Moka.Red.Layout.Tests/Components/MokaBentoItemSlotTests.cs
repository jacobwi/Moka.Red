using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Moka.Red.Icons;
using Moka.Red.Layout.BentoGrid;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Layout.Tests.Components;

// IconContent and EndContent in an item's head: what an app draws in the icon's place and at the head's end.
public sealed class MokaBentoItemSlotTests : BunitContext
{
	private const string Css = "MokaBentoItem.razor.css";
	private const string Picture = """<img src="team.png" alt="">""";

	public MokaBentoItemSlotTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		JSInterop.SetupModule("./_content/Moka.Red.Core/moka-keys.js").Mode = JSRuntimeMode.Loose;
		JSInterop.SetupModule("./_content/Moka.Red.Layout/BentoGrid/MokaBentoGrid.razor.js").Mode = JSRuntimeMode.Loose;
	}

	[Fact]
	public void AHeadWithoutTheSlots_DrawsNeither()
	{
		IRenderedComponent<MokaBentoItem> cut = Render<MokaBentoItem>(p => p
			.Add(x => x.Card, new MokaBoardCard("devices"))
			.Add(x => x.Icon, MokaIcons.Content.Copy)
			.Add(x => x.Href, "/devices"));

		Assert.Empty(cut.FindAll(".moka-icon-slot"));
		Assert.Empty(cut.FindAll(".moka-bento-item__end"));
		IElement icon = cut.Find(".moka-bento-item__head > .moka-bento-item__icon");
		Assert.False(icon.HasAttribute("style"));
		Assert.NotNull(icon.QuerySelector("svg"));
	}

	[Fact]
	public void IconContent_DrawsInTheIconsBox_HiddenFromScreenReaders()
	{
		IRenderedComponent<MokaBentoItem> cut = Render<MokaBentoItem>(p => p
			.Add(x => x.Card, new MokaBoardCard("team"))
			.Add(x => x.Title, "Team")
			.Add(x => x.IconContent, Picture));

		IElement head = cut.Find(".moka-bento-item__head");
		IElement slot = head.Children[0];
		Assert.Equal(["moka-icon-slot", "moka-bento-item__icon"], slot.ClassList);
		Assert.Equal("true", slot.GetAttribute("aria-hidden"));
		Assert.Equal("--moka-icon-slot: 16px", slot.GetAttribute("style"));
		Assert.Equal("team.png", slot.QuerySelector("img")?.GetAttribute("src"));
		Assert.Equal("Team", head.QuerySelector("h2")?.TextContent);
	}

	[Fact]
	public void IconContent_WinsOverIcon()
	{
		IRenderedComponent<MokaBentoItem> cut = Render<MokaBentoItem>(p => p
			.Add(x => x.Card, new MokaBoardCard("team"))
			.Add(x => x.Icon, MokaIcons.Content.Copy)
			.Add(x => x.IconContent, Picture));

		IElement icon = Assert.Single(cut.FindAll(".moka-bento-item__icon"));
		Assert.Contains("moka-icon-slot", icon.ClassList);
		Assert.Empty(cut.FindAll(".moka-bento-item__head svg"));
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void EitherSlot_GivesAPlainItemAHead(bool iconContent)
	{
		const string content = "<span>3</span>";
		IRenderedComponent<MokaBentoItem> cut = Render<MokaBentoItem>(p =>
		{
			p.Add(x => x.Title, "Alerts").Add(x => x.ColSpan, 2);
			if (iconContent)
			{
				p.Add(x => x.IconContent, content);
			}
			else
			{
				p.Add(x => x.EndContent, content);
			}
		});

		IElement item = cut.Find(".moka-bento-item");
		Assert.Contains("moka-bento-item--tile", item.ClassList);
		Assert.Equal("grid-column: span 2", item.GetAttribute("style"));
		Assert.False(item.HasAttribute("role"));
		Assert.Equal("Alerts", cut.Find(".moka-bento-item__head > h2").TextContent);
		Assert.Empty(cut.FindAll(".moka-bento-item__title"));
	}

	[Fact]
	public void EndContent_SitsAtTheEndOfTheHead_BeforeTheOpenLink()
	{
		IRenderedComponent<MokaBentoItem> cut = Render<MokaBentoItem>(p => p
			.Add(x => x.Card, new MokaBoardCard("plugins"))
			.Add(x => x.Title, "Plugins")
			.Add(x => x.Source, "Plugin")
			.Add(x => x.Href, "/plugins")
			.Add(x => x.EndContent, """<span class="count">3</span>"""));

		IElement head = cut.Find(".moka-bento-item__head");
		Assert.Equal(["moka-bento-item__heading", "moka-bento-item__source", "moka-bento-item__end", "moka-bento-item__open"],
			head.Children.Select(child => child.ClassName));
		Assert.Equal("3", head.QuerySelector(".moka-bento-item__end > .count")?.TextContent);
	}

	[Fact]
	public void AClickOnAControlInEndContent_DoesNotClickTheItem()
	{
		int itemClicks = 0;
		int countClicks = 0;
		IRenderedComponent<MokaBentoItem> cut = Render<MokaBentoItem>(p => p
			.Add(x => x.Title, "Alerts")
			.Add(x => x.Clickable, true)
			.Add(x => x.OnClick, () => itemClicks++)
			.Add(x => x.EndContent, CountButton(() => countClicks++)));

		cut.Find(".moka-bento-item__end > button").Click();
		Assert.Equal(1, countClicks);
		Assert.Equal(0, itemClicks);

		cut.Find(".moka-bento-item__heading").Click();
		Assert.Equal(1, itemClicks);
	}

	// bUnit applies no CSS, so these rules are read as text: plain content in the slot lets a click through to the item,
	// and only a control in it takes the click and stops it there.
	[Fact]
	public void OnAClickableItem_OnlyAControlInEndContent_TakesTheClick()
	{
		Assert.Equal("none", ScopedCss.Declarations(Css, ".moka-bento-item--clickable .moka-bento-item__end")["pointer-events"]);
		Assert.Equal("auto", ScopedCss.Declarations(Css,
				".moka-bento-item--clickable .moka-bento-item__end ::deep :is(a[href], button, input, select, textarea, label, [tabindex], .moka-tooltip-trigger)")
			["pointer-events"]);
		Assert.False(ScopedCss.Declarations(Css, ".moka-bento-item__end").ContainsKey("pointer-events"));
	}

	[Fact]
	public void OnACustomizedBoard_TheToolsStayUsable_BesideEndContent()
	{
		List<IReadOnlyList<MokaBoardCard>> raised = [];
		RenderFragment<MokaBoardCard> template = card => builder =>
		{
			builder.OpenComponent<MokaBentoItem>(0);
			builder.AddComponentParameter(1, nameof(MokaBentoItem.Card), card);
			builder.AddComponentParameter(2, nameof(MokaBentoItem.IconContent), (RenderFragment)(icon => icon.AddMarkupContent(0, Picture)));
			builder.AddComponentParameter(3, nameof(MokaBentoItem.EndContent), CountButton(() => { }));
			builder.CloseComponent();
		};
		IRenderedComponent<MokaBentoGrid> cut = Render<MokaBentoGrid>(p => p
			.Add(x => x.Board, [new MokaBoardCard("a"), new MokaBoardCard("b", MokaBoardSize.Small)])
			.Add(x => x.Customizing, true)
			.Add(x => x.CardTemplate, template)
			.Add(x => x.BoardChanged, layout => raised.Add(layout)));

		IElement card = cut.Find("[data-card=\"a\"]");
		Assert.NotNull(card.QuerySelector(".moka-bento-item__tools"));
		Assert.NotNull(card.QuerySelector(".moka-bento-item__head > .moka-icon-slot img"));
		Assert.NotNull(card.QuerySelector(".moka-bento-item__head > .moka-bento-item__end > button"));

		cut.Find("[data-card=\"a\"] [data-tool=\"size\"]").Click();
		Assert.Equal(MokaBoardSize.Full, Assert.Single(raised)[0].Size);
	}

	private RenderFragment CountButton(Action onClick) => builder =>
	{
		builder.OpenElement(0, "button");
		builder.AddAttribute(1, "type", "button");
		builder.AddAttribute(2, "onclick", EventCallback.Factory.Create(this, onClick));
		builder.AddContent(3, "3");
		builder.CloseElement();
	};
}
