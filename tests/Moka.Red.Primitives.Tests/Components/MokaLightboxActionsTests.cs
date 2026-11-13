using System.Text.RegularExpressions;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Keys;
using Moka.Red.Core.Theming;
using Moka.Red.Feedback.Tooltip;
using Moka.Red.Icons;
using Moka.Red.Primitives.Media;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Primitives.Tests.Components;

// The lightbox's row of actions, MokaLightboxAction, and the stylesheet that sizes its buttons from the theme.
public sealed partial class MokaLightboxActionsTests : BunitContext
{
	private const string Stylesheet = "MokaLightbox.razor.css";

	private static readonly MokaMediaItem[] Items =
	[
		new() { Src = "/a.jpg", Alt = "Harbour" },
		new() { Src = "/b.jpg", Alt = "Market" },
		new() { Src = "/c.jpg", Alt = "Dunes" }
	];

	public MokaLightboxActionsTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	private static RenderFragment<MokaMediaItem> Download(Action? clicked = null) => item => builder =>
	{
		builder.OpenComponent<MokaLightboxAction>(0);
		builder.AddAttribute(1, nameof(MokaLightboxAction.Icon), MokaIcons.Action.Download);
		builder.AddAttribute(2, nameof(MokaLightboxAction.Text), $"Download {item.Alt}");
		if (clicked is not null)
		{
			builder.AddAttribute(3, nameof(MokaLightboxAction.OnClick),
				EventCallback.Factory.Create<MouseEventArgs>(new object(), clicked));
		}

		builder.CloseComponent();
		builder.OpenComponent<MokaLightboxAction>(4);
		builder.AddAttribute(5, nameof(MokaLightboxAction.Icon), MokaIcons.Action.Share);
		builder.AddAttribute(6, nameof(MokaLightboxAction.Text), "Share");
		builder.CloseComponent();
	};

	private IRenderedComponent<MokaLightbox> RenderOpen(int index,
		Action<ComponentParameterCollectionBuilder<MokaLightbox>>? more = null) =>
		Render<MokaLightbox>(p =>
		{
			p.Add(x => x.Items, Items).Add(x => x.Open, true).Add(x => x.Index, index);
			more?.Invoke(p);
		});

	private IRenderedComponent<MokaLightboxAction> RenderAction(
		Action<ComponentParameterCollectionBuilder<MokaLightboxAction>>? more = null) =>
		Render<MokaLightboxAction>(p =>
		{
			p.Add(x => x.Icon, MokaIcons.Action.Download).Add(x => x.Text, "Download");
			more?.Invoke(p);
		});

	[Fact]
	public void WithoutActions_TheCloseButtonSitsInTheCorner_AsBefore()
	{
		IRenderedComponent<MokaLightbox> cut = RenderOpen(0, p => p.Add(x => x.Facts,
			item => builder => builder.AddContent(0, item.Alt)));

		Assert.Empty(cut.FindAll(".moka-lightbox-top, .moka-lightbox-toolbar"));
		Assert.Single(cut.FindAll(".moka-lightbox > .moka-lightbox-facts"));
		IElement closeTip = Assert.Single(cut.FindAll(".moka-lightbox > .moka-tooltip-trigger"));
		Assert.Contains("moka-lightbox-close-tip", closeTip.ClassList);
		Assert.Single(closeTip.QuerySelectorAll(".moka-lightbox-close"));
	}

