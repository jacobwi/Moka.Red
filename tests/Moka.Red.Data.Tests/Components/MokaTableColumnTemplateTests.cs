using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Moka.Red.Data.Table;
using static Moka.Red.Data.Tests.Components.TableKit;

namespace Moka.Red.Data.Tests.Components;

// Blazor hands a column its parameters after the table has rendered, and the table drew its cells and headings in its own
// render, so a template that captured a local of the page's render showed the value the page had the time before, and a
// template swapped for another never showed. The table now draws its markup once its parts have their parameters.
public class MokaTableColumnTemplateTests : BunitContext
{
	public MokaTableColumnTemplateTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	public enum Placement
	{
		Direct,
		InAGroup,
		InAComponent
	}

	[Theory]
	[InlineData(Placement.Direct)]
	[InlineData(Placement.InAGroup)]
	[InlineData(Placement.InAComponent)]
	public void ATemplateThatCapturesALocal_ShowsTheNewValue_InTheSameRender(Placement placement)
	{
		IRenderedComponent<Page> page = Render<Page>(p => p.Add(x => x.Suffix, "one").Add(x => x.Placement, placement));
		Assert.Equal("Ada one", FirstCell(page));
		Assert.Equal("Name one", NameHeading(page));

		page.Render(p => p.Add(x => x.Suffix, "two"));

		Assert.Equal("Ada two", FirstCell(page));
		Assert.Equal("Name two", NameHeading(page));
	}

	[Fact]
	public void ATemplateSwappedForAnother_ShowsTheNewOne()
	{
		IRenderedComponent<MokaTable<Person>> cut = Render<MokaTable<Person>>(p => p
			.Add(t => t.Items, People)
			.Add(t => t.ChildContent, Columns(Name with { CellTemplate = Upper }, Team)));
		Assert.Equal("ADA", FirstRowTexts(cut)[0]);

		cut.Render(p => p.Add(t => t.ChildContent, Columns(Name with { CellTemplate = Initial }, Team)));

		Assert.Equal("A.", FirstRowTexts(cut)[0]);
	}

	// The cells draw after the columns have their parameters, so a new setting costs no second render either.
	[Fact]
	public void ANewTitle_CostsOneRender()
	{
		IRenderedComponent<CountingTable> cut = Render<CountingTable>(p => p
			.Add(t => t.Items, People)
			.Add(t => t.ChildContent, Columns(Name, Team)));
		int renders = cut.Instance.Renders;

		cut.Render(p => p.Add(t => t.ChildContent, Columns(Name with { Title = "Full name" }, Team)));

		Assert.Equal(renders + 1, cut.Instance.Renders);
		Assert.Equal("Full name", cut.Find("thead th .moka-table-header-text").TextContent);
	}

	private static readonly RenderFragment<Person> Upper = person => builder => builder.AddContent(0, person.Name.ToUpperInvariant());

	private static readonly RenderFragment<Person> Initial = person => builder => builder.AddContent(0, $"{person.Name[0]}.");

	// A column rendered by a component of the page joins after the table's own, so the Name column is found by its heading.
	private static string FirstCell(IRenderedComponent<Page> page) =>
		page.Find($"tbody tr[data-row-index] td[data-col-index='{NameIndex(page)}']").TextContent.Trim();

	private static string NameHeading(IRenderedComponent<Page> page) =>
		page.Find($"thead tr:last-child th[data-col-index='{NameIndex(page)}']").TextContent.Trim();

	private static string NameIndex(IRenderedComponent<Page> page) =>
		page.FindAll("thead tr:last-child th[data-col-index]")
			.Single(th => th.TextContent.Trim().StartsWith("Name", StringComparison.Ordinal))
			.GetAttribute("data-col-index")!;

	// A page whose column templates capture a local of its own render, as @{ var } or a loop variable does in markup.
	private sealed class Page : ComponentBase
	{
		[Parameter]
		public string Suffix { get; set; } = "";

		[Parameter]
		public Placement Placement { get; set; }

		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
			string suffix = Suffix;
			RenderFragment heading = b => b.AddContent(0, $"Name {suffix}");
			RenderFragment<Person> cell = person => b => b.AddContent(0, $"{person.Name} {suffix}");

			builder.OpenComponent<MokaTable<Person>>(0);
			builder.AddComponentParameter(1, nameof(MokaTable<Person>.Items), People);
			builder.AddComponentParameter(2, nameof(MokaTable<Person>.ChildContent), (RenderFragment)(parts =>
			{
				switch (Placement)
				{
					case Placement.InAGroup:
						parts.OpenComponent<MokaColumnGroup>(10);
						parts.AddComponentParameter(11, nameof(MokaColumnGroup.Title), "Person");
						parts.AddComponentParameter(12, nameof(MokaColumnGroup.ChildContent),
							(RenderFragment)(group => TemplateColumn.Write(group, heading, cell)));
						parts.CloseComponent();
						break;
					case Placement.InAComponent:
						parts.OpenComponent<TemplateColumn>(20);
						parts.AddComponentParameter(21, nameof(TemplateColumn.Heading), heading);
						parts.AddComponentParameter(22, nameof(TemplateColumn.Cell), cell);
						parts.CloseComponent();
						break;
					default:
						TemplateColumn.Write(parts, heading, cell);
						break;
				}

				parts.OpenRegion(40);
				Team.Write(parts);
				parts.CloseRegion();
			}));
			builder.CloseComponent();
		}
	}

	// A reusable column of the page's own, which renders the table's column a level down.
	private sealed class TemplateColumn : ComponentBase
	{
		[Parameter]
		public RenderFragment? Heading { get; set; }

		[Parameter]
		public RenderFragment<Person>? Cell { get; set; }

		internal static void Write(RenderTreeBuilder builder, RenderFragment? heading, RenderFragment<Person>? cell)
		{
			builder.OpenComponent<MokaColumn<Person>>(30);
			builder.AddComponentParameter(31, nameof(MokaColumn<Person>.Title), "Name");
			builder.AddComponentParameter(32, nameof(MokaColumn<Person>.Field), (Func<Person, object?>)(static x => x.Name));
			builder.AddComponentParameter(33, nameof(MokaColumn<Person>.HeaderTemplate), heading);
			builder.AddComponentParameter(34, nameof(MokaColumn<Person>.CellTemplate), cell);
			builder.CloseComponent();
		}

		protected override void BuildRenderTree(RenderTreeBuilder builder) => Write(builder, Heading, Cell);
	}
}
