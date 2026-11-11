namespace Moka.Red.Feedback.Tests.Conventions;

/// <summary>
///     Rule 5: icons are <c>MokaIcon</c>, never an inline <c>svg</c>. Only the files whose shapes are data or animation
///     MokaIcon can't express may write one, and every such file is listed here. The list is exact: a file outside it
///     fails, and so does a listed file that no longer writes one, so rule 5's list can't go stale.
/// </summary>
public sealed class InlineSvgFilesTests
{
	private static readonly string[] AllowedFiles =
	[
		"Moka.Red.Primitives/Icon/MokaIcon.razor",
		"Moka.Red.Primitives/Gauge/MokaGauge.razor",
		"Moka.Red.Primitives/Button/MokaButton.razor",
		"Moka.Red.Primitives/Sparkline/MokaSparkline.razor",
		"Moka.Red.Feedback/Progress/MokaProgress.razor",
		"Moka.Red.Feedback/Loading/MokaSpinner.razor",
		"Moka.Red.Forms/Checkbox/MokaCheckbox.razor",
		"Moka.Red.Forms/Knob/MokaKnob.razor",
		"Moka.Red.Data/Chart/MokaBarChart.razor",
		"Moka.Red.Data/Chart/MokaDonutChart.razor",
		"Moka.Red.Data/Chart/MokaLineChart.razor",
		"Moka.Red.Data/Chart/MokaScatterChart.razor",
		"Moka.Red.Data/Chart/MokaBoxPlot.razor",
		"Moka.Red.Data/Chart/MokaSankey.razor"
	];

	[Fact]
	public void ExactlyTheListedFiles_WriteAnInlineSvg()
	{
		string src = Path.Combine(RepositoryRoot(), "src");
		List<string> found = [.. Directory.EnumerateFiles(src, "*.razor", SearchOption.AllDirectories)
			.Select(file => Path.GetRelativePath(src, file).Replace(Path.DirectorySeparatorChar, '/'))
			.Where(static relative => !relative.Split('/').Any(static part => part is "bin" or "obj"))
			.Where(relative => File.ReadAllText(Path.Combine(src, relative)).Contains("<svg", StringComparison.Ordinal))
			.Order(StringComparer.Ordinal)];
		List<string> outside = [.. found.Where(relative => !AllowedFiles.Contains(relative, StringComparer.Ordinal))];
		List<string> gone = [.. AllowedFiles.Where(relative => !found.Contains(relative, StringComparer.Ordinal))];

		Assert.True(outside.Count == 0,
			$"These write an inline svg; use MokaIcon, or add the file here and to rule 5: {string.Join(", ", outside)}");
		Assert.True(gone.Count == 0,
			$"These are listed but write no inline svg any more; take them out here and in rule 5: {string.Join(", ", gone)}");
	}

	private static string RepositoryRoot()
	{
		for (DirectoryInfo? folder = new(AppContext.BaseDirectory); folder is not null; folder = folder.Parent)
		{
			if (File.Exists(Path.Combine(folder.FullName, "Moka.Red.slnx")))
			{
				return folder.FullName;
			}
		}

		Assert.Fail("The tests are not running inside the repository, so there are no source files to read.");
		return "";
	}
}
