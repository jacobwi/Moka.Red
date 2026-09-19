using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.StatusDot;

/// <summary>
///     The signature Moka glow-dot status indicator: a small colored dot with an optional
///     label, e.g. "ONLINE" or "saving...". Supports a pulse animation for in-progress
///     states and a soft currentColor glow ring.
///     Distinct from <c>MokaBadge</c>'s Dot mode, which overlays a positioned dot on top of
///     other content; MokaStatusDot is a standalone inline status element.
/// </summary>
public partial class MokaStatusDot : MokaVisualComponentBase
{
	/// <summary>Optional text rendered next to the dot (e.g. "ONLINE", "saving...").</summary>
	[Parameter]
	public string? Label { get; set; }

	/// <summary>Whether the dot pulses (scale/opacity, 1.4s ease-in-out infinite). Defaults to false.</summary>
	[Parameter]
	public bool Pulse { get; set; }

	/// <summary>Whether the dot casts a soft currentColor glow. Defaults to true.</summary>
	[Parameter]
	public bool Glow { get; set; } = true;

	/// <summary>
	///     Whether the label renders in the uppercase micro-label style
	///     (extra-small, semibold, wide letter-spacing). Defaults to true.
	///     Set to false to render the label as-is (e.g. "saving...").
	/// </summary>
	[Parameter]
	public bool Uppercase { get; set; } = true;

	/// <summary>
	///     Whether screen readers announce changes to <see cref="Label" />, by making the dot a polite
	///     live region (<c>role="status"</c>). Defaults to false. Turn it on for a single status that
	///     changes while the user works, such as "saving..."; leave it off for dots in lists and
	///     tables, where many live regions would talk over each other.
	/// </summary>
	[Parameter]
	public bool Live { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-status-dot";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-status-dot--{SizeToKebab(Size)}")
		.AddClass($"moka-status-dot--{ColorToKebab(ResolvedColor)}")
		.AddClass("moka-status-dot--pulse", Pulse)
		.AddClass("moka-status-dot--glow", Glow)
		.AddClass("moka-status-dot--uppercase", Uppercase)
		.AddClass(Class)
		.Build();

	// The root only lines the dot up with its label and draws nothing, so a radius there would not
	// show. It shapes the dot, so Rounded can square it off. Margin and padding stay on the root.

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("--moka-status-dot-size", SizeValue)
		.AddStyle("margin", ResolvedMargin)
		.AddStyle("padding", ResolvedPadding)
		.AddStyle(Style)
		.Build();

	private string? DotStyle => new StyleBuilder()
		.AddStyle("border-radius", ResolvedRounding)
		.Build();

	private MokaColor ResolvedColor => Color ?? MokaColor.Success;

	private bool HasLabel => !string.IsNullOrEmpty(Label);

	private string? Role
	{
		get
		{
			if (Live)
			{
				return "status";
			}

			// A plain span cannot carry a name, so a dot without a label that the consumer named
			// with aria-label or aria-labelledby becomes an image with that name. An unnamed one
			// stays decoration.
			return !HasLabel && IsNamedByAttribute ? "img" : null;
		}
	}

	private bool IsNamedByAttribute =>
		HasTextAttribute("aria-label") || HasTextAttribute("aria-labelledby");

	private bool HasTextAttribute(string name) =>
		AdditionalAttributes?.TryGetValue(name, out object? value) == true
		&& value is string text
		&& !string.IsNullOrWhiteSpace(text);
}
