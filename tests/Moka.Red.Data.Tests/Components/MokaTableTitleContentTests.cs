using AngleSharp.Dom;
using Bunit;
using Moka.Red.Data.Table;
using Moka.Red.Tests.Shared;
using static Moka.Red.Data.Tests.Components.TableKit;

namespace Moka.Red.Data.Tests.Components;

// TitleContent puts the app's own parts where the title string goes, such as a count in mono, a status and an action.
public class MokaTableTitleContentTests : BunitContext
{
	private const string Css = "MokaTable.razor.css";
	private const string Count = "<span class=\"count\">4</span> people <button type=\"button\" class=\"invite\">Invite</button>";

	public MokaTableTitleContentTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public void TitleContent_ShowsAtTheStartOfTheToolbar()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.TitleContent, Count));

		IElement toolbar = cut.Find(".moka-table-toolbar");
		IElement title = toolbar.Children[0];
		Assert.Contains("moka-table-title", title.ClassList);
		Assert.Contains("moka-table-title--content", title.ClassList);
		Assert.Equal("4", title.QuerySelector(".count")!.TextContent);
		Assert.NotNull(title.QuerySelector("button.invite"));
	}

	[Fact]
	public void TitleContent_AloneIsEnough_ToShowTheToolbar()
	{
		Assert.Empty(Table().FindAll(".moka-table-toolbar"));
		Assert.Single(Table(p => p.Add(t => t.TitleContent, Count)).FindAll(".moka-table-toolbar"));
	}

	[Fact]
	public void WithBoth_TitleContentShows_InPlaceOfTheTitle()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(t => t.Title, "People")
			.Add(t => t.TitleContent, Count));

		IElement title = Assert.Single(cut.FindAll(".moka-table-title"));
		Assert.Contains("moka-table-title--content", title.ClassList);
		Assert.DoesNotContain("People", cut.Find(".moka-table-toolbar").TextContent, StringComparison.Ordinal);
	}

	// Title names nothing for screen readers, then or now: Label does, on the table element.
	[Fact]
	public void Label_StillNamesTheTable()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(t => t.Title, "People")
			.Add(t => t.Label, "People in the team")
			.Add(t => t.TitleContent, Count));

		Assert.Equal("People in the team", cut.Find("table").GetAttribute("aria-label"));
	}

	[Fact]
	public void WithoutTitleContent_TheTitleIsAsItWas()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.Title, "People"));

		IElement title = cut.Find(".moka-table-title");
		Assert.Equal("SPAN", title.TagName);
		Assert.Equal("moka-table-title", title.ClassName);
		Assert.Equal("People", title.TextContent);
		Assert.False(cut.Find("table").HasAttribute("aria-label"));
	}

	[Fact]
	public void TitleContent_LaysItsPartsOutInARow()
	{
		IReadOnlyDictionary<string, string> content = ScopedCss.Declarations(Css, ".moka-table-title--content");

		Assert.Equal("flex", content["display"]);
		Assert.Equal("center", content["align-items"]);
		Assert.Equal("var(--moka-spacing-sm)", content["gap"]);
		Assert.Equal("0", content["min-width"]);
	}

	private IRenderedComponent<MokaTable<Person>> Table(
		Action<ComponentParameterCollectionBuilder<MokaTable<Person>>>? extra = null) =>
		Render<MokaTable<Person>>(p =>
		{
			p.Add(t => t.Items, People);
			p.Add(t => t.ChildContent, DefaultColumns);
			extra?.Invoke(p);
		});
}
