using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Moka.Red.Core.Enums;
using Moka.Red.Data.Table;

namespace Moka.Red.Data.Tests.Components;

/// <summary>Rows, column definitions and DOM queries the table test classes share.</summary>
internal static class TableKit
{
	internal const string TableModule = "./_content/Moka.Red.Data/moka-table.js";

	internal static readonly Person[] People =
	[
		new("Ada", "Engineering", 36, "London"),
		new("Grace", "Research", 45, "Paris"),
		new("Alan", "Engineering", 41, "Berlin"),
		new("Barbara", "Research", 29, "Madrid")
	];

	internal static Col Name => new("Name", static x => x.Name);

	internal static Col Team => new("Team", static x => x.Team);

	internal static Col Age => new("Age", static x => x.Age);

	internal static Col City => new("City", static x => x.City);

	/// <summary>Name, Team, Age and City, all with default settings.</summary>
	internal static RenderFragment DefaultColumns => Columns(Name, Team, Age, City);

	/// <summary>Writes one MokaColumn per spec, at fixed sequence numbers, so a later render with
	/// changed specs updates the same column components.</summary>
	internal static RenderFragment Columns(params Col[] columns) => builder =>
	{
		foreach (Col col in columns)
		{
			col.Write(builder);
		}
	};

	/// <summary>The columns, then a pager part with the given settings.</summary>
	internal static RenderFragment ColumnsWithPager(Pager pager, params Col[] columns) => builder =>
	{
		foreach (Col col in columns)
		{
			col.Write(builder);
		}

		pager.Write(builder);
	};

	/// <summary>The default columns with a pager part.</summary>
	internal static RenderFragment DefaultColumnsWithPager(Pager pager) => ColumnsWithPager(pager, Name, Team, Age, City);

	/// <summary>The load more pager: "Show N more" and "Show all" under the rows.</summary>
	internal static Pager LoadMore => new() { Look = MokaPagerLook.LoadMore };

	/// <summary>A state that sorts by one column.</summary>
	internal static MokaTableState SortedBy(string column, MokaSortDirection direction) =>
		new() { Sorts = [new MokaTableSort(column, direction)] };

	internal static List<string> HeaderTitles(IRenderedComponent<MokaTable<Person>> cut) =>
		cut.FindAll("thead tr:first-child th .moka-table-header-text")
			.Select(e => e.TextContent.Trim())
			.ToList();

	internal static IElement HeaderCell(IRenderedComponent<MokaTable<Person>> cut, string title) =>
		cut.FindAll("thead tr:first-child th")
			.First(th => string.Equals(
				th.QuerySelector(".moka-table-header-text")?.TextContent.Trim(), title, StringComparison.Ordinal));

	internal static IReadOnlyList<IElement> DataRows(IRenderedComponent<MokaTable<Person>> cut) =>
		cut.FindAll("tbody tr[data-row-index]");

	/// <summary>The text of every data cell in the given visible column, top to bottom.</summary>
	internal static List<string> ColumnTexts(IRenderedComponent<MokaTable<Person>> cut, int colIndex) =>
		DataRows(cut)
			.Select(row => row.QuerySelectorAll("td[data-col-index]")[colIndex].TextContent.Trim())
			.ToList();

	/// <summary>The text of every data cell in the first data row, left to right.</summary>
	internal static List<string> FirstRowTexts(IRenderedComponent<MokaTable<Person>> cut) =>
		DataRows(cut)[0].QuerySelectorAll("td[data-col-index]").Select(td => td.TextContent.Trim()).ToList();

	internal static List<IElement> TabStops(IRenderedComponent<MokaTable<Person>> cut) =>
		cut.FindAll("tbody td[data-col-index]")
			.Where(td => string.Equals(td.GetAttribute("tabindex"), "0", StringComparison.Ordinal))
			.ToList();

	internal sealed record Person(string Name, string Team, int Age, string City);

	/// <summary>
	///     A table that counts its own renders. bUnit's RenderCount counts its parts' renders too, and the columns render
	///     whenever the parent passes them again, whatever the table does.
	/// </summary>
	internal sealed class CountingTable : MokaTable<Person>
	{
		public int Renders { get; private set; }

		protected override Task OnAfterRenderAsync(bool firstRender)
		{
			Renders++;
			return base.OnAfterRenderAsync(firstRender);
		}
	}

	/// <summary>The settings of a MokaTablePager in a test table.</summary>
	internal sealed record Pager
	{
		public MokaPagerLook Look { get; init; }

		public MokaTablePaginationPosition Position { get; init; } = MokaTablePaginationPosition.Bottom;

		public bool Sticky { get; init; }

		public IReadOnlyList<int>? PageSizeOptions { get; init; }

		public bool ShowFirstLast { get; init; } = true;

		public bool ShowPageInfo { get; init; } = true;

