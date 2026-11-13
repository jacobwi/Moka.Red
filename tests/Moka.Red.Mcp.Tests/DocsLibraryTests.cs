namespace Moka.Red.Mcp.Tests;

public sealed class DocsLibraryTests
{
	private static readonly DocsLibrary Docs = new();

	[Fact]
	public void EmbedsEveryComponentPage()
	{
		string folder = Path.Combine(RepositoryRoot(), "docs", "docs", "components");
		string[] expected = [.. Directory.GetFiles(folder, "*.md").Select(file => Path.GetFileNameWithoutExtension(file)).Order(StringComparer.Ordinal)];

		Assert.Equal(expected, Docs.ComponentPages.Select(page => page.Slug).Order(StringComparer.Ordinal));
	}

	[Fact]
	public void EmbedsTheGettingStartedGuides() =>
		Assert.Contains(Docs.GuidePages, page => page.Slug == "installation");

	[Fact]
	public void APageCoversItsOwnComponentAndTheOnesItHasSectionsFor()
	{
		Assert.Equal("button", Docs.ForComponent("MokaButton")?.Slug);
		Assert.Equal("button", Docs.ForComponent("MokaButtonGroup")?.Slug);
	}

	[Theory]
	[InlineData("bar-chart")]
	[InlineData("MokaBarChart")]
	[InlineData("BarChart")]
	public void Find_TakesAComponentOrAPageName(string name) =>
		Assert.Equal("bar-chart", Docs.Find(name)?.Slug);

	[Fact]
	public void ParameterTables_BelongToTheComponentAboveThem()
	{
		Assert.Equal("Label content", Docs.Parameter("MokaButton", "ChildContent")?.Description);
		Assert.Equal("Row", Docs.Parameter("MokaButtonGroup", "Orientation")?.Default);
		Assert.Null(Docs.Parameter("MokaButtonGroup", "Loading"));
	}

	[Fact]
	public void ADashInTheDefaultColumnMeansNone() =>
		Assert.Null(Docs.Parameter("MokaButton", "ChildContent")?.Default);

	[Fact]
	public void Summary_ComesOnlyFromThePagesOwnComponent()
	{
		Assert.False(string.IsNullOrWhiteSpace(Docs.Summary("MokaButton")));
		Assert.Null(Docs.Summary("MokaButtonGroup"));
	}

	[Fact]
	public void Section_RunsToTheNextHeadingOfItsLevelAndSkipsCodeBlocks()
	{
		DocsPage page = new(
			"demo",
			"Demo",
			"",
			"# Demo\n\n## Usage\n\n```razor\n## not a heading\n```\n\n### Detail\n\nText\n\n## Other\n\nMore",
			[],
			new Dictionary<string, IReadOnlyDictionary<string, DocsParameter>>());

		string? usage = page.Section("usage");

		Assert.NotNull(usage);
		Assert.StartsWith("## Usage", usage, StringComparison.Ordinal);
		Assert.Contains("## not a heading", usage, StringComparison.Ordinal);
		Assert.Contains("### Detail", usage, StringComparison.Ordinal);
		Assert.DoesNotContain("## Other", usage, StringComparison.Ordinal);
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

		throw new InvalidOperationException("Moka.Red.slnx not found above the test output.");
	}
}
