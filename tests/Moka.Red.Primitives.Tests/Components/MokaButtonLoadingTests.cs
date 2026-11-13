using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using Moka.Red.Core.Enums;
using Moka.Red.Icons;
using Moka.Red.Primitives.Button;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Primitives.Tests.Components;

// Up to 0.4.4 a busy button was natively disabled, so a keyboard user who pressed Save lost focus to the page (the
// HTML focus fixup rule), and nothing told a screen reader why the button stopped working. It also grew by the
// spinner's width when it had no start icon.
public sealed class MokaButtonLoadingTests : BunitContext
{
	private const string ButtonCss = "MokaButton.razor.css";

	[Fact]
	public void Loading_KeepsTheButtonFocusable_AndSaysItIsBusy()
	{
		IElement button = Render<MokaButton>(p => p
			.Add(x => x.Loading, true)
			.AddChildContent("Save")).Find("button");

		Assert.False(button.HasAttribute("disabled"));
		Assert.Equal("true", button.GetAttribute("aria-disabled"));
		Assert.Equal("true", button.GetAttribute("aria-busy"));
		Assert.Contains("moka-btn--loading", button.ClassList);
	}

	// A submit button that is only aria-disabled would still submit its form on a click and on Enter in a field,
	// which fires a click at the form's default button. Cancelling the click stops both.
	[Theory]
	[InlineData("submit")]
	[InlineData("button")]
	public void Loading_CancelsTheClicksDefaultAction(string type)
	{
		IRenderedComponent<MokaButton> cut = Render<MokaButton>(p => p
			.Add(x => x.Type, type)
			.Add(x => x.Loading, true)
			.AddChildContent("Save"));

		Assert.True(cut.Find("button").HasAttribute("blazor:onclick:preventDefault"));

		cut.Render(p => p.Add(x => x.Loading, false));

		Assert.False(cut.Find("button").HasAttribute("blazor:onclick:preventDefault"));
	}

	[Fact]
	public async Task Loading_IgnoresTheClick()
	{
		int clicks = 0;
		IRenderedComponent<MokaButton> cut = Render<MokaButton>(p => p
			.Add(x => x.Loading, true)
			.Add(x => x.OnClick, _ => clicks++)
			.AddChildContent("Save"));

		await cut.Find("button").ClickAsync(new MouseEventArgs());
		Assert.Equal(0, clicks);

		cut.Render(p => p.Add(x => x.Loading, false));
		await cut.Find("button").ClickAsync(new MouseEventArgs());
		Assert.Equal(1, clicks);
	}

	[Fact]
	public void Loading_AnnouncesItsText_FromAStatusRegionInTheButton()
	{
		IRenderedComponent<MokaButton> cut = Render<MokaButton>(p => p.AddChildContent("Save"));
		Assert.Empty(cut.FindAll("[role=status]"));

		cut.Render(p => p.Add(x => x.Loading, true));
		IElement status = cut.Find("button > [role=status]");
		Assert.Contains("moka-visually-hidden", status.ClassList);
		Assert.Equal("Loading", status.TextContent);

		cut.Render(p => p.Add(x => x.LoadingText, "Saving"));
		Assert.Equal("Saving", cut.Find("[role=status]").TextContent);

		cut.Render(p => p.Add(x => x.Loading, false));
		Assert.Empty(cut.FindAll("[role=status]"));
	}

	// The region is drawn empty and filled on the following render: one that arrives already holding its text isn't
	// announced by every screen reader.
	[Fact]
	public void TheStatusRegion_IsDrawnEmptyBeforeItIsFilled()
	{
		IRenderedComponent<MokaButton> cut = Render<MokaButton>(p => p.AddChildContent("Save"));
		List<string> seen = [];
		cut.OnMarkupUpdated += (_, _) =>
		{
			if (cut.FindAll("[role=status]") is { Count: 1 } regions)
			{
				seen.Add(regions[0].TextContent);
			}
		};

		cut.Render(p => p.Add(x => x.Loading, true));

		Assert.Equal(["", "Loading"], seen);
	}

