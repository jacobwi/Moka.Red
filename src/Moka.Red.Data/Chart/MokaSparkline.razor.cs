using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Data.Chart;

/// <summary>
///     A small inline line chart, the kind that sits beside a number in a dashboard card. Draws a
///     polyline over its values with an optional area fill and a dot on the last point.
/// </summary>
public partial class MokaSparkline : MokaVisualComponentBase
{
	// The viewBox the line is drawn in; the root element is sized by the size classes and the svg
	// fills it, so a sparkline scales with the text around it.
	private const double ViewWidth = 120;
	private const double ViewHeight = 36;

	/// <summary>The values to draw. Null or fewer than two values renders an empty box.</summary>
	[Parameter]
	public IReadOnlyList<double>? Values { get; set; }

	/// <summary>Whether the area under the line is filled. Defaults to true.</summary>
	[Parameter]
	public bool ShowArea { get; set; } = true;

	/// <summary>Whether a dot marks the last value. Defaults to true.</summary>
	[Parameter]
	public bool ShowLastPoint { get; set; } = true;

	/// <summary>Stroke width of the line, in viewBox units. Defaults to 2.</summary>
	[Parameter]
	public double StrokeWidth { get; set; } = 2;

	/// <summary>What the chart shows, for assistive technology. Defaults to "Sparkline".</summary>
	[Parameter]
	public string? Label { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-sparkline";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-sparkline--{SizeToKebab(Size)}")
		.AddClass(Color.HasValue ? $"moka-chart-color-{ColorToKebab(Color.Value)}" : null)
		.AddClass(Class)
		.Build();

	private bool HasLine => Values is { Count: >= 2 };

	private string LinePath
	{
		get
		{
			double min = Min;
			double span = Max - Min;

			var sb = new System.Text.StringBuilder();
			for (int i = 0; i < Values!.Count; i++)
			{
				double x = i * ViewWidth / (Values.Count - 1);
				double norm = span <= 0 ? 0.5 : (Values[i] - min) / span;
				double y = StrokeWidth / 2 + (1 - norm) * (ViewHeight - StrokeWidth);
				sb.Append(i == 0 ? "M " : " L ");
				sb.Append(x.ToString("F2", CultureInfo.InvariantCulture));
				sb.Append(' ');
				sb.Append(y.ToString("F2", CultureInfo.InvariantCulture));
			}

			return sb.ToString();
		}
	}

	// The line closed along the bottom: the area fill.
	private string AreaPath => $"M 0 {ViewHeight} {LinePath[2..]} L {ViewWidth.ToString("F2", CultureInfo.InvariantCulture)} {ViewHeight} Z";

	private (double X, double Y) LastPoint
	{
		get
		{
			double span = Max - Min;
			double norm = span <= 0 ? 0.5 : (Values![^1] - Min) / span;
			return (ViewWidth,
				StrokeWidth / 2 + (1 - norm) * (ViewHeight - StrokeWidth));
		}
	}

	private double Min
	{
		get
		{
			double min = double.MaxValue;
			foreach (double value in Values!)
			{
				if (value < min)
				{
					min = value;
				}
			}

			return min;
		}
	}

	private double Max
	{
		get
		{
			double max = double.MinValue;
			foreach (double value in Values!)
			{
				if (value > max)
				{
					max = value;
				}
			}

			return max;
		}
	}

	private double Radius => Math.Max(StrokeWidth, 3);
}
