using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.Typography;

/// <summary>
///     Anchor link component with underline behavior, color, and external link support.
///     Renders a semantic &lt;a&gt; element, or a &lt;button&gt; drawn as a link when it has an
///     <see cref="OnClick" /> and no <see cref="Href" />.
/// </summary>
public partial class MokaLink
{
	/// <summary>Content to render inside the link.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>
	///     The URL the link points to. Left unset, the link points to <c>#</c>, unless it has an <see cref="OnClick" />,
	///     which makes it a button. A <c>javascript:</c>, <c>vbscript:</c> or <c>data:</c> URL is not rendered, so the
	///     link has no <c>href</c> (see <see cref="UrlValues.HasBlockedScheme" />).
	/// </summary>
	[Parameter]
	public string? Href { get; set; }

	/// <summary>
	///     Raised on a click. Without an <see cref="Href" /> the link is a <c>&lt;button type="button"&gt;</c> that looks
	///     like the link: inline, no box, the surrounding font, the link color and <see cref="Underline" />, with a focus
	///     ring. <c>Disabled</c> disables it. With an <see cref="Href" /> it stays an anchor that also raises this.
	/// </summary>
	[Parameter]
	public EventCallback<MouseEventArgs> OnClick { get; set; }

	/// <summary>When true, opens link in a new tab with rel="noopener noreferrer".</summary>
	[Parameter]
	public bool External { get; set; }

	/// <summary>Controls underline behavior. Defaults to Hover.</summary>
	[Parameter]
	public MokaLinkUnderline Underline { get; set; } = MokaLinkUnderline.Hover;

	/// <summary>Optional font weight override.</summary>
	[Parameter]
	public MokaFontWeight? Weight { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-link";

	private bool IsButton => Href is null && OnClick.HasDelegate;

	private string? LinkHref => UrlValues.SafeHref(Href ?? "#");

	// No listener at all on a plain link, since every listener costs a round trip.
	private EventCallback<MouseEventArgs> LinkClickHandler => OnClick.HasDelegate
		? EventCallback.Factory.Create<MouseEventArgs>(this, HandleClickAsync)
		: default;

	/// <inheritdoc />
	protected override string? CssStyle => SpacingStyle()
		.AddStyle("font-size", SizeValue ?? MokaEnumHelpers.ToFontSize(Size))
		.AddStyle("font-weight", Weight.HasValue ? MokaEnumHelpers.ToCssValue(Weight.Value) : null)
		.AddStyle("color", Color.HasValue ? $"var(--moka-color-{ColorToKebab(Color.Value)})" : null)
		.AddStyle(Style)
		.Build();

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-link--underline-{MokaEnumHelpers.ToCssClass(Underline)}")
		.AddClass("moka-link--button", IsButton)
		.AddClass(Class)
		.Build();

	private Task HandleClickAsync(MouseEventArgs args) =>
		IsButton && Disabled ? Task.CompletedTask : OnClick.InvokeAsync(args);
}