	// Facts first, then the actions, then the close button: the order Tab takes, which is the order they read in.
	[Fact]
	public void Actions_ShareARowWithTheCloseButton_BeforeItInTabOrder()
	{
		IRenderedComponent<MokaLightbox> cut = RenderOpen(1, p => p
			.Add(x => x.Facts, item => builder => builder.AddContent(0, item.Alt))
			.Add(x => x.Actions, Download()));

		IElement top = cut.Find(".moka-lightbox-top");
		Assert.Equal(["moka-lightbox-facts", "moka-lightbox-toolbar"], top.Children.Select(child => child.ClassName));
		Assert.True(cut.Find(".moka-lightbox-toolbar").HasAttribute("blazor:onclick:stopPropagation"));

		Assert.Equal(["Previous image", "Next image", "Download Market", "Share", "Close"],
			cut.Find(".moka-lightbox").QuerySelectorAll("button, a").Select(control => control.GetAttribute("aria-label")));

		Assert.Empty(cut.FindAll(".moka-lightbox > .moka-tooltip-trigger, .moka-lightbox-close-tip"));
		IElement closeTip = cut.FindAll(".moka-lightbox-toolbar > .moka-tooltip-trigger")[^1];
		Assert.Single(closeTip.QuerySelectorAll(".moka-lightbox-close"));
		IconTooltip.AssertNamed(cut.Find(".moka-lightbox-close"), "Close", MokaTooltipPosition.BottomEnd);
		IconTooltip.AssertNamed(cut.FindAll(".moka-lightbox-action")[0], "Download Market", MokaTooltipPosition.Bottom);
	}

	[Fact]
	public async Task Actions_AreGivenTheItemOnShow_AtEveryStep()
	{
		IRenderedComponent<MokaLightbox> cut = RenderOpen(0, p => p.Add(x => x.Actions, Download()));

		Assert.Equal("Download Harbour", cut.FindAll(".moka-lightbox-action")[0].GetAttribute("aria-label"));

		await cut.Find(".moka-lightbox").TriggerEventAsync("onmokakeydown",
			new MokaKeyEventArgs { Key = "End", Owner = MokaKeyOwner.Modal });

		Assert.Equal("Download Dunes", cut.FindAll(".moka-lightbox-action")[0].GetAttribute("aria-label"));
	}

	[Fact]
	public async Task AnAction_Runs_AndLeavesTheLightboxOpen()
	{
		int clicks = 0;
		List<bool> reported = [];
		IRenderedComponent<MokaLightbox> cut = RenderOpen(0, p => p
			.Add(x => x.Actions, Download(() => clicks++))
			.Add(x => x.OpenChanged, open => reported.Add(open)));

		await cut.FindAll(".moka-lightbox-action")[0].ClickAsync(new MouseEventArgs());

		Assert.Equal(1, clicks);
		Assert.Empty(reported);
		Assert.Single(cut.FindAll(".moka-lightbox"));
	}

	[Fact]
	public async Task TheCloseButtonInTheRow_StillCloses()
	{
		List<bool> reported = [];
		IRenderedComponent<MokaLightbox> cut = RenderOpen(0, p => p
			.Add(x => x.Actions, Download())
			.Add(x => x.OpenChanged, open => reported.Add(open)));

		await cut.Find(".moka-lightbox-close").ClickAsync(new MouseEventArgs());

		Assert.Equal([false], reported);
		Assert.Empty(cut.Nodes);
	}

	[Fact]
	public async Task TheGallery_HandsItsLightboxTheActions()
	{
		IRenderedComponent<MokaMediaGallery> cut = Render<MokaMediaGallery>(p => p
			.Add(x => x.Items, Items)
			.Add(x => x.LightboxActions, Download()));

		await cut.FindAll(".moka-media-gallery-item")[2].ClickAsync(new MouseEventArgs());

		Assert.Equal("Download Dunes", cut.Find(".moka-lightbox-toolbar .moka-lightbox-action").GetAttribute("aria-label"));
	}

	[Fact]
	public void AnAction_IsANamedButtonInATooltip()
	{
		IRenderedComponent<MokaLightboxAction> cut = RenderAction();

		IElement button = cut.Find("button");
		Assert.Equal("button", button.GetAttribute("type"));
		Assert.Equal("moka-lightbox-action", button.GetAttribute("class"));
		IconTooltip.AssertNamed(button, "Download", MokaTooltipPosition.Bottom);
		Assert.Contains("moka-lightbox-action-tip", cut.Find(".moka-tooltip-trigger").ClassList);
		Assert.Equal("width: var(--_lightbox-icon, 1.5em); height: var(--_lightbox-icon, 1.5em)",
			IconSizeOf(button));

		cut.Render(p => p.Add(x => x.TooltipPosition, MokaTooltipPosition.BottomStart));

		IconTooltip.AssertNamed(cut.Find("button"), "Download", MokaTooltipPosition.BottomStart);
	}

