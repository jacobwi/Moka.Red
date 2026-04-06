using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Core.Base;

/// <summary>
///     Abstract base class for form input components that share common appearance parameters
///     such as size, color, variant, and disabled state.
///     Extends <see cref="MokaInputBase{TValue}" /> with shared visual properties and helpers.
/// </summary>
/// <typeparam name="TValue">The type of the input value.</typeparam>
public abstract class MokaVisualInputBase<TValue> : MokaInputBase<TValue>
{
	/// <summary>Size from the standard scale.</summary>
	[Parameter]
	public MokaSize Size { get; set; } = MokaSize.Md;

	/// <summary>
	///     Custom size value that overrides <see cref="Size" /> when set.
	///     Accepts any CSS size string (e.g., "18px", "1.5rem").
	/// </summary>
	[Parameter]
	public string? SizeValue { get; set; }

	/// <summary>Semantic color from the theme palette.</summary>
	[Parameter]
	public MokaColor? Color { get; set; }

	/// <summary>Visual style variant.</summary>
	[Parameter]
	public MokaVariant Variant { get; set; } = MokaVariant.Filled;

	/// <summary>Whether the component is disabled.</summary>
	[Parameter]
	public bool Disabled { get; set; }

	/// <summary>Margin from the spacing scale. Null = no margin applied.</summary>
	[Parameter]
	public MokaSpacingScale? Margin { get; set; }

	/// <summary>Custom margin value that overrides <see cref="Margin" />. Any CSS margin value.</summary>
	[Parameter]
	public string? MarginValue { get; set; }

	/// <summary>Padding from the spacing scale. Null = no padding applied.</summary>
	[Parameter]
	public MokaSpacingScale? Padding { get; set; }

	/// <summary>Custom padding value that overrides <see cref="Padding" />. Any CSS padding value.</summary>
	[Parameter]
	public string? PaddingValue { get; set; }

	/// <summary>
	///     Border radius from the rounding scale. Null = component uses its own CSS default.
	///     When set, applies border-radius to the root element.
	/// </summary>
	[Parameter]
	public MokaRounding? Rounded { get; set; }

	/// <summary>
	///     Custom border-radius value that overrides <see cref="Rounded" />.
	///     Accepts any CSS border-radius value (e.g., "8px", "50%", "1rem 0").
	/// </summary>
	[Parameter]
	public string? RoundedValue { get; set; }

	/// <summary>
	///     Gets the resolved size as a CSS value.
	///     Returns <see cref="SizeValue" /> if set, otherwise maps <see cref="Size" /> enum to pixels.
	/// </summary>
	protected string ResolvedSize => SizeValue ?? MapSizeToPx(Size);

	/// <summary>Resolved margin CSS value. Returns MarginValue if set, Margin enum mapping if set, null otherwise.</summary>
	protected string? ResolvedMargin =>
		MarginValue ?? (Margin.HasValue ? MokaEnumHelpers.ToCssValue(Margin.Value) : null);

	/// <summary>Resolved padding CSS value. Returns PaddingValue if set, Padding enum mapping if set, null otherwise.</summary>
	protected string? ResolvedPadding =>
		PaddingValue ?? (Padding.HasValue ? MokaEnumHelpers.ToCssValue(Padding.Value) : null);

	/// <summary>Resolved border-radius CSS value. Returns RoundedValue if set, Rounded enum mapping if set, null otherwise.</summary>
	protected string? ResolvedRounding =>
		RoundedValue ?? (Rounded.HasValue ? MokaEnumHelpers.ToCssValue(Rounded.Value) : null);

	/// <summary>Maps a <see cref="MokaSize" /> enum to a pixel string.</summary>
	protected static string MapSizeToPx(MokaSize size) => MokaEnumHelpers.ToPixels(size);

	/// <summary>Converts <see cref="MokaSize" /> to kebab-case for CSS class names.</summary>
	protected static string SizeToKebab(MokaSize size) => MokaEnumHelpers.ToCssClass(size);

	/// <summary>Converts <see cref="MokaColor" /> to kebab-case for CSS class names.</summary>
	protected static string ColorToKebab(MokaColor color) => MokaEnumHelpers.ToCssClass(color);

	/// <summary>Converts <see cref="MokaVariant" /> to kebab-case for CSS class names.</summary>
	protected static string VariantToKebab(MokaVariant variant) => MokaEnumHelpers.ToCssClass(variant);
}
