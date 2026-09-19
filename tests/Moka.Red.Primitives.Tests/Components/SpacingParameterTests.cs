using AngleSharp.Dom;
using Bunit;
using Bunit.Rendering;
using Moka.Red.Core.Enums;
using Moka.Red.Icons;
using Moka.Red.Primitives.Avatar;
using Moka.Red.Primitives.Badge;
using Moka.Red.Primitives.Button;
using Moka.Red.Primitives.Callout;
using Moka.Red.Primitives.Chat;
using Moka.Red.Primitives.Chip;
using Moka.Red.Primitives.ColorSwatch;
using Moka.Red.Primitives.Countdown;
using Moka.Red.Primitives.Fab;
using Moka.Red.Primitives.Gauge;
using Moka.Red.Primitives.Icon;
using Moka.Red.Primitives.List;
using Moka.Red.Primitives.Media;
using Moka.Red.Primitives.Meter;
using Moka.Red.Primitives.Notice;
using Moka.Red.Primitives.NumberTicker;
using Moka.Red.Primitives.Ribbon;
using Moka.Red.Primitives.SegmentedControl;
using Moka.Red.Primitives.SelectionBar;
using Moka.Red.Primitives.SplitButton;
using Moka.Red.Primitives.Stat;
using Moka.Red.Primitives.StatusDot;
using Moka.Red.Primitives.Steps;
using Moka.Red.Primitives.ThemeSwitcher;
using Moka.Red.Primitives.TransferList;
using Moka.Red.Primitives.Typography;
using Moka.Red.Tests.Shared;
using static Moka.Red.Tests.Shared.SpacingCase;

namespace Moka.Red.Primitives.Tests.Components;

// Margin, Padding and Rounded are declared on every visual component, and these dropped some or
// all of them. Each case sets all three and checks where they land. Without them every style
// attribute has to read as it did before.
public class SpacingParameterTests : BunitContext
{
	// Two days ahead, so every unit renders and nothing completes during a test.
	private static readonly DateTime Target = DateTime.UtcNow.AddDays(2);

