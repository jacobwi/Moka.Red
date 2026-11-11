using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Data.Chart;

/// <summary>
///     Steps in order with how many reached each, and the share that went on between them: a sign-up flow, a device's
///     setup. Drawn as bars from the same start, which compare more exactly than a trapezoid, in one hue that steps
///     darker to lighter by stage because the order means something.
/// </summary>
public partial class MokaFunnel : MokaCartesianChartBase
{
	private const int Stages = 5;

	private MokaFunnelStep[]? _snapshot;
	private Step[] _steps = [];

	/// <summary>The steps, first to last.</summary>
	[Parameter]
	public IReadOnlyList<MokaFunnelStep>? Steps { get; set; }

	/// <summary>Whether the share that went on to the next step is written under each bar. Defaults to true.</summary>
	[Parameter]
	public bool ShowConversion { get; set; } = true;

	/// <summary>Writes a share for the tooltip, given the percent and what it is a share of. Defaults to "62% of Visited".</summary>
	[Parameter]
	public Func<string, string, string>? ShareText { get; set; }

	/// <summary>What the first step is called in the tooltip's share of everything, "31% of all". Defaults to "all".</summary>
	[Parameter]
	public string AllText { get; set; } = "all";

	/// <summary>Writes the share that went on to the next step, under each bar. Defaults to "62% went on".</summary>
	[Parameter]
	public Func<string, string>? ConversionText { get; set; }

	/// <summary>The table's heading for the share of the step before. Defaults to "Went on".</summary>
	[Parameter]
	public string ConversionLabel { get; set; } = "Went on";

	/// <inheritdoc />
	protected override string RootClass => "moka-funnel";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass(ModernRootClasses)
		.AddClass("moka-funnel--conversion", ShowConversion)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	internal override string DefaultLabel => _steps.Length == 0
		? "Funnel"
		: $"Funnel: {string.Join(", ", _steps.Take(8).Select(step => $"{step.Source.Name} {step.Figure}"))}";

	/// <inheritdoc />
	internal override bool HasData => _steps.Length > 0;

	/// <inheritdoc />
	internal override IReadOnlyList<ChartSeriesInfo> SeriesInfo => [];

	/// <inheritdoc />
	internal override bool DrawsRows => true;

	/// <inheritdoc />
	internal override bool SurfaceRows => true;

	/// <inheritdoc />
	internal override MokaChartTooltip AutoTooltip => MokaChartTooltip.Column;

	/// <inheritdoc />
	internal override int RowCount => _steps.Length;

	/// <inheritdoc />
	internal override int CategoryCount => _steps.Length;

	/// <inheritdoc />
	internal override int DrawnCount(int panel) => _steps.Length;

	/// <inheritdoc />
	internal override int LatestDrawn(int panel) => _steps.Length > 0 ? 0 : -1;

	/// <inheritdoc />
	internal override double DefaultRowHeight => ShowConversion ? Math.Round(Spec.RowHeight * 1.4) : Spec.RowHeight;

	/// <inheritdoc />
	internal override string RowLabel(int index) => index >= 0 && index < _steps.Length ? _steps[index].Source.Name : string.Empty;

	/// <inheritdoc />
	internal override string? RowFigure(int index) => index >= 0 && index < _steps.Length ? _steps[index].Figure : null;

	/// <inheritdoc />
	internal override ChartTableModel? TableModel => new([Words.Name, ValueName, Words.Share, ConversionLabel], _steps.Length,
		(row, column) => column switch
		{
			0 => _steps[row].Source.Name,
			1 => _steps[row].Figure,
			2 => Percent(_steps[row].OfAll),
			_ => row == 0 ? string.Empty : Percent(_steps[row].OfBefore)
		},
		(row, column) => column switch
		{
			0 => _steps[row].Source.Name,
			1 => Raw(_steps[row].Source.Value),
			2 => Raw(_steps[row].OfAll),
			_ => row == 0 ? string.Empty : Raw(_steps[row].OfBefore)
		});

	private string ValueName => XAxis?.Title is { Length: > 0 } title ? title : Words.Value;

	private bool NativeTooltips => ResolvedTooltip == MokaChartTooltip.Native;

	/// <summary>A drawn step: its record, its stage color, its shares and what it reads at its end.</summary>
	private sealed record Step(MokaFunnelStep Source, string ColorClass, string? ColorStyle, double OfAll, double OfBefore,
		double OnNext, string Figure);

	/// <inheritdoc />
	internal override bool SameValue(string name, object? before, object? after) => name == nameof(Steps)
		? ChartSnapshot.SameItems(after as IReadOnlyList<MokaFunnelStep>, _snapshot)
		: base.SameValue(name, before, after);

	/// <inheritdoc />
	internal override (double Min, double Max) ValueRange(int panel)
	{
		double min = double.PositiveInfinity;
		double max = double.NegativeInfinity;
		foreach (Step step in _steps)
		{
			if (double.IsFinite(step.Source.Value))
			{
				min = Math.Min(min, step.Source.Value);
				max = Math.Max(max, step.Source.Value);
			}
		}

		return min <= max ? (min, max) : (double.NaN, double.NaN);
	}

