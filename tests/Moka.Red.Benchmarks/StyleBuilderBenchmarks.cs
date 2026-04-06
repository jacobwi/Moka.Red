using BenchmarkDotNet.Attributes;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Benchmarks;

/// <summary>
///     Benchmarks for <see cref="StyleBuilder" /> inline style construction.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class StyleBuilderBenchmarks
{
	[Benchmark(Description = "Empty (returns null)")]
	public string? Empty() => new StyleBuilder().Build();

	[Benchmark(Description = "Single property")]
	public string? SingleProperty()
	{
		return new StyleBuilder()
			.AddStyle("color", "red")
			.Build();
	}

	[Benchmark(Description = "3 properties")]
	public string? ThreeProperties()
	{
		return new StyleBuilder()
			.AddStyle("display", "flex")
			.AddStyle("flex-direction", "column")
			.AddStyle("gap", "0.5rem")
			.Build();
	}

	[Benchmark(Description = "Flexbox-realistic (8 properties)")]
	public string? FlexboxRealistic()
	{
		return new StyleBuilder()
			.AddStyle("display", "flex")
			.AddStyle("flex-direction", "row")
			.AddStyle("justify-content", "center")
			.AddStyle("align-items", "center")
			.AddStyle("flex-wrap", "wrap", true)
			.AddStyle("gap", "0.5rem")
			.AddStyle("margin", null)
			.AddStyle("padding", "var(--moka-spacing-md)")
			.Build();
	}

	[Benchmark(Description = "With null skips")]
	public string? WithNullSkips()
	{
		return new StyleBuilder()
			.AddStyle("width", null)
			.AddStyle("height", null)
			.AddStyle("color", "var(--moka-color-primary)")
			.AddStyle("opacity", null)
			.Build();
	}

	[Benchmark(Description = "With conditional")]
	public string? WithConditional()
	{
		return new StyleBuilder()
			.AddStyle("width", "100%", true)
			.AddStyle("height", "auto", false)
			.AddStyle("display", "none", false)
			.AddStyle("color", "inherit", true)
			.Build();
	}
}
