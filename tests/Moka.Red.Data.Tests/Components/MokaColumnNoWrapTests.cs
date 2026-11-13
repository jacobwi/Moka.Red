using AngleSharp.Dom;
using Bunit;
using Moka.Red.Data.Table;
using Moka.Red.Tests.Shared;
using static Moka.Red.Data.Tests.Components.TableKit;

namespace Moka.Red.Data.Tests.Components;

// MokaColumn.NoWrap: short values such as names and event ids stay on one line, cut off once the column has a width.
public class MokaColumnNoWrapTests : BunitContext
{
	private const string Css = "MokaTable.razor.css";
	private const string NoWrapClass = "moka-table-cell--nowrap";
	private const string PhoneCell = ".moka-table-container > .moka-table--phone > tbody > .moka-table-row--phone > td.moka-table-cell--nowrap";

	public MokaColumnNoWrapTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public void ByDefault_NoCellCarriesTheClass_AndAWidthWritesNoMaxWidth()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(Name with { Width = "120px" }, City);

		Assert.Empty(cut.FindAll($".{NoWrapClass}"));
		Assert.Equal("width: 120px", DataRows(cut)[0].QuerySelectorAll("td")[0].GetAttribute("style"));
	}

	[Fact]
	public void NoWrap_MarksEveryCellOfItsColumn_AndNoOther()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(
			p => p.Add(t => t.ShowFilters, true),
			Name with { NoWrap = true, Filterable = true, Aggregate = MokaAggregateType.Count },
			City with { Filterable = true });

		Assert.Contains(NoWrapClass, HeaderCell(cut, "Name").ClassList);
		Assert.DoesNotContain(NoWrapClass, HeaderCell(cut, "City").ClassList);
		Assert.All(DataRows(cut), row =>
		{
			IHtmlCollection<IElement> cells = row.QuerySelectorAll("td");
			Assert.Contains(NoWrapClass, cells[0].ClassList);
			Assert.DoesNotContain(NoWrapClass, cells[1].ClassList);
		});
		Assert.Contains(NoWrapClass, cut.FindAll(".moka-table-filter-cell")[0].ClassList);
		Assert.DoesNotContain(NoWrapClass, cut.FindAll(".moka-table-filter-cell")[1].ClassList);
		Assert.Contains(NoWrapClass, cut.FindAll(".moka-table-aggregate-cell")[0].ClassList);
	}

	// An automatic table grows a column to fit a value that can't wrap, so the width alone wouldn't hold it.
	[Fact]
	public void NoWrap_WithAWidth_HoldsTheCellsAtIt_ButNotTheHeading()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(Name with { NoWrap = true, Width = "120px" }, City);

		Assert.Equal("width: 120px; max-width: 120px", DataRows(cut)[0].QuerySelectorAll("td")[0].GetAttribute("style"));
		Assert.Equal("width: 120px", HeaderCell(cut, "Name").GetAttribute("style"));
	}

	[Fact]
	public void NoWrap_WithoutAWidth_WritesNoStyle()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(Name with { NoWrap = true }, City);

		Assert.Null(DataRows(cut)[0].QuerySelectorAll("td")[0].GetAttribute("style"));
	}

	[Fact]
	public async Task NoWrap_HoldsTheWidthAColumnWasDraggedTo()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(Name with { NoWrap = true }, City);

		await cut.InvokeAsync(() => cut.Instance.OnColumnResized(0, 90));

		Assert.Equal("width: 90px; max-width: 90px", DataRows(cut)[0].QuerySelectorAll("td")[0].GetAttribute("style"));
		Assert.Null(DataRows(cut)[0].QuerySelectorAll("td")[1].GetAttribute("style"));
	}

	[Fact]
	public void TurningNoWrapOn_RedrawsTheTable()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(Name, City);

		cut.Render(p => p.Add(t => t.ChildContent, Columns(Name with { NoWrap = true }, City)));

		Assert.Contains(NoWrapClass, DataRows(cut)[0].QuerySelectorAll("td")[0].ClassList);
	}

	[Fact]
	public void TheCells_KeepOneLine_AndCutOffWithAnEllipsis()
	{
		IReadOnlyDictionary<string, string> cell = ScopedCss.Declarations(Css, ".moka-table > tbody > tr > td.moka-table-cell--nowrap");

		Assert.Equal("nowrap", cell["white-space"]);
		Assert.Equal("hidden", cell["overflow"]);
		Assert.Equal("ellipsis", cell["text-overflow"]);
		Assert.Equal("nowrap", ScopedCss.Declarations(Css, ".moka-table > tfoot > tr > td.moka-table-cell--nowrap")["white-space"]);
	}

	// Headings never wrap, on either design, which is why the class needs no rule there.
	[Fact]
	public void Headings_NeverWrap()
	{
		Assert.Equal("nowrap", ScopedCss.Declarations(Css, ".moka-table th")["white-space"]);
		Assert.False(ScopedCss.Declarations(Css, ".moka-gate .moka-table th").ContainsKey("white-space"));
	}

	// The list on a phone lays cells out in a grid of its own, where the column's width means nothing.
	[Fact]
	public void OnAPhone_TheValueStaysOnOneLine_AtTheTracksWidth()
	{
		IReadOnlyDictionary<string, string> cell = ScopedCss.Declarations(Css, PhoneCell);

		Assert.Equal("none !important", cell["max-width"]);
		Assert.Equal("nowrap", cell["white-space"]);
		Assert.Equal("normal", cell["overflow-wrap"]);
		Assert.Equal("ellipsis", cell["text-overflow"]);
	}

	[Fact]
	public void OnAPhone_ANoWrapPrimaryCell_StillTakesItsSlot()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(
			Name with { NoWrap = true, Phone = MokaColumnPhone.Primary },
			City with { Phone = MokaColumnPhone.Secondary });

		IElement name = DataRows(cut)[0].QuerySelectorAll("td")[0];
		Assert.Contains(NoWrapClass, name.ClassList);
		Assert.Contains("moka-table-cell--phone-primary", name.ClassList);
	}

	private IRenderedComponent<MokaTable<Person>> Table(params Col[] columns) => Table(null, columns);

	private IRenderedComponent<MokaTable<Person>> Table(
		Action<ComponentParameterCollectionBuilder<MokaTable<Person>>>? extra,
		params Col[] columns) =>
		Render<MokaTable<Person>>(p =>
		{
			p.Add(t => t.Items, People);
			p.Add(t => t.ChildContent, Columns(columns));
			extra?.Invoke(p);
		});
}
