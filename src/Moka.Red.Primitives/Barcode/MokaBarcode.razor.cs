using System.Globalization;
using System.Security;
using System.Text;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.Barcode;

/// <summary>
///     Generates and renders a 1D barcode as inline SVG. Supports Code 128B encoding.
/// </summary>
public partial class MokaBarcode : MokaVisualComponentBase
{
	private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
	private string? _cachedBg;
	private string? _cachedFg;
	private MokaBarcodeFormat? _cachedFormat;
	private int _cachedHeight;
	private bool _cachedShowText;
	private string? _cachedTextSize;
	private string? _cachedValue;
	private int _cachedWidth;

	private string _svgCache = "";

	/// <summary>The data to encode. Required.</summary>
	[Parameter]
	[EditorRequired]
	public string Value { get; set; } = "";

	/// <summary>Barcode symbology. Default Code128.</summary>
	[Parameter]
	public MokaBarcodeFormat BarcodeFormat { get; set; } = MokaBarcodeFormat.Code128;

	/// <summary>Width of the barcode SVG in pixels. Default 200.</summary>
	[Parameter]
	public int BarcodeWidth { get; set; } = 200;

	/// <summary>Height of the barcode SVG in pixels. Default 80.</summary>
	[Parameter]
	public int BarcodeHeight { get; set; } = 80;

	/// <summary>Foreground (bar) color. Default "#000000".</summary>
	[Parameter]
	public string ForegroundColor { get; set; } = "#000000";

	/// <summary>Background color. Default "#ffffff".</summary>
	[Parameter]
	public string BackgroundColor { get; set; } = "#ffffff";

	/// <summary>Whether to show the encoded text below the barcode. Default true.</summary>
	[Parameter]
	public bool ShowText { get; set; } = true;

	/// <summary>Font size for the text below the barcode. Default "12px".</summary>
	[Parameter]
	public string TextSize { get; set; } = "12px";

	/// <inheritdoc />
	protected override string RootClass => "moka-barcode";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		if (_cachedValue != Value || _cachedFormat != BarcodeFormat
		                          || _cachedFg != ForegroundColor || _cachedBg != BackgroundColor
		                          || _cachedWidth != BarcodeWidth || _cachedHeight != BarcodeHeight
		                          || _cachedShowText != ShowText || _cachedTextSize != TextSize)
		{
			_cachedValue = Value;
			_cachedFormat = BarcodeFormat;
			_cachedFg = ForegroundColor;
			_cachedBg = BackgroundColor;
			_cachedWidth = BarcodeWidth;
			_cachedHeight = BarcodeHeight;
			_cachedShowText = ShowText;
			_cachedTextSize = TextSize;
			GenerateSvg();
		}
	}

	private void GenerateSvg()
	{
		if (string.IsNullOrEmpty(Value))
		{
			_svgCache = "";
			return;
		}

		try
		{
			bool[] modules = BarcodeFormat switch
			{
				MokaBarcodeFormat.Code128 => BarcodeGenerator.GenerateCode128(Value),
				_ => BarcodeGenerator.GenerateCode128(Value)
			};

			int textHeight = ShowText ? 18 : 0;
			int barHeight = BarcodeHeight - textHeight;
			double moduleWidth = (double)BarcodeWidth / modules.Length;

			var sb = new StringBuilder(modules.Length * 30);
			sb.Append(Inv,
				$"<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 {BarcodeWidth} {BarcodeHeight}' width='{BarcodeWidth}' height='{BarcodeHeight}'>");
			sb.Append(Inv, $"<rect width='{BarcodeWidth}' height='{BarcodeHeight}' fill='{BackgroundColor}'/>");

			// Render bars using run-length encoding for efficiency
			double x = 0.0;
			int i = 0;
			while (i < modules.Length)
			{
				if (modules[i])
				{
					double start = x;
					while (i < modules.Length && modules[i])
					{
						x += moduleWidth;
						i++;
					}

					double width = x - start;
					sb.Append(Inv,
						$"<rect x='{start:F2}' y='0' width='{width:F2}' height='{barHeight}' fill='{ForegroundColor}'/>");
				}
				else
				{
					x += moduleWidth;
					i++;
				}
			}

			if (ShowText)
			{
				sb.Append(Inv,
					$"<text x='{BarcodeWidth / 2}' y='{BarcodeHeight - 3}' text-anchor='middle' font-family='monospace' font-size='{TextSize}' fill='{ForegroundColor}'>");
				sb.Append(SecurityElement.Escape(Value));
				sb.Append("</text>");
			}

			sb.Append("</svg>");
			_svgCache = sb.ToString();
		}
		catch (ArgumentException ex)
		{
			_svgCache = string.Create(Inv,
				            $"<svg xmlns='http://www.w3.org/2000/svg' width='{BarcodeWidth}' height='{BarcodeHeight}'>")
			            + string.Create(Inv,
				            $"<rect width='{BarcodeWidth}' height='{BarcodeHeight}' fill='{BackgroundColor}'/>")
			            + string.Create(Inv,
				            $"<text x='50%' y='50%' text-anchor='middle' dominant-baseline='middle' fill='{ForegroundColor}' font-size='12'>{SecurityElement.Escape(ex.Message)}</text>")
			            + "</svg>";
		}
	}
}
