using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Theming;
using Moka.Red.Forms.SettingsForm;
using Moka.Red.Icons;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Forms.Tests.Components;

// MokaTheme.WithSettingsHeaders(Tinted): the icon on a tint of the accent, and what's set now as a quiet line in the
// header. Today's header stays as it is, and the original design never changes.
public class MokaSettingsFormHeaderTests : BunitContext
{
	private const string Css = "MokaSettingsForm.razor.css";

	public MokaSettingsFormHeaderTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public void TodaysHeader_KeepsTheNowBoxUnderIt()
	{
		IRenderedComponent<MokaSettingsForm> cut = Form(MokaTheme.Red);

		Assert.DoesNotContain("moka-settings-form--tinted", cut.Find(".moka-settings-form").ClassList);
		Assert.Equal("Now: last copy 2 h ago", Words(cut.Find(".moka-settings-form__now")));
		Assert.Empty(cut.FindAll(".moka-settings-form__now-line"));
	}

	[Fact]
	public void TheTintedHeader_PutsTheNowLineUnderTheTitle()
	{
		IRenderedComponent<MokaSettingsForm> cut = Form(MokaTheme.Red.WithSettingsHeaders(MokaSettingsHeaderLook.Tinted));

		Assert.Contains("moka-settings-form--tinted", cut.Find(".moka-settings-form").ClassList);
		Assert.Empty(cut.FindAll(".moka-settings-form__now"));
		IElement line = cut.Find(".moka-settings-form__titles > .moka-settings-form__now-line");
		Assert.Equal("Now: last copy 2 h ago", Words(line));
		Assert.Equal("Now:", line.QuerySelector(".moka-visually-hidden")?.TextContent);
	}

	[Fact]
	public void TheTintedLine_FollowsTheLead()
	{
		IRenderedComponent<MokaSettingsForm> cut = Form(MokaTheme.Ember, p => p.Add(x => x.Lead, "Where copies go."));

		IElement titles = cut.Find(".moka-settings-form__titles");
		Assert.Equal(["moka-settings-form__title", "moka-settings-form__lead moka-about", "moka-settings-form__now-line"],
			titles.Children.Select(child => child.ClassName));
	}

	[Fact]
	public void TheTintedLine_TakesNowContent()
	{
		IRenderedComponent<MokaSettingsForm> cut = Form(MokaTheme.Ember, p => p
			.Add(x => x.NowContent, b => b.AddMarkupContent(0, "<strong>3</strong> targets")));

		Assert.Equal("3", cut.Find(".moka-settings-form__now-line strong").TextContent);
	}

	[Fact]
	public void TheTintedHeader_InCards_KeepsTheLineInTheHeaderCard()
	{
		IRenderedComponent<MokaSettingsForm> cut = Form(MokaTheme.Ember, p => p.Add(x => x.Appearance, MokaSettingsAppearance.Cards));

		Assert.NotNull(cut.Find(".moka-settings-form__head").QuerySelector(".moka-settings-form__now-line"));
	}

	[Fact]
	public void WithoutNow_TheTintedHeaderHasNoLine() =>
		Assert.Empty(Form(MokaTheme.Ember, now: null).FindAll(".moka-settings-form__now-line"));

	// A theme on the original design that holds the choice anyway, as one read from JSON could.
	[Fact]
	public void TheOriginalDesign_KeepsTheBox()
	{
		IRenderedComponent<MokaSettingsForm> cut = Form(MokaTheme.Dark with { SettingsHeaders = MokaSettingsHeaderLook.Tinted });

		Assert.DoesNotContain("moka-settings-form--tinted", cut.Find(".moka-settings-form").ClassList);
		Assert.NotEmpty(cut.FindAll(".moka-settings-form__now"));
	}

	[Fact]
	public void AThemeChange_MovesTheLine()
	{
		IRenderedComponent<CascadingValue<MokaTheme>> cut = Render<CascadingValue<MokaTheme>>(p => p
			.Add(x => x.Value, MokaTheme.Red)
			.AddChildContent<MokaSettingsForm>(Backups));
		Assert.NotEmpty(cut.FindAll(".moka-settings-form__now"));

		cut.Render(p => p
			.Add(x => x.Value, MokaTheme.Ember)
			.AddChildContent<MokaSettingsForm>(Backups));

		Assert.Empty(cut.FindAll(".moka-settings-form__now"));
		Assert.NotEmpty(cut.FindAll(".moka-settings-form__now-line"));

		static void Backups(ComponentParameterCollectionBuilder<MokaSettingsForm> form) => form
			.Add(x => x.Title, "Backups")
			.Add(x => x.Now, "last copy 2 h ago");
	}

	[Fact]
	public void TheTintedTile_IsTheAccentsTintInARing()
	{
		IReadOnlyDictionary<string, string> tile = ScopedCss.Declarations(Css, ".moka-gate .moka-settings-form--tinted .moka-settings-form__icon");

		Assert.Equal("var(--moka-color-primary-bg)", tile["background"]);
		Assert.Equal("inset 0 0 0 var(--moka-border-width) var(--moka-color-primary-ring)", tile["box-shadow"]);
		Assert.Equal("var(--moka-color-primary-light)", tile["color"]);
		Assert.Equal("var(--moka-color-primary)", ScopedCss.Declarations(Css, ".moka-settings-form__icon")["background"]);
	}

	[Fact]
	public void TheLine_IsQuietMonospace()
	{
		IReadOnlyDictionary<string, string> line = ScopedCss.Declarations(Css, ".moka-gate .moka-settings-form__now-line");

		Assert.Equal("var(--moka-font-family-mono)", line["font-family"]);
		Assert.Equal("var(--moka-color-on-surface-variant)", line["color"]);
		Assert.False(line.ContainsKey("border"));
	}

	private IRenderedComponent<MokaSettingsForm> Form(MokaTheme theme, Action<ComponentParameterCollectionBuilder<MokaSettingsForm>>? more = null,
		string? now = "last copy 2 h ago") =>
		Render<MokaSettingsForm>(p =>
		{
			p.Add(x => x.Title, "Backups")
				.Add(x => x.Icon, MokaIcons.File.Archive)
				.Add(x => x.Now, now)
				.AddCascadingValue(theme);
			more?.Invoke(p);
		});

	private static string Words(IElement element) =>
		string.Join(' ', element.TextContent.Split(default(char[]), StringSplitOptions.RemoveEmptyEntries));
}
