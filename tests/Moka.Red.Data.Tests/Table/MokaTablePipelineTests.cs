using Moka.Red.Data.Table;

namespace Moka.Red.Data.Tests.Table;

// The data pipeline on its own: search, filter, sort, page, group and total, over plain fields with no table drawn.
public sealed class MokaTablePipelineTests
{
	private static readonly Row[] Rows =
	[
		new("Ada", "Engineering", 36, new DateTimeOffset(2026, 1, 5, 0, 0, 0, TimeSpan.Zero), true),
		new("Grace", "Research", 45, new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero), false),
		new("Alan", "Engineering", 41, new DateTimeOffset(2024, 3, 9, 0, 0, 0, TimeSpan.Zero), true),
		new("Barbara", "Research", 29, new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero), false)
	];

	private static readonly ITableField<Row>[] Fields =
	[
		new Field("Name", static r => r.Name),
		new Field("Team", static r => r.Team),
		new Field("Age", static r => r.Age, static r => $"{r.Age} years"),
		new Field("Joined", static r => r.Joined),
		new Field("Active", static r => r.Active)
	];

	[Fact]
	public void Search_MatchesAnyColumnsText_IgnoringCase()
	{
		Assert.Equal(["Grace", "Barbara"], Names(MokaTablePipeline.Search(Rows, Fields, "RESEARCH")));
		Assert.Equal(["Alan"], Names(MokaTablePipeline.Search(Rows, Fields, "41 years")));
	}

	[Fact]
	public void Search_WithABlankTerm_KeepsEveryRow() =>
		Assert.Equal(4, MokaTablePipeline.Search(Rows, Fields, "  ").Count());

	[Theory]
	[InlineData(MokaFilterOperator.Contains, new[] { "ar" }, new[] { "Barbara" })]
	[InlineData(MokaFilterOperator.StartsWith, new[] { "a" }, new[] { "Ada", "Alan" })]
	[InlineData(MokaFilterOperator.EndsWith, new[] { "A" }, new[] { "Ada", "Barbara" })]
	[InlineData(MokaFilterOperator.Is, new[] { "grace" }, new[] { "Grace" })]
	[InlineData(MokaFilterOperator.IsNot, new[] { "Grace" }, new[] { "Ada", "Alan", "Barbara" })]
	[InlineData(MokaFilterOperator.IsAnyOf, new[] { "Ada", "Alan" }, new[] { "Ada", "Alan" })]
	[InlineData(MokaFilterOperator.IsNoneOf, new[] { "Ada", "Alan" }, new[] { "Grace", "Barbara" })]
	[InlineData(MokaFilterOperator.DoesNotContain, new[] { "a" }, new string[0])]
	public void TextFilters_ReadTheName(MokaFilterOperator op, string[] values, string[] expected) =>
		Assert.Equal(expected, Names(MokaTablePipeline.Filter(Rows, Fields, [new MokaTableFilter("Name", op, values)])));

	[Theory]
	[InlineData(MokaFilterOperator.GreaterThan, new[] { "40" }, new[] { "Grace", "Alan" })]
	[InlineData(MokaFilterOperator.AtLeast, new[] { "41" }, new[] { "Grace", "Alan" })]
	[InlineData(MokaFilterOperator.LessThan, new[] { "36" }, new[] { "Barbara" })]
	[InlineData(MokaFilterOperator.AtMost, new[] { "36" }, new[] { "Ada", "Barbara" })]
	[InlineData(MokaFilterOperator.Between, new[] { "30", "42.5" }, new[] { "Ada", "Alan" })]
	[InlineData(MokaFilterOperator.Is, new[] { "45" }, new[] { "Grace" })]
	public void NumberFilters_CompareTheValue_InTheInvariantCulture(MokaFilterOperator op, string[] values, string[] expected) =>
		Assert.Equal(expected, Names(MokaTablePipeline.Filter(Rows, Fields, [new MokaTableFilter("Age", op, values)])));

	[Fact]
	public void DateFilters_ReadIsoDates() =>
		Assert.Equal(["Ada", "Barbara"],
			Names(MokaTablePipeline.Filter(Rows, Fields, [new MokaTableFilter("Joined", MokaFilterOperator.AtLeast, "2026-01-01")])));

	[Fact]
	public void YesNoFilters_ReadTrueAndFalse() =>
		Assert.Equal(["Grace", "Barbara"],
			Names(MokaTablePipeline.Filter(Rows, Fields, [new MokaTableFilter("Active", MokaFilterOperator.Is, "false")])));

	[Fact]
	public void Filters_AllHaveToPass() =>
		Assert.Equal(["Alan"], Names(MokaTablePipeline.Filter(Rows, Fields,
		[
			new MokaTableFilter("Team", MokaFilterOperator.Is, "Engineering"),
			new MokaTableFilter("Age", MokaFilterOperator.GreaterThan, "40")
		])));

	[Fact]
	public void AFilterOnAnUnknownColumn_FiltersNothing() =>
		Assert.Equal(4, MokaTablePipeline.Filter(Rows, Fields, [new MokaTableFilter("Nope", MokaFilterOperator.Is, "x")]).Count());

	[Fact]
	public void ANumberFilterWithText_MatchesNothing() =>
		Assert.Empty(MokaTablePipeline.Filter(Rows, Fields, [new MokaTableFilter("Age", MokaFilterOperator.LessThan, "old")]));

	[Fact]
	public void Sort_OrdersByEachSortInTurn() =>
		Assert.Equal(["Alan", "Ada", "Grace", "Barbara"], Names(MokaTablePipeline.Sort(Rows, Fields,
		[
			new MokaTableSort("Team", MokaSortDirection.Ascending),
			new MokaTableSort("Age", MokaSortDirection.Descending)
		])));

	[Fact]
	public void Sort_UsesAColumnsComparer()
	{
		ITableField<Row>[] fields = [new Field("Name", static r => r.Name, comparer: static (a, b) => a.Name.Length.CompareTo(b.Name.Length))];

		Assert.Equal(["Ada", "Alan", "Grace", "Barbara"],
			Names(MokaTablePipeline.Sort(Rows, fields, [new MokaTableSort("Name", MokaSortDirection.Ascending)])));
	}

	[Fact]
	public void Page_CutsOnePage_AndClampFindsTheLast()
	{
		Assert.Equal(["Alan", "Barbara"], Names(MokaTablePipeline.Page(Rows, 2, 2)));
		Assert.Equal(2, MokaTablePipeline.ClampPage(9, total: 4, pageSize: 2));
		Assert.Equal(1, MokaTablePipeline.PageCount(0, 10));
	}

	[Fact]
	public void Group_KeepsFirstAppearanceOrder_AndTheRowsOrder()
	{
		List<(string Key, List<Row> Rows)> groups = MokaTablePipeline.Group(Rows, static r => r.Team);

		Assert.Equal(["Engineering", "Research"], groups.Select(static g => g.Key));
		Assert.Equal(["Ada", "Alan"], Names(groups[0].Rows));
	}

	[Theory]
	[InlineData(MokaAggregateType.Sum, 151d)]
	[InlineData(MokaAggregateType.Average, 37.75d)]
	[InlineData(MokaAggregateType.Min, 29d)]
	[InlineData(MokaAggregateType.Max, 45d)]
	[InlineData(MokaAggregateType.Count, 4d)]
	public void Aggregate_TotalsTheValues(MokaAggregateType type, double expected) =>
		Assert.Equal(expected, MokaTablePipeline.Aggregate(Rows.Select(static r => (object?)r.Age), type));

	[Fact]
	public void Aggregate_SkipsWhatIsNotANumber() =>
		Assert.Equal(3d, MokaTablePipeline.Aggregate(["1", 2, null, "two", DateTime.Now], MokaAggregateType.Sum));

	private static List<string> Names(IEnumerable<Row> rows) => rows.Select(static r => r.Name).ToList();

	private sealed record Row(string Name, string Team, int Age, DateTimeOffset Joined, bool Active);

	private sealed class Field(
		string title,
		Func<Row, object?> value,
		Func<Row, string>? text = null,
		Func<Row, Row, int>? comparer = null) : ITableField<Row>
	{
		public string? Title => title;

		public bool HasValue => true;

		public Func<Row, Row, int>? Comparer => comparer;

		public object? ValueOf(Row item) => value(item);

		public string TextOf(Row item) => text?.Invoke(item) ?? value(item)?.ToString() ?? string.Empty;

		public string GroupKeyOf(Row item) => TextOf(item);
	}
}
