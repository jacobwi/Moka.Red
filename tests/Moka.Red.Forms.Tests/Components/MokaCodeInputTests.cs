using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using AngleSharp.Dom;
using Bunit;
using Bunit.Rendering;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Moka.Red.Core.Enums;
using Moka.Red.Forms.OtpInput;
using Moka.Red.Forms.PinInput;

namespace Moka.Red.Forms.Tests.Components;

/// <summary>
///     The one-time code and the PIN were a box per digit, an input each: a pasted code filled one box, the browser's
///     one-time code offer filled one box, and a screen reader heard six fields called "Digit 1 of 6". They are one
///     field over the boxes now, as web.dev, GOV.UK and the WCAG 2.2 understanding of 3.3.8 point to.
/// </summary>
public class MokaCodeInputTests : BunitContext
{
	private const string ModulePath = "./_content/Moka.Red.Forms/moka-code.js";
	private const string RequiredMessage = "Code is required";

	private static readonly Dictionary<string, (Type Type, string Root, int Length, string Autocomplete, string FieldType)> Cases = new()
	{
		["Otp"] = (typeof(MokaOtpInput), "moka-otp", 6, "one-time-code", "text"),
		["Pin"] = (typeof(MokaPinInput), "moka-pin", 4, "off", "password")
	};

	public MokaCodeInputTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	public static TheoryData<string> Inputs => [.. Cases.Keys];

	[Theory]
	[MemberData(nameof(Inputs))]
	public void RendersOneField_OverABoxPerDigit(string input)
	{
		IRenderedComponent<ContainerFragment> cut = RenderCode(input);

		IElement field = Assert.Single(cut.FindAll("input"));
		Assert.Equal(Cases[input].Autocomplete, field.GetAttribute("autocomplete"));
		Assert.Equal(Cases[input].FieldType, field.GetAttribute("type"));
		Assert.Equal("numeric", field.GetAttribute("inputmode"));
		Assert.Equal("false", field.GetAttribute("spellcheck"));
		Assert.False(field.HasAttribute("maxlength"));
		Assert.Equal(Cases[input].Length.ToString(System.Globalization.CultureInfo.InvariantCulture), field.GetAttribute("data-moka-length"));
		Assert.Equal(Cases[input].Length, cut.FindAll("[data-moka-boxes] > [data-moka-box]").Count);
		Assert.Empty(cut.FindAll("[role=group]"));
	}

	[Theory]
	[MemberData(nameof(Inputs))]
	public void TheLabel_NamesTheField(string input)
	{
		IRenderedComponent<ContainerFragment> cut = RenderCode(input, ("Label", "Code"));

		IElement field = cut.Find("input");
		IElement label = cut.Find("label");
		Assert.Equal(field.Id, label.GetAttribute("for"));
		Assert.False(string.IsNullOrEmpty(field.Id));
		Assert.False(field.HasAttribute("aria-label"));
	}

	[Theory]
	[InlineData("Otp", "One-time code")]
	[InlineData("Pin", "PIN")]
	public void WithoutALabel_TheFieldHasANameOfItsOwn(string input, string name)
	{
		IRenderedComponent<ContainerFragment> cut = RenderCode(input);

		Assert.Equal(name, cut.Find("input").GetAttribute("aria-label"));
	}

	[Theory]
	[MemberData(nameof(Inputs))]
	public void TheAppsOwnName_WinsOverTheDefault(string input)
	{
		IRenderedComponent<ContainerFragment> cut = RenderCode(input, ("aria-label", "Bank code"));

		Assert.Equal("Bank code", cut.Find("input").GetAttribute("aria-label"));
	}

	[Theory]
	[MemberData(nameof(Inputs))]
	public void IdAndAttributes_GoOnTheField_ClassAndStyleOnTheRoot(string input)
	{
		IRenderedComponent<ContainerFragment> cut = RenderCode(input, ("Id", "code"), ("Class", "mine"), ("Style", "color: red"),
			("data-test", "x"));

		IElement field = cut.Find("input");
		IElement root = cut.Find($".{Cases[input].Root}");
		Assert.Equal("code", field.Id);
		Assert.Equal("x", field.GetAttribute("data-test"));
		Assert.Contains("mine", root.ClassList);
		Assert.Contains("color: red", root.GetAttribute("style"), StringComparison.Ordinal);
	}

