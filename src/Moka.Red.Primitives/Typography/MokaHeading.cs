using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Moka.Red.Core.Base;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.Typography;

/// <summary>
///     Heading component that renders &lt;h1&gt; through &lt;h6&gt; elements
///     with automatic font size and weight defaults based on level.
/// </summary>
public class MokaHeading : MokaVisualComponentBase
{
	/// <summary>Content to render inside the heading element.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Heading level (1-6). Renders the corresponding &lt;h1&gt;-&lt;h6&gt; element. Defaults to 2.</summary>
	[Parameter]
	public int Level { get; set; } = 2;

	/// <summary>Optional font weight override. Defaults vary by level.</summary>
	[Parameter]
	public MokaFontWeight? Weight { get; set; }

	/// <summary>Optional text alignment.</summary>
	[Parameter]
	public MokaTextAlign? Align { get; set; }

	/// <summary>When true, applies text-overflow: ellipsis truncation.</summary>
	[Parameter]
	public bool Truncate { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-heading";

	private string ResolvedElement => Level switch
	{
		1 => "h1",
		2 => "h2",
		3 => "h3",
		4 => "h4",
		5 => "h5",
		6 => "h6",
		_ => "h2"
	};

	private string DefaultFontSize => Level switch
	{
		1 => "var(--moka-font-size-xxl)",
		2 => "var(--moka-font-size-xl)",
		3 => "var(--moka-font-size-lg)",
		4 => "var(--moka-font-size-md)",
		5 => "var(--moka-font-size-base)",
		6 => "var(--moka-font-size-sm)",
		_ => "var(--moka-font-size-xl)"
	};

	private string DefaultFontWeight => Level switch
	{
		1 or 2 or 3 => "var(--moka-font-weight-bold)",
		_ => "var(--moka-font-weight-semibold)"
	};

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("font-size", SizeValue ?? DefaultFontSize)
		.AddStyle("font-weight", Weight.HasValue ? MokaEnumHelpers.ToCssValue(Weight.Value) : DefaultFontWeight)
		.AddStyle("line-height", "var(--moka-line-height-tight)")
		.AddStyle("text-align", Align.HasValue ? MokaEnumHelpers.ToCssValue(Align.Value) : null)
		.AddStyle("color", Color.HasValue ? $"var(--moka-color-{ColorToKebab(Color.Value)})" : null)
		.AddStyle("margin", ResolvedMargin ?? "0")
		.AddStyle("padding", ResolvedPadding)
		.AddStyle(Style)
		.Build();

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-heading--truncate", Truncate)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		ArgumentNullException.ThrowIfNull(builder);

		builder.OpenElement(0, ResolvedElement);
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
