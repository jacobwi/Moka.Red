using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Moka.Red.Data.Table;

namespace Moka.Red.Data.Tests.Components;

// The table had multi-sort, filters, inline edit, detail rows, resize, reorder and export, and no way to
// gather rows under a heading.
public class MokaTableGroupingTests : BunitContext
{
	private sealed record Task(string Name, string Status);

	private static readonly Task[] Tasks =
	[
		new("Write docs", "Open"),
		new("Fix the grid", "Open"),
		new("Ship it", "Done"),
		new("Review", "Open")
	];

	private static readonly string[] OpenOnly = ["Open"];

	public MokaTableGroupingTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	private IRenderedComponent<MokaTable<Task>> Table(
		Action<ComponentParameterCollectionBuilder<MokaTable<Task>>>? extra = null) =>
		Render<MokaTable<Task>>(p =>
		{
			p.Add(x => x.Items, Tasks);
			p.Add(x => x.GroupBy, t => t.Status);
			p.AddChildContent<MokaColumn<Task>>(c => c
				.Add(x => x.Title, "Name")
				.Add(x => x.Field, t => t.Name));
			extra?.Invoke(p);
		});

	private static List<string> Keys(IRenderedComponent<MokaTable<Task>> table) =>
		table.FindAll(".moka-table-group-key").Select(k => k.TextContent.Trim()).ToList();

	private static List<string> BodyRows(IRenderedComponent<MokaTable<Task>> table) =>
		table.FindAll("tbody tr:not(.moka-table-group-row) td:first-child")
			.Select(c => c.TextContent.Trim())
			.ToList();

	[Fact]
	public void RowsAreGatheredUnderTheirKeys()
	{
		IRenderedComponent<MokaTable<Task>> table = Table();

		Assert.Equal(["Open", "Done"], Keys(table));
	}

	// The count is what makes a collapsed group readable, so it is on the heading from the start.
	[Fact]
	public void EachHeadingCountsItsRows()
	{
		IRenderedComponent<MokaTable<Task>> table = Table();

		Assert.Equal(["3", "1"], table.FindAll(".moka-table-group-count").Select(c => c.TextContent.Trim()));
	}

	// Grouping gathers; it does not reorder. A group appears where its first row was, and its rows keep the
	// order the sort and the filters gave them.
	[Fact]
	public void GroupingKeepsTheRowOrderItWasGiven()
	{
		IRenderedComponent<MokaTable<Task>> table = Table();

		Assert.Equal(["Write docs", "Fix the grid", "Review", "Ship it"], BodyRows(table));
	}

	[Fact]
	public void AHeadingCollapsesItsRows()
	{
		IRenderedComponent<MokaTable<Task>> table = Table();

		table.FindAll(".moka-table-group-toggle")[0].Click();

		Assert.Equal(["Ship it"], BodyRows(table));
		Assert.Equal(["Open", "Done"], Keys(table));
	}

	[Fact]
	public void CollapsingAndExpandingComesBack()
	{
		IRenderedComponent<MokaTable<Task>> table = Table();
		table.FindAll(".moka-table-group-toggle")[0].Click();

		table.FindAll(".moka-table-group-toggle")[0].Click();

		Assert.Equal(4, BodyRows(table).Count);
	}

	[Fact]
	public void TheToggleReportsWhetherItIsOpen()
	{
		IRenderedComponent<MokaTable<Task>> table = Table();
		IElement first = table.FindAll(".moka-table-group-toggle")[0];
		Assert.Equal("true", first.GetAttribute("aria-expanded"));

		first.Click();

		Assert.Equal("false", table.FindAll(".moka-table-group-toggle")[0].GetAttribute("aria-expanded"));
	}

