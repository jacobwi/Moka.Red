namespace Moka.Red.Mcp.Tests;

public sealed class DocsLibraryTests
{
	private static readonly DocsLibrary Docs = new();

	[Fact]
	public void EmbedsEveryComponentAndLayoutPage()
	{
		string docs = Path.Combine(Repository.Root(), "docs", "docs");
		string[] expected =
		[
			.. ((string[])["components", "layout"])
				.SelectMany(folder => Directory.GetFiles(Path.Combine(docs, folder), "*.md"))
				.Select(file => Path.GetFileNameWithoutExtension(file))
				.Order(StringComparer.Ordinal)
		];

		Assert.Equal(expected, Docs.ComponentPages.Select(page => page.Slug).Order(StringComparer.Ordinal));
	}

	[Theory]
	[InlineData("table-columns")]
	[InlineData("table-filters")]
	[InlineData("table-editing")]
	[InlineData("table-export")]
	[InlineData("theme-studio")]
	[InlineData("banner")]
	[InlineData("announcer")]
	[InlineData("system-notifications")]
	[InlineData("dock-layout")]
	[InlineData("dock-layout-state")]
	public void ThePagesNewIn060_AreFoundByName(string slug) =>
		Assert.Equal(slug, Docs.Find(slug)?.Slug);

	[Theory]
	[InlineData("MokaThemeStudio", "theme-studio")]
	[InlineData("MokaBannerHost", "banner")]
	[InlineData("MokaAnnouncerHost", "announcer")]
	[InlineData("MokaDockLayout", "dock-layout")]
	[InlineData("MokaTable", "table")]
	public void TheNewComponents_PointAtTheirPages(string component, string slug) =>
		Assert.Equal(slug, Docs.ForComponent(component)?.Slug);

	// MokaAnnouncerHost is named after the announcer page's MokaAnnouncer; MokaStatistics isn't named after MokaStat.
	[Fact]
	public void AComponentNamedAfterAPagesOwn_BelongsOnItsPage_AtAWordBoundary()
	{
		Assert.Equal("announcer", Docs.ForComponent("MokaAnnouncerHost")?.Slug);
		Assert.Equal("stat", Docs.ForComponent("MokaStat")?.Slug);
		Assert.Null(Docs.ForComponent("MokaStatistics"));
	}

	[Fact]
	public void ARowNamingSeveralParameters_DescribesEachOfThem()
	{
		Assert.NotNull(Docs.Parameter("MokaBottomSheet", "Margin"));
		Assert.NotNull(Docs.Parameter("MokaBottomSheet", "MarginValue"));
		Assert.NotNull(Docs.Parameter("MokaFileBrowser", "Folder"));
		Assert.Null(Docs.Parameter("MokaFileBrowser", "@bind-Folder"));
	}

	[Fact]
	public void TheTablesParameters_NameNoRemovedOne()
	{
		foreach (string removed in (string[])["OnCellEdit", "ShowFilters", "Layout"])
		{
			Assert.Null(Docs.Parameter("MokaTable", removed));
		}
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
}