	private static readonly Dictionary<string, SpacingCase> Cases = new()
	{
		["AvatarGroup"] = new(typeof(MokaAvatarGroup), "--moka-avatar-group-spacing: -8px"),
		["Badge"] = new(typeof(MokaBadge), "", ("Content", "3")) { RadiusOn = ".moka-badge__indicator" },
		["Blockquote"] = new(typeof(MokaBlockquote), "margin: var(--moka-spacing-md) 0"),
		["ButtonGroup"] = new(typeof(MokaButtonGroup), ""),
		["Callout"] = new(typeof(MokaCallout), IconStyle("16px")),
		["Caption"] = new(typeof(MokaCaption),
			"font-size: var(--moka-font-size-xs); color: var(--moka-color-on-surface); opacity: 0.7; " +
			"line-height: var(--moka-line-height-base)"),
		["Chat"] = new(typeof(MokaChat), $"height: 400px | {IconStyle("16px")}",
			("Messages", Array.Empty<MokaChatMessage>())),
		["Chip"] = new(typeof(MokaChip), "", ("Text", "Chip")),
		["Code"] = new(typeof(MokaCode), ""),
		["Countdown"] = new(typeof(MokaCountdown), "", ("TargetDate", Target)) { RadiusOn = ".moka-countdown-unit" },
		["CountdownFlip"] = new(typeof(MokaCountdown), "", ("TargetDate", Target),
			("CountdownStyle", MokaCountdownStyle.Flip)) { RadiusOn = ".moka-countdown-value" },
		["CountdownInline"] = new(typeof(MokaCountdown), "", ("TargetDate", Target),
			("CountdownStyle", MokaCountdownStyle.Inline)),
		["ColorSwatch"] = new(typeof(MokaColorSwatch),
			"--moka-swatch-columns: 8; --moka-swatch-size: 24px | background-color: #ff0000",
			("Colors", new[] { "#ff0000" })),
		["FloatingActionButton"] = new(typeof(MokaFloatingActionButton), IconStyle("20px"),
			("Icon", MokaIcons.Action.Add)),
		["Gauge"] = new(typeof(MokaGauge), "", ("Value", 40d)),
		["Heading"] = new(typeof(MokaHeading),
			"font-size: var(--moka-font-size-xl); font-weight: var(--moka-font-weight-bold); " +
			"line-height: var(--moka-line-height-tight); margin: 0"),
		["Icon"] = new(typeof(MokaIcon), IconStyle("20px"), ("Icon", MokaIcons.Action.Add)),
		["Label"] = new(typeof(MokaLabel),
			"font-size: var(--moka-font-size-base); font-weight: var(--moka-font-weight-medium)"),
		["Link"] = new(typeof(MokaLink), "font-size: var(--moka-font-size-base)", ("Href", "/docs")),
		["ListItem"] = new(typeof(MokaListItem), "", ("Text", "Row")),
		["ListItemLink"] = new(typeof(MokaListItem), "", ("Text", "Row"), ("Href", "/docs")) { Box = "a" },
		["MediaGallery"] = new(typeof(MokaMediaGallery),
			$"grid-template-columns: repeat(3, 1fr); gap: var(--moka-spacing-sm) | {IconStyle("24px")} | {IconStyle("24px")}",
			("Items", new[] { new MokaMediaItem { Src = "a.png" }, new MokaMediaItem { Src = "b.png" } }))
		{
			RadiusOn = ".moka-media-gallery-item"
		},
		["Meter"] = new(typeof(MokaMeter), "width: 40%;", ("Value", 40d)) { RadiusOn = ".moka-meter__track" },
		["Notice"] = new(typeof(MokaNotice), IconStyle("16px")),
		["NumberTicker"] = new(typeof(MokaNumberTicker),
			"--moka-ticker-duration: 1000ms | transform: translateY(-40%) | transform: translateY(-20%)",
			("Value", 42d)),
		["Paragraph"] = new(typeof(MokaParagraph),
			"font-size: var(--moka-font-size-base); line-height: var(--moka-line-height-relaxed)"),
		["Ribbon"] = new(typeof(MokaRibbon), "", ("Text", "NEW")),
		["SegmentedControl"] = new(typeof(MokaSegmentedControl), ""),
		["SelectionBar"] = new(typeof(MokaSelectionBar), IconStyle("16px"), ("Count", 3)) { Root = ".moka-selection-bar" },
		["SelectionBarFixed"] = new(typeof(MokaSelectionBar), IconStyle("16px"), ("Count", 3), ("Fixed", true))
		{
			Root = ".moka-selection-bar"
		},
		["SplitButton"] = new(typeof(MokaSplitButton), IconStyle("16px")),
		["Stat"] = new(typeof(MokaStat), "", ("Value", "12")),
		["StatusDot"] = new(typeof(MokaStatusDot), "") { RadiusOn = ".moka-status-dot-dot" },
		["Steps"] = new(typeof(MokaSteps), "", ("Steps", new[] { "One", "Two" })),
		["Text"] = new(typeof(MokaText), "font-size: var(--moka-font-size-base)"),
		["ThemeSwitcher"] = new(typeof(MokaThemeSwitcher), IconStyle("14px")) { Box = ".moka-theme-switcher__trigger" },
		["TransferList"] = new(typeof(MokaTransferList<string>), IconStyle("16px") + " | " + IconStyle("16px"),
			("AvailableItems", new List<string> { "Ada" }), ("SelectedItems", new List<string> { "Grace" }))
		{
			RadiusOn = ".moka-transfer-list-panel"
		},
		["VideoEmbed"] = new(typeof(MokaVideoEmbed), "aspect-ratio: 16/9", ("Src", "clip.mp4"))
	};

	// The components that draw several boxes and nothing around them. Each box takes the radius,
	// the way the segmented inputs give it to every digit box.
	public static TheoryData<string, int> BoxedCases => new()
	{
		{ "Countdown", 4 },
		{ "CountdownFlip", 4 },
		{ "MediaGallery", 2 },
		{ "TransferList", 2 }
	};

