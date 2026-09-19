using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Moka.Red.Forms.IpAddressInput;
using Moka.Red.Forms.MacAddressInput;
using Moka.Red.Forms.OtpInput;
using Moka.Red.Forms.PinInput;

namespace Moka.Red.Forms.Tests.Components;

/// <summary>
///     OTP, PIN, IP and MAC share <c>MokaSegmentedInputBase</c>, which used to sit outside EditForm:
///     DataAnnotations messages never showed, the field classes never reached the DOM, and typing
///     never reached the EditContext. Parameters go in by name so these tests also run against a
///     base without them.
/// </summary>
public class MokaSegmentedInputValidationTests : BunitContext
{
	private const string RequiredMessage = "Code is required";

	private static readonly Dictionary<string, (Type Type, string RootClass)> Cases = new()
	{
		[nameof(MokaOtpInput)] = (typeof(MokaOtpInput), "moka-otp"),
		[nameof(MokaPinInput)] = (typeof(MokaPinInput), "moka-pin"),
		[nameof(MokaIpAddressInput)] = (typeof(MokaIpAddressInput), "moka-ip"),
		[nameof(MokaMacAddressInput)] = (typeof(MokaMacAddressInput), "moka-mac")
	};

	public MokaSegmentedInputValidationTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	public static TheoryData<string> Inputs => new()
	{
		nameof(MokaOtpInput),
		nameof(MokaPinInput),
		nameof(MokaIpAddressInput),
		nameof(MokaMacAddressInput)
	};

	[Theory]
	[MemberData(nameof(Inputs))]
	public void FailedSubmit_ShowsTheMessage_DescribingEveryBox(string input)
	{
		IRenderedComponent<EditForm> cut = RenderInForm(input, new Model());

		cut.Find("form").Submit();

		IReadOnlyList<IElement> boxes = cut.FindAll("input");
		string? messageId = boxes[0].GetAttribute("aria-describedby");
		Assert.False(string.IsNullOrEmpty(messageId));
		Assert.All(boxes, box => Assert.Equal(messageId, box.GetAttribute("aria-describedby")));
		Assert.Equal(RequiredMessage, cut.Find($"#{messageId}").TextContent.Trim());
	}

	[Theory]
	[MemberData(nameof(Inputs))]
	public void FailedSubmit_MarksEveryBoxInvalid(string input)
	{
		IRenderedComponent<EditForm> cut = RenderInForm(input, new Model());
		Assert.All(cut.FindAll("input"), box => Assert.Equal("false", box.GetAttribute("aria-invalid")));

		cut.Find("form").Submit();

		Assert.All(cut.FindAll("input"), box => Assert.Equal("true", box.GetAttribute("aria-invalid")));
	}

	[Theory]
	[MemberData(nameof(Inputs))]
	public void FailedSubmit_AddsTheInvalidAndErrorClasses(string input)
	{
		IRenderedComponent<EditForm> cut = RenderInForm(input, new Model());
		Assert.Contains("valid", Root(cut, input).ClassList);

		cut.Find("form").Submit();

		IElement root = Root(cut, input);
		Assert.Contains("invalid", root.ClassList);
		Assert.Contains($"{Cases[input].RootClass}--error", root.ClassList);
	}

	[Theory]
	[MemberData(nameof(Inputs))]
	public async Task Typing_NotifiesTheEditContext(string input)
	{
		Model model = new();
		EditContext context = new(model);
		List<FieldIdentifier> changed = [];
		context.OnFieldChanged += (_, e) => changed.Add(e.FieldIdentifier);
		IRenderedComponent<EditForm> cut = RenderInForm(input, model, context);

		await cut.FindAll("input")[0].InputAsync(new ChangeEventArgs { Value = "1" });

		FieldIdentifier code = new(model, nameof(Model.Code));
		Assert.Equal(code, Assert.Single(changed));
		Assert.True(context.IsModified(code));
		Assert.Contains("modified", Root(cut, input).ClassList);
	}

	// The validator reruns on the change and the input redraws from the EditContext's event.
	[Theory]
	[MemberData(nameof(Inputs))]
	public async Task Typing_AfterAFailedSubmit_ClearsTheMessage(string input)
	{
		IRenderedComponent<EditForm> cut = RenderInForm(input, new Model());
		await cut.Find("form").SubmitAsync();

		await cut.FindAll("input")[0].InputAsync(new ChangeEventArgs { Value = "1" });

		IElement root = Root(cut, input);
		Assert.Contains("valid", root.ClassList);
		Assert.DoesNotContain("invalid", root.ClassList);
		Assert.All(cut.FindAll("input"), box => Assert.Equal("false", box.GetAttribute("aria-invalid")));
		Assert.DoesNotContain(RequiredMessage, cut.Markup, StringComparison.Ordinal);
	}

