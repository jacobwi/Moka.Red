using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Moka.Red.Core.Base;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.Typography;

/// <summary>
///     General-purpose text component that renders any HTML element
///     with typography styling via design tokens.
/// </summary>
public class MokaText : MokaVisualComponentBase
{
	/// <summary>Content to render inside the text element.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>The HTML element to render (e.g., "p", "span", "div"). Defaults to "p".</summary>
	[Parameter]
	public string Element { get; set; } = "p";

	/// <summary>Optional font weight override.</summary>
	[Parameter]
	public MokaFontWeight? Weight { get; set; }

	/// <summary>Optional text alignment.</summary>
	[Parameter]
	public MokaTextAlign? Align { get; set; }

	/// <summary>Optional text transformation (e.g., uppercase).</summary>
	[Parameter]
	public MokaTextTransform? Transform { get; set; }

	/// <summary>When true, renders text in italic.</summary>
	[Parameter]
	public bool Italic { get; set; }

	/// <summary>When true, applies text-overflow: ellipsis truncation.</summary>
	[Parameter]
	public bool Truncate { get; set; }

	/// <summary>When set with <see cref="Truncate" />, enables multi-line truncation via -webkit-line-clamp.</summary>
	[Parameter]
	public int? Lines { get; set; }

	/// <summary>When true, uses monospace font family.</summary>
	[Parameter]
	public bool Mono { get; set; }

	/// <summary>Custom line-height override (e.g., "1.8", "2rem").</summary>
	[Parameter]
	public string? Leading { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-text";

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("font-size", SizeValue ?? MokaEnumHelpers.ToFontSize(Size))
		.AddStyle("font-weight", Weight.HasValue ? MokaEnumHelpers.ToCssValue(Weight.Value) : null)
		.AddStyle("text-align", Align.HasValue ? MokaEnumHelpers.ToCssValue(Align.Value) : null)
		.AddStyle("text-transform", Transform.HasValue ? MokaEnumHelpers.ToCssValue(Transform.Value) : null)
		.AddStyle("font-style", "italic", Italic)
		.AddStyle("font-family", "var(--moka-font-family-mono)", Mono)
		.AddStyle("color", Color.HasValue ? $"var(--moka-color-{ColorToKebab(Color.Value)})" : null)
		.AddStyle("line-height", Leading)
		.AddStyle("-webkit-line-clamp", Lines?.ToString(CultureInfo.InvariantCulture), Truncate && Lines.HasValue)
		.AddStyle("margin", ResolvedMargin)
		.AddStyle("padding", ResolvedPadding)
		.AddStyle(Style)
		.Build();

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-text--truncate", Truncate && !Lines.HasValue)
		.AddClass("moka-text--clamp", Truncate && Lines.HasValue)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		ArgumentNullException.ThrowIfNull(builder);

		builder.OpenElement(0, Element);
		builder.AddAttribute(1, "class", CssClass);

		if (CssStyle is not null)
		{
			builder.AddAttribute(2, "style", CssStyle);
		}

		if (Id is not null)
		{
			builder.AddAttribute(3, "id", Id);
		}

		builder.AddMultipleAttributes(4, AdditionalAttributes);
		builder.AddContent(5, ChildContent);
		builder.CloseElement();
	}
}
