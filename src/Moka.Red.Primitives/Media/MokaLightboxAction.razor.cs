using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Base;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Utilities;
using Moka.Red.Feedback.Tooltip;

namespace Moka.Red.Primitives.Media;

/// <summary>
///     One action in a <see cref="MokaLightbox" />'s <see cref="MokaLightbox.Actions" /> row, such as Download or Share:
///     a round icon button in the look of the lightbox's own buttons, named by <see cref="Text" /> through
///     <c>aria-label</c> and a <see cref="MokaTooltip" />. With <see cref="Href" /> it is a link, which with
///     <see cref="Download" /> saves what it points at. It takes its look from the lightbox around it.
/// </summary>
public partial class MokaLightboxAction : MokaVisualComponentBase
{
	// Set by the lightbox's stylesheet, so the icon follows the buttons' size; 1.5em where nothing sets it.
	private const string IconSize = "var(--_lightbox-icon, 1.5em)";

	/// <summary>The icon it shows. Required.</summary>
	[Parameter]
	[EditorRequired]
	public MokaIconDefinition Icon { get; set; }

	/// <summary>What it does, such as "Download". Its name for screen readers and the text of its tooltip. Required.</summary>
	[Parameter]
	[EditorRequired]
	public string Text { get; set; } = "";

	/// <summary>Raised when it is pressed, unless it is <see cref="Busy" /> or disabled. Works on a link too.</summary>
	[Parameter]
	public EventCallback<MouseEventArgs> OnClick { get; set; }

	/// <summary>
	///     Makes it a link to this address, such as the full image. Addresses that would run script
	///     (<c>javascript:</c>, <c>vbscript:</c>, <c>data:</c>) are dropped, leaving a button. A disabled or busy action is
	///     a button.
	/// </summary>
	[Parameter]
	public string? Href { get; set; }

	/// <summary>
	///     With <see cref="Href" />, saves what the link points at rather than opening it: the file name to save it as, or
	///     an empty string for the name the address gives. Null, the default, is an ordinary link.
	/// </summary>
	[Parameter]
	public string? Download { get; set; }

	/// <summary>
	///     Whether its work is under way, such as a download being prepared. It shows a turning ring in place of its icon
	///     and ignores presses, and stays focusable, so the keyboard keeps its place.
	/// </summary>
	[Parameter]
	public bool Busy { get; set; }

	/// <summary>Where its tooltip opens. Default <see cref="MokaTooltipPosition.Bottom" />, below the row.</summary>
	[Parameter]
	public MokaTooltipPosition TooltipPosition { get; set; } = MokaTooltipPosition.Bottom;

	/// <inheritdoc />
	protected override string RootClass => "moka-lightbox-action";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-lightbox-action--busy", Busy)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin)
		.AddStyle("padding", ResolvedPadding)
		.AddStyle("border-radius", ResolvedRounding)
		.AddStyle(Style)
		.Build();

	private string? LinkHref => Disabled || Busy || string.IsNullOrWhiteSpace(Href) ? null : UrlValues.SafeHref(Href);

	// No listener at all while nothing listens or a press would be ignored.
	private EventCallback<MouseEventArgs> ClickHandler => Busy || Disabled ? default : OnClick;
}