	// A button that nothing listens to sends nothing to .NET.
	[Fact]
	public async Task AnAction_ListensOnlyWhenSomethingListens()
	{
		IRenderedComponent<MokaLightboxAction> idle = RenderAction();
		int clicks = 0;
		IRenderedComponent<MokaLightboxAction> cut = RenderAction(p => p.Add(x => x.OnClick, _ => clicks++));

		Assert.False(idle.Find("button").HasAttribute("blazor:onclick"));
		await cut.Find("button").ClickAsync(new MouseEventArgs());
		Assert.Equal(1, clicks);
	}

	[Theory]
	[InlineData(null, false)]
	[InlineData("", true)]
	[InlineData("harbour.jpg", true)]
	public void WithHref_AnActionIsALink_ThatCanSaveWhatItPointsAt(string? download, bool saves)
	{
		IRenderedComponent<MokaLightboxAction> cut = RenderAction(p => p
			.Add(x => x.Href, "/full/a.jpg")
			.Add(x => x.Download, download));

		IElement link = cut.Find("a.moka-lightbox-action");
		Assert.Equal("/full/a.jpg", link.GetAttribute("href"));
		Assert.Equal(saves, link.HasAttribute("download"));
		Assert.Equal(download, link.GetAttribute("download"));
		IconTooltip.AssertNamed(link, "Download", MokaTooltipPosition.Bottom);
		Assert.Empty(cut.FindAll("button"));
	}

	[Theory]
	[InlineData("javascript:alert(1)")]
	[InlineData("  ")]
	public void AnAddressThatCantBeALink_LeavesAButton(string href)
	{
		IRenderedComponent<MokaLightboxAction> cut = RenderAction(p => p.Add(x => x.Href, href));

		Assert.Empty(cut.FindAll("a"));
		Assert.Single(cut.FindAll("button.moka-lightbox-action"));
	}

	// Busy keeps focus where it is (a disabled button would drop it to the page) and turns a ring in the icon's place.
	[Fact]
	public void ABusyAction_TurnsARing_IgnoresPresses_AndStaysFocusable()
	{
		IRenderedComponent<MokaLightboxAction> cut = RenderAction(p => p
			.Add(x => x.Busy, true)
			.Add(x => x.Href, "/full/a.jpg")
			.Add(x => x.OnClick, _ => { }));

		IElement button = cut.Find("button");
		Assert.Equal("moka-lightbox-action moka-lightbox-action--busy", button.GetAttribute("class"));
		Assert.Equal("true", button.GetAttribute("aria-disabled"));
		Assert.False(button.HasAttribute("disabled"));
		Assert.False(button.HasAttribute("blazor:onclick"));
		Assert.Contains("moka-spin", button.QuerySelector("svg")!.ClassList);
		Assert.Equal(MokaIcons.Status.Spinner.SvgPath, button.QuerySelector("path")?.GetAttribute("d"));
		Assert.Empty(cut.FindAll("a"));
	}

	[Fact]
	public void ADisabledAction_IsADisabledButton_WithNoListener()
	{
		IRenderedComponent<MokaLightboxAction> cut = RenderAction(p => p
			.Add(x => x.Disabled, true)
			.Add(x => x.Href, "/full/a.jpg")
			.Add(x => x.OnClick, _ => { }));

		IElement button = cut.Find("button");
		Assert.True(button.HasAttribute("disabled"));
		Assert.False(button.HasAttribute("aria-disabled"));
		Assert.False(button.HasAttribute("blazor:onclick"));
		Assert.Empty(cut.FindAll("a"));
	}

	[Fact]
	public void TheLightboxsIcons_TakeTheirSizeFromTheStylesheet()
	{
		IRenderedComponent<MokaLightbox> cut = RenderOpen(1);

		Assert.All(cut.FindAll(".moka-lightbox-nav, .moka-lightbox-close"), button =>
			Assert.Equal("width: var(--_lightbox-icon); height: var(--_lightbox-icon)", IconSizeOf(button)));
	}

