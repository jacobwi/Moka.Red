using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Theming;
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

	/// <summary>
	///     The small tag that flags something as new: accent tint, tight corners, bold tracked text. Set it and
	///     <c>Variant</c>, <c>Color</c> and <c>Size</c> are ignored.
	/// </summary>
	[Parameter]
	public bool New { get; set; }

	/// <summary>Draws the text in the mono font, for addresses, versions, hashes and other technical values.</summary>
	[Parameter]
	public bool Mono { get; set; }

	/// <summary>
	///     A color someone gave this tag: its tint behind readable ink, and a dot in its color before the text when there
	///     is no <see cref="Icon" />. It replaces <c>Color</c>. <c>Variant</c> still shapes it: <see cref="MokaVariant.Soft" />
	///     and <see cref="MokaVariant.Filled" /> draw the tint, <see cref="MokaVariant.Outlined" /> a border in the label's
	///     color on no background, and <see cref="MokaVariant.Text" /> the ink alone. Ignored on a <see cref="New" /> tag.
	///     The shades follow the theme, light or dark, and <c>MokaTheme.WithLabelTone</c>.
	/// </summary>
	[Parameter]
	public MokaLabelColor? Label { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-tag";

	private MokaColor ResolvedColor => Color ?? MokaColor.Surface;

	private bool IsLabeled => Label is not null && !New;

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-tag--{VariantToKebab(Variant)}", !New)
		.AddClass($"moka-tag--{ColorToKebab(ResolvedColor)}", !New && !IsLabeled)
		.AddClass("moka-tag--label", IsLabeled)
		.AddClass($"moka-tag--{SizeToKebab(Size)}", !New)
		.AddClass("moka-tag--pill", Pill)
		.AddClass("moka-tag--new", New)
		.AddClass("moka-tag--mono", Mono)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin)
		.AddStyle("padding", ResolvedPadding)
		.AddStyle("border-radius", ResolvedRounding)
		.AddStyle("--_tag-label", IsLabeled ? MokaLabelColors.Solid(Label!.Value) : null)
		.AddStyle("--_tag-label-bg", IsLabeled ? MokaLabelColors.Background(Label!.Value) : null)
		.AddStyle("--_tag-label-ink", IsLabeled ? MokaLabelColors.Ink(Label!.Value) : null)
		.AddStyle(Style)
		.Build();

	/// <summary>Maps tag size to a slightly smaller icon size.</summary>
	private MokaSize IconSize => New ? MokaSize.Xs : Size switch
	{
		MokaSize.Xs => MokaSize.Xs,
		MokaSize.Sm => MokaSize.Xs,
		MokaSize.Md => MokaSize.Sm,
		MokaSize.Lg => MokaSize.Sm,
		_ => MokaSize.Xs
	};
}
