using System.Text.RegularExpressions;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Theming;
using Moka.Red.Feedback.Dialog;
using Moka.Red.Feedback.Extensions;
using Moka.Red.Icons;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Feedback.Tests.Components;

/// <summary>
///     The dialog looks BC picked from the board: Bands, IconLead, Centered and Terminal. A dialog's own
///     <see cref="MokaDialog.Look" /> wins over the theme's, Standard draws what the dialog always drew, and the
///     original design keeps its dialog whatever either says.
/// </summary>
public sealed partial class MokaDialogLookTests : BunitContext
{
	private const string Css = "MokaDialog.razor.css";
	private const string GateMarker = "/* Gate look.";

	public MokaDialogLookTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddMokaFeedback();
	}

	private IMokaDialogService Dialogs => Services.GetRequiredService<IMokaDialogService>();

	[Theory]
	[InlineData(MokaDialogLook.Bands, "moka-dialog--bands")]
	[InlineData(MokaDialogLook.IconLead, "moka-dialog--icon-lead")]
	[InlineData(MokaDialogLook.Centered, "moka-dialog--centered")]
	[InlineData(MokaDialogLook.Terminal, "moka-dialog--terminal")]
	public void ALook_DrawsItsClass(MokaDialogLook look, string expected)
	{
		IRenderedComponent<MokaDialog> cut = Render<MokaDialog>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Title, "Delete 3 files?")
			.Add(x => x.Look, look));

		Assert.Contains(expected, cut.Find("[role=dialog]").ClassList);
	}

	[Fact]
	public void Standard_AddsNothing()
	{
		IRenderedComponent<MokaDialog> cut = Render<MokaDialog>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Title, "Delete 3 files?")
			.Add(x => x.Look, MokaDialogLook.Standard));

		Assert.Equal("moka-dialog moka-dialog--md", cut.Find("[role=dialog]").ClassName);
	}

	[Fact]
	public void WithNoLookOfItsOwn_ADialogTakesTheThemes()
	{
		IRenderedComponent<CascadingValue<MokaTheme>> cut = Themed(MokaTheme.Red.WithDialogs(MokaDialogLook.Terminal));

		Assert.Contains("moka-dialog--terminal", cut.Find("[role=dialog]").ClassList);
	}

	[Fact]
	public void ADialogsOwnLook_BeatsTheThemes()
	{
		IRenderedComponent<CascadingValue<MokaTheme>> cut =
			Themed(MokaTheme.Red.WithDialogs(MokaDialogLook.Terminal), MokaDialogLook.Bands);

		IElement box = cut.Find("[role=dialog]");
		Assert.Contains("moka-dialog--bands", box.ClassList);
		Assert.DoesNotContain("moka-dialog--terminal", box.ClassList);
	}

	[Fact]
	public void ADialogsOwnStandard_BeatsTheThemes()
	{
		IRenderedComponent<CascadingValue<MokaTheme>> cut =
			Themed(MokaTheme.Red.WithDialogs(MokaDialogLook.Terminal), MokaDialogLook.Standard);

		Assert.Equal("moka-dialog moka-dialog--md", cut.Find("[role=dialog]").ClassName);
	}

	[Theory]
	[InlineData(MokaDialogLook.Bands)]
	[InlineData(MokaDialogLook.IconLead)]
	[InlineData(MokaDialogLook.Centered)]
	[InlineData(MokaDialogLook.Terminal)]
	public void TheOriginalDesign_KeepsItsDialog_WhateverTheLookSays(MokaDialogLook look)
	{
		string before = Markup(Themed(MokaTheme.Dark, icon: true));
		string themed = Markup(Themed(MokaTheme.Dark.WithDialogs(look), icon: true));
		string own = Markup(Themed(MokaTheme.Light, look, icon: true));

		Assert.Equal(before, themed);
		Assert.Equal(Markup(Themed(MokaTheme.Light, icon: true)), own);
		Assert.DoesNotContain("moka-dialog-icon-tile", own, StringComparison.Ordinal);
	}

	[Fact]
	public void Standard_OnTheNewDesign_DrawsTheSameMarkupAsNoLook()
	{
		string none = Markup(Themed(MokaTheme.Red, icon: true));
		string standard = Markup(Themed(MokaTheme.Red, MokaDialogLook.Standard, icon: true));

		Assert.Equal(none, standard);
	}

	[Theory]
	[InlineData(MokaDialogLook.Standard)]
	[InlineData(MokaDialogLook.Bands)]
	[InlineData(MokaDialogLook.Terminal)]
	public void TheIconStandsAlone_OutsideTheTileLooks(MokaDialogLook look)
	{
		IRenderedComponent<MokaDialog> cut = WithIcon(look);

		Assert.Empty(cut.FindAll(".moka-dialog-icon-tile"));
		IElement icon = cut.Find(".moka-dialog-header > svg.moka-dialog-icon");
		Assert.Contains("width:18px", Style(icon), StringComparison.Ordinal);
	}

	[Theory]
	[InlineData(MokaDialogLook.IconLead, "20px")]
	[InlineData(MokaDialogLook.Centered, "22px")]
	public void TheTileLooks_PutTheIconInATile(MokaDialogLook look, string size)
	{
		IRenderedComponent<MokaDialog> cut = WithIcon(look);

		IElement tile = cut.Find(".moka-dialog-header > .moka-dialog-icon-tile");
		Assert.Equal("moka-dialog-icon-tile", tile.ClassName);
		IElement icon = tile.QuerySelector("svg.moka-dialog-icon")!;
		Assert.Contains($"width:{size}", Style(icon), StringComparison.Ordinal);
		Assert.Equal("true", icon.GetAttribute("aria-hidden"));
	}

	[Theory]
	[InlineData(MokaDialogLook.IconLead)]
	[InlineData(MokaDialogLook.Centered)]
	public void WithoutAnIcon_TheTileLooksDrawNoTile(MokaDialogLook look)
	{
		IRenderedComponent<MokaDialog> cut = Render<MokaDialog>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Title, "Delete 3 files?")
			.Add(x => x.Look, look));

		Assert.Empty(cut.FindAll(".moka-dialog-icon-tile"));
		Assert.NotNull(cut.Find("h2.moka-dialog-title"));
	}

	[Theory]
	[InlineData(MokaColor.Error, "moka-dialog-icon-tile--error")]
	[InlineData(MokaColor.Warning, "moka-dialog-icon-tile--warning")]
	[InlineData(MokaColor.Success, "moka-dialog-icon-tile--success")]
	[InlineData(MokaColor.Info, "moka-dialog-icon-tile--info")]
	public void AMeaningsColor_TintsTheTile(MokaColor color, string expected)
	{
		IRenderedComponent<MokaDialog> cut = WithIcon(MokaDialogLook.IconLead, color);

		Assert.Contains(expected, cut.Find(".moka-dialog-icon-tile").ClassList);
	}

	[Theory]
	[InlineData(MokaColor.Primary)]
	[InlineData(MokaColor.Secondary)]
	[InlineData(MokaColor.Surface)]
	public void AnyOtherColor_LeavesTheTileInTheAccent(MokaColor color)
	{
		IRenderedComponent<MokaDialog> cut = WithIcon(MokaDialogLook.Centered, color);

		Assert.Equal("moka-dialog-icon-tile", cut.Find(".moka-dialog-icon-tile").ClassName);
	}

	[Fact]
	public void TheServicesLook_ReachesTheDialog()
	{
		IRenderedComponent<MokaDialogHost> cut = Render<MokaDialogHost>();

		_ = Dialogs.ConfirmAsync("They go to the trash.", "Delete 3 files?", o => o.Look = MokaDialogLook.Centered);

		cut.WaitForAssertion(() => Assert.Contains("moka-dialog--centered", cut.Find("[role=dialog]").ClassList));
	}

	[Fact]
	public void WithNoLookInItsOptions_AServiceDialogTakesTheThemes()
	{
		IRenderedComponent<CascadingValue<MokaTheme>> cut = Render<CascadingValue<MokaTheme>>(p => p
			.Add(x => x.Value, MokaTheme.Red.WithDialogs(MokaDialogLook.Bands))
			.AddChildContent<MokaDialogHost>());

		_ = Dialogs.ConfirmAsync("They go to the trash.", "Delete 3 files?");

		cut.WaitForAssertion(() => Assert.Contains("moka-dialog--bands", cut.Find("[role=dialog]").ClassList));
	}

	// A confirm whose button is red is a destructive one, so its icon says so before the text is read.
	[Fact]
	public void ADestructiveConfirm_TintsItsIconLikeItsButton()
	{
		IRenderedComponent<MokaDialogHost> cut = Render<MokaDialogHost>();

		_ = Dialogs.ConfirmAsync("They go to the trash.", "Delete 3 files?", o =>
		{
			o.Look = MokaDialogLook.IconLead;
			o.Icon = MokaIcons.Action.Delete;
			o.ConfirmColor = MokaColor.Error;
		});

		cut.WaitForAssertion(() =>
			Assert.Contains("moka-dialog-icon-tile--error", cut.Find(".moka-dialog-icon-tile").ClassList));
	}

	[Fact]
	public void TheOptionsIconColor_WinsOverTheButtons()
	{
		IRenderedComponent<MokaDialogHost> cut = Render<MokaDialogHost>();

		_ = Dialogs.ConfirmAsync("They go to the trash.", "Delete 3 files?", o =>
		{
			o.Look = MokaDialogLook.Centered;
			o.Icon = MokaIcons.Action.Delete;
			o.ConfirmColor = MokaColor.Error;
			o.IconColor = MokaColor.Warning;
		});

		cut.WaitForAssertion(() =>
		{
			IElement tile = cut.Find(".moka-dialog-icon-tile");
			Assert.Contains("moka-dialog-icon-tile--warning", tile.ClassList);
			Assert.DoesNotContain("moka-dialog-icon-tile--error", tile.ClassList);
		});
	}

	// Centered draws the main action on top, but the markup keeps Cancel first, so a dialog with no close
	// button still opens on the safe choice.
	[Fact]
	public void Centered_KeepsCancelFirstInTheMarkup()
	{
		IRenderedComponent<MokaDialogHost> cut = Render<MokaDialogHost>();

		_ = Dialogs.ConfirmAsync("They go to the trash.", "Delete 3 files?", o => o.Look = MokaDialogLook.Centered);

		cut.WaitForAssertion(() =>
		{
			string[] buttons = [.. cut.FindAll(".moka-dialog-actions > button").Select(b => b.TextContent.Trim())];
			Assert.Equal(["Cancel", "OK"], buttons);
		});
		Assert.Equal("column-reverse",
			ScopedCss.Declarations(Css, ".moka-gate .moka-dialog--centered > .moka-dialog-actions")["flex-direction"]);
	}

	[Fact]
	public void EveryLookRule_SitsInTheGateBlock()
	{
		string css = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Css", Css));
		string matrix = css[..css.IndexOf(GateMarker, StringComparison.Ordinal)];

		foreach (string name in new[] { "--bands", "--icon-lead", "--centered", "--terminal", "icon-tile" })
		{
			Assert.DoesNotContain(name, matrix, StringComparison.Ordinal);
		}
	}

	[Fact]
	public void Bands_PutTheHeaderAndButtonsInTintedBands()
	{
		IReadOnlyDictionary<string, string> header =
			ScopedCss.Declarations(Css, ".moka-gate .moka-dialog--bands > .moka-dialog-header");
		IReadOnlyDictionary<string, string> actions =
			ScopedCss.Declarations(Css, ".moka-gate .moka-dialog--bands > .moka-dialog-actions");

		Assert.Equal("var(--moka-color-surface-variant)", header["background-color"]);
		Assert.Equal("1px solid var(--moka-color-outline)", header["border-bottom"]);
		Assert.Equal("inherit", header["border-top-left-radius"]);
		Assert.Equal("var(--moka-color-surface-variant)", actions["background-color"]);
		Assert.Equal("1px solid var(--moka-color-outline)", actions["border-top"]);
		Assert.Equal("inherit", actions["border-bottom-right-radius"]);
	}

	// A body the app asked to be flush stays flush in every look.
	[Fact]
	public void TheLooksPad_OnlyABodyThatIsNotFlush()
	{
		string css = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Css", Css));
		string gate = Comment().Replace(css[css.IndexOf(GateMarker, StringComparison.Ordinal)..], " ");

		List<string> padded = [.. BodyPaddingRule().Matches(gate).Select(rule => rule.Groups["selector"].Value.Trim())];

		Assert.NotEmpty(padded);
		Assert.All(padded, selector => Assert.Contains(":not(.moka-dialog-body--flush)", selector, StringComparison.Ordinal));
	}

	[Fact]
	public void TheTile_IsTheAccentsTint()
	{
		IReadOnlyDictionary<string, string> tile = ScopedCss.Declarations(Css, ".moka-gate .moka-dialog-icon-tile");

		Assert.Equal("var(--moka-color-primary-bg)", tile["background-color"]);
		Assert.Equal("var(--moka-color-primary-light)", tile["color"]);
		Assert.Equal("var(--moka-control-height)", tile["width"]);
	}

	[Fact]
	public void Centered_RoundsTheTile_AndMovesTheCloseButtonToTheCorner()
	{
		Assert.Equal("var(--moka-radius-full)", ScopedCss.Declarations(Css,
			".moka-gate .moka-dialog--centered > .moka-dialog-header > .moka-dialog-icon-tile")["border-radius"]);
		Assert.Equal("absolute", ScopedCss.Declarations(Css,
			".moka-gate .moka-dialog--centered > .moka-dialog-header ::deep > .moka-tooltip-trigger")["position"]);
		Assert.Equal("center", ScopedCss.Declarations(Css,
			".moka-gate .moka-dialog--centered > .moka-dialog-body")["text-align"]);
	}

	// The prompt before the title is decoration: its empty alternative text keeps it out of the dialog's name.
	[Fact]
	public void Terminal_PromptsTheTitle_WithoutNamingIt()
	{
		IReadOnlyDictionary<string, string> title = ScopedCss.Declarations(Css,
			".moka-gate .moka-dialog--terminal > .moka-dialog-header > .moka-dialog-title");
		IReadOnlyDictionary<string, string> prompt = ScopedCss.Declarations(Css,
			".moka-gate .moka-dialog--terminal > .moka-dialog-header > .moka-dialog-title::before");

		Assert.Equal("var(--moka-font-family-mono)", title["font-family"]);
		Assert.Equal("uppercase", title["text-transform"]);
		Assert.Equal("\"> \" / \"\"", prompt["content"]);
	}

	[Fact]
	public void Terminal_GlowsAndDrawsItsButtonsOutlined()
	{
		IReadOnlyDictionary<string, string> box = ScopedCss.Declarations(Css, ".moka-gate .moka-dialog--terminal");
		IReadOnlyDictionary<string, string> buttons = ScopedCss.Declarations(Css,
			".moka-gate .moka-dialog--terminal > .moka-dialog-actions ::deep .moka-btn");

		Assert.Equal("var(--moka-color-primary-border)", box["border-color"]);
		Assert.Contains("var(--moka-color-primary-glow-strong)", box["box-shadow"], StringComparison.Ordinal);
		Assert.Equal("transparent", buttons["background-color"]);
		Assert.Equal("1px solid var(--moka-color-outline-strong)", buttons["border"]);
		Assert.Equal("var(--moka-font-family-mono)", buttons["font-family"]);
		Assert.Equal("uppercase", buttons["text-transform"]);
	}

	// The edge rules name fewer classes than the look, so the look squares their window-facing corners itself.
	[Theory]
	[InlineData("left", "border-top-left-radius", "border-bottom-left-radius")]
	[InlineData("right", "border-top-right-radius", "border-bottom-right-radius")]
	[InlineData("top", "border-top-left-radius", "border-top-right-radius")]
	[InlineData("bottom", "border-bottom-left-radius", "border-bottom-right-radius")]
	public void Terminal_KeepsAnEdgePanelsCornersSquare(string edge, string first, string second)
	{
		IReadOnlyDictionary<string, string> corners = ScopedCss.Declarations(Css,
			$".moka-gate .moka-dialog--terminal.moka-dialog--{edge}:not(.moka-dialog--fullscreen)");

		Assert.Equal("0", corners[first]);
		Assert.Equal("0", corners[second]);
	}

	private IRenderedComponent<MokaDialog> WithIcon(MokaDialogLook look, MokaColor? color = null) =>
		Render<MokaDialog>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Title, "Delete 3 files?")
			.Add(x => x.Icon, MokaIcons.Action.Delete)
			.Add(x => x.IconColor, color)
			.Add(x => x.Look, look));

	private IRenderedComponent<CascadingValue<MokaTheme>> Themed(MokaTheme theme, MokaDialogLook? look = null,
		bool icon = false) =>
		Render<CascadingValue<MokaTheme>>(p => p
			.Add(x => x.Value, theme)
			.AddChildContent<MokaDialog>(dialog =>
			{
				dialog.Add(x => x.Open, true);
				dialog.Add(x => x.Title, "Delete 3 files?");
				dialog.Add(x => x.Look, look);
				dialog.Add(x => x.Actions, Actions);
				if (icon)
				{
					dialog.Add(x => x.Icon, MokaIcons.Action.Delete);
				}
			}));

	private static void Actions(RenderTreeBuilder builder) =>
		builder.AddMarkupContent(0, "<button type=\"button\">Delete</button>");

	private static string Style(IElement element) =>
		(element.GetAttribute("style") ?? "").Replace(" ", "", StringComparison.Ordinal);

	// The title's id, the tooltip's anchor and Blazor's handler and element ids are made per instance.
	private static string Markup(IRenderedComponent<CascadingValue<MokaTheme>> cut) =>
		InstanceId().Replace(BlazorAttribute().Replace(cut.Markup, ""), "id");

	[GeneratedRegex("[0-9a-f]{32}")]
	private static partial Regex InstanceId();

	[GeneratedRegex(@"\sblazor:[^=\s>]+(=""[^""]*"")?")]
	private static partial Regex BlazorAttribute();

	[GeneratedRegex(@"(?<selector>[^{}]*moka-dialog--[\w-]+[^{}]*>\s*\.moka-dialog-body[^{}]*)\{[^{}]*padding[^{}]*\}")]
	private static partial Regex BodyPaddingRule();

	[GeneratedRegex(@"/\*.*?\*/", RegexOptions.Singleline)]
	private static partial Regex Comment();
}
