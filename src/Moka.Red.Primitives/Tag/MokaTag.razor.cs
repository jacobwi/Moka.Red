using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.Tag;

/// <summary>
///     A simple non-interactive label tag for categorization and status display.
///     Unlike <c>MokaChip</c>, tags are purely visual with no click or close behavior.
///     Unlike <c>MokaAttribute</c>, tags display a single text label rather than a key-value pair.
/// </summary>
public partial class MokaTag
{
	/// <inheritdoc />
	/// <remarks>Tags default to <see cref="MokaSize.Sm" /> for compact inline display.</remarks>
	public override MokaSize Size { get; set; } = MokaSize.Sm;

	/// <inheritdoc />
	/// <remarks>Tags default to <see cref="MokaVariant.Soft" /> for subtle appearance.</remarks>
	public override MokaVariant Variant { get; set; } = MokaVariant.Soft;

	/// <summary>The tag label text.</summary>
	[Parameter]
	[EditorRequired]
	public string Text { get; set; } = string.Empty;

	/// <summary>Optional leading icon.</summary>
	[Parameter]
	public MokaIconDefinition? Icon { get; set; }

	/// <summary>
	///     Whether to use a fully rounded (pill) shape. Defaults to false.
	///     When false, uses the standard border radius.
	/// </summary>
	[Parameter]
	public bool Pill { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-tag";

	private MokaColor ResolvedColor => Color ?? MokaColor.Surface;

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-tag--{VariantToKebab(Variant)}")
		.AddClass($"moka-tag--{ColorToKebab(ResolvedColor)}")
		.AddClass($"moka-tag--{SizeToKebab(Size)}")
		.AddClass("moka-tag--pill", Pill)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin)
		.AddStyle("padding", ResolvedPadding)
		.AddStyle("border-radius", ResolvedRounding)
		.AddStyle(Style)
		.Build();

	/// <summary>Maps tag size to a slightly smaller icon size.</summary>
	private MokaSize IconSize => Size switch
	{
		MokaSize.Xs => MokaSize.Xs,
		MokaSize.Sm => MokaSize.Xs,
		MokaSize.Md => MokaSize.Sm,
		MokaSize.Lg => MokaSize.Sm,
		_ => MokaSize.Xs
	};
}
