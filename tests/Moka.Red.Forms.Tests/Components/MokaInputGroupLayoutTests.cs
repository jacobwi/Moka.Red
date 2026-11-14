using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Moka.Red.Forms.InputGroup;
using Moka.Red.Forms.TextField;
using Moka.Red.Primitives.Button;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Forms.Tests.Components;

// The group was a flex row that stretched its children, so a field with a label or helper text made a joined button
// as tall as label, box and helper together. It is now a grid of three rows, labels, boxes and helper lines, so the
// button meets the box whatever a field draws above or below it.
public class MokaInputGroupLayoutTests : BunitContext
{
	private const string Css = "MokaInputGroup.razor.css";

	[Fact]
	public void EveryChild_SitsInTheBoxRow()
	{
		IReadOnlyDictionary<string, string> group = ScopedCss.Declarations(Css, ".moka-input-group");
		Assert.Equal("inline-grid", group["display"]);
		Assert.Equal("auto auto auto", group["grid-template-rows"]);
		Assert.Equal("2", ScopedCss.Declarations(Css, ".moka-input-group ::deep > *")["grid-row"]);
	}

	[Fact]
	public void AField_SpansTheThreeRows_WithItsLabelBoxAndHelperOnThem()
	{
		IReadOnlyDictionary<string, string> field = ScopedCss.Declarations(Css, ".moka-input-group ::deep > .moka-field.moka-field");
		Assert.Equal("1 / span 3", field["grid-row"]);
		Assert.Equal("subgrid", field["grid-template-rows"]);
		Assert.Equal("0", field["row-gap"]);
		Assert.Equal("1", ScopedCss.Declarations(Css, ".moka-input-group ::deep > .moka-field > .moka-field-label")["grid-row"]);
		Assert.Equal("2", ScopedCss.Declarations(Css, ".moka-input-group ::deep > .moka-field > .moka-field-input-wrapper")["grid-row"]);
		Assert.Equal("3", ScopedCss.Declarations(Css, ".moka-input-group ::deep > .moka-field > .moka-field-helper")["grid-row"]);
	}

	// The rules name the field's own elements, so they hold only while a field renders them as the group's children.
	[Fact]
	public void ALabeledFieldAndAButton_AreTheGroupsChildren_AsTheRulesExpect()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		IRenderedComponent<MokaInputGroup> cut = Render<MokaInputGroup>(p => p
			.AddChildContent(builder =>
			{
				builder.OpenComponent<MokaTextField>(0);
				builder.AddAttribute(1, nameof(MokaTextField.Label), "Folder name");
				builder.AddAttribute(2, nameof(MokaTextField.HelperText), "Letters, digits and dashes.");
				builder.CloseComponent();
				builder.OpenComponent<MokaButton>(3);
				builder.AddAttribute(4, nameof(MokaButton.ChildContent), (RenderFragment)(b => b.AddContent(0, "Create")));
				builder.CloseComponent();
			}));

		IElement group = cut.Find(".moka-input-group");
		IElement field = group.Children[0];
		Assert.Contains("moka-field", field.ClassList);
		Assert.Equal(["moka-field-label", "moka-field-input-wrapper", "moka-field-helper"],
			field.Children.Select(child => child.ClassList[0]));
		Assert.Contains("moka-btn", group.Children[1].ClassList);
	}
}