	[Theory]
	[MemberData(nameof(Inputs))]
	public void Required_MarksTheLabelAndEveryBox(string input)
	{
		IRenderedComponent<EditForm> cut = RenderInForm(input, new Model(), extra: builder =>
		{
			builder.AddAttribute(10, "Label", "Code");
			builder.AddAttribute(11, "Required", true);
		});

		string? labelId = cut.Find("[role=group]").GetAttribute("aria-labelledby");
		Assert.False(string.IsNullOrEmpty(labelId));
		IElement label = cut.Find($"#{labelId}");
		Assert.StartsWith("Code", label.TextContent.Trim(), StringComparison.Ordinal);
		IElement marker = Assert.Single(label.QuerySelectorAll("[aria-hidden=true]"));
		Assert.Equal("*", marker.TextContent.Trim());
		Assert.All(cut.FindAll("input"), box => Assert.Equal("true", box.GetAttribute("aria-required")));
	}

	[Theory]
	[MemberData(nameof(Inputs))]
	public void ExplicitErrorText_WinsOverTheValidationMessage(string input)
	{
		IRenderedComponent<EditForm> cut = RenderInForm(input, new Model(),
			extra: builder => builder.AddAttribute(10, "ErrorText", "Custom message"));

		cut.Find("form").Submit();

		string? messageId = cut.Find("input").GetAttribute("aria-describedby");
		Assert.False(string.IsNullOrEmpty(messageId));
		Assert.Equal("Custom message", cut.Find($"#{messageId}").TextContent.Trim());
	}

	// Value without ValueExpression names no field, so the input stays out of validation.
	[Theory]
	[MemberData(nameof(Inputs))]
	public void WithoutAValueExpression_StaysOutOfValidation(string input)
	{
		IRenderedComponent<EditForm> cut = RenderInForm(input, new Model(), bind: false);

		cut.Find("form").Submit();

		IElement root = Root(cut, input);
		Assert.DoesNotContain("valid", root.ClassList);
		Assert.DoesNotContain("invalid", root.ClassList);
		Assert.All(cut.FindAll("input"), box => Assert.Equal("false", box.GetAttribute("aria-invalid")));
	}

	// bind:get with a method call gives an expression FieldIdentifier cannot parse. It used to work,
	// so it must not start throwing now that the input reads ValueExpression.
	[Theory]
	[MemberData(nameof(Inputs))]
	public void AValueExpressionThatNamesNoField_StaysOutOfValidation(string input)
	{
		Model model = new();
		IRenderedComponent<EditForm> cut = RenderInForm(input, model, bind: false, extra: builder =>
			builder.AddAttribute(10, "ValueExpression", (Expression<Func<string?>>)(() => model.ReadCode())));

		cut.Find("form").Submit();

		Assert.DoesNotContain("invalid", Root(cut, input).ClassList);
		Assert.All(cut.FindAll("input"), box => Assert.Equal("false", box.GetAttribute("aria-invalid")));
	}

	[Theory]
	[MemberData(nameof(Inputs))]
	public void OutsideAnEditForm_RendersPlainAriaStates(string input)
	{
		IElement root = RenderAlone(input);

		Assert.DoesNotContain("valid", root.ClassList);
		Assert.All(root.QuerySelectorAll("input"), box =>
		{
			Assert.Equal("false", box.GetAttribute("aria-invalid"));
			Assert.False(box.HasAttribute("aria-required"));
			Assert.False(box.HasAttribute("aria-describedby"));
		});
	}

	private static IElement Root(IRenderedComponent<EditForm> cut, string input) =>
		cut.Find($".{Cases[input].RootClass}");

	private IRenderedComponent<EditForm> RenderInForm(
		string input,
		Model model,
		EditContext? context = null,
		bool bind = true,
		Action<RenderTreeBuilder>? extra = null)
	{
		EditContext editContext = context ?? new EditContext(model);
		return Render<EditForm>(p => p
			.Add(f => f.EditContext, editContext)
			.Add(f => f.ChildContent, _ => builder =>
			{
				builder.OpenComponent<DataAnnotationsValidator>(0);
				builder.CloseComponent();
				builder.OpenComponent(1, Cases[input].Type);
				builder.AddAttribute(2, "Value", model.Code);
				if (bind)
				{
					builder.AddAttribute(3, "ValueChanged",
						EventCallback.Factory.Create<string?>(this, value => model.Code = value));
					builder.AddAttribute(4, "ValueExpression", (Expression<Func<string?>>)(() => model.Code));
				}

				extra?.Invoke(builder);
				builder.CloseComponent();
			}));
	}

	private IElement RenderAlone(string input) => input switch
	{
		nameof(MokaOtpInput) => Render<MokaOtpInput>(p => p.Add(x => x.Value, "1")).Find(".moka-otp"),
		nameof(MokaPinInput) => Render<MokaPinInput>(p => p.Add(x => x.Value, "1")).Find(".moka-pin"),
		nameof(MokaIpAddressInput) => Render<MokaIpAddressInput>(p => p.Add(x => x.Value, "1")).Find(".moka-ip"),
		nameof(MokaMacAddressInput) => Render<MokaMacAddressInput>(p => p.Add(x => x.Value, "1")).Find(".moka-mac"),
		_ => throw new ArgumentOutOfRangeException(nameof(input), input, null)
	};

	private sealed class Model
	{
		[Required(ErrorMessage = RequiredMessage)]
		public string? Code { get; set; }

		public string? ReadCode() => Code;
	}
}
