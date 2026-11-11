using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Moka.Red.Data.Chart;

namespace Moka.Red.Data.Tests.Charts;

/// <summary>
///     What chart tests share. bUnit throws when a component reads <c>RendererInfo</c> before a test sets it, and a
///     modern chart reads it to decide whether to bind its script, so a test picks <see cref="Static" /> or
///     <see cref="Interactive" />. Packages add helpers in files of their own.
/// </summary>
internal static class ChartTestKit
{
	/// <summary>The shared chart module's path, as the charts import it.</summary>
	public const string ChartModule = "./_content/Moka.Red.Data/moka-chart.js";

	/// <summary>Renders as a static page or the docs previews do: nothing binds.</summary>
	public static void Static(this BunitContext context)
	{
		ArgumentNullException.ThrowIfNull(context);
		context.SetRendererInfo(new RendererInfo("Static", false));
	}

	/// <summary>Renders as Blazor Server does, with the chart module answering every call, which the test can read back.</summary>
	/// <returns>The chart module.</returns>
	public static BunitJSModuleInterop Interactive(this BunitContext context)
	{
		ArgumentNullException.ThrowIfNull(context);
		context.SetRendererInfo(new RendererInfo("Server", true));
		BunitJSModuleInterop module = context.JSInterop.SetupModule(ChartModule);
		module.Mode = JSRuntimeMode.Loose;
		return module;
	}
}

/// <summary>
///     A chart built only for tests, on <see cref="MokaChartBase" /> through InternalsVisibleTo. It counts its rebuilds
///     and renders, compares <see cref="Values" /> by value like a real chart's data, and draws one div that says what
///     the base resolved.
/// </summary>
public sealed class TestChart : MokaChartBase
{
	private double[]? _snapshot;

	/// <summary>The data, compared by value against a snapshot.</summary>
	[Parameter]
	public IReadOnlyList<double>? Values { get; set; }

	/// <summary>The series' keys, in legend order.</summary>
	[Parameter]
	public IReadOnlyList<string>? Keys { get; set; }

	/// <summary>Whether it draws the frozen classic path.</summary>
	[Parameter]
	public bool Classic { get; set; }

	/// <summary>A parameter that only changes the chrome.</summary>
	[Parameter]
	public string? Caption { get; set; }

	/// <summary>How many times the geometry was rebuilt.</summary>
	public int Rebuilds { get; private set; }

	/// <summary>How many times it rendered.</summary>
	public int Renders { get; private set; }

	/// <summary>What the base resolved, read through the friend assembly.</summary>
	internal MokaChartBase Base => this;

	/// <inheritdoc />
	protected override string RootClass => "test-chart";

	internal override bool IsClassic => Classic;

	internal override bool HasData => Values is { Count: > 0 };

	internal override IReadOnlyList<ChartSeriesInfo> SeriesInfo =>
	[
		.. (Keys ?? []).Select((key, i) => new ChartSeriesInfo(key, key, SlotFor(i, null), SeriesClass(SlotFor(i, null), null, IsMuted(key)),
			SeriesStyle(SlotFor(i, null), null), ChartMark.Line, IsHidden(key), IsMuted(key), null))
	];

	internal override void Rebuild()
	{
		Rebuilds++;
		_snapshot = DataVersion is null ? Values?.ToArray() : null;
	}

	internal override ChartActiveInfo? DescribeActive(int panel, int series, int drawn, int? pinnedDrawn) => null;

	internal override bool AffectsGeometry(string parameterName) =>
		parameterName != nameof(Caption) && base.AffectsGeometry(parameterName);

	internal override bool SameValue(string name, object? before, object? after) => name == nameof(Values)
		? ChartSnapshot.SameItems(after as IReadOnlyList<double>, _snapshot)
		: Equals(before, after);

	/// <inheritdoc />
	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		ArgumentNullException.ThrowIfNull(builder);
		Renders++;
		builder.OpenElement(0, "div");
		builder.AddAttribute(1, "class", CssClass);
		builder.AddAttribute(2, "data-look", ResolvedLook.ToString());
		builder.AddAttribute(3, "data-version", GeometryVersion);
		builder.AddElementReferenceCapture(4, element => RootElement = element);
		builder.AddContent(5, Caption);
		builder.CloseElement();
	}
}
