using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Moka.Red.Forms.AutoComplete;
using Moka.Red.Forms.ColorInput;
using Moka.Red.Forms.ColorPicker;
using Moka.Red.Forms.CurrencyInput;
using Moka.Red.Forms.DatePicker;
using Moka.Red.Forms.NumericField;
using Moka.Red.Forms.PasswordField;
using Moka.Red.Forms.PhoneInput;
using Moka.Red.Forms.SelectField;
using Moka.Red.Forms.TextArea;
using Moka.Red.Forms.TextField;
using Moka.Red.Forms.TimePicker;

namespace Moka.Red.Forms.Tests.Components;

// Required showed an asterisk that is hidden from screen readers and nothing else, so a required
// field was never announced as required.
public class AriaRequiredTests : BunitContext
{
	private static readonly string[] SelectItems = ["One", "Two"];

	public AriaRequiredTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	public static TheoryData<string> Inputs =>
	[
		"TextField", "TextArea", "PasswordField", "NumericField", "PhoneInput", "ColorInput",
		"DatePicker", "TimePicker", "ColorPicker", "Select", "AutoComplete", "CurrencyInput"
	];

	[Theory]
	[MemberData(nameof(Inputs))]
	public void ARequiredField_SaysSo_OnItsControl(string input) =>
		Assert.Equal("true", Control(input, true).GetAttribute("aria-required"));

	[Theory]
	[MemberData(nameof(Inputs))]
	public void AnOptionalField_LeavesTheAttributeOut(string input) =>
		Assert.False(Control(input, false).HasAttribute("aria-required"));

	private IElement Control(string input, bool required)
	{
		(Type type, string selector) = input switch
		{
			"TextField" => (typeof(MokaTextField), "input"),
			"TextArea" => (typeof(MokaTextArea), "textarea"),
			"PasswordField" => (typeof(MokaPasswordField), "input"),
			"NumericField" => (typeof(MokaNumericField<int>), "input"),
			"PhoneInput" => (typeof(MokaPhoneInput), "input"),
			"ColorInput" => (typeof(MokaColorInput), "input:not([type=color])"),
			"DatePicker" => (typeof(MokaDatePicker), "input"),
			"TimePicker" => (typeof(MokaTimePicker), "input"),
			"ColorPicker" => (typeof(MokaColorPicker), "input[aria-haspopup]"),
			"Select" => (typeof(MokaSelect<string>), "[role=combobox]"),
			"AutoComplete" => (typeof(MokaAutoComplete<string>), "input[role=combobox]"),
			"CurrencyInput" => (typeof(MokaCurrencyInput), "input"),
			_ => throw new ArgumentOutOfRangeException(nameof(input))
		};

		IRenderedComponent<IComponent> cut = Render<IComponent>(b =>
		{
			b.OpenComponent(0, type);
			b.AddAttribute(1, "Required", required);
			if (input == "Select")
			{
				b.AddAttribute(2, nameof(MokaSelect<string>.Items), SelectItems);
			}
			else if (input == "AutoComplete")
			{
				b.AddAttribute(2, nameof(MokaAutoComplete<string>.SearchFunc),
					(Func<string, Task<IEnumerable<string>>>)(_ => Task.FromResult<IEnumerable<string>>([])));
			}

			b.CloseComponent();
		});

		return cut.Find(selector);
	}
}
