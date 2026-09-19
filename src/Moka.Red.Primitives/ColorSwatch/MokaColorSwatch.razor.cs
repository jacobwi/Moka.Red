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
	private string? _lastSelectedColor;
	private string? _selectedColor;
	private bool _showCustomInput;

	/// <summary>
	///     The colors to display as swatches: hex values, color keywords or color functions such as
	///     <c>rgb()</c> or <c>var()</c>. A value that is not a CSS color gets an empty swatch.
	/// </summary>
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
	protected override string? CssStyle => SpacingStyle()
		.AddStyle("--moka-swatch-columns", Columns.ToString(CultureInfo.InvariantCulture))
		.AddStyle("--moka-swatch-size", SwatchSize)
		.AddStyle(Style)
		.Build();

	// The color goes into a declaration, where a semicolon or a parenthesis would end it.
	private static string? SwatchStyle(string? color) => new StyleBuilder()
		.AddStyle("background-color", CssValues.IsColor(color) ? color.Trim() : null)
		.Build();

	private string ShapeClass => Shape == MokaSwatchShape.Circle
		? "moka-color-swatch-item--circle"
		: "moka-color-swatch-item--square";

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		// SelectedColor only seeds the selection when the parent passes a new value, so a parent render
		// that passes the old one again cannot undo the user's pick.
		if (!string.Equals(_lastSelectedColor, SelectedColor, StringComparison.Ordinal))
		{
			_lastSelectedColor = SelectedColor;
			_selectedColor = SelectedColor;
		}
	}

	private bool IsSelected(string color) => color.Equals(_selectedColor, StringComparison.OrdinalIgnoreCase);

	private string ItemCssClass(string color) => new CssBuilder("moka-color-swatch-item")
		.AddClass(ShapeClass)
		.AddClass("moka-color-swatch-item--selected", IsSelected(color))
		.Build();

	private async Task HandleSelect(string color)
	{
		_selectedColor = color;
		if (SelectedColorChanged.HasDelegate)
		{
			await SelectedColorChanged.InvokeAsync(color);
		}
	}

	private void HandleAddCustomClick()
	{
		_showCustomInput = true;
		_customValue = _selectedColor ?? "#000000";
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