	// A code pasted with its spaces, or typed on a full width keyboard, fits its boxes.
	[Theory]
	[InlineData("Otp", "123 456", "123456")]
	[InlineData("Otp", "12-34-56-78", "123456")]
	[InlineData("Otp", "１２３", "123")]
	[InlineData("Otp", "a1b2", "12")]
	[InlineData("Pin", "9 8 7 6 5", "9876")]
	public async Task Text_KeepsItsDigits_UpToTheLength(string input, string typed, string kept)
	{
		string? value = null;
		IRenderedComponent<ContainerFragment> cut = RenderCode(input,
			("ValueChanged", EventCallback.Factory.Create<string?>(this, v => value = v)));

		await cut.Find("input").InputAsync(new ChangeEventArgs { Value = typed });

		Assert.Equal(kept, value);
		Assert.Equal(kept, cut.Find("input").GetAttribute("value"));
	}

	// The browser holds the letter until the next render writes the digits back, which @bind makes it do.
	[Theory]
	[MemberData(nameof(Inputs))]
	public async Task ARejectedLetter_IsClearedFromTheField(string input)
	{
		IRenderedComponent<ContainerFragment> cut = RenderCode(input);
		await cut.Find("input").InputAsync(new ChangeEventArgs { Value = "1" });

		await cut.Find("input").InputAsync(new ChangeEventArgs { Value = "1a" });

		Assert.Equal("1", cut.Find("input").GetAttribute("value"));
	}

	[Theory]
	[MemberData(nameof(Inputs))]
	public async Task OnComplete_IsRaisedOnce_WhenTheBoxesFill(string input)
	{
		List<string> completed = [];
		IRenderedComponent<ContainerFragment> cut = RenderCode(input,
			("OnComplete", EventCallback.Factory.Create<string>(this, completed.Add)));
		string code = new('7', Cases[input].Length);

		await cut.Find("input").InputAsync(new ChangeEventArgs { Value = code[..^1] });
		await cut.Find("input").InputAsync(new ChangeEventArgs { Value = code });
		await cut.Find("input").InputAsync(new ChangeEventArgs { Value = code + "1" });

		Assert.Equal([code], completed);
	}

	// Typing never writes the Value parameter, so a parent passing Value one way keeps what was typed until it passes
	// a new value.
	[Fact]
	public async Task TypedDigits_SurviveAParentRerender_UntilANewValueArrives()
	{
		IRenderedComponent<MokaOtpInput> cut = Render<MokaOtpInput>(p => p.Add(x => x.Value, "1"));
		await cut.Find("input").InputAsync(new ChangeEventArgs { Value = "12" });

		cut.Render(p => p.Add(x => x.Value, "1"));
		Assert.Equal("12", cut.Find("input").GetAttribute("value"));

		cut.Render(p => p.Add(x => x.Value, "9"));
		Assert.Equal("9", cut.Find("input").GetAttribute("value"));
	}

	// The boxes show what fits: a longer value from the parent shows its first digits.
	[Fact]
	public void AParentValue_ShowsItsDigits_UpToTheLength()
	{
		IRenderedComponent<MokaPinInput> cut = Render<MokaPinInput>(p => p.Add(x => x.Value, "12 34 56"));

		Assert.Equal("1234", cut.Find("input").GetAttribute("value"));
	}

	[Theory]
	[InlineData("Otp", false, "text")]
	[InlineData("Otp", true, "password")]
	[InlineData("Pin", false, "text")]
	[InlineData("Pin", true, "password")]
	public void Masked_MakesItAPasswordField(string input, bool masked, string type)
	{
		IRenderedComponent<ContainerFragment> cut = RenderCode(input, ("Masked", masked));

		Assert.Equal(type, cut.Find("input").GetAttribute("type"));
		Assert.Equal(masked, cut.Find($".{Cases[input].Root}").ClassList.Contains($"{Cases[input].Root}--masked"));
	}

