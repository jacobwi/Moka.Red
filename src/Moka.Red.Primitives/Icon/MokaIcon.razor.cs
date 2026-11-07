using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.Icon;

/// <summary>
///     Renders an inline SVG icon from a <see cref="MokaIconDefinition" />.
///     Supports size and color variants via CSS classes.
/// </summary>
public partial class MokaIcon
{
	private RenderInputs? _rendered;

	/// <summary>The icon definition to render.</summary>
	[Parameter]
	[EditorRequired]
	public MokaIconDefinition Icon { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-icon";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-icon--{SizeToKebab(Size)}")
		.AddClass(Color.HasValue ? $"moka-icon--{ColorToKebab(Color.Value)}" : null)
		.AddClass(Class)
		.Build();

	// An icon is foreground: Surface means the text colour on a surface. The surface colour itself
	// would draw the icon in the colour of the background behind it.
	private string? IconColor => Color switch
	{
		null => null,
		MokaColor.Surface => "var(--moka-color-on-surface)",
		MokaColor c => $"var(--moka-color-{ColorToKebab(c)})"
	};

	/// <inheritdoc />
	protected override string? CssStyle => SpacingStyle()
		.AddStyle("width", ResolvedSize)
		.AddStyle("height", ResolvedSize)
		.AddStyle("color", IconColor)
		.AddStyle(Style)
		.Build();

	// Blazor can't tell that a struct or enum parameter is unchanged, so every parent render lands here. A
	// page holds thousands of icons and nothing about one changes without a parameter changing.
	// MokaIconDefinition's own equality skips the path and view box, so they're compared one by one, and
	// extra attributes can hold anything, including a new delegate, so they always render.
	private RenderInputs CurrentInputs => new(Icon.Name, Icon.SvgPath, Icon.ViewBox, Icon.Filled, Size, SizeValue, Color,
		Class, Style, Id, Margin, MarginValue, Padding, PaddingValue, Rounded, RoundedValue);

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();
		_rendered ??= CurrentInputs;
	}

	/// <inheritdoc />
	protected override bool ShouldRender()
	{
		RenderInputs current = CurrentInputs;
		if (AdditionalAttributes is null && current == _rendered)
		{
			return false;
		}

		_rendered = current;
		return base.ShouldRender();
	}

	private readonly record struct RenderInputs(
		string? Name,
		string? SvgPath,
		string? ViewBox,
		bool Filled,
		MokaSize Size,
		string? SizeValue,
		MokaColor? Color,
		string? Class,
		string? Style,
		string? Id,
		MokaSpacingScale? Margin,
		string? MarginValue,
		MokaSpacingScale? Padding,
		string? PaddingValue,
		MokaRounding? Rounded,
		string? RoundedValue);
}
