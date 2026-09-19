using AngleSharp.Dom;
using Bunit;
using Bunit.Rendering;
using Moka.Red.Forms.AutoComplete;
using Moka.Red.Forms.Checkbox;
using Moka.Red.Forms.ColorInput;
using Moka.Red.Forms.ColorPicker;
using Moka.Red.Forms.ColorWheel;
using Moka.Red.Forms.CreditCard;
using Moka.Red.Forms.CurrencyInput;
using Moka.Red.Forms.DatePicker;
using Moka.Red.Forms.DateRangePicker;
using Moka.Red.Forms.FileUpload;
using Moka.Red.Forms.IpAddressInput;
using Moka.Red.Forms.Knob;
using Moka.Red.Forms.MacAddressInput;
using Moka.Red.Forms.NumericField;
using Moka.Red.Forms.OtpInput;
using Moka.Red.Forms.PasswordField;
using Moka.Red.Forms.PhoneInput;
using Moka.Red.Forms.PinInput;
using Moka.Red.Forms.RadioGroup;
using Moka.Red.Forms.Rating;
using Moka.Red.Forms.SearchInput;
using Moka.Red.Forms.SelectField;
using Moka.Red.Forms.SignaturePad;
using Moka.Red.Forms.Slider;
using Moka.Red.Forms.Switch;
using Moka.Red.Forms.TagInput;
using Moka.Red.Forms.TextArea;
using Moka.Red.Forms.TextField;
using Moka.Red.Forms.TimePicker;
using Moka.Red.Forms.ToggleGroup;
using Moka.Red.Forms.TreeSelect;
using Moka.Red.Tests.Shared;
using static Moka.Red.Tests.Shared.SpacingCase;

namespace Moka.Red.Forms.Tests.Components;

// Margin, Padding and Rounded are declared on every visual component, and these dropped some or
// all of them. A field inside MokaFieldWrapper takes the margin on the wrapper and the padding and
// radius on the element that draws the field. Without them every style attribute has to read as it
// did before.
public class SpacingParameterTests : BunitContext
{
	private static readonly Func<string, Task<IEnumerable<string>>> NoResults =
		_ => Task.FromResult(Enumerable.Empty<string>());