	// Every size, offset and time comes from a token, and the picture's shadow is the theme's modal shadow.
	[Fact]
	public void TheStylesheet_HoldsNoPixelsOrTimesOfItsOwn()
	{
		string css = Comment().Replace(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Css", Stylesheet)), "");

		Assert.Empty(Pixels().Matches(css).Select(match => match.Value));
		Assert.Empty(Times().Matches(css).Select(match => match.Value));
		Assert.Equal("var(--moka-shadow-modal)", ScopedCss.Declarations(Stylesheet, ".moka-lightbox-image")["box-shadow"]);
		Assert.Equal("moka-lightbox-in var(--moka-transition-normal)",
			ScopedCss.Declarations(Stylesheet, ".moka-lightbox")["animation"]);
		Assert.Equal("var(--moka-radius-full)",
			ScopedCss.Declarations(Stylesheet, ".moka-lightbox ::deep .moka-lightbox-action")["border-radius"]);
	}

	// The arrows come to 40px, the other buttons to 36px and the icons to 24px, as the literal sizes did, on both designs.
	[Fact]
	public void WithTheDefaultTokens_TheButtonsKeepTheirSizes()
	{
		IReadOnlyDictionary<string, string> root = ScopedCss.Declarations(Stylesheet, ".moka-lightbox");
		Assert.Equal("calc(var(--moka-control-height) * 1.25)", root["--_lightbox-nav"]);
		Assert.Equal("calc(var(--_lightbox-nav) * 0.9)", root["--_lightbox-button"]);
		Assert.Equal("calc(var(--_lightbox-nav) * 0.6)", root["--_lightbox-icon"]);
		Assert.Equal("var(--moka-control-height)", ScopedCss.Declarations(Stylesheet, ".moka-gate .moka-lightbox")["--_lightbox-nav"]);

		Assert.Equal("2rem", MokaTheme.Dark.Spacing.ControlHeight);
		Assert.Equal("2rem", MokaTheme.Light.Spacing.ControlHeight);
		Assert.Contains("--moka-control-height: 40px;", MokaTheme.Default.ToCssVariables(), StringComparison.Ordinal);
		Assert.Contains("--moka-control-height: 40px;", MokaTheme.Ember.ToCssVariables(), StringComparison.Ordinal);
	}

	// What the picture's var(--moka-shadow-modal) turns into: a drop shadow by default, a ring and glow with Glow
	// (Ember's choice), nothing with Line.
	[Fact]
	public void ThePicturesShadow_FollowsTheThemesElevation()
	{
		Assert.DoesNotContain("--moka-shadow-modal: none", MokaTheme.Default.ToCssVariables(), StringComparison.Ordinal);
		Assert.Contains("--moka-shadow-modal: none;",
			MokaTheme.Default.WithElevation(MokaElevation.Line).ToCssVariables(), StringComparison.Ordinal);
		Assert.Contains("--moka-shadow-modal: 0 0 0 1px var(--moka-color-primary-ring)",
			MokaTheme.Default.WithElevation(MokaElevation.Glow).ToCssVariables(), StringComparison.Ordinal);
		Assert.Contains("--moka-shadow-modal: 0 0 0 1px var(--moka-color-primary-ring)",
			MokaTheme.Ember.ToCssVariables(), StringComparison.Ordinal);
	}

	private static string? IconSizeOf(IElement control)
	{
		string? style = control.QuerySelector("svg")?.GetAttribute("style");
		return style is null ? null : SizePart().Match(style).Value;
	}

	[GeneratedRegex(@"/\*.*?\*/", RegexOptions.Singleline)]
	private static partial Regex Comment();

	[GeneratedRegex(@"\b\d+(\.\d+)?px\b")]
	private static partial Regex Pixels();

	[GeneratedRegex(@"(?<![\w-])\d*\.?\d+m?s\b")]
	private static partial Regex Times();

	[GeneratedRegex(@"width: [^;]+; height: [^;]+")]
	private static partial Regex SizePart();
}
