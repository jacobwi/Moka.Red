using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Theming;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Feedback.Tooltip;

/// <summary>
///     A pure CSS tooltip that appears on hover or on keyboard focus.
///     Zero JS interop, zero C# event handlers, zero allocations per hover.
///     The tooltip is always in the DOM but takes no layout until it shows; CSS handles show/hide.
///     Where the browser supports anchor positioning the popup is placed against the window rather than
///     inside its trigger, so a container that clips its overflow cannot cut it off, and it flips to the
///     side with room at the window's edge.
/// </summary>
public partial class MokaTooltip : MokaComponentBase
{
	private string? _keysFor;
	private IReadOnlyList<string> _keys = [];

	/// <summary>The trigger element that the tooltip wraps.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>
	///     Tooltip text content. Ignored if <see cref="Content" /> is set. With a <see cref="Title" /> it is the line under
	///     it.
	/// </summary>
	[Parameter]
	public string? Text { get; set; }

	/// <summary>
	///     Rich tooltip content. Overrides <see cref="Text" />, <see cref="Title" />, <see cref="Shortcut" />,
	///     <see cref="Icon" /> and <see cref="Dot" />.
	/// </summary>
	[Parameter]
	public RenderFragment? Content { get; set; }

	/// <summary>Position relative to the trigger. Defaults to <see cref="MokaTooltipPosition.Top" />.</summary>
	[Parameter]
	public MokaTooltipPosition Position { get; set; } = MokaTooltipPosition.Top;

	/// <summary>Delay in milliseconds before showing the tooltip. Defaults to 300. Hiding never waits.</summary>
	[Parameter]
	public int Delay { get; set; } = 300;

	/// <summary>
	///     The widest the popup grows, as a CSS length such as <c>20rem</c>, before longer text wraps onto more lines.
	///     Never wider than the window, and a line break in <see cref="Text" /> starts a new line. Null, the default,
	///     keeps the text on one line, which suits a short name; set it for a sentence or a value of unknown length. A
	///     tooltip with a <see cref="Title" /> wraps at 20rem when this is null.
	/// </summary>
	[Parameter]
	public string? MaxWidth { get; set; }

	/// <summary>
	///     A bold first line, with <see cref="Text" /> under it. The tooltip wraps, at <see cref="MaxWidth" /> or 20rem.
	///     Ignored if <see cref="Content" /> is set.
	/// </summary>
	[Parameter]
	public string? Title { get; set; }

	/// <summary>
	///     A keyboard shortcut drawn as key caps after the text, such as <c>"Ctrl+Shift+P"</c>: one <c>MokaKbd</c> per key.
	///     Ignored if <see cref="Content" /> is set.
	/// </summary>
	[Parameter]
	public string? Shortcut { get; set; }

	/// <summary>An icon before the text. Wins over <see cref="Dot" />. Ignored if <see cref="Content" /> is set.</summary>
	[Parameter]
	public MokaIconDefinition? Icon { get; set; }

	/// <summary>
	///     A glowing status dot before the text, in this color. Ignored when <see cref="Icon" /> or
	///     <see cref="Content" /> is set.
	/// </summary>
	[Parameter]
	public MokaColor? Dot { get; set; }

	/// <summary>
	///     Whether the text is drawn in the monospace font with figures of one width, for a value such as an address or a
	///     count. Default false.
	/// </summary>
	[Parameter]
	public bool Mono { get; set; }

	/// <summary>
	///     How this tooltip is drawn. Null takes the theme's look (<see cref="MokaTheme.WithTooltips" />);
	///     <see cref="MokaTooltipLook.Standard" /> keeps this one plain under a theme that picked another. Only the new
	///     design draws the looks: on <see cref="MokaTheme.Dark" /> and <see cref="MokaTheme.Light" /> it changes nothing.
	/// </summary>
	[Parameter]
	public MokaTooltipLook? Look { get; set; }

	/// <summary>
	///     Whether this tooltip draws an arrow pointing at its trigger. Null takes the theme's choice
	///     (<see cref="MokaTheme.WithTooltipArrows" />). The arrow follows the tooltip to the side the browser moves it to
	///     for room; where the browser can't say which side that is, the arrow isn't drawn. New design only.
	/// </summary>
	[Parameter]
	public bool? Arrow { get; set; }

	/// <summary>
	///     How this tooltip comes into view. Null takes the theme's motion (<see cref="MokaTheme.WithTooltipMotion" />).
	///     New design only.
	/// </summary>
	[Parameter]
	public MokaTooltipMotion? Motion { get; set; }

	/// <summary>
	///     How much room this tooltip's text takes. Null takes the theme's size (<see cref="MokaTheme.WithTooltipSize" />).
	///     New design only.
	/// </summary>
	[Parameter]
	public MokaTooltipSize? Size { get; set; }

	// Each trigger names itself as the anchor its popup is placed against. The name is per tooltip: with one
	// shared name every popup would be placed against whichever trigger carried it last.
	private readonly string _anchor = $"--moka-tooltip-{Guid.NewGuid():N}";

	/// <inheritdoc />
	protected override string RootClass => "moka-tooltip-trigger";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("anchor-name", _anchor)
		.AddStyle(Style)
		.Build();

	private bool HasText => !string.IsNullOrEmpty(Text);

	private bool HasTitle => !string.IsNullOrEmpty(Title);

	// The parts beyond plain text. Without them the popup holds only the text, as it always has.
	private bool IsRich => HasTitle || _keys.Count > 0 || Icon is not null || Dot is not null;

	private bool HasPopup => Content is not null || HasText || HasTitle || _keys.Count > 0;

	private bool Wraps => !string.IsNullOrWhiteSpace(MaxWidth) || (Content is null && HasTitle);

	private string PopupCss => new CssBuilder("moka-tooltip-popup")
		.AddClass($"moka-tooltip-popup--{MokaEnumHelpers.ToCssClass(Position)}")
		.AddClass("moka-tooltip-popup--wrap", Wraps)
		.AddClass("moka-tooltip-popup--mono", Mono)
		.AddClass(LookClass)
		.AddClass(ArrowClass)
		.AddClass(MotionClass)
		.AddClass(SizeClass)
		.Build();

	// The same classes the theme puts on its root, here on the popup itself, so they win over the root's: they only
	// set variables, and the nearest declaration of a variable is the one that counts.
	private string? LookClass => Look is { } look ? $"moka-tooltips-{MokaEnumHelpers.ToCssClass(look)}" : null;

	private string? ArrowClass => Arrow switch
	{
		true => "moka-tooltips-arrow",
		false => "moka-tooltips-no-arrow",
		null => null
	};

	private string? MotionClass => Motion is { } motion ? $"moka-tooltips-motion-{MokaEnumHelpers.ToCssClass(motion)}" : null;

	private string? SizeClass => Size is { } size ? $"moka-tooltips-size-{MokaEnumHelpers.ToCssClass(size)}" : null;

	// The delay is a variable the shown state reads, so only showing waits for it.
	private string? PopupStyle => new StyleBuilder()
		.AddStyle("position-anchor", _anchor)
		.AddStyle("--moka-tooltip-delay", $"{Delay}ms")
		.AddStyle("--moka-tooltip-max-width", MaxWidth, !string.IsNullOrWhiteSpace(MaxWidth))
		.Build();

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		if (!string.Equals(_keysFor, Shortcut, StringComparison.Ordinal))
		{
			_keysFor = Shortcut;
			_keys = ShortcutKeys.Split(Shortcut);
		}
	}
}