	[Theory]
	[MemberData(nameof(Inputs))]
	public void Length_SetsTheBoxes_AndBelowOneIsOne(string input)
	{
		IRenderedComponent<ContainerFragment> cut = RenderCode(input, ("Length", 8));
		Assert.Equal(8, cut.FindAll("[data-moka-box]").Count);
		Assert.Contains("--_code-length: 8", cut.Find("[data-moka-boxes]").GetAttribute("style"), StringComparison.Ordinal);

		IRenderedComponent<ContainerFragment> none = RenderCode(input, ("Length", 0));
		Assert.Single(none.FindAll("[data-moka-box]"));
	}

	[Theory]
	[MemberData(nameof(Inputs))]
	public void Disabled_DisablesTheField(string input)
	{
		IRenderedComponent<ContainerFragment> cut = RenderCode(input, ("Disabled", true));

		Assert.True(cut.Find("input").HasAttribute("disabled"));
		Assert.Contains($"{Cases[input].Root}--disabled", cut.Find($".{Cases[input].Root}").ClassList);
	}

	[Fact]
	public void ThePin_TakesItsSize()
	{
		IRenderedComponent<MokaPinInput> cut = Render<MokaPinInput>(p => p.Add(x => x.Size, MokaSize.Lg));

		Assert.Contains("moka-pin--lg", cut.Find(".moka-pin").ClassList);
	}

	[Fact]
	public void TheOtp_TakesFocusByDefault()
	{
		Assert.True(Render<MokaOtpInput>().Find("input").HasAttribute("autofocus"));
		Assert.False(Render<MokaOtpInput>(p => p.Add(x => x.AutoFocus, false)).Find("input").HasAttribute("autofocus"));
		Assert.False(Render<MokaPinInput>().Find("input").HasAttribute("autofocus"));
	}

	// A static page posts the form, so the field carries the bound field's name.
	[Theory]
	[MemberData(nameof(Inputs))]
	public void BoundToAField_TheFieldIsNamedForAPost(string input)
	{
		Model model = new() { Code = "12" };
		IRenderedComponent<ContainerFragment> cut = RenderCode(input,
			("Value", model.Code), ("ValueExpression", (Expression<Func<string?>>)(() => model.Code)));

		Assert.Equal("model.Code", cut.Find("input").GetAttribute("name"));
	}

	[Theory]
	[MemberData(nameof(Inputs))]
	public async Task TheField_IsBoundToTheBrowsersScript_AndUnboundWhenDisposed(string input)
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(ModulePath);
		module.Mode = JSRuntimeMode.Loose;
		IRenderedComponent<ContainerFragment> cut = RenderCode(input);

		module.VerifyInvoke("bindCode");
		module.VerifyNotInvoke("readSmsCode");

