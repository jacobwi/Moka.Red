using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.ColorSwatch;

/// <summary>
///     A grid of color swatches for color selection.
/// </summary>
public partial class MokaColorSwatch
{
	private string _customValue = "#000000";
	private bool _showCustomInput;

	/// <summary>The colors to display as swatches (hex values).</summary>
	[Parameter]
	public IEnumerable<string> Colors { get; set; } = [];

	/// <summary>Currently selected color. Two-way bindable.</summary>
	[Parameter]
	public string? SelectedColor { get; set; }

	/// <summary>Callback when selected color changes.</summary>
	[Parameter]
	public EventCallback<string?> SelectedColorChanged { get; set; }

	/// <summary>Number of columns in the grid. Default 8.</summary>
	[Parameter]
	public int Columns { get; set; } = 8;

	/// <summary>Size of each swatch (CSS value). Default "24px".</summary>
	[Parameter]
	public string SwatchSize { get; set; } = "24px";

	/// <summary>Shape of the swatches. Default Circle.</summary>
	[Parameter]
	public MokaSwatchShape Shape { get; set; } = MokaSwatchShape.Circle;

	/// <summary>Show a + button to add custom colors. Default false.</summary>
	[Parameter]
	public bool AllowCustom { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-color-swatch";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("--moka-swatch-columns", Columns.ToString(CultureInfo.InvariantCulture))
		.AddStyle("--moka-swatch-size", SwatchSize)
		.AddStyle("margin", ResolvedMargin)
		.AddStyle("padding", ResolvedPadding)
		.AddStyle(Style)
		.Build();

	private string ShapeClass => Shape == MokaSwatchShape.Circle
		? "moka-color-swatch-item--circle"
		: "moka-color-swatch-item--square";

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	private async Task HandleSelect(string color)
	{
		SelectedColor = color;
		if (SelectedColorChanged.HasDelegate)
		{
			await SelectedColorChanged.InvokeAsync(color);
		}
	}

	private void HandleAddCustomClick()
	{
		_showCustomInput = true;
		_customValue = SelectedColor ?? "#000000";
	}

	private async Task HandleCustomConfirm()
	{
		if (!string.IsNullOrWhiteSpace(_customValue))
		{
			await HandleSelect(_customValue);
			_showCustomInput = false;
		}
	}
}