		internal void Write(RenderTreeBuilder builder)
		{
			builder.OpenComponent<MokaTablePager>(100);
			builder.AddComponentParameter(101, nameof(MokaTablePager.Look), Look);
			builder.AddComponentParameter(102, nameof(MokaTablePager.Position), Position);
			builder.AddComponentParameter(103, nameof(MokaTablePager.Sticky), Sticky);
			if (PageSizeOptions is not null)
			{
				builder.AddComponentParameter(104, nameof(MokaTablePager.PageSizeOptions), PageSizeOptions);
			}

			builder.AddComponentParameter(105, nameof(MokaTablePager.ShowFirstLast), ShowFirstLast);
			builder.AddComponentParameter(106, nameof(MokaTablePager.ShowPageInfo), ShowPageInfo);

			builder.CloseComponent();
		}
	}

	/// <summary>The settings of one MokaColumn in a test table.</summary>
	internal sealed record Col(string? Title, Func<Person, object?>? Field)
	{
		public bool Visible { get; init; } = true;

		public MokaColumnPin Pin { get; init; }

		public bool HideOnMobile { get; init; }

		public bool Filterable { get; init; }

		public bool Editable { get; init; }

		public string? Format { get; init; }

		public MokaAggregateType Aggregate { get; init; }

		public Func<Person, Person, int>? SortComparer { get; init; }

		public RenderFragment? HeaderTemplate { get; init; }

		public RenderFragment<Person>? CellTemplate { get; init; }

		public Func<Person, object?, Person>? SetField { get; init; }

		public Func<Person, object?, string?>? Validate { get; init; }

		public MokaColumnEditor Editor { get; init; }

		public IReadOnlyList<MokaFilterOption>? EditOptions { get; init; }

		public string? Property { get; init; }

		public bool Sortable { get; init; } = true;

		public string? Width { get; init; }

		public MokaTextAlign Align { get; init; }

		public MokaColumnPhone? Phone { get; init; }

		public bool Fixed { get; init; }

		public bool Mono { get; init; }

		public string? Class { get; init; }

		public bool SortDescendingFirst { get; init; }

		public bool NoWrap { get; init; }

		public MokaSortDirection DefaultSort { get; init; }

		public MokaColumnFilterType FilterType { get; init; }

		public string? AggregateFormat { get; init; }

		internal void Write(RenderTreeBuilder builder)
		{
			builder.OpenComponent<MokaColumn<Person>>(0);
			builder.AddComponentParameter(1, nameof(MokaColumn<Person>.Title), Title);
			builder.AddComponentParameter(2, nameof(MokaColumn<Person>.Field), Field);
			builder.AddComponentParameter(3, nameof(MokaColumn<Person>.Visible), Visible);
			builder.AddComponentParameter(4, nameof(MokaColumn<Person>.Pin), Pin);
			builder.AddComponentParameter(5, nameof(MokaColumn<Person>.HideOnMobile), HideOnMobile);
			builder.AddComponentParameter(6, nameof(MokaColumn<Person>.Filterable), Filterable);
			builder.AddComponentParameter(7, nameof(MokaColumn<Person>.Editable), Editable);
			builder.AddComponentParameter(8, nameof(MokaColumn<Person>.Format), Format);
			builder.AddComponentParameter(9, nameof(MokaColumn<Person>.Aggregate), Aggregate);
			builder.AddComponentParameter(10, nameof(MokaColumn<Person>.SortComparer), SortComparer);
			builder.AddComponentParameter(11, nameof(MokaColumn<Person>.HeaderTemplate), HeaderTemplate);
			builder.AddComponentParameter(12, nameof(MokaColumn<Person>.CellTemplate), CellTemplate);
			builder.AddComponentParameter(13, nameof(MokaColumn<Person>.SetField), SetField);
			builder.AddComponentParameter(14, nameof(MokaColumn<Person>.Sortable), Sortable);
			builder.AddComponentParameter(15, nameof(MokaColumn<Person>.Width), Width);
			builder.AddComponentParameter(16, nameof(MokaColumn<Person>.Align), Align);
			builder.AddComponentParameter(17, nameof(MokaColumn<Person>.Phone), Phone);
			builder.AddComponentParameter(18, nameof(MokaColumn<Person>.Fixed), Fixed);
			builder.AddComponentParameter(19, nameof(MokaColumn<Person>.Mono), Mono);
			builder.AddComponentParameter(20, nameof(MokaColumn<Person>.Class), Class);
			builder.AddComponentParameter(21, nameof(MokaColumn<Person>.SortDescendingFirst), SortDescendingFirst);
			builder.AddComponentParameter(22, nameof(MokaColumn<Person>.NoWrap), NoWrap);
			builder.AddComponentParameter(23, nameof(MokaColumn<Person>.DefaultSort), DefaultSort);
			builder.AddComponentParameter(24, nameof(MokaColumn<Person>.FilterType), FilterType);
			builder.AddComponentParameter(25, nameof(MokaColumn<Person>.AggregateFormat), AggregateFormat);
			builder.AddComponentParameter(26, nameof(MokaColumn<Person>.Validate), Validate);
			builder.AddComponentParameter(27, nameof(MokaColumn<Person>.Editor), Editor);
			builder.AddComponentParameter(28, nameof(MokaColumn<Person>.EditOptions), EditOptions);
			builder.AddComponentParameter(29, nameof(MokaColumn<Person>.Property), Property);
			builder.CloseComponent();
		}
	}
}