	public SpacingParameterTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	public static TheoryData<string> Names => [.. Cases.Keys];

	[Theory]
	[MemberData(nameof(Names))]
	public void SpacingParameters_LandOnTheComponent(string name)
	{
		SpacingCase spacing = Cases[name];
		IRenderedComponent<ContainerFragment> cut = Render(spacing.Render(withSpacing: true));
		spacing.OpenIfNeeded(cut);

		spacing.AssertSpacing(cut);
	}

	[Theory]
	[MemberData(nameof(Names))]
	public void Styles_AreUnchanged_WithoutSpacingParameters(string name)
	{
		SpacingCase spacing = Cases[name];
		IRenderedComponent<ContainerFragment> cut = Render(spacing.Render(withSpacing: false));
		spacing.OpenIfNeeded(cut);

		Assert.Equal(spacing.DefaultStyles, SpacingCase.Styles(cut));
	}

	// The two halves had fixed corners, which hid a larger radius on the split button and stuck
	// out past a smaller one. They take their outer corners from it now, as the buttons in a
	// MokaButtonGroup do.
	[Theory]
	[InlineData(".moka-split-btn__primary", "border-top-left-radius", "border-bottom-left-radius")]
	[InlineData(".moka-split-btn__toggle", "border-top-right-radius", "border-bottom-right-radius")]
	public void SplitButtonHalves_TakeTheirOuterCornersFromTheButton(string half, string top, string bottom)
	{
		IReadOnlyDictionary<string, string> css = ScopedCss.Declarations("MokaSplitButton.razor.css", half);

		Assert.Equal("0", css["border-radius"]);
		Assert.Equal("inherit", css[top]);
		Assert.Equal("inherit", css[bottom]);
	}

	// A pill track around rounded-rectangle segments looked broken, so with Rounded set the segments
	// take the track's corners. Without it they keep their own.
	[Fact]
	public void SegmentedControl_WithRounded_LetsTheSegmentsFollowTheTrack()
	{
		IRenderedComponent<MokaSegmentedControl> plain = Render<MokaSegmentedControl>();
		IRenderedComponent<MokaSegmentedControl> rounded = Render<MokaSegmentedControl>(p => p
			.Add(x => x.Rounded, MokaRounding.Full));

		Assert.DoesNotContain("moka-segmented--rounded", plain.Find(".moka-segmented").ClassList);
		Assert.Contains("moka-segmented--rounded", rounded.Find(".moka-segmented").ClassList);
		Assert.Equal("inherit", ScopedCss.Declarations("MokaSegmentedControl.razor.css",
			".moka-segmented--rounded ::deep .moka-segment")["border-radius"]);
	}

	// The radius went on the root, which draws nothing in these, so it never showed.
	[Theory]
	[MemberData(nameof(BoxedCases))]
	public void Rounded_ShapesEveryBox(string name, int boxes)
	{
		SpacingCase spacing = Cases[name];
		IRenderedComponent<ContainerFragment> cut = Render(spacing.Render(withSpacing: true));

		IReadOnlyList<IElement> shaped = cut.FindAll(spacing.RadiusOn!);
		Assert.Equal(boxes, shaped.Count);
		Assert.All(shaped, box => Assert.Equal("border-radius: 3px", box.GetAttribute("style")));
		Assert.DoesNotContain("border-radius", cut.Nodes.OfType<IElement>().First().GetAttribute("style"),
			StringComparison.Ordinal);
	}

	// The Inline countdown draws no boxes, so the radius stays on the root, where a background
	// given through Style or Class shows it.
	[Fact]
	public void InlineCountdown_KeepsTheRadiusOnTheRoot()
	{
		IRenderedComponent<ContainerFragment> cut = Render(Cases["CountdownInline"].Render(withSpacing: true));

		Assert.Equal("margin: 7px; padding: 5px; border-radius: 3px", cut.Find(".moka-countdown").GetAttribute("style"));
		Assert.Empty(cut.FindAll(".moka-countdown-unit[style], .moka-countdown-value[style]"));
	}
}