	/// <inheritdoc />
	internal override void Rebuild()
	{
		_snapshot = DataVersion is null ? ChartSnapshot.Copy(Steps) : null;
		IReadOnlyList<MokaFunnelStep> steps = Steps ?? [];
		double first = steps.Count > 0 ? steps[0].Value : double.NaN;
		Step[] drawn = new Step[steps.Count];
		for (int i = 0; i < steps.Count; i++)
		{
			MokaFunnelStep step = steps[i];
			bool muted = IsMuted(step.Name);
			string stage = Math.Min(i + 1, Stages).ToString(CultureInfo.InvariantCulture);
			string? style = muted ? null : new StyleBuilder().AddStyle("--moka-chart-color", $"var(--moka-chart-ord-{stage})").Build();
			drawn[i] = new Step(step, muted ? "moka-chart-muted" : string.Empty, style, Ratio(step.Value, first),
				i > 0 ? Ratio(step.Value, steps[i - 1].Value) : double.NaN,
				i < steps.Count - 1 ? Ratio(steps[i + 1].Value, step.Value) : double.NaN, ChartRows.Write(this, step.Value));
		}

		_steps = drawn;
	}

	/// <inheritdoc />
	internal override ChartActiveInfo? DescribeActive(int panel, int series, int drawn, int? pinnedDrawn)
	{
		if (drawn < 0 || drawn >= _steps.Length)
		{
			return null;
		}

		Step step = _steps[drawn];
		List<string> notes = [];
		if (drawn > 0 && double.IsFinite(step.OfBefore))
		{
			notes.Add(Share(step.OfBefore, _steps[drawn - 1].Source.Name));
		}

		if (drawn > 1 && double.IsFinite(step.OfAll))
		{
			notes.Add(Share(step.OfAll, AllText));
		}

		if (!string.IsNullOrEmpty(step.Source.Detail))
		{
			notes.Add(step.Source.Detail);
		}

		notes.AddRange(AnnotationNotes(step.Source.Value));
		MokaChartPoint point = new(step.Source.Name, step.Source.Name, drawn, drawn, step.Source.Value, step.Figure)
		{
			Category = step.Source.Name,
			Total = _steps.Length > 0 ? _steps[0].Source.Value : null,
			Share = double.IsFinite(step.OfAll) ? step.OfAll : null
		};
		return ChartRows.Describe(drawn, step.Source.Name,
			[new ChartActiveRow(ValueName, step.ColorClass, step.ColorStyle, step.Figure, null, false)], notes, [point]);
	}

	private static string LaneClass(Step step) => new CssBuilder("moka-funnel__lane")
		.AddClass(step.ColorClass)
		.Build();

	private string? LaneStyle(Step step)
	{
		double value = Place(step.Source.Value);
		double start = Place(0);
		if (!double.IsFinite(start))
		{
			start = 0;
		}

		return new StyleBuilder()
			.AddStyle(step.ColorStyle)
			.AddStyle("--_fn-from", double.IsFinite(value) ? ChartRows.Css(Math.Min(start, value)) : null)
			.AddStyle("--_fn-len", double.IsFinite(value) ? ChartRows.Css(Math.Abs(value - start)) : null)
			.Build();
	}

	private string? NativeTitle(int row) => NativeTooltips
		? row > 0 && double.IsFinite(_steps[row].OfBefore)
			? $"{_steps[row].Source.Name}: {_steps[row].Figure}, {Share(_steps[row].OfBefore, _steps[row - 1].Source.Name)}"
			: $"{_steps[row].Source.Name}: {_steps[row].Figure}"
		: null;

	private string? Conversion(int row) => ShowConversion && row < _steps.Length - 1 && double.IsFinite(_steps[row].OnNext)
		? ConversionText?.Invoke(Percent(_steps[row].OnNext)) ?? $"{Percent(_steps[row].OnNext)} went on"
		: null;

	private string Share(double share, string whole) =>
		ShareText?.Invoke(Percent(share), whole) ?? $"{Percent(share)} of {whole}";

	private double Place(double value)
	{
		if (RowPlotModel().Scale is not { } scale || !double.IsFinite(value))
		{
			return double.NaN;
		}

		double share = Math.Clamp(scale.Share(value), 0, 1);
		return XAxis?.Reverse == true ? 1 - share : share;
	}

	private static double Ratio(double part, double whole) =>
		double.IsFinite(part) && double.IsFinite(whole) && whole != 0 ? part / whole : double.NaN;

	private static string Percent(double share) => double.IsFinite(share) ? ChartWholes.Percent(share) : string.Empty;

	private static string Raw(double value) => double.IsFinite(value) ? value.ToString(CultureInfo.InvariantCulture) : string.Empty;
}
