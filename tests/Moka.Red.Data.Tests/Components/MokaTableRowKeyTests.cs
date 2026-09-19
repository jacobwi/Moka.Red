using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Data.Table;

namespace Moka.Red.Data.Tests.Components;

/// <summary>
///     Row keys. Up to 0.1.8 the rows were keyed siblings, so a repeated ItemKey threw Blazor's
///     duplicate-key error on the next render. From 0.1.9 each row sat in its own render-tree region:
///     that hid the duplicates, but @key never compared two rows either, so a row that changed
///     position was rebuilt rather than moved and lost its DOM and component state. Each cell holds
///     a <see cref="RowProbe" />, whose instance number shows whether a row was moved or rebuilt.
/// </summary>
public class MokaTableRowKeyTests : BunitContext
{
	public MokaTableRowKeyTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public async Task UniqueKeys_MovedRows_KeepTheirComponentState()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderTable([new(1, "Ada"), new(2, "Grace"), new(3, "Alan")]);
		string ada = InstanceOf(cut, "Ada");
		string grace = InstanceOf(cut, "Grace");
		string alan = InstanceOf(cut, "Alan");

		await SortByNameDescending(cut);

		Assert.Equal(["Grace", "Alan", "Ada"], Names(cut));
		Assert.Equal(ada, InstanceOf(cut, "Ada"));
		Assert.Equal(grace, InstanceOf(cut, "Grace"));
		Assert.Equal(alan, InstanceOf(cut, "Alan"));
	}

	[Fact]
	public async Task DuplicateKeys_DoNotThrow_AndUniqueRowsKeepTheirState()
	{
		// Bob and Cy share Id 2.
		IRenderedComponent<MokaTable<Person>> cut = RenderTable(
			[new(1, "Ada"), new(2, "Bob"), new(2, "Cy"), new(3, "Dee")]);
		string ada = InstanceOf(cut, "Ada");
		string dee = InstanceOf(cut, "Dee");

		await SortByNameDescending(cut);

		Assert.Equal(["Dee", "Cy", "Bob", "Ada"], Names(cut));
		Assert.Equal(ada, InstanceOf(cut, "Ada"));
		Assert.Equal(dee, InstanceOf(cut, "Dee"));

		// The clash goes away: every row is keyed again, and Ada and Dee never lost their keys.
		cut.Render(p => p.Add(t => t.Items, [new(1, "Ada"), new(2, "Bob"), new(4, "Cy"), new(3, "Dee")]));

		Assert.Equal(["Dee", "Cy", "Bob", "Ada"], Names(cut));
		Assert.Equal(ada, InstanceOf(cut, "Ada"));
		Assert.Equal(dee, InstanceOf(cut, "Dee"));
		string bob = InstanceOf(cut, "Bob");
		string cy = InstanceOf(cut, "Cy");

		// A new clash between two rows that were unique: only those two lose their keys.
		cut.Render(p => p.Add(t => t.Items, [new(3, "Ada"), new(2, "Bob"), new(4, "Cy"), new(3, "Dee")]));

		Assert.Equal(["Dee", "Cy", "Bob", "Ada"], Names(cut));
		Assert.Equal(bob, InstanceOf(cut, "Bob"));
		Assert.Equal(cy, InstanceOf(cut, "Cy"));
	}

	[Fact]
	public async Task EqualItems_WithoutItemKey_DoNotThrow_AndOtherRowsKeepTheirState()
	{
		// Without ItemKey the item is the key, and records compare by value: both Bob rows collide.
		IRenderedComponent<MokaTable<Person>> cut = RenderTable(
			[new(1, "Ada"), new(2, "Bob"), new(2, "Bob"), new(3, "Dee")],
			keyById: false);
		string ada = InstanceOf(cut, "Ada");
		string dee = InstanceOf(cut, "Dee");

		await SortByNameDescending(cut);

		Assert.Equal(["Dee", "Bob", "Bob", "Ada"], Names(cut));
		Assert.Equal(ada, InstanceOf(cut, "Ada"));
		Assert.Equal(dee, InstanceOf(cut, "Dee"));
	}

	[Fact]
	public async Task DuplicateKeys_WithOpenDetailRows_DoNotThrow_AndUniqueDetailRowsKeepTheirState()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderTable(
			[new(1, "Ada"), new(2, "Bob"), new(2, "Cy"), new(3, "Dee")],
			extra: p => p
				.Add(t => t.Expandable, true)
				.Add(t => t.DetailTemplate, person => builder => Probe(builder, "detail " + person.Name)));

		await ExpandRowOfAsync(cut, "Ada");

		// Bob and Cy share a key, and the key is what records an open row, so both open.
		await ExpandRowOfAsync(cut, "Bob");
		Assert.Equal(3, cut.FindAll("tbody tr.moka-table-detail-row").Count);
		string adaDetail = InstanceOf(cut, "detail Ada");

		await SortByNameDescending(cut);

		Assert.Equal(["Dee", "Cy", "Bob", "Ada"], Names(cut));
		Assert.Equal(3, cut.FindAll("tbody tr.moka-table-detail-row").Count);
		Assert.Equal(adaDetail, InstanceOf(cut, "detail Ada"));
	}

	[Fact]
	public async Task Virtualized_DuplicateKeys_DoNotThrow_AndUniqueRowsKeepTheirState()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderTable(
			[new(1, "Ada"), new(2, "Bob"), new(2, "Cy"), new(3, "Dee")],
			extra: p => p
				.Add(t => t.Virtualize, true)
				.Add(t => t.Height, "300px"));
		string ada = InstanceOf(cut, "Ada");
		string dee = InstanceOf(cut, "Dee");

		await SortByNameDescending(cut);

		Assert.Equal(["Dee", "Cy", "Bob", "Ada"], Names(cut));
		Assert.Equal(ada, InstanceOf(cut, "Ada"));
		Assert.Equal(dee, InstanceOf(cut, "Dee"));
	}

	private IRenderedComponent<MokaTable<Person>> RenderTable(
		Person[] people,
		bool keyById = true,
		Action<ComponentParameterCollectionBuilder<MokaTable<Person>>>? extra = null) =>
		Render<MokaTable<Person>>(p =>
		{
			p.Add(t => t.Items, people);
			p.Add(t => t.ChildContent, Columns);
			if (keyById)
			{
				p.Add(t => t.ItemKey, static x => x.Id);
			}

			extra?.Invoke(p);
		});

	// The first click sorts ascending, the second descending.
	private static async Task SortByNameDescending(IRenderedComponent<MokaTable<Person>> cut)
	{
		await cut.FindAll("thead th")[^1].ClickAsync(new MouseEventArgs());
		await cut.FindAll("thead th")[^1].ClickAsync(new MouseEventArgs());
	}

	private static async Task ExpandRowOfAsync(IRenderedComponent<MokaTable<Person>> cut, string name)
	{
		IElement row = ProbeOf(cut, name).Closest("tr")!;
		await row.QuerySelector(".moka-table-expand-btn")!.ClickAsync(new MouseEventArgs());
	}

	private static List<string> Names(IRenderedComponent<MokaTable<Person>> cut) =>
		cut.FindAll("tbody tr[data-row-index] .row-probe")
			.Select(e => e.GetAttribute("data-name") ?? "")
			.ToList();

	private static string InstanceOf(IRenderedComponent<MokaTable<Person>> cut, string name) =>
		ProbeOf(cut, name).GetAttribute("data-instance") ?? "";

	private static IElement ProbeOf(IRenderedComponent<MokaTable<Person>> cut, string name) =>
		cut.Find($".row-probe[data-name='{name}']");

	private static void Columns(RenderTreeBuilder builder)
	{
		builder.OpenComponent<MokaColumn<Person>>(0);
		builder.AddAttribute(1, nameof(MokaColumn<Person>.Title), "Name");
		builder.AddAttribute(2, nameof(MokaColumn<Person>.Field), (Func<Person, object?>)(static x => x.Name));
		builder.AddAttribute(3, nameof(MokaColumn<Person>.CellTemplate),
			(RenderFragment<Person>)(person => b => Probe(b, person.Name)));
		builder.CloseComponent();
	}

	private static void Probe(RenderTreeBuilder builder, string name)
	{
		builder.OpenComponent<RowProbe>(0);
		builder.AddAttribute(1, nameof(RowProbe.Name), name);
		builder.CloseComponent();
	}

	private sealed record Person(int Id, string Name);

	/// <summary>Renders the number of the component instance, which changes when a row is rebuilt.</summary>
	public sealed class RowProbe : ComponentBase
	{
		private static int _created;
		private readonly int _instance = Interlocked.Increment(ref _created);

		/// <summary>The name of the row the probe sits in.</summary>
		[Parameter]
		public string Name { get; set; } = "";

		/// <inheritdoc />
		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
			ArgumentNullException.ThrowIfNull(builder);
			builder.OpenElement(0, "span");
			builder.AddAttribute(1, "class", "row-probe");
			builder.AddAttribute(2, "data-name", Name);
			builder.AddAttribute(3, "data-instance", _instance.ToString(CultureInfo.InvariantCulture));
			builder.CloseElement();
		}
	}
}
