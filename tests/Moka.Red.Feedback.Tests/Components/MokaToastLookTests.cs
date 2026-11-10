using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Core.Theming;
using Moka.Red.Feedback.Extensions;
using Moka.Red.Feedback.Toast;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Feedback.Tests.Components;

public class MokaToastLookTests : BunitContext
{
	private const string Css = "MokaToastHost.razor.css";
	private const string StandardHost = "moka-toast-host moka-toast-host--top-right";

	public MokaToastLookTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddMokaFeedback();
	}

	private IMokaToastService Toasts => Services.GetRequiredService<IMokaToastService>();

	private void Show(string message, MokaToastSeverity severity = MokaToastSeverity.Info, string? title = null) =>
		Toasts.Show(message, severity, o =>
		{
			o.Title = title;
			o.DurationMs = 0;
		});

	private IRenderedComponent<CascadingValue<MokaTheme>> RenderIn(MokaTheme theme,
		Action<ComponentParameterCollectionBuilder<MokaToastHost>>? host = null) =>
		Render<CascadingValue<MokaTheme>>(p => p
			.Add(x => x.Value, theme)
			.AddChildContent<MokaToastHost>(h => host?.Invoke(h)));

	private static string[] Messages<TComponent>(IRenderedComponent<TComponent> cut) where TComponent : IComponent =>
		[.. cut.FindAll(".moka-toast-message").Select(m => m.TextContent)];

	[Theory]
	[InlineData(MokaToastLook.ToneEdge, "moka-toast-host--tone-edge")]
	[InlineData(MokaToastLook.Tinted, "moka-toast-host--tinted")]
	[InlineData(MokaToastLook.IconTile, "moka-toast-host--icon-tile")]
	[InlineData(MokaToastLook.Snackbar, "moka-toast-host--snackbar")]
	[InlineData(MokaToastLook.Pill, "moka-toast-host--pill")]
	[InlineData(MokaToastLook.Terminal, "moka-toast-host--terminal")]
	public void ALook_DrawsItsClass(MokaToastLook look, string expected)
	{
		IRenderedComponent<CascadingValue<MokaTheme>> cut = RenderIn(MokaTheme.Red, h => h.Add(x => x.Look, look));

		Assert.Equal($"{StandardHost} {expected}", cut.Find(".moka-toast-host").ClassName);
	}

	[Fact]
	public void Standard_AddsNoClass()
	{
		IRenderedComponent<CascadingValue<MokaTheme>> cut =
			RenderIn(MokaTheme.Red, h => h.Add(x => x.Look, MokaToastLook.Standard));

		Assert.Equal(StandardHost, cut.Find(".moka-toast-host").ClassName);
	}

	[Fact]
	public void WithNoLookOfItsOwn_TheHostTakesTheThemes()
	{
		IRenderedComponent<CascadingValue<MokaTheme>> cut = RenderIn(MokaTheme.Red.WithToasts(MokaToastLook.Tinted));

		Assert.Contains("moka-toast-host--tinted", cut.Find(".moka-toast-host").ClassList);
	}

	[Fact]
	public void TheHostsOwnLook_BeatsTheThemes()
	{
		IRenderedComponent<CascadingValue<MokaTheme>> cut = RenderIn(
			MokaTheme.Red.WithToasts(MokaToastLook.Terminal),
			h => h.Add(x => x.Look, MokaToastLook.Snackbar));

		Assert.Equal($"{StandardHost} moka-toast-host--snackbar", cut.Find(".moka-toast-host").ClassName);
	}

	[Fact]
	public void TheHostsOwnStandard_BeatsTheThemesLook()
	{
		IRenderedComponent<CascadingValue<MokaTheme>> cut = RenderIn(
			MokaTheme.Red.WithToasts(MokaToastLook.Terminal),
			h => h.Add(x => x.Look, MokaToastLook.Standard));
		Show("Upload failed", MokaToastSeverity.Error, "Upload");

		Assert.Equal(StandardHost, cut.Find(".moka-toast-host").ClassName);
		Assert.Empty(cut.FindAll(".moka-toast-tag"));
	}

	// Without a theme to say which design it is on, the host does as the status bar does and writes the look.
	[Fact]
	public void WithNoTheme_ALookStillDrawsItsClass()
	{
		IRenderedComponent<MokaToastHost> cut = Render<MokaToastHost>(p => p.Add(x => x.Look, MokaToastLook.ToneEdge));

		Assert.Contains("moka-toast-host--tone-edge", cut.Find(".moka-toast-host").ClassList);
	}

	// The looks and the pile are the new design's. On the original design the host renders exactly as it always did.
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void TheOriginalDesign_GetsNothing(bool light)
	{
		MokaTheme original = light ? MokaTheme.Light : MokaTheme.Dark;
		Show("First", MokaToastSeverity.Error, "One");
		Show("Second");

		IRenderedComponent<CascadingValue<MokaTheme>> cut = RenderIn(original, h => h
			.Add(x => x.Look, MokaToastLook.Terminal)
			.Add(x => x.Stacking, MokaToastStacking.Pile));

		Assert.Equal(StandardHost, cut.Find(".moka-toast-host").ClassName);
		Assert.Empty(cut.FindAll(".moka-toast-tag"));
		Assert.Equal(2, cut.FindAll(".moka-toast-icon").Count);
		Assert.Equal(["First", "Second"], Messages(cut));
	}

	// Standard's markup is what it was: the title's text alone, the icon, and a plain message.
	[Fact]
	public void Standard_KeepsItsMarkup()
	{
		IRenderedComponent<CascadingValue<MokaTheme>> cut = RenderIn(MokaTheme.Red);
		Show("report.pdf is over the limit.", MokaToastSeverity.Error, "Upload failed");

		Assert.Equal("Upload failed", cut.Find(".moka-toast-title").InnerHtml);
		Assert.Equal("moka-toast-message", cut.Find(".moka-toast-message").ClassName);
		Assert.Single(cut.FindAll(".moka-toast-icon"));
		Assert.Empty(cut.FindAll(".moka-toast-tag"));
	}

	[Theory]
	[InlineData(MokaToastSeverity.Info, "[INFO]")]
	[InlineData(MokaToastSeverity.Success, "[ OK ]")]
	[InlineData(MokaToastSeverity.Warning, "[WARN]")]
	[InlineData(MokaToastSeverity.Error, "[FAIL]")]
	public void Terminal_TagsTheTitle_InPlaceOfTheIcon(MokaToastSeverity severity, string tag)
	{
		IRenderedComponent<CascadingValue<MokaTheme>> cut =
			RenderIn(MokaTheme.Red, h => h.Add(x => x.Look, MokaToastLook.Terminal));
		Show("Something happened.", severity, "Heads up");

		IElement title = cut.Find(".moka-toast-title");
		IElement tagElement = title.FirstElementChild!;

		Assert.Equal("moka-toast-tag", tagElement.ClassName);
		Assert.Equal("true", tagElement.GetAttribute("aria-hidden"));
		Assert.Equal(tag, tagElement.TextContent);
		Assert.Equal($"{tag}Heads up", title.TextContent);
		Assert.Empty(cut.FindAll(".moka-toast-icon"));
	}

	[Fact]
	public void Terminal_TagsTheMessage_WhenThereIsNoTitle()
	{
		IRenderedComponent<CascadingValue<MokaTheme>> cut =
			RenderIn(MokaTheme.Red.WithToasts(MokaToastLook.Terminal));
		Show("Disk almost full.", MokaToastSeverity.Warning);

		Assert.Empty(cut.FindAll(".moka-toast-title"));
		Assert.Equal("[WARN]Disk almost full.", cut.Find(".moka-toast-message").TextContent);
		Assert.Equal("true", cut.Find(".moka-toast-message .moka-toast-tag").GetAttribute("aria-hidden"));
	}

	// One line: the title, or the message when there is none. A message under a title stays for screen readers.
	[Theory]
	[InlineData(MokaToastLook.Snackbar)]
	[InlineData(MokaToastLook.Pill)]
	public void OneLineLooks_HideTheMessageUnderATitle_FromTheEyeOnly(MokaToastLook look)
	{
		IRenderedComponent<CascadingValue<MokaTheme>> cut = RenderIn(MokaTheme.Red, h => h.Add(x => x.Look, look));
		Show("12 items synced from the server.", MokaToastSeverity.Success, "Vault unlocked");
		Show("Copied");

		IReadOnlyList<IElement> messages = cut.FindAll(".moka-toast-message");

		Assert.Equal("moka-toast-message moka-visually-hidden", messages[0].ClassName);
		Assert.Equal("12 items synced from the server.", messages[0].TextContent);
		Assert.Equal("moka-toast-message", messages[1].ClassName);
	}

	[Theory]
	[InlineData(MokaToastLook.ToneEdge)]
	[InlineData(MokaToastLook.Tinted)]
	[InlineData(MokaToastLook.IconTile)]
	[InlineData(MokaToastLook.Terminal)]
	public void OtherLooks_ShowTheMessage(MokaToastLook look)
	{
		IRenderedComponent<CascadingValue<MokaTheme>> cut = RenderIn(MokaTheme.Red, h => h.Add(x => x.Look, look));
		Show("12 items synced.", MokaToastSeverity.Success, "Vault unlocked");

		Assert.Equal("moka-toast-message", cut.Find(".moka-toast-message").ClassName);
	}

	[Theory]
	[InlineData(MokaToastPosition.TopRight, "moka-toast-host--pile-top")]
	[InlineData(MokaToastPosition.TopLeft, "moka-toast-host--pile-top")]
	[InlineData(MokaToastPosition.TopCenter, "moka-toast-host--pile-top")]
	[InlineData(MokaToastPosition.BottomRight, "moka-toast-host--pile-bottom")]
	[InlineData(MokaToastPosition.BottomLeft, "moka-toast-host--pile-bottom")]
	[InlineData(MokaToastPosition.BottomCenter, "moka-toast-host--pile-bottom")]
	public void APile_NamesTheEdgeItSitsAgainst(MokaToastPosition position, string edge)
	{
		IRenderedComponent<CascadingValue<MokaTheme>> cut = RenderIn(MokaTheme.Red, h => h
			.Add(x => x.Position, position)
			.Add(x => x.Stacking, MokaToastStacking.Pile));

		IElement host = cut.Find(".moka-toast-host");

		Assert.Contains("moka-toast-host--pile", host.ClassList);
		Assert.Contains(edge, host.ClassList);
		Assert.Equal(4, host.ClassList.Length);
	}

	// The newest toast is the pile's front, against the window's edge: first at the top, last at the bottom.
	[Fact]
	public void APileAtTheTop_PutsTheNewestFirst()
	{
		IRenderedComponent<CascadingValue<MokaTheme>> cut =
			RenderIn(MokaTheme.Red.WithToastStacking(MokaToastStacking.Pile));
		Show("Oldest");
		Show("Middle");
		Show("Newest");

		Assert.Equal(["Newest", "Middle", "Oldest"], Messages(cut));
	}

	[Fact]
	public void APileAtTheBottom_KeepsTheNewestLast()
	{
		IRenderedComponent<CascadingValue<MokaTheme>> cut = RenderIn(
			MokaTheme.Red.WithToastStacking(MokaToastStacking.Pile),
			h => h.Add(x => x.Position, MokaToastPosition.BottomRight));
		Show("Oldest");
		Show("Middle");
		Show("Newest");

		Assert.Equal(["Oldest", "Middle", "Newest"], Messages(cut));
	}

	[Fact]
	public void TheHostsOwnList_BeatsTheThemesPile()
	{
		IRenderedComponent<CascadingValue<MokaTheme>> cut = RenderIn(
			MokaTheme.Red.WithToastStacking(MokaToastStacking.Pile),
			h => h.Add(x => x.Stacking, MokaToastStacking.List));
		Show("Oldest");
		Show("Newest");

		Assert.Equal(StandardHost, cut.Find(".moka-toast-host").ClassName);
		Assert.Equal(["Oldest", "Newest"], Messages(cut));
	}

	[Fact]
	public void APile_StillShowsOnlyMaxVisible_TheNewestOnes()
	{
		IRenderedComponent<CascadingValue<MokaTheme>> cut = RenderIn(MokaTheme.Red, h => h
			.Add(x => x.Stacking, MokaToastStacking.Pile)
			.Add(x => x.MaxVisible, 3));
		foreach (string message in new[] { "1", "2", "3", "4", "5" })
		{
			Show(message);
		}

		Assert.Equal(["5", "4", "3"], Messages(cut));
	}

	[Fact]
	public void APile_KeepsItsCloseAndActionButtons()
	{
		int actions = 0;
		IRenderedComponent<CascadingValue<MokaTheme>> cut =
			RenderIn(MokaTheme.Red, h => h.Add(x => x.Stacking, MokaToastStacking.Pile));
		Show("Oldest");
		Toasts.ShowError("Newest", o =>
		{
			o.DurationMs = 0;
			o.ActionText = "Retry";
			o.OnAction = () => actions++;
		});

		cut.Find(".moka-toast-action").Click();
		Assert.Equal(1, actions);

		cut.FindAll(".moka-toast-close")[0].Click();
		Assert.Equal(["Oldest"], Messages(cut));
	}

	[Fact]
	public void APile_KeepsTheCountdown()
	{
		IRenderedComponent<CascadingValue<MokaTheme>> cut =
			RenderIn(MokaTheme.Red, h => h.Add(x => x.Stacking, MokaToastStacking.Pile));
		Toasts.ShowInfo("Timed", o => o.DurationMs = 60_000);

		Assert.Equal("animation-duration: 60000ms", cut.Find(".moka-toast-progress").GetAttribute("style"));
	}

	[Fact]
	public void EveryLookAndThePile_LiveUnderTheNewDesign()
	{
		string css = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Css", Css));
		string gate = css[css.IndexOf("/* Gate look.", StringComparison.Ordinal)..];
		string matrix = css[..css.IndexOf("/* Gate look.", StringComparison.Ordinal)];

		foreach (string look in new[] { "tone-edge", "tinted", "icon-tile", "snackbar", "pill", "terminal", "pile" })
		{
			Assert.Contains($".moka-toast-host--{look}", gate, StringComparison.Ordinal);
			Assert.DoesNotContain($".moka-toast-host--{look}", matrix, StringComparison.Ordinal);
		}
	}

	[Theory]
	[InlineData("info")]
	[InlineData("success")]
	[InlineData("warning")]
	[InlineData("error")]
	public void EachSeverity_GivesTheLooksItsShades(string severity)
	{
		IReadOnlyDictionary<string, string> rules = ScopedCss.Declarations(Css, $".moka-gate .moka-toast--{severity}");

		Assert.Equal($"var(--moka-color-{severity})", rules["--_toast-tone"]);
		Assert.Equal($"var(--moka-color-{severity}-bg)", rules["--_toast-bg"]);
		Assert.Equal($"var(--moka-color-{severity}-ink)", rules["--_toast-ink"]);
	}

	[Fact]
	public void ToneEdge_DrawsAThickStartEdgeInTheSeverity() =>
		Assert.Equal("calc(var(--moka-border-width) * 3) solid var(--_toast-tone)",
			ScopedCss.Declarations(Css, ".moka-gate .moka-toast-host--tone-edge .moka-toast")["border-inline-start"]);

	[Fact]
	public void Tinted_FillsWithTheSeveritysTint()
	{
		IReadOnlyDictionary<string, string> toast = ScopedCss.Declarations(Css, ".moka-gate .moka-toast-host--tinted .moka-toast");

		Assert.Equal("var(--_toast-bg)", toast["background"]);
		Assert.Equal("color-mix(in srgb, var(--_toast-tone) 35%, transparent)", toast["border-color"]);
		Assert.Equal("var(--_toast-ink)",
			ScopedCss.Declarations(Css, ".moka-gate .moka-toast-host--tinted .moka-toast-title")["color"]);
	}

	[Fact]
	public void IconTile_PutsTheIconOnTheTint()
	{
		IReadOnlyDictionary<string, string> icon =
			ScopedCss.Declarations(Css, ".moka-gate .moka-toast-host--icon-tile ::deep .moka-toast-icon");

		Assert.Equal("content-box", icon["box-sizing"]);
		Assert.Equal("var(--_toast-bg)", icon["background"]);
		Assert.Equal("var(--moka-border-width) solid var(--moka-color-outline-strong)",
			ScopedCss.Declarations(Css, ".moka-gate .moka-toast-host--icon-tile .moka-toast-action")["border"]);
	}

	// Inverted: the text color on light, a lighter panel than the page on dark.
	[Fact]
	public void Snackbar_IsTheInvertedSurface()
	{
		Assert.Equal("var(--moka-color-on-surface)",
			ScopedCss.Declarations(Css, ".moka-gate .moka-toast-host--snackbar .moka-toast")["background"]);
		Assert.Equal("color-mix(in srgb, var(--moka-color-surface) 86%, var(--moka-color-lift))",
			ScopedCss.Declarations(Css, ".moka-gate.moka-dark .moka-toast-host--snackbar .moka-toast")["background"]);
	}

	[Theory]
	[InlineData("snackbar")]
	[InlineData("pill")]
	public void OneLineLooks_HideTheCountdownBar(string look) =>
		Assert.Equal("none",
			ScopedCss.Declarations(Css, $".moka-gate .moka-toast-host--{look} .moka-toast-progress")["display"]);

	// Centred on its edge whatever Position says, without a transform that would capture the close tooltips.
	[Fact]
	public void Pill_CentresTheHost_WithAutoMargins()
	{
		IReadOnlyDictionary<string, string> host = ScopedCss.Declarations(Css, ".moka-gate .moka-toast-host--pill");

		Assert.Equal("var(--moka-toast-offset-left, 0px)", host["left"]);
		Assert.Equal("var(--moka-toast-offset-right, 0px)", host["right"]);
		Assert.Equal("auto", host["margin-inline"]);
		Assert.Equal("center", host["align-items"]);
		Assert.False(host.ContainsKey("transform"));
	}

	[Fact]
	public void Terminal_IsMonospaceOnThePageColor()
	{
		IReadOnlyDictionary<string, string> toast = ScopedCss.Declarations(Css, ".moka-gate .moka-toast-host--terminal .moka-toast");

		Assert.Equal("var(--moka-color-background)", toast["background"]);
		Assert.Equal("var(--moka-font-family-mono)", toast["font-family"]);
		Assert.Equal("var(--_toast-tone)",
			ScopedCss.Declarations(Css, ".moka-gate .moka-toast-host--terminal .moka-toast-tag")["color"]);
	}

	// Folded only while neither the pointer nor focus is inside, so hovering or tabbing in fans every toast out.
	[Theory]
	[InlineData(".moka-gate .moka-toast-host--pile-top:not(:hover):not(:focus-within) .moka-toast:nth-child(n+2)", "margin-top")]
	[InlineData(".moka-gate .moka-toast-host--pile-bottom:not(:hover):not(:focus-within) .moka-toast:nth-last-child(n+2)", "margin-bottom")]
	public void APile_FoldsOlderToastsBehindTheNewest_UntilHoveredOrFocused(string selector, string tuck)
	{
		IReadOnlyDictionary<string, string> folded = ScopedCss.Declarations(Css, selector);

		Assert.Equal("var(--_pile-card)", folded["height"]);
		Assert.Equal("hidden", folded["overflow"]);
		Assert.StartsWith("calc(-1 *", folded[tuck], StringComparison.Ordinal);
	}

	[Fact]
	public void APile_FoldsNothingOutsideTheFoldedState()
	{
		foreach (string selector in new[] { ".moka-gate .moka-toast-host--pile", ".moka-gate .moka-toast-host--pile .moka-toast" })
		{
			IReadOnlyDictionary<string, string> rules = ScopedCss.Declarations(Css, selector);
			Assert.False(rules.ContainsKey("height"));
			Assert.False(rules.ContainsKey("transform"));
		}
	}

	// The host lets the pointer through, but while it is over the pile the gaps count too, or crossing one would
	// fold the pile under the pointer.
	[Fact]
	public void AHoveredPile_TakesPointerEvents() =>
		Assert.Equal("auto", ScopedCss.Declarations(Css, ".moka-gate .moka-toast-host--pile:hover")["pointer-events"]);

	[Fact]
	public void TheFrontToast_IsOnTop()
	{
		Assert.Equal("3", ScopedCss.Declarations(Css, ".moka-gate .moka-toast-host--pile-top .moka-toast:nth-child(1)")["z-index"]);
		Assert.Equal("3", ScopedCss.Declarations(Css, ".moka-gate .moka-toast-host--pile-bottom .moka-toast:nth-last-child(1)")["z-index"]);
	}
}
