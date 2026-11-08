using System.Globalization;
using System.Text.Json;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Feedback.Tooltip;
using Moka.Red.Layout.Toolbar;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Layout.Tests.Components;

// What runs out of room in a browser is measured there and checked by hand. These cover what the browser is given.
public class MokaToolbarFitTests : BunitContext
{
	private const string Css = "MokaToolbar.razor.css";
	private const string Module = "./_content/Moka.Red.Core/moka-fit.js";

	private readonly BunitJSModuleInterop _module;

	public MokaToolbarFitTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_module = JSInterop.SetupModule(Module);
		_module.Mode = JSRuntimeMode.Loose;
	}

	private IRenderedComponent<MokaToolbar> RenderExpandable() =>
		Render<MokaToolbar>(p => p
			.Add(x => x.Expandable, true)
			.Add(x => x.ChildContent, "<button id=\"one\">One</button>"));

	private JsonElement BoundLayout()
	{
		string json = JsonSerializer.Serialize(Assert.Single(_module.Invocations["bindFit"]).Arguments[2]);
		return JsonDocument.Parse(json).RootElement;
	}

	[Fact]
	public void WithoutExpandable_TheMarkupStaysAsItWas()
	{
		IRenderedComponent<MokaToolbar> cut = Render<MokaToolbar>(p => p
			.Add(x => x.ChildContent, "<button id=\"one\">One</button>"));

		IElement bar = cut.Find("[role=toolbar]");
		Assert.Equal("moka-toolbar moka-toolbar--dense moka-toolbar--bordered moka-toolbar--rounded", bar.ClassName);
		Assert.Equal(bar, cut.Find("#one").ParentElement);
		Assert.False(bar.HasAttribute("data-moka-expanded"));
		Assert.Empty(cut.FindAll("[data-moka-more]"));
		Assert.Empty(cut.FindAll(".moka-toolbar__items"));
	}

	[Fact]
	public void WithoutExpandable_NoScriptLoads()
	{
		Render<MokaToolbar>();

		Assert.DoesNotContain(JSInterop.Invocations, i => i.Identifier == "import" && Equals(i.Arguments[0], Module));
		Assert.Empty(_module.Invocations["bindFit"]);
	}

	[Fact]
	public void WrapKeepsItsClass_WhenNotExpandable()
	{
		IRenderedComponent<MokaToolbar> cut = Render<MokaToolbar>(p => p.Add(x => x.Wrap, true));

		Assert.Contains("moka-toolbar--wrap", cut.Find("[role=toolbar]").ClassList);
	}

	[Fact]
	public void Expandable_WinsOverWrap()
	{
		IRenderedComponent<MokaToolbar> cut = Render<MokaToolbar>(p => p.Add(x => x.Wrap, true).Add(x => x.Expandable, true));

		Assert.DoesNotContain("moka-toolbar--wrap", cut.Find("[role=toolbar]").ClassList);
		Assert.Contains("moka-toolbar--expandable", cut.Find("[role=toolbar]").ClassList);
	}

	[Fact]
	public void Expandable_PutsTheControlsInARowOfTheirOwn()
	{
		IRenderedComponent<MokaToolbar> cut = RenderExpandable();

		IElement items = cut.Find("[role=toolbar] > .moka-toolbar__items");
		Assert.Equal(items, cut.Find("#one").ParentElement);
	}

	[Fact]
	public void TheBar_IsWatchedOnce()
	{
		IRenderedComponent<MokaToolbar> cut = RenderExpandable();

		cut.Render(p => p.Add(x => x.Expandable, true).Add(x => x.Bordered, false));

		JSRuntimeInvocation bind = Assert.Single(_module.Invocations["bindFit"]);
		Assert.IsType<ElementReference>(bind.Arguments[0]);
		Assert.NotNull(bind.Arguments[1]);
	}

	[Fact]
	public void TheScript_IsToldWhatDropsAndWhatOnlyFillsRoom()
	{
		RenderExpandable();

		JsonElement layout = BoundLayout();
		Assert.Equal(":scope > .moka-toolbar__items", Assert.Single(layout.GetProperty("sides").EnumerateArray()).GetString());
		Assert.Equal(":scope > .moka-toolbar__more [data-moka-more]", layout.GetProperty("more").GetString());
		Assert.True(layout.GetProperty("flexible").GetBoolean());
		string filler = layout.GetProperty("filler").GetString()!;
		Assert.Contains("div:empty", filler, StringComparison.Ordinal);
		Assert.Contains("input[type=\"hidden\"]", filler, StringComparison.Ordinal);
	}

	[Fact]
	public async Task TheBar_StopsBeingWatchedWhenItGoes()
	{
		IRenderedComponent<MokaToolbar> cut = RenderExpandable();

		await cut.Instance.DisposeAsync();

		Assert.Single(_module.Invocations["unbindFit"]);
	}

	[Fact]
	public async Task ABarThatNeverAsked_HasNothingToStop()
	{
		IRenderedComponent<MokaToolbar> cut = Render<MokaToolbar>();

		await cut.Instance.DisposeAsync();

		Assert.Empty(_module.Invocations["unbindFit"]);
	}

	[Fact]
	public void TurningExpandableOff_StopsWatchingAndForgetsTheCount()
	{
		IRenderedComponent<MokaToolbar> cut = RenderExpandable();
		cut.InvokeAsync(() => cut.Instance.OnDroppedChanged(2));
		cut.Find("[data-moka-more]").Click();

		cut.Render(p => p.Add(x => x.Expandable, false));

		Assert.Single(_module.Invocations["unbindFit"]);
		Assert.Empty(cut.FindAll("[data-moka-more]"));
		Assert.False(cut.Find("[role=toolbar]").HasAttribute("data-moka-expanded"));
	}

	[Fact]
	public void TheButton_IsNamedAndSitsInATooltip()
	{
		IRenderedComponent<MokaToolbar> cut = RenderExpandable();
		cut.InvokeAsync(() => cut.Instance.OnDroppedChanged(2));

		IElement button = cut.Find("button.moka-toolbar__more-button");
		IconTooltip.AssertNamed(button, "Show 2 more tools", MokaTooltipPosition.BottomEnd);
		Assert.Equal("button", button.GetAttribute("type"));
		Assert.Equal("false", button.GetAttribute("aria-expanded"));
		Assert.True(button.HasAttribute("data-moka-more"));
		Assert.NotNull(button.QuerySelector("svg"));
	}

	[Fact]
	public void TheButtonsName_SaysOneWhenThereIsOne()
	{
		IRenderedComponent<MokaToolbar> cut = RenderExpandable();

		cut.InvokeAsync(() => cut.Instance.OnDroppedChanged(1));

		Assert.Equal("Show 1 more tool", cut.Find("[data-moka-more]").GetAttribute("aria-label"));
	}

	[Fact]
	public void Expandable_OpensAndCloses()
	{
		IRenderedComponent<MokaToolbar> cut = RenderExpandable();
		cut.InvokeAsync(() => cut.Instance.OnDroppedChanged(2));

		cut.Find("[data-moka-more]").Click();

		IElement bar = cut.Find("[role=toolbar]");
		Assert.Contains("moka-toolbar--expanded", bar.ClassList);
		Assert.True(bar.HasAttribute("data-moka-expanded"));
		IElement button = cut.Find("[data-moka-more]");
		Assert.Equal("Show fewer tools", button.GetAttribute("aria-label"));
		Assert.Equal("true", button.GetAttribute("aria-expanded"));

		button.Click();

		Assert.DoesNotContain("moka-toolbar--expanded", cut.Find("[role=toolbar]").ClassList);
		Assert.False(cut.Find("[role=toolbar]").HasAttribute("data-moka-expanded"));
	}

	[Fact]
	public void Expandable_ClosesOnceEverythingFits()
	{
		IRenderedComponent<MokaToolbar> cut = RenderExpandable();
		cut.InvokeAsync(() => cut.Instance.OnDroppedChanged(2));
		cut.Find("[data-moka-more]").Click();

		cut.InvokeAsync(() => cut.Instance.OnDroppedChanged(0));

		Assert.DoesNotContain("moka-toolbar--expanded", cut.Find("[role=toolbar]").ClassList);
	}

	[Fact]
	public void TheButtonsName_CanBeReplaced()
	{
		IRenderedComponent<MokaToolbar> cut = Render<MokaToolbar>(p => p
			.Add(x => x.Expandable, true)
			.Add(x => x.MoreLabel, count => $"{count} de plus")
			.Add(x => x.LessLabel, "Moins"));

		cut.InvokeAsync(() => cut.Instance.OnDroppedChanged(3));
		Assert.Equal("3 de plus", cut.Find("[data-moka-more]").GetAttribute("aria-label"));

		cut.Find("[data-moka-more]").Click();
		Assert.Equal("Moins", cut.Find("[data-moka-more]").GetAttribute("aria-label"));
	}

	[Fact]
	public void ADivider_IsASeparatorTheScriptCanTidy()
	{
		IRenderedComponent<MokaToolbarDivider> cut = Render<MokaToolbarDivider>();

		Assert.Equal("separator", cut.Find(".moka-toolbar-divider").GetAttribute("role"));
	}

	[Fact]
	public void AGroup_HasNoPriorityByDefault()
	{
		IRenderedComponent<MokaToolbarGroup> cut = Render<MokaToolbarGroup>();

		Assert.False(cut.Find(".moka-toolbar-group").HasAttribute("data-moka-priority"));
	}

	// Swedish writes a minus as U+2212, which the script could not read as a number.
	[Fact]
	public void APriority_IsWrittenTheSameInEveryCulture()
	{
		CultureInfo before = CultureInfo.CurrentCulture;
		CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("sv-SE");
		try
		{
			IRenderedComponent<MokaToolbarGroup> cut = Render<MokaToolbarGroup>(p => p.Add(x => x.Priority, -2));

			Assert.Equal("-2", cut.Find(".moka-toolbar-group").GetAttribute("data-moka-priority"));
		}
		finally
		{
			CultureInfo.CurrentCulture = before;
		}
	}

	[Fact]
	public void BeforeItIsMeasured_NothingIsHidden()
	{
		IReadOnlyDictionary<string, string> rules = ScopedCss.Declarations(Css, ".moka-toolbar__items");

		Assert.Equal("wrap", rules["flex-wrap"]);
		Assert.Equal("0", rules["min-width"]);
		Assert.False(rules.ContainsKey("overflow"));
		Assert.Equal("auto", ScopedCss.Declarations(Css, ".moka-toolbar--expandable")["height"]);
	}

	[Fact]
	public void OnceMeasured_TheControlsAreOneRow()
	{
		IReadOnlyDictionary<string, string> rules = ScopedCss.Declarations(Css, ".moka-toolbar[data-moka-fitted] > .moka-toolbar__items");

		Assert.Equal("nowrap", rules["flex-wrap"]);
		Assert.Equal("nowrap", rules["white-space"]);
	}

	// With min-width: auto the group's items wrap on the new design, so the bar would grow instead of dropping one.
	[Fact]
	public void OnceMeasured_AGroupKeepsItsSize()
	{
		Assert.Equal("none", ScopedCss.Declarations(Css, ".moka-toolbar[data-moka-fitted] > .moka-toolbar__items ::deep > .moka-toolbar-group")["flex"]);
		Assert.Equal("nowrap", ScopedCss.Declarations(Css, ".moka-toolbar[data-moka-fitted] > .moka-toolbar__items ::deep .moka-toolbar-group-items")["flex-wrap"]);
	}

	// A MokaButton has overflow: hidden, so its automatic minimum is zero and it would squeeze away.
	[Fact]
	public void OnceMeasured_AButtonCantSqueezeAway() =>
		Assert.Equal("min-content", ScopedCss.Declarations(Css, ":where(.moka-toolbar[data-moka-fitted] > .moka-toolbar__items) ::deep > *")["min-width"]);

	[Fact]
	public void APopupInTheBar_StillWrapsItsText() =>
		Assert.Equal("normal", ScopedCss.Declarations(Css, ".moka-toolbar[data-moka-fitted] > .moka-toolbar__items ::deep [data-moka-popup]")["white-space"]);

	[Fact]
	public void ADroppedControl_TakesNoPlaceAndNoFocus() =>
		Assert.Equal("none", ScopedCss.Declarations(Css, ".moka-toolbar--expandable > .moka-toolbar__items ::deep > [data-moka-dropped]")["display"]);

	[Fact]
	public void TheButton_ShowsOnlyOnceSomethingDidNotFit()
	{
		Assert.Equal("none", ScopedCss.Declarations(Css, ".moka-toolbar__more")["display"]);
		Assert.Equal("inline-flex", ScopedCss.Declarations(Css, ".moka-toolbar[data-moka-has-more] > .moka-toolbar__more")["display"]);
	}

	[Fact]
	public void TheNewDesign_DoesNotWrapTheBarAroundItsOwnRow() =>
		Assert.Equal("nowrap", ScopedCss.Declarations(Css, ".moka-gate .moka-toolbar--expandable")["flex-wrap"]);

	[Fact]
	public void AButtonInTheBar_CanStillBeClicked()
	{
		int clicks = 0;
		IRenderedComponent<MokaToolbar> cut = Render<MokaToolbar>(p => p
			.Add(x => x.Expandable, true)
			.Add(x => x.ChildContent, builder =>
			{
				builder.OpenElement(0, "button");
				builder.AddAttribute(1, "id", "one");
				builder.AddAttribute(2, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, () => clicks++));
				builder.CloseElement();
			}));

		cut.Find("#one").Click();

		Assert.Equal(1, clicks);
	}
}
