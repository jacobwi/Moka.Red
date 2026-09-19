using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.Barcode;

/// <summary>
///     Generates and renders a 1D barcode as inline SVG.
///     Supports Code 128 Subset B, Code 39, EAN-13, EAN-8 and UPC-A.
/// </summary>
public partial class MokaBarcode : MokaVisualComponentBase
{
	private const string DefaultForeground = "#000000";
	private const string DefaultBackground = "#ffffff";
	private const string DefaultTextSize = "12px";

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

	/// <summary>
	///     The data to encode. Required. EAN-13 takes 12 or 13 digits, EAN-8 takes 7 or 8,
	///     UPC-A takes 11 or 12; the trailing check digit is computed when omitted and verified
	///     when supplied. Invalid data renders an inline error message instead of a symbol.
	/// </summary>
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

	/// <summary>
	///     Foreground (bar and text) color: a hex value, a color keyword or a color function such as
	///     <c>rgb()</c> or <c>var()</c>. Anything else draws the default. Default "#000000".
	/// </summary>
	[Parameter]
	public string ForegroundColor { get; set; } = DefaultForeground;

	/// <summary>Background color, with the same rules as <see cref="ForegroundColor" />. Default "#ffffff".</summary>
	[Parameter]
	public string BackgroundColor { get; set; } = DefaultBackground;

	/// <summary>
	///     Whether to show the encoded text below the barcode. Default true.
	///     EAN and UPC print the normalized code including its check digit.
	/// </summary>
	[Parameter]
	public bool ShowText { get; set; } = true;

	/// <summary>
	///     Font size for the text below the barcode: a number with an optional CSS unit, such as "14px" or
	///     "0.8rem". Anything else uses the default. Default "12px".
	/// </summary>
	[Parameter]
	public string TextSize { get; set; } = DefaultTextSize;

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

		// The markup is rendered raw, so every string goes in checked and escaped.
		string foreground = CssValues.EscapeXml(CssValues.ColorOrDefault(ForegroundColor, DefaultForeground));
		string background = CssValues.EscapeXml(CssValues.ColorOrDefault(BackgroundColor, DefaultBackground));
		string textSize = CssValues.EscapeXml(CssValues.LengthOrDefault(TextSize, DefaultTextSize));

		try
		{
			bool[] modules = BarcodeGenerator.Generate(BarcodeFormat, Value);
			string displayText = BarcodeGenerator.GetDisplayText(BarcodeFormat, Value);
			(int quietLeft, int quietRight) = BarcodeGenerator.GetQuietZone(BarcodeFormat);

			int textHeight = ShowText ? 18 : 0;
			int barHeight = Math.Max(1, BarcodeHeight - textHeight);

			// Quiet zones are part of the symbol: a barcode printed flush to the canvas edge cannot be read.
			int totalModules = quietLeft + modules.Length + quietRight;
			double moduleWidth = (double)BarcodeWidth / totalModules;

			var sb = new StringBuilder(modules.Length * 30);
			sb.Append(Inv,
				$"<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 {BarcodeWidth} {BarcodeHeight}' width='{BarcodeWidth}' height='{BarcodeHeight}'>");
			sb.Append(Inv, $"<rect width='{BarcodeWidth}' height='{BarcodeHeight}' fill='{background}'/>");

			// Render bars using run-length encoding for efficiency
			double x = quietLeft * moduleWidth;
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
						$"<rect x='{start:F2}' y='0' width='{width:F2}' height='{barHeight}' fill='{foreground}'/>");
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
					$"<text x='{BarcodeWidth / 2}' y='{BarcodeHeight - 3}' text-anchor='middle' font-family='monospace' font-size='{textSize}' fill='{foreground}'>");
				sb.Append(CssValues.EscapeXml(displayText));
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
				            $"<rect width='{BarcodeWidth}' height='{BarcodeHeight}' fill='{background}'/>")
			            + string.Create(Inv,
				            $"<text x='50%' y='50%' text-anchor='middle' dominant-baseline='middle' fill='{foreground}' font-size='12'>{CssValues.EscapeXml(ex.Message)}</text>")
			            + "</svg>";
		}
	}
}
