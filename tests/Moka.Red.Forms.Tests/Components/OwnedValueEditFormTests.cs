using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Moka.Red.Forms.CodeEditor;
using Moka.Red.Forms.NumericField;
using Moka.Red.Forms.Switch;
using Moka.Red.Forms.TextField;

namespace Moka.Red.Forms.Tests.Components;

// Inside an EditForm the field is validated on the parent's value: a change the parent refuses is validated as the
// value the model kept, and one it takes as the new value.
public sealed class OwnedValueEditFormTests : BunitContext
{
	public OwnedValueEditFormTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Theory]
	[InlineData(false, true)]
	[InlineData(true, false)]
	public async Task AName_IsValidatedAsTheModelHoldsIt(bool accept, bool required)
	{
		IRenderedComponent<FormHost> cut = Render<FormHost>(p => p.Add(x => x.Accept, accept).Add(x => x.Field, nameof(Plan.Name)));

		await cut.Find("input").InputAsync(new ChangeEventArgs { Value = "Grace" });

		Assert.Equal(accept ? "Grace" : "", cut.Instance.Model.Name);
		Assert.Equal(accept ? "Grace" : "", cut.Find("input").GetAttribute("value"));
		Assert.Equal(required, cut.Instance.Messages(() => cut.Instance.Model.Name).Any());
	}

	[Theory]
	[InlineData(false, false)]
	[InlineData(true, true)]
	public async Task ACount_IsValidatedAsTheModelHoldsIt(bool accept, bool outOfRange)
	{
		IRenderedComponent<FormHost> cut = Render<FormHost>(p => p.Add(x => x.Accept, accept).Add(x => x.Field, nameof(Plan.Count)));

		await cut.Find("input").InputAsync(new ChangeEventArgs { Value = "20" });

		Assert.Equal(accept ? 20 : 1, cut.Instance.Model.Count);
		Assert.Equal(accept ? "20" : "1", cut.Find("input").GetAttribute("value"));
		Assert.Equal(outOfRange, cut.Instance.Messages(() => cut.Instance.Model.Count).Any());
	}

	[Theory]
	[InlineData(false, true)]
	[InlineData(true, false)]
	public async Task AnAgreement_IsValidatedAsTheModelHoldsIt(bool accept, bool missing)
	{
		IRenderedComponent<FormHost> cut = Render<FormHost>(p => p.Add(x => x.Accept, accept).Add(x => x.Field, nameof(Plan.Agree)));

		await cut.Find("input[type=checkbox]").ChangeAsync(new ChangeEventArgs { Value = true });

		Assert.Equal(accept, cut.Instance.Model.Agree);
		Assert.Equal(accept, cut.Find("input[type=checkbox]").HasAttribute("checked"));
		Assert.Equal(missing, cut.Instance.Messages(() => cut.Instance.Model.Agree).Any());
	}

	[Theory]
	[InlineData(false, true)]
	[InlineData(true, false)]
	public async Task Code_IsValidatedAsTheModelHoldsIt(bool accept, bool required)
	{
		IRenderedComponent<FormHost> cut = Render<FormHost>(p => p.Add(x => x.Accept, accept).Add(x => x.Field, nameof(Plan.Code)));

		await cut.Find("textarea").InputAsync(new ChangeEventArgs { Value = "{ }" });

		Assert.Equal(accept ? "{ }" : "", cut.Instance.Model.Code);
		Assert.Equal(accept ? "{ }" : "", cut.Find("textarea").GetAttribute("value"));
		Assert.Equal(required, cut.Instance.Messages(() => cut.Instance.Model.Code).Any());
	}

	public sealed class Plan
	{
		[Required]
		public string Name { get; set; } = "";

		[Range(1, 10)]
		public int Count { get; set; } = 1;

		[Range(typeof(bool), "true", "true")]
		public bool Agree { get; set; }

		[Required]
		public string Code { get; set; } = "";
	}

	// Owns the model's value one way: Value, ValueChanged and ValueExpression, taking a change only when Accept is set.
	private sealed class FormHost : ComponentBase
	{
		private EditContext? _editContext;

		[Parameter]
		public bool Accept { get; set; }

		[Parameter]
		public string Field { get; set; } = "";

		public Plan Model { get; } = new();

		public IEnumerable<string> Messages<T>(Expression<Func<T>> field) => _editContext!.GetValidationMessages(FieldIdentifier.Create(field));

		protected override void OnInitialized() => _editContext = new EditContext(Model);

		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
			builder.OpenComponent<EditForm>(0);
			builder.AddComponentParameter(1, nameof(EditForm.EditContext), _editContext);
			builder.AddComponentParameter(2, nameof(EditForm.ChildContent), (RenderFragment<EditContext>)(_ => Fields));
			builder.CloseComponent();
		}

		private void Fields(RenderTreeBuilder builder)
		{
			builder.OpenComponent<DataAnnotationsValidator>(0);
			builder.CloseComponent();

			switch (Field)
			{
				case nameof(Plan.Name):
					builder.OpenComponent<MokaTextField>(1);
					builder.AddComponentParameter(2, nameof(MokaTextField.Value), Model.Name);
					builder.AddComponentParameter(3, nameof(MokaTextField.ValueChanged),
						EventCallback.Factory.Create<string>(this, value => Model.Name = Accept ? value : Model.Name));
					builder.AddComponentParameter(4, nameof(MokaTextField.ValueExpression), (Expression<Func<string>>)(() => Model.Name));
					builder.CloseComponent();
					break;
				case nameof(Plan.Count):
					builder.OpenComponent<MokaNumericField<int>>(5);
					builder.AddComponentParameter(6, nameof(MokaNumericField<int>.Value), Model.Count);
					builder.AddComponentParameter(7, nameof(MokaNumericField<int>.ValueChanged),
						EventCallback.Factory.Create<int>(this, value => Model.Count = Accept ? value : Model.Count));
					builder.AddComponentParameter(8, nameof(MokaNumericField<int>.ValueExpression), (Expression<Func<int>>)(() => Model.Count));
					builder.CloseComponent();
					break;
				case nameof(Plan.Code):
					builder.OpenComponent<MokaCodeEditor>(13);
					builder.AddComponentParameter(14, nameof(MokaCodeEditor.Value), Model.Code);
					builder.AddComponentParameter(15, nameof(MokaCodeEditor.ValueChanged),
						EventCallback.Factory.Create<string>(this, value => Model.Code = Accept ? value : Model.Code));
					builder.AddComponentParameter(16, nameof(MokaCodeEditor.ValueExpression), (Expression<Func<string>>)(() => Model.Code));
					builder.CloseComponent();
					break;
				default:
					builder.OpenComponent<MokaSwitch>(9);
					builder.AddComponentParameter(10, nameof(MokaSwitch.Value), Model.Agree);
					builder.AddComponentParameter(11, nameof(MokaSwitch.ValueChanged),
						EventCallback.Factory.Create<bool>(this, value => Model.Agree = Accept ? value : Model.Agree));
					builder.AddComponentParameter(12, nameof(MokaSwitch.ValueExpression), (Expression<Func<bool>>)(() => Model.Agree));
					builder.CloseComponent();
					break;
			}
		}
	}
}