	[Fact]
	public void NotLoading_TheMarkupHasNoBusyState()
	{
		IElement button = Render<MokaButton>(p => p.AddChildContent("Save")).Find("button");

		Assert.False(button.HasAttribute("aria-busy"));
		Assert.False(button.HasAttribute("aria-disabled"));
		Assert.False(button.HasAttribute("blazor:onclick:preventDefault"));
		Assert.Empty(button.QuerySelectorAll(".moka-btn-spinner"));
	}

	[Fact]
	public void Disabled_StaysNativelyDisabled_WhileLoading()
	{
		IElement button = Render<MokaButton>(p => p
			.Add(x => x.Disabled, true)
			.Add(x => x.Loading, true)
			.AddChildContent("Save")).Find("button");

		Assert.True(button.HasAttribute("disabled"));
		Assert.False(button.HasAttribute("aria-disabled"));
		Assert.Equal("true", button.GetAttribute("aria-busy"));
	}

	// Prerendered or static, nothing could cancel a click, so the button is natively disabled until it's live.
	[Fact]
	public async Task ABusyButton_RenderedStatically_IsNativelyDisabled()
	{
		await using ServiceProvider services = new ServiceCollection()
			.AddLogging()
			.AddSingleton<IJSRuntime>(new NoJs())
			.BuildServiceProvider();
		await using HtmlRenderer renderer = new(services, NullLoggerFactory.Instance);

		string html = await renderer.Dispatcher.InvokeAsync(async () =>
			(await renderer.RenderComponentAsync<MokaButton>(ParameterView.FromDictionary(new Dictionary<string, object?>
			{
				[nameof(MokaButton.Loading)] = true,
				[nameof(MokaButton.Type)] = "submit",
				[nameof(MokaButton.ChildContent)] = (RenderFragment)(b => b.AddContent(0, "Save"))
			}))).ToHtmlString());

		Assert.Contains(" disabled", html, StringComparison.Ordinal);
		Assert.DoesNotContain("aria-disabled", html, StringComparison.Ordinal);
		Assert.Contains("aria-busy=\"true\"", html, StringComparison.Ordinal);
	}

	[Fact]
	public void ALinkButton_IsDisabledAsBefore_WhileLoading()
	{
		IElement link = Render<MokaButton>(p => p
			.Add(x => x.Href, "/report")
			.Add(x => x.Loading, true)
			.AddChildContent("Report")).Find("a");

		Assert.False(link.HasAttribute("href"));
		Assert.Equal("true", link.GetAttribute("aria-disabled"));
		Assert.Equal("-1", link.GetAttribute("tabindex"));
		Assert.True(link.HasAttribute("blazor:onclick:preventDefault"));
		Assert.Equal("true", link.GetAttribute("aria-busy"));
	}

	[Fact]
	public void TheSpinner_TakesTheStartIconsPlace_AtTheIconsSize()
	{
		IRenderedComponent<MokaButton> cut = Render<MokaButton>(p => p
			.Add(x => x.StartIcon, MokaIcons.Action.Save)
			.Add(x => x.Loading, true)
			.AddChildContent("Save"));
		IElement button = cut.Find("button");

		Assert.Contains("moka-btn-spinner", button.FirstElementChild!.ClassList);
		Assert.Empty(cut.FindAll(".moka-btn-icon--start"));
		Assert.DoesNotContain("moka-btn--loading-over", button.ClassList);
		Assert.Equal(IconWidth(Render<MokaButton>(p => p.Add(x => x.StartIcon, MokaIcons.Action.Save).AddChildContent("Save"))
			.Find(".moka-btn-icon--start")), IconWidth(cut.Find(".moka-btn-spinner svg")));
	}

	[Fact]
	public void TheSpinner_TakesTheEndIconsPlace_WhenThereIsNoStartIcon()
	{
		IRenderedComponent<MokaButton> cut = Render<MokaButton>(p => p
			.Add(x => x.EndIcon, MokaIcons.Navigation.ArrowRight)
			.Add(x => x.Loading, true)
			.AddChildContent("Next"));
		IElement button = cut.Find("button");

		Assert.Contains("moka-btn-content", button.FirstElementChild!.ClassList);
		Assert.Contains("moka-btn-spinner", button.Children[1].ClassList);
		Assert.Empty(cut.FindAll(".moka-btn-icon--end"));
	}

