using BenchmarkDotNet.Attributes;
using Moka.Red.Core.Theming;

namespace Moka.Red.Benchmarks;

/// <summary>
///     Benchmarks for theme CSS variable generation.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class ThemeBenchmarks
{
	private static readonly MokaTheme LightTheme = MokaTheme.Light;
	private static readonly MokaTheme DarkTheme = MokaTheme.Dark;

	private static readonly MokaTheme CustomTheme = new()
	{
		Palette = MokaPalette.Light with
		{
			Primary = "#1976d2",
			PrimaryLight = "#63a4ff",
			PrimaryDark = "#004ba0"
		},
		Typography = MokaTypography.Default with
		{
			FontSizeBase = "0.875rem"
		}
	};

	[Benchmark(Description = "Light theme ToCssVariables")]
	public string LightToCss() => LightTheme.ToCssVariables();

	[Benchmark(Description = "Dark theme ToCssVariables")]
	public string DarkToCss() => DarkTheme.ToCssVariables();

	[Benchmark(Description = "Custom theme ToCssVariables")]
	public string CustomToCss() => CustomTheme.ToCssVariables();

	[Benchmark(Description = "Theme creation + ToCssVariables")]
	public string CreateAndGenerate()
	{
		var theme = new MokaTheme
		{
			Palette = MokaPalette.Light with { Primary = "#00897b" }
		};
		return theme.ToCssVariables();
	}
}