		await DisposeComponentsAsync();
		module.VerifyInvoke("unbindCode");
	}

	[Fact]
	public void FillFromSms_AsksTheBrowser_AndStopsWhenTurnedOff()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(ModulePath);
		module.Mode = JSRuntimeMode.Loose;
		IRenderedComponent<MokaOtpInput> cut = Render<MokaOtpInput>(p => p.Add(x => x.FillFromSms, true));

		Assert.True(Assert.Single(module.Invocations, i => i.Identifier == "readSmsCode").Arguments[1] is true);

		cut.Render(p => p.Add(x => x.FillFromSms, false));

		Assert.Equal([true, false], module.Invocations.Where(i => i.Identifier == "readSmsCode").Select(i => i.Arguments[1]));
	}

	[Theory]
	[MemberData(nameof(Inputs))]
	public void FailedSubmit_ShowsTheMessage_DescribingTheField(string input)
	{
		IRenderedComponent<EditForm> cut = RenderInForm(input, new Model());
		Assert.Equal("false", cut.Find("input").GetAttribute("aria-invalid"));

		cut.Find("form").Submit();

		IElement field = cut.Find("input");
		string? messageId = field.GetAttribute("aria-describedby");
		Assert.False(string.IsNullOrEmpty(messageId));
		Assert.Equal(RequiredMessage, cut.Find($"#{messageId}").TextContent.Trim());
		Assert.Equal("true", field.GetAttribute("aria-invalid"));
		IElement root = cut.Find($".{Cases[input].Root}");
		Assert.Contains("invalid", root.ClassList);
		Assert.Contains($"{Cases[input].Root}--error", root.ClassList);
	}

	[Theory]
	[MemberData(nameof(Inputs))]
	public async Task Typing_NotifiesTheEditContext_AndClearsTheMessage(string input)
	{
		Model model = new();
		EditContext context = new(model);
		IRenderedComponent<EditForm> cut = RenderInForm(input, model, context);
		await cut.Find("form").SubmitAsync();

		await cut.Find("input").InputAsync(new ChangeEventArgs { Value = "1" });

		FieldIdentifier code = new(model, nameof(Model.Code));
		Assert.True(context.IsModified(code));
		Assert.Equal("1", model.Code);
		IElement root = cut.Find($".{Cases[input].Root}");
		Assert.Contains("modified", root.ClassList);
		Assert.DoesNotContain("invalid", root.ClassList);
		Assert.Equal("false", cut.Find("input").GetAttribute("aria-invalid"));
		Assert.DoesNotContain(RequiredMessage, cut.Markup, StringComparison.Ordinal);
	}

	[Theory]
	[MemberData(nameof(Inputs))]
	public void Required_MarksTheLabelAndTheField(string input)
	{
		IRenderedComponent<EditForm> cut = RenderInForm(input, new Model(), extra: builder =>
		{
			builder.AddAttribute(10, "Label", "Code");
			builder.AddAttribute(11, "Required", true);
		});

		IElement label = cut.Find("label");
		Assert.StartsWith("Code", label.TextContent.Trim(), StringComparison.Ordinal);
		Assert.Equal("*", Assert.Single(label.QuerySelectorAll("[aria-hidden=true]")).TextContent.Trim());
		Assert.Equal("true", cut.Find("input").GetAttribute("aria-required"));
	}

	[Theory]
	[MemberData(nameof(Inputs))]
	public void ExplicitErrorText_WinsOverTheValidationMessage(string input)
	{
		IRenderedComponent<EditForm> cut = RenderInForm(input, new Model(),
			extra: builder => builder.AddAttribute(10, "ErrorText", "Custom message"));

		cut.Find("form").Submit();

		string? messageId = cut.Find("input").GetAttribute("aria-describedby");
		Assert.Equal("Custom message", cut.Find($"#{messageId}").TextContent.Trim());
	}

	[Theory]
	[MemberData(nameof(Inputs))]
	public void TheAppsDescription_ComesFirst(string input)
	{
		IRenderedComponent<ContainerFragment> cut = RenderCode(input, ("HelperText", "Sent to your phone"),
			("aria-describedby", "hint"));

		string? describedBy = cut.Find("input").GetAttribute("aria-describedby");
		Assert.StartsWith("hint ", describedBy, StringComparison.Ordinal);
		Assert.Equal("Sent to your phone", cut.Find($"#{describedBy!["hint ".Length..]}").TextContent.Trim());
	}

	[Theory]
	[MemberData(nameof(Inputs))]
	public void OutsideAnEditForm_RendersPlainAriaStates(string input)
	{
		IElement field = RenderCode(input).Find("input");

		Assert.Equal("false", field.GetAttribute("aria-invalid"));
		Assert.False(field.HasAttribute("aria-required"));
		Assert.False(field.HasAttribute("aria-describedby"));
	}

	private IRenderedComponent<ContainerFragment> RenderCode(string input, params (string Name, object? Value)[] parameters) =>
		Render(tree =>
		{
			tree.OpenComponent(0, Cases[input].Type);
			for (int i = 0; i < parameters.Length; i++)
			{
				tree.AddAttribute(i + 1, parameters[i].Name, parameters[i].Value);
			}

			tree.CloseComponent();
		});

	private IRenderedComponent<EditForm> RenderInForm(
		string input,
		Model model,
		EditContext? context = null,
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
				builder.AddAttribute(3, "ValueChanged",
					EventCallback.Factory.Create<string?>(this, value => model.Code = value));
				builder.AddAttribute(4, "ValueExpression", (Expression<Func<string?>>)(() => model.Code));
				extra?.Invoke(builder);
				builder.CloseComponent();
			}));
	}

	private sealed class Model
	{
		[Required(ErrorMessage = RequiredMessage)]
		public string? Code { get; set; }
	}
}