	private static readonly Dictionary<string, SpacingCase> Cases = new()
	{
		["AutoComplete"] = new(typeof(MokaAutoComplete<string>), "", ("SearchFunc", NoResults)) { Box = "input" },
		["Checkbox"] = new(typeof(MokaCheckbox), "", ("Label", "Accept"))
		{
			Box = "label.moka-checkbox", RadiusOn = ".moka-checkbox-box"
		},
		// The colour components' own styles go through StyleBuilder, which ends no declaration with a
		// semicolon. Apart from that they read as recorded.
		["ColorInput"] = new(typeof(MokaColorInput), "background-color: transparent") { Box = ".moka-color-input" },
		["ColorPicker"] = new(typeof(MokaColorPicker), "background: #000000") { Box = ".moka-colorpicker-input" },
		["ColorWheel"] = new(typeof(MokaColorWheel),
			"width: 12rem; height: 12rem; background: conic-gradient(from 0deg, hsl(0,100%,50%), " +
			"hsl(60,100%,50%), hsl(120,100%,50%), hsl(180,100%,50%), hsl(240,100%,50%), hsl(300,100%,50%), " +
			"hsl(360,100%,50%)) | background: linear-gradient(to right, hsl(1, 0%, 50%), hsl(1, 100%, 50%)) | " +
			"background: linear-gradient(to bottom, hsl(0, 0%, 100%), transparent, hsl(0, 0%, 0%)) | " +
			"left: 83%; top: 37% | background-color: #ef5350"),
		["DatePicker"] = new(typeof(MokaDatePicker), IconStyle("14px")) { Box = ".moka-datepicker-input" },
		["DateRangePicker"] = new(typeof(MokaDateRangePicker), "") { Box = ".moka-daterange__input" },
		["FileUpload"] = new(typeof(MokaFileUpload), IconStyle("24px")) { Box = ".moka-fileupload-dropzone" },
		["NumericField"] = new(typeof(MokaNumericField<int>), "") { Box = "input" },
		["PasswordField"] = new(typeof(MokaPasswordField), IconStyle("14px")) { Box = "input" },
		["PhoneInput"] = new(typeof(MokaPhoneInput), "") { Box = "input" },
		["RadioGroup"] = new(typeof(MokaRadioGroup<string>), "") { Box = ".moka-radiogroup" },
		["Rating"] = new(typeof(MokaRating), string.Join(" | ", Enumerable.Repeat(IconStyle("1em"), 5)))
		{
			Box = ".moka-rating"
		},
		["Select"] = new(typeof(MokaSelect<string>), IconStyle("14px"), ("Items", new[] { "Ada" }))
		{
			Box = ".moka-select-trigger"
		},
		["Switch"] = new(typeof(MokaSwitch), "", ("Label", "Wi-Fi"))
		{
			Box = "label.moka-switch", RadiusOn = ".moka-switch-track"
		},
		["TagInput"] = new(typeof(MokaTagInput), "") { Box = ".moka-taginput-container" },
		["TextArea"] = new(typeof(MokaTextArea), "") { Box = "textarea" },
		["TextField"] = new(typeof(MokaTextField), "") { Box = "input" },
		["TimePicker"] = new(typeof(MokaTimePicker), IconStyle("14px")) { Box = ".moka-timepicker-input" },
		["ToggleGroup"] = new(typeof(MokaToggleGroup), ""),
		["TreeSelect"] = new(typeof(MokaTreeSelect<string>), IconStyle("14px")) { Box = ".moka-tree-select-trigger" },
		["Knob"] = new(typeof(MokaKnob), ""),
		["SignaturePad"] = new(typeof(MokaSignaturePad),
			"width: 100% | height: 200px; background-color: #ffffff | " + IconStyle("12px") + " | " + IconStyle("12px"))
		{
			Box = ".moka-signature-pad__canvas-wrap"
		},
		["CurrencyInput"] = new(typeof(MokaCurrencyInput), "") { Box = "input" },
		["SearchInput"] = new(typeof(MokaSearchInput), IconStyle("14px")) { Box = "input" },
		["CreditCardInput"] = new(typeof(MokaCreditCardInput), "") { RadiusOn = ".moka-creditcard-input" },
		["RangeSlider"] = new(typeof(MokaRangeSlider), "--moka-range-start: 0.00%; --moka-range-end: 100.00%")
		{
			Box = ".moka-range-slider", RadiusOn = ".moka-range-slider-track"
		},
		["Slider"] = new(typeof(MokaSlider), "--moka-slider-fill: 0.00%")
		{
			Box = ".moka-slider", RadiusOn = ".moka-slider-input"
		},
		["OtpInput"] = new(typeof(MokaOtpInput), "") { RadiusOn = ".moka-otp-digit" },
		["PinInput"] = new(typeof(MokaPinInput), "") { RadiusOn = ".moka-pin-digit" },
		["IpAddressInput"] = new(typeof(MokaIpAddressInput), "") { RadiusOn = ".moka-ip-octet" },
		["IpAddressInputV6"] = new(typeof(MokaIpAddressInput), "", ("AllowIPv6", true)) { RadiusOn = ".moka-ip-octet" },
		["MacAddressInput"] = new(typeof(MokaMacAddressInput), "") { RadiusOn = ".moka-mac-octet" }
	};

	// The components that draw several boxes for one value. Each box draws its own border, so each
	// takes the radius.
	public static TheoryData<string, int> BoxedCases => new()
	{
		{ "CreditCardInput", 4 },
		{ "OtpInput", 6 },
		{ "PinInput", 4 },
		{ "IpAddressInput", 4 },
		{ "IpAddressInputV6", 8 },
		{ "MacAddressInput", 6 }
	};

	public SpacingParameterTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	public static TheoryData<string> Names => [.. Cases.Keys];

	[Theory]
	[MemberData(nameof(Names))]
	public void SpacingParameters_LandOnTheComponent(string name)
	{
		SpacingCase spacing = Cases[name];
		IRenderedComponent<ContainerFragment> cut = Render(spacing.Render(withSpacing: true));
		spacing.OpenIfNeeded(cut);

		spacing.AssertSpacing(cut);
	}

	[Theory]
	[MemberData(nameof(Names))]
	public void Styles_AreUnchanged_WithoutSpacingParameters(string name)
	{
		SpacingCase spacing = Cases[name];
		IRenderedComponent<ContainerFragment> cut = Render(spacing.Render(withSpacing: false));
		spacing.OpenIfNeeded(cut);

		Assert.Equal(spacing.DefaultStyles, SpacingCase.Styles(cut));
	}

	[Theory]
	[MemberData(nameof(BoxedCases))]
	public void Rounded_ShapesEveryBox(string name, int boxes)
	{
		SpacingCase spacing = Cases[name];
		IRenderedComponent<ContainerFragment> cut = Render(spacing.Render(withSpacing: true));

		IReadOnlyList<IElement> shaped = cut.FindAll(spacing.RadiusOn!);
		Assert.Equal(boxes, shaped.Count);
		Assert.All(shaped, box => Assert.Equal("border-radius: 3px", box.GetAttribute("style")));
	}
}
