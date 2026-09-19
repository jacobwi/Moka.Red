using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Forms.FormBuilder;

namespace Moka.Red.Forms.Tests.Components;

public class MokaFormBuilderTests : BunitContext
{
	public MokaFormBuilderTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	// Rows were keyed by Id. Fields loaded from outside can share one, and Blazor throws on a
	// repeated sibling key at the next render.
	[Fact]
	public void FieldsThatShareAnId_SurviveARerender()
	{
		List<MokaFormField> fields =
		[
			new() { Id = "same", Label = "First" },
			new() { Id = "same", Label = "Second" }
		];

		IRenderedComponent<MokaFormBuilder> cut = Render<MokaFormBuilder>(p => p.Add(x => x.Fields, fields));
		cut.Render(p => p.Add(x => x.Fields, fields));

		Assert.Equal(2, cut.FindAll(".moka-form-builder__field-item").Count);
	}

	[Fact]
	public async Task SelectingAField_HighlightsOnlyThatOne_WhenIdsRepeat()
	{
		List<MokaFormField> fields =
		[
			new() { Id = "same", Label = "First" },
			new() { Id = "same", Label = "Second" }
		];
		IRenderedComponent<MokaFormBuilder> cut = Render<MokaFormBuilder>(p => p.Add(x => x.Fields, fields));

		await cut.FindAll(".moka-form-builder__field-item")[1].ClickAsync(new MouseEventArgs());

		Assert.Single(cut.FindAll(".moka-form-builder__field-item--selected"));
	}

	// The builder used to add, remove and move fields in the list the parent passed.
	[Fact]
	public async Task AddingAField_ReportsANewList_AndLeavesTheCallersListAlone()
	{
		List<MokaFormField> fields = [new() { Label = "Existing" }];
		IList<MokaFormField>? reported = null;

		IRenderedComponent<MokaFormBuilder> cut = Render<MokaFormBuilder>(p => p
			.Add(x => x.Fields, fields)
			.Add(x => x.FieldsChanged, list => reported = list));

		await cut.Find(".moka-form-builder__palette-item").ClickAsync(new MouseEventArgs());

		Assert.Single(fields);
		Assert.NotNull(reported);
		Assert.NotSame(fields, reported);
		Assert.Equal(2, reported.Count);
		Assert.Equal(2, cut.FindAll(".moka-form-builder__field-item").Count);
	}
}
