using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Theming;
using Moka.Red.Layout.StatusBar;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Layout.Tests.Components;

// What runs out of room in a browser is measured there and checked by hand. These cover what the browser is given.
public class MokaStatusBarFitTests : BunitContext
{
	private const string Css = "MokaStatusBar.razor.css";
	private const string Module = "./_content/Moka.Red.Layout/StatusBar/MokaStatusBar.razor.js";

	private readonly BunitJSModuleInterop _module;

	public MokaStatusBarFitTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_module = JSInterop.SetupModule(Module);
		_module.Mode = JSRuntimeMode.Loose;
	}

	private IRenderedComponent<MokaStatusBar> RenderExpandable() =>
		Render<MokaStatusBar>(p => p.Add(x => x.Expandable, true));

	[Fact]
	public void TheBar_IsWatchedOnce()
	{
		IRenderedComponent<MokaStatusBar> cut = Render<MokaStatusBar>();

		cut.Render(p => p.Add(x => x.Bordered, false));

		JSRuntimeInvocation bind = Assert.Single(_module.Invocations["bindStatusBar"]);
		Assert.IsType<ElementReference>(bind.Arguments[0]);
	}

	[Fact]
	public async Task TheBar_StopsBeingWatchedWhenItGoes()
	{
		IRenderedComponent<MokaStatusBar> cut = Render<MokaStatusBar>();

		await cut.Instance.DisposeAsync();

		Assert.Single(_module.Invocations["unbindStatusBar"]);
	}

	[Fact]
	public void TheSides_CannotMeet()
	{
		Assert.Equal("1 1 0", ScopedCss.Declarations(Css, ".moka-statusbar-start")["flex"]);
		Assert.Equal("0 1 auto", ScopedCss.Declarations(Css, ".moka-statusbar-end")["flex"]);

		foreach (string side in new[] { ".moka-statusbar-start", ".moka-statusbar-end" })
		{
			IReadOnlyDictionary<string, string> rules = ScopedCss.Declarations(Css, side);
			Assert.Equal("clip", rules["overflow"]);
			Assert.Equal("wrap", rules["flex-wrap"]);
			Assert.Equal("nowrap", rules["white-space"]);
			Assert.Equal("0", rules["min-width"]);
		}
	}

	[Theory]
	[InlineData(".moka-statusbar[data-moka-fitted] .moka-statusbar-start")]
	[InlineData(".moka-statusbar[data-moka-fitted] .moka-statusbar-end")]
	public void OnceMeasured_ASideIsOneRowThatClipsNothing(string side)
	{
		IReadOnlyDictionary<string, string> rules = ScopedCss.Declarations(Css, side);

		Assert.Equal("nowrap", rules["flex-wrap"]);
		Assert.Equal("visible", rules["overflow"]);
		Assert.Equal("auto", rules["align-self"]);
	}

	[Theory]
	[InlineData(".moka-statusbar-start ::deep > [data-moka-dropped]")]
	[InlineData(".moka-statusbar-end ::deep > [data-moka-dropped]")]
	public void ADroppedItem_TakesNoPlaceAndNoFocus(string selector) =>
		Assert.Equal("none", ScopedCss.Declarations(Css, selector)["display"]);

	[Theory]
	[InlineData(MokaStatusBarLook.Accent, "moka-statusbar--accent")]
	[InlineData(MokaStatusBarLook.Hairline, "moka-statusbar--hairline")]
	[InlineData(MokaStatusBarLook.Floating, "moka-statusbar--floating")]
	[InlineData(MokaStatusBarLook.Chips, "moka-statusbar--chips")]
	[InlineData(MokaStatusBarLook.Segments, "moka-statusbar--segments")]
	[InlineData(MokaStatusBarLook.Terminal, "moka-statusbar--terminal")]
	public void ALook_DrawsItsClass(MokaStatusBarLook look, string expected)
	{
		IRenderedComponent<MokaStatusBar> cut = Render<MokaStatusBar>(p => p.Add(x => x.Look, look));

		Assert.Contains(expected, cut.Find("footer").ClassList);
	}

	[Fact]
	public void Standard_AddsNothing()
	{
		IRenderedComponent<MokaStatusBar> cut = Render<MokaStatusBar>(p => p.Add(x => x.Look, MokaStatusBarLook.Standard));

		Assert.Equal("moka-statusbar moka-statusbar--bordered", cut.Find("footer").ClassName);
	}

	[Fact]
	public void WithNoLookOfItsOwn_ABarTakesTheThemes()
	{
		IRenderedComponent<CascadingValue<MokaTheme>> cut = Render<CascadingValue<MokaTheme>>(p => p
			.Add(x => x.Value, MokaTheme.Red.WithStatusBar(MokaStatusBarLook.Terminal))
			.AddChildContent<MokaStatusBar>());

		Assert.Contains("moka-statusbar--terminal", cut.Find("footer").ClassList);
	}

	[Fact]
	public void ABarsOwnLook_BeatsTheThemes()
	{
		IRenderedComponent<CascadingValue<MokaTheme>> cut = Render<CascadingValue<MokaTheme>>(p => p
			.Add(x => x.Value, MokaTheme.Red.WithStatusBar(MokaStatusBarLook.Terminal))
			.AddChildContent<MokaStatusBar>(bar => bar.Add(x => x.Look, MokaStatusBarLook.Standard)));

		Assert.Equal("moka-statusbar moka-statusbar--bordered", cut.Find("footer").ClassName);
	}

	[Fact]
	public void WithoutExpandable_ThereIsNoButton_AndNothingCallsBack()
	{
		IRenderedComponent<MokaStatusBar> cut = Render<MokaStatusBar>();

		Assert.Empty(cut.FindAll(".moka-statusbar-more"));
		Assert.Null(Assert.Single(_module.Invocations["bindStatusBar"]).Arguments[1]);
	}

	[Fact]
	public void Expandable_SaysHowManyDidNotFit()
	{
		IRenderedComponent<MokaStatusBar> cut = RenderExpandable();

		cut.InvokeAsync(() => cut.Instance.OnDroppedChanged(2));

		IElement button = cut.Find("button.moka-statusbar-more");
		Assert.Equal("+2", button.TextContent);
		Assert.Equal("Show 2 more items", button.GetAttribute("aria-label"));
		Assert.Equal("false", button.GetAttribute("aria-expanded"));
		Assert.True(button.HasAttribute("data-moka-more"));
		Assert.NotNull(Assert.Single(_module.Invocations["bindStatusBar"]).Arguments[1]);
	}

	[Fact]
	public void Expandable_OpensAndCloses()
	{
		IRenderedComponent<MokaStatusBar> cut = RenderExpandable();
		cut.InvokeAsync(() => cut.Instance.OnDroppedChanged(2));

		cut.Find("button.moka-statusbar-more").Click();

		IElement bar = cut.Find("footer");
		Assert.Contains("moka-statusbar--expanded", bar.ClassList);
		Assert.True(bar.HasAttribute("data-moka-expanded"));
		IElement button = cut.Find("button.moka-statusbar-more");
		Assert.Equal("-2", button.TextContent);
		Assert.Equal("Show fewer items", button.GetAttribute("aria-label"));
		Assert.Equal("true", button.GetAttribute("aria-expanded"));

		button.Click();

		Assert.DoesNotContain("moka-statusbar--expanded", cut.Find("footer").ClassList);
		Assert.False(cut.Find("footer").HasAttribute("data-moka-expanded"));
	}

	[Fact]
	public void Expandable_ClosesOnceEverythingFits()
	{
		IRenderedComponent<MokaStatusBar> cut = RenderExpandable();
		cut.InvokeAsync(() => cut.Instance.OnDroppedChanged(2));
		cut.Find("button.moka-statusbar-more").Click();

		cut.InvokeAsync(() => cut.Instance.OnDroppedChanged(0));

		Assert.DoesNotContain("moka-statusbar--expanded", cut.Find("footer").ClassList);
	}

	[Fact]
	public void TheButtonsName_CanBeReplaced()
	{
		IRenderedComponent<MokaStatusBar> cut = Render<MokaStatusBar>(p => p
			.Add(x => x.Expandable, true)
			.Add(x => x.MoreLabel, count => $"{count} de plus")
			.Add(x => x.LessLabel, "Moins"));

		cut.InvokeAsync(() => cut.Instance.OnDroppedChanged(3));
		Assert.Equal("3 de plus", cut.Find("button.moka-statusbar-more").GetAttribute("aria-label"));

		cut.Find("button.moka-statusbar-more").Click();
		Assert.Equal("Moins", cut.Find("button.moka-statusbar-more").GetAttribute("aria-label"));
	}

	[Fact]
	public void TheButton_ShowsOnlyOnceSomethingDidNotFit()
	{
		Assert.Equal("none", ScopedCss.Declarations(Css, ".moka-statusbar-more")["display"]);
		Assert.Equal("inline-flex", ScopedCss.Declarations(Css, ".moka-statusbar[data-moka-has-more] .moka-statusbar-more")["display"]);
	}

	[Fact]
	public void AnOpenBar_Grows()
	{
		Assert.Equal("auto", ScopedCss.Declarations(Css, ".moka-statusbar--expanded")["height"]);
	}

	[Fact]
	public void AnItem_HasNoPriorityByDefault()
	{
		IRenderedComponent<MokaStatusBarItem> cut = Render<MokaStatusBarItem>(p => p.Add(x => x.Text, "Ready"));

		Assert.False(cut.Find(".moka-statusbar-item").HasAttribute("data-moka-priority"));
	}

	// Swedish writes a minus as U+2212, which the script could not read as a number.
	[Fact]
	public void APriority_IsWrittenTheSameInEveryCulture()
	{
		CultureInfo before = CultureInfo.CurrentCulture;
		CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("sv-SE");
		try
		{
			IRenderedComponent<MokaStatusBarItem> cut = Render<MokaStatusBarItem>(p => p.Add(x => x.Priority, -2));

			Assert.Equal("-2", cut.Find(".moka-statusbar-item").GetAttribute("data-moka-priority"));
		}
		finally
		{
			CultureInfo.CurrentCulture = before;
		}
	}

	[Fact]
	public void APriority_StaysOnTheItemInsideATooltip()
	{
		IRenderedComponent<MokaStatusBarItem> cut = Render<MokaStatusBarItem>(p => p
			.Add(x => x.Text, "Ready")
			.Add(x => x.Tooltip, "All good")
			.Add(x => x.Priority, 5));

		IElement item = cut.Find(".moka-statusbar-item");
		Assert.Equal("5", item.GetAttribute("data-moka-priority"));
		Assert.NotNull(item.Closest(".moka-statusbar-item-tooltip"));
	}
}
