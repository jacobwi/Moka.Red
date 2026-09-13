using System.ComponentModel.DataAnnotations;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Moka.Red.Forms.Checkbox;
using Moka.Red.Forms.TextField;

namespace Moka.Red.Forms.Tests.Components;

/// <summary>
///     Covers the EditContext wiring. Before it existed, no Forms component surfaced
///     DataAnnotations results: HasError was purely a function of the ErrorText parameter and
///     InputBase's modified/valid/invalid classes never reached the DOM.
/// </summary>
public class MokaValidationTests : BunitContext
{
	[Fact]
	public void TextField_ShowsDataAnnotationsMessage_WhenValidationFails()
	{
		IRenderedComponent<EditForm> cut = RenderForm(new TestModel { Name = "" });

		cut.Find("form").Submit();

		IElement helper = cut.Find(".moka-field-helper");
		Assert.Contains("Name is required", helper.TextContent, StringComparison.Ordinal);
	}

	[Fact]
	public void TextField_MarksFieldAsError_WhenValidationFails()
	{
		IRenderedComponent<EditForm> cut = RenderForm(new TestModel { Name = "" });

		cut.Find("form").Submit();

		IElement field = cut.Find(".moka-field");
		Assert.Contains("moka-field--error", field.ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void TextField_EmitsInputBaseValidationClasses_WhenValidationFails()
	{
		IRenderedComponent<EditForm> cut = RenderForm(new TestModel { Name = "" });

		cut.Find("form").Submit();

		IElement root = cut.Find(".moka-textfield");
		Assert.Contains("invalid", root.ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void TextField_ShowsNoError_WhenModelIsValid()
	{
		IRenderedComponent<EditForm> cut = RenderForm(new TestModel { Name = "Ada" });

		cut.Find("form").Submit();

		IElement field = cut.Find(".moka-field");
		Assert.DoesNotContain("moka-field--error", field.ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void ExplicitErrorText_WinsOverValidationMessage()
	{
		IRenderedComponent<EditForm> cut = Render<EditForm>(p => p
			.Add(f => f.Model, new TestModel { Name = "" })
			.Add(f => f.ChildContent, _ => builder =>
			{
				builder.OpenComponent<DataAnnotationsValidator>(0);
				builder.CloseComponent();
				builder.OpenComponent<MokaTextField>(1);
				builder.AddAttribute(2, nameof(MokaTextField.Value), "");
				builder.AddAttribute(3, nameof(MokaTextField.ErrorText), "Custom message");
				builder.CloseComponent();
			}));

		cut.Find("form").Submit();

		IElement helper = cut.Find(".moka-field-helper");
		Assert.Contains("Custom message", helper.TextContent, StringComparison.Ordinal);
	}

	[Fact]
	public void TextField_RendersOutsideEditForm_WithoutThrowing()
	{
		// MokaInputBase fabricates a ValueExpression so inputs work with no EditContext.
		IRenderedComponent<MokaTextField> cut = Render<MokaTextField>(p => p.Add(f => f.Value, "hello"));

		IElement input = cut.Find("input");
		Assert.Equal("hello", input.GetAttribute("value"));
	}

	[Fact]
	public void Checkbox_AcceptsErrorText()
	{
		IRenderedComponent<MokaCheckbox> cut = Render<MokaCheckbox>(p => p
			.Add(c => c.ErrorText, "You must accept the terms"));

		IElement helper = cut.Find(".moka-field-helper");
		Assert.Contains("You must accept the terms", helper.TextContent, StringComparison.Ordinal);
	}

	[Fact]
	public void Checkbox_MarksFieldAsError_WhenErrorTextSet()
	{
		IRenderedComponent<MokaCheckbox> cut = Render<MokaCheckbox>(p => p
			.Add(c => c.ErrorText, "Required"));

		IElement field = cut.Find(".moka-field");
		Assert.Contains("moka-field--error", field.ClassName, StringComparison.Ordinal);
	}

	private IRenderedComponent<EditForm> RenderForm(TestModel model) =>
		Render<EditForm>(p => p
			.Add(f => f.Model, model)
			.Add(f => f.ChildContent, _ => builder =>
			{
				builder.OpenComponent<DataAnnotationsValidator>(0);
				builder.CloseComponent();
				builder.OpenComponent<MokaTextField>(1);
				builder.AddAttribute(2, nameof(MokaTextField.Value), model.Name);
				builder.AddAttribute(3, nameof(MokaTextField.ValueExpression),
					(System.Linq.Expressions.Expression<Func<string>>)(() => model.Name));
				builder.CloseComponent();
			}));

	private sealed class TestModel
	{
		[Required(ErrorMessage = "Name is required")]
		public string Name { get; set; } = string.Empty;
	}
}