	// With no icon to stand in for, the spinner covers the label, which keeps the button's width and its name.
	[Fact]
	public void TheSpinner_CoversALabelWithNoIcon()
	{
		IRenderedComponent<MokaButton> cut = Render<MokaButton>(p => p
			.Add(x => x.Loading, true)
			.AddChildContent("Save"));

		Assert.Contains("moka-btn--loading-over", cut.Find("button").ClassList);
		Assert.Contains("moka-btn-spinner--over", cut.Find(".moka-btn-spinner").ClassList);
		Assert.Equal("Save", cut.Find(".moka-btn-content").TextContent);
	}

	[Theory]
	[InlineData(MokaSize.Xs, "14px")]
	[InlineData(MokaSize.Md, "16px")]
	[InlineData(MokaSize.Lg, "20px")]
	public void AQuietOrIconOnlyButton_ShowsTheSpinnerInTheIconsPlace(MokaSize size, string width)
	{
		IRenderedComponent<MokaButton> quiet = Render<MokaButton>(p => p
			.Add(x => x.Quiet, true)
			.Add(x => x.Size, size)
			.Add(x => x.StartIcon, MokaIcons.Action.Delete)
			.Add(x => x.Loading, true)
			.AddUnmatched("aria-label", "Delete"));

		Assert.Single(quiet.FindAll("svg"));
		Assert.Equal(width, IconWidth(quiet.Find(".moka-btn-spinner svg")));
		Assert.Contains("moka-btn--icon-only", quiet.Find("button").ClassList);
	}

	[Fact]
	public void TheSpinner_IsTheSpinnerIcon_Turning()
	{
		IElement svg = Render<MokaButton>(p => p
			.Add(x => x.Loading, true)
			.AddChildContent("Save")).Find(".moka-btn-spinner svg");

		Assert.Contains("moka-icon", svg.ClassList);
		Assert.Contains("moka-spin", svg.ClassList);
		Assert.Equal(MokaIcons.Status.Spinner.SvgPath, svg.QuerySelector("path")!.GetAttribute("d"));
		Assert.Equal("true", svg.GetAttribute("aria-hidden"));
	}

	[Fact]
	public void ABusyButton_TakesTheClickWithAProgressCursor_AfterTheDisabledRule()
	{
		string css = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Css", ButtonCss)).ReplaceLineEndings("\n");
		IReadOnlyDictionary<string, string> loading = ScopedCss.Declarations(ButtonCss, ".moka-btn--loading");

		Assert.Equal("progress", loading["cursor"]);
		Assert.Equal("auto", loading["pointer-events"]);
		Assert.True(css.IndexOf(".moka-btn--disabled {", StringComparison.Ordinal)
			< css.IndexOf(".moka-btn--loading {", StringComparison.Ordinal));
	}

	[Fact]
	public void TheCoveringSpinner_HidesTheLabelWithoutTakingItOutOfTheName()
	{
		Assert.Equal("0", ScopedCss.Declarations(ButtonCss, ".moka-btn--loading-over .moka-btn-content")["opacity"]);
		Assert.Equal("absolute", ScopedCss.Declarations(ButtonCss, ".moka-btn-spinner--over")["position"]);
		Assert.Equal("relative", ScopedCss.Declarations(ButtonCss, ".moka-btn")["position"]);
	}

	[Fact]
	public void TheSpinClass_TurnsOnceEveryEightTenthsOfASecond()
	{
		Assert.Equal("moka-turn 0.8s linear infinite", ScopedCss.Declarations("moka.css", ".moka-spin")["animation"]);
		Assert.Contains("@keyframes moka-turn", File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Css", "moka.css")),
			StringComparison.Ordinal);
	}

	private static string? IconWidth(IElement svg) =>
		svg.GetAttribute("style")?.Split(';').Select(d => d.Trim())
			.FirstOrDefault(d => d.StartsWith("width:", StringComparison.Ordinal))?["width:".Length..].Trim();

	private sealed class NoJs : IJSRuntime
	{
		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => new(default(TValue)!);

		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
			new(default(TValue)!);
	}
}
