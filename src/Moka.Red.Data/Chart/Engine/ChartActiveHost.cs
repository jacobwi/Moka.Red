using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Moka.Red.Data.Chart;

/// <summary>
///     What the tooltip host draws: the active point, the pin, and how many live samples wait. The positional members
///     are what every host needs; the rest say how the surface draws them.
/// </summary>
/// <param name="Info">The active point's tooltip, or null when nothing is active.</param>
/// <param name="Pinned">The pinned point's position, if one is pinned.</param>
/// <param name="Waiting">How many samples a held live chart has not drawn yet.</param>
internal sealed record ChartActiveState(ChartActiveInfo? Info, int? Pinned, int Waiting)
{
	/// <summary>Nothing active, nothing pinned, nothing waiting.</summary>
	public static ChartActiveState Idle { get; } = new(null, null, 0);

	/// <summary>The active series in legend order, or -1 for a whole column or nothing.</summary>
	public int Series { get; init; } = -1;

	/// <summary>The active drawn point, or -1.</summary>
	public int Drawn { get; init; } = -1;

	/// <summary>Whether the keyboard moved to the active point.</summary>
	public bool Keyboard { get; init; }

	/// <summary>The pinned point's whole column, for the pin card.</summary>
	public ChartActiveInfo? PinInfo { get; init; }

	/// <summary>The latest point, which a readout shows while nothing is active.</summary>
	public ChartActiveInfo? Latest { get; init; }

	/// <summary>The word a readout puts before the latest point's heading, such as "Latest".</summary>
	public string? LatestWord { get; init; }

	/// <summary>Whether a live chart is holding still for the pointer or the focus.</summary>
	public bool Holding { get; init; }

	/// <summary>What a live chart's chip says, such as "Live" or "Paused · 3 new"; null when the chart doesn't hold.</summary>
	public string? LiveText { get; init; }

	/// <summary>The sentence the polite status region says, such as "Pinned at 14:30.".</summary>
	public string? Said { get; init; }
}

/// <summary>
///     The only part of a chart that renders on hover. It draws <see cref="Content" />, which the surface authors so the
///     markup carries the surface's scope. The chart calls <see cref="Show" /> and only this host renders. When the chart
///     itself renders, the host renders again only if the geometry or what the surface reads from the chart moved, and
///     then it asks the chart for a fresh state, so a tooltip held open over live data shows the new values.
/// </summary>
internal sealed class ChartActiveHost : ComponentBase, IDisposable
{
	private ChartActiveState _state = ChartActiveState.Idle;
	private MokaChartBase? _registered;
	private long _version = -1;
	private object? _surfaceKey;

	/// <summary>The chart it shows the active point of.</summary>
	[Parameter]
	public MokaChartBase? Chart { get; set; }

	/// <summary>The panel it belongs to.</summary>
	[Parameter]
	public int Panel { get; set; }

	/// <summary>The markup, authored by the surface.</summary>
	[Parameter]
	public RenderFragment<ChartActiveState>? Content { get; set; }

	/// <summary>
	///     A host for markup: <c>@ChartActiveHost.For(Chart, Panel, Content)</c>. Razor doesn't find an internal component
	///     by its tag, so it is opened here.
	/// </summary>
	/// <param name="chart">The chart.</param>
	/// <param name="panel">The panel.</param>
	/// <param name="content">The markup.</param>
	public static RenderFragment For(MokaChartBase chart, int panel, RenderFragment<ChartActiveState> content) => builder =>
	{
		builder.OpenComponent<ChartActiveHost>(0);
		builder.AddComponentParameter(1, nameof(Chart), chart);
		builder.AddComponentParameter(2, nameof(Panel), panel);
		builder.AddComponentParameter(3, nameof(Content), content);
		builder.CloseComponent();
	};

	/// <summary>Shows a new active point, pin or waiting count, rendering this host alone.</summary>
	/// <param name="state">What to show.</param>
	public void Show(ChartActiveState state)
	{
		ArgumentNullException.ThrowIfNull(state);

		if (state == _state)
		{
			return;
		}

		_state = state;
		StateHasChanged();
	}

	/// <inheritdoc />
	public override Task SetParametersAsync(ParameterView parameters)
	{
		if (_registered is not null
		    && parameters.TryGetValue(nameof(Chart), out MokaChartBase? chart) && ReferenceEquals(chart, _registered)
		    && parameters.TryGetValue(nameof(Panel), out int panel) && panel == Panel
		    && parameters.TryGetValue(nameof(Content), out RenderFragment<ChartActiveState>? content) && content == Content
		    && chart.GeometryVersion == _version && Equals(chart.SurfaceKey, _surfaceKey))
		{
			return Task.CompletedTask;
		}

		return base.SetParametersAsync(parameters);
	}

	/// <inheritdoc />
	public void Dispose() => _registered?.DetachHost(this);

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		if (!ReferenceEquals(_registered, Chart))
		{
			_registered?.DetachHost(this);
			_registered = Chart;
			_registered?.AttachHost(this);
		}

		if (Chart is not null)
		{
			_version = Chart.GeometryVersion;
			_surfaceKey = Chart.SurfaceKey;
			_state = Chart.SurfaceState(Panel);
		}
	}

	/// <inheritdoc />
	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		ArgumentNullException.ThrowIfNull(builder);
		if (Chart is not null && Content is not null)
		{
			builder.AddContent(0, Content(_state));
		}
	}
}