	[Fact]
	public void TogglingIsReported()
	{
		List<(string Key, bool Collapsed)> reported = [];
		IRenderedComponent<MokaTable<Task>> table = Table(p => p
			.Add(x => x.OnGroupToggled, EventCallback.Factory.Create<(string, bool)>(this, reported.Add)));

		table.FindAll(".moka-table-group-toggle")[0].Click();
		table.FindAll(".moka-table-group-toggle")[0].Click();

		Assert.Equal([("Open", true), ("Open", false)], reported);
	}

	[Fact]
	public void GroupsCanStartCollapsed()
	{
		IRenderedComponent<MokaTable<Task>> table = Table(p => p
			.Add(x => x.InitiallyCollapsedGroups, OpenOnly));

		Assert.Equal(["Ship it"], BodyRows(table));
	}

	// The starting state is applied once, so an unrelated re-render does not undo what the user folded away.
	[Fact]
	public void ARerenderDoesNotUndoTheUsersCollapsing()
	{
		string[] initial = ["Open"];
		IRenderedComponent<MokaTable<Task>> table = Table(p => p
			.Add(x => x.InitiallyCollapsedGroups, initial));
		table.FindAll(".moka-table-group-toggle")[0].Click();
		Assert.Equal(4, BodyRows(table).Count);

		table.Render(p => p
			.Add(x => x.Items, Tasks)
			.Add(x => x.GroupBy, (Func<Task, string>)(t => t.Status))
			.Add(x => x.InitiallyCollapsedGroups, initial)
			.AddChildContent<MokaColumn<Task>>(c => c
				.Add(x => x.Title, "Name")
				.Add(x => x.Field, (Func<Task, object?>)(t => t.Name))));

		Assert.Equal(4, BodyRows(table).Count);
	}

	[Fact]
	public void WithoutCollapsibleGroupsThereIsNoToggle()
	{
		IRenderedComponent<MokaTable<Task>> table = Table(p => p.Add(x => x.GroupsCollapsible, false));

		Assert.Empty(table.FindAll(".moka-table-group-toggle"));
		Assert.Equal(["Open", "Done"], Keys(table));
	}

	[Fact]
	public void AHeaderTemplateReplacesTheKeyAndCount()
	{
		IRenderedComponent<MokaTable<Task>> table = Table(p => p
			.Add(x => x.GroupHeaderTemplate, (RenderFragment<MokaTableGroup<Task>>)(group => builder =>
				builder.AddMarkupContent(0, $"<span class=\"mine\">{group.Key}: {group.Count}</span>"))));

		Assert.Equal(
			["Open: 3", "Done: 1"],
			table.FindAll(".mine").Select(m => m.TextContent.Trim()));
		Assert.Empty(table.FindAll(".moka-table-group-key"));
	}

	// Without GroupBy nothing changes, so the table is exactly what it was for everyone not using this.
	[Fact]
	public void WithoutAGroupByThereAreNoHeadings()
	{
		IRenderedComponent<MokaTable<Task>> table = Render<MokaTable<Task>>(p =>
		{
			p.Add(x => x.Items, Tasks);
			p.AddChildContent<MokaColumn<Task>>(c => c
				.Add(x => x.Title, "Name")
				.Add(x => x.Field, t => t.Name));
		});

		Assert.Empty(table.FindAll(".moka-table-group-row"));
		Assert.Equal(4, BodyRows(table).Count);
	}

	// A heading spans the table rather than sitting in the first column.
	[Fact]
	public void TheHeadingSpansEveryColumn()
	{
		IRenderedComponent<MokaTable<Task>> table = Render<MokaTable<Task>>(p =>
		{
			p.Add(x => x.Items, Tasks);
			p.Add(x => x.GroupBy, t => t.Status);
			p.AddChildContent<MokaColumn<Task>>(c => c.Add(x => x.Title, "Name").Add(x => x.Field, t => t.Name));
			p.AddChildContent<MokaColumn<Task>>(c => c.Add(x => x.Title, "Status").Add(x => x.Field, t => t.Status));
		});

		Assert.Equal("2", table.Find(".moka-table-group-cell").GetAttribute("colspan"));
	}
}
