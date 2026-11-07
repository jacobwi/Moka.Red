using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Data.Table;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Data.Tests.Components;

// The table's wrapper hides its overflow for the rounded frame, so the column menu was cut off under a table of
// one or two rows. The menu now goes to the top layer through Core's moka-popup.js; what that does in the browser
// can't run here, so these tests cover what the table renders for it and when it calls it.
public class TopLayerPanelTests : BunitContext
{
	private static readonly Person[] People = [new("Ada", "Engineering"), new("Alan", "Research")];

	private static readonly RenderFragment Columns = builder =>
	{
		builder.OpenComponent<MokaColumn<Person>>(0);
		builder.AddComponentParameter(1, nameof(MokaColumn<Person>.Title), "Name");
		builder.AddComponentParameter(2, nameof(MokaColumn<Person>.Field), (Func<Person, object?>)(p => p.Name));
		builder.CloseComponent();
		builder.OpenComponent<MokaColumn<Person>>(3);
		builder.AddComponentParameter(4, nameof(MokaColumn<Person>.Title), "Team");
		builder.AddComponentParameter(5, nameof(MokaColumn<Person>.Field), (Func<Person, object?>)(p => p.Team));
		builder.CloseComponent();
	};

	private readonly BunitJSModuleInterop _popup;

	public TopLayerPanelTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_popup = TopLayerPopup.Setup(JSInterop);
	}

	[Fact]
	public async Task TheColumnMenu_OpensUnderItsButton_AndClosesWithIt()
	{
		// The table renders again once its columns register, and bUnit writes a reference id only on the render that
		// creates the element, so the toggle is added by a render of its own.
		IRenderedComponent<MokaTable<Person>> cut = RenderTable(showToggle: false);
		cut.Render(p => p.Add(t => t.ShowColumnToggle, true));
		string? toggleId = cut.Find(".moka-table-column-toggle").GetAttribute("blazor:elementReference");

		await cut.Find(".moka-table-column-toggle-btn").ClickAsync(new MouseEventArgs());
		TopLayerPopup.AssertOpened(_popup, cut.Find(".moka-table-column-menu"), toggleId, "bottom-end");

		await cut.Find(".moka-table-column-toggle-btn").ClickAsync(new MouseEventArgs());

		Assert.Empty(cut.FindAll(".moka-table-column-menu"));
		TopLayerPopup.AssertClosed(_popup);
	}

	[Fact]
	public async Task TheColumnMenu_ClosesWhenTheTableIsDisposedWithItOpen()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderTable();
		await cut.Find(".moka-table-column-toggle-btn").ClickAsync(new MouseEventArgs());

		await DisposeComponentsAsync();

		TopLayerPopup.AssertClosed(_popup);
	}

	[Fact]
	public void ATableWhoseMenuNeverOpens_CallsNothing()
	{
		RenderTable();

		Assert.Equal(0, TopLayerPopup.Count(_popup, "open"));
		Assert.Equal(0, TopLayerPopup.Count(_popup, "close"));
	}

	private IRenderedComponent<MokaTable<Person>> RenderTable(bool showToggle = true) =>
		Render<MokaTable<Person>>(p => p
			.Add(t => t.Items, People)
			.Add(t => t.ShowColumnToggle, showToggle)
			.Add(t => t.ChildContent, Columns));

	private sealed record Person(string Name, string Team);
}
