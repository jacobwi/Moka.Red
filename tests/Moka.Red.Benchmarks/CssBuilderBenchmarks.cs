using BenchmarkDotNet.Attributes;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Benchmarks;

/// <summary>
///     Benchmarks for <see cref="CssBuilder" /> class string construction.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class CssBuilderBenchmarks
{
	[Benchmark(Description = "Single class")]
	public string SingleClass() => new CssBuilder("moka-btn").Build();

	[Benchmark(Description = "3 classes")]
	public string ThreeClasses()
	{
		return new CssBuilder("moka-btn")
			.AddClass("moka-btn--filled")
			.AddClass("moka-btn--primary")
			.Build();
	}

	[Benchmark(Description = "6 classes with conditions")]
	public string SixClassesConditional()
	{
		return new CssBuilder("moka-btn")
			.AddClass("moka-btn--filled")
			.AddClass("moka-btn--primary")
			.AddClass("moka-btn--md")
			.AddClass("moka-btn--full-width", true)
			.AddClass("moka-btn--loading", false)
			.AddClass("moka-btn--disabled", false)
			.AddClass("custom-class")
			.Build();
	}

	[Benchmark(Description = "10 classes (component-realistic)")]
	public string TenClasses()
	{
		return new CssBuilder("moka-table")
			.AddClass("moka-table--dense")
			.AddClass("moka-table--striped")
			.AddClass("moka-table--hoverable")
			.AddClass("moka-table--bordered", true)
			.AddClass("moka-table--sortable", true)
			.AddClass("moka-table--selectable", false)
			.AddClass("moka-table--loading", false)
			.AddClass("moka-table--paginated")
			.AddClass("moka-table--searchable")
			.AddClass("user-custom-class")
			.Build();
	}

	[Benchmark(Description = "With null/empty skips")]
	public string WithNullSkips()
	{
		return new CssBuilder("moka-card")
			.AddClass(null)
			.AddClass("")
			.AddClass("  ")
			.AddClass("moka-card--elevation-2")
			.AddClass(null)
			.AddClass("moka-card--rounded")
			.Build();
	}

	[Benchmark(Description = "With Func<bool> conditions")]
	public string WithFuncConditions()
	{
		bool isActive = true;
		bool isDisabled = false;

		return new CssBuilder("moka-menu-item")
			.AddClass("moka-menu-item--active", () => isActive)
			.AddClass("moka-menu-item--disabled", () => isDisabled)
			.AddClass("moka-menu-item--expanded", () => true)
			.Build();
	}
}
