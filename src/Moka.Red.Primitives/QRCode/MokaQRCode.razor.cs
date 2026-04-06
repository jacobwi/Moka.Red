using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.QRCode;

/// <summary>
///     Generates and renders a QR code as inline SVG — pure C#, no external dependencies.
///     Supports byte mode encoding for versions 1-10.
/// </summary>
public partial class MokaQRCode : MokaVisualComponentBase
{
	private static readonly CultureInfo inv = CultureInfo.InvariantCulture;
	private string? _cachedBg;
	private MokaQRErrorCorrection? _cachedEc;
	private string? _cachedFg;
	private int _cachedQuietZone;
	private bool _cachedRoundedModules;
	private int _cachedSize;
	private string? _cachedValue;
	private string _svgCache = "";

	/// <summary>The data to encode in the QR code. Required.</summary>
	[Parameter]
	[EditorRequired]
	public string Value { get; set; } = "";

	/// <summary>Size of the QR code in pixels. Default 256.</summary>
	[Parameter]
	public int QRSize { get; set; } = 256;

	/// <summary>Foreground (dark module) color. Default "#000000".</summary>
	[Parameter]
	public string ForegroundColor { get; set; } = "#000000";

	/// <summary>Background color. Default "#ffffff".</summary>
	[Parameter]
	public string BackgroundColor { get; set; } = "#ffffff";

	/// <summary>Error correction level. Default Medium.</summary>
	[Parameter]
	public MokaQRErrorCorrection ErrorCorrection { get; set; } = MokaQRErrorCorrection.Medium;

	/// <summary>Quiet zone (border) in modules. Default 4.</summary>
	[Parameter]
	public int QuietZone { get; set; } = 4;

	/// <summary>Whether to use rounded module corners in the QR grid. Default false.</summary>
	[Parameter]
	public bool RoundedModules { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-qrcode";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		if (_cachedValue != Value || _cachedEc != ErrorCorrection || _cachedRoundedModules != RoundedModules
		    || _cachedFg != ForegroundColor || _cachedBg != BackgroundColor
		    || _cachedSize != QRSize || _cachedQuietZone != QuietZone)
		{
			_cachedValue = Value;
			_cachedEc = ErrorCorrection;
			_cachedRoundedModules = RoundedModules;
			_cachedFg = ForegroundColor;
			_cachedBg = BackgroundColor;
			_cachedSize = QRSize;
			_cachedQuietZone = QuietZone;
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
			bool[][] grid = QRCodeGenerator.Generate(Value, ErrorCorrection);
			int gridSize = grid.Length;
			int totalSize = gridSize + QuietZone * 2;
			double moduleSize = (double)QRSize / totalSize;

			CultureInfo inv = CultureInfo.InvariantCulture;
			var sb = new StringBuilder(gridSize * gridSize * 20);
			sb.Append(inv,
				$"<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 {QRSize} {QRSize}' width='{QRSize}' height='{QRSize}'>");
			sb.Append(inv, $"<rect width='{QRSize}' height='{QRSize}' fill='{BackgroundColor}'/>");

			double radius = RoundedModules ? moduleSize * 0.3 : 0;

			for (int y = 0; y < gridSize; y++)
			{
				for (int x = 0; x < gridSize; x++)
				{
					if (grid[y][x])
					{
						double px = (x + QuietZone) * moduleSize;
						double py = (y + QuietZone) * moduleSize;
						if (RoundedModules)
						{
							sb.Append(inv,
								$"<rect x='{px:F1}' y='{py:F1}' width='{moduleSize:F1}' height='{moduleSize:F1}' rx='{radius:F1}' fill='{ForegroundColor}'/>");
						}
						else
						{
							sb.Append(inv,
								$"<rect x='{px:F1}' y='{py:F1}' width='{moduleSize:F1}' height='{moduleSize:F1}' fill='{ForegroundColor}'/>");
						}
					}
				}
			}

			sb.Append("</svg>");
			_svgCache = sb.ToString();
		}
		catch (ArgumentException)
		{
			_svgCache = string.Create(inv,
				            $"<svg xmlns='http://www.w3.org/2000/svg' width='{QRSize}' height='{QRSize}'>")
			            + string.Create(inv, $"<rect width='{QRSize}' height='{QRSize}' fill='{BackgroundColor}'/>")
			            + string.Create(inv,
				            $"<text x='50%' y='50%' text-anchor='middle' dominant-baseline='middle' fill='{ForegroundColor}' font-size='12'>Data too long</text>")
			            + "</svg>";
		}
	}
}
