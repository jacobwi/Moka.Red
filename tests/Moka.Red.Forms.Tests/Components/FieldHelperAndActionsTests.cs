using AngleSharp.Dom;
using Bunit;
using Bunit.Rendering;
using Microsoft.AspNetCore.Components;
using Moka.Red.Forms.AutoComplete;
using Moka.Red.Forms.Cascader;
using Moka.Red.Forms.Checkbox;
using Moka.Red.Forms.CodeEditor;
using Moka.Red.Forms.ColorInput;
using Moka.Red.Forms.ColorPicker;
using Moka.Red.Forms.Common;
using Moka.Red.Forms.CountrySelect;
using Moka.Red.Forms.CurrencyInput;
using Moka.Red.Forms.DatePicker;
using Moka.Red.Forms.EmojiPicker;
using Moka.Red.Forms.FileUpload;
using Moka.Red.Forms.NativeSelect;
using Moka.Red.Forms.NumericField;
using Moka.Red.Forms.PasswordField;
using Moka.Red.Forms.PhoneInput;
using Moka.Red.Forms.RadioGroup;
using Moka.Red.Forms.RangeBar;
using Moka.Red.Forms.SelectField;
using Moka.Red.Forms.Slider;
using Moka.Red.Forms.Switch;
using Moka.Red.Forms.TagInput;
using Moka.Red.Forms.TextArea;
using Moka.Red.Forms.TextField;
using Moka.Red.Forms.TimePicker;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Forms.Tests.Components;

// A field's helper and error line sat under the control with nothing tying it to the control, so a screen reader
// never read it with the field. And a field had no place for a button beside its box: put next to the field in a
// row, the button lined up with the label, the helper or the middle of all three, never with the box.
public class FieldHelperAndActionsTests : BunitContext
{
	private const string Help = "One letter or emoji.";

	private static readonly string[] Fruits = ["Apple", "Banana"];

	private static readonly Func<string, Task<IEnumerable<string>>> SearchFruits = query =>
		Task.FromResult(Fruits.Where(f => f.StartsWith(query, StringComparison.OrdinalIgnoreCase)));

	// Every field that draws a helper line, with the selector of the control a screen reader lands on.
	private static readonly Dictionary<string, Field> Fields = new()
	{
		["TextField"] = new(typeof(MokaTextField), "input"),
		["TextArea"] = new(typeof(MokaTextArea), "textarea"),
		["PasswordField"] = new(typeof(MokaPasswordField), "input"),
		["NumericField"] = new(typeof(MokaNumericField<int>), "input"),
		["PhoneInput"] = new(typeof(MokaPhoneInput), "input[type=tel]"),
		["ColorInput"] = new(typeof(MokaColorInput), "input:not([type=color])"),
		["CurrencyInput"] = new(typeof(MokaCurrencyInput), "input"),
		["DatePicker"] = new(typeof(MokaDatePicker), "input:not([type=hidden])"),
		["TimePicker"] = new(typeof(MokaTimePicker), "input:not([type=hidden])"),
		["ColorPicker"] = new(typeof(MokaColorPicker), "input[aria-haspopup]"),
		["Select"] = new(typeof(MokaSelect<string>), "[role=combobox]", ("Items", Fruits)),
		["NativeSelect"] = new(typeof(MokaNativeSelect<string>), "select", ("Items", Fruits)),
		["AutoComplete"] = new(typeof(MokaAutoComplete<string>), "input[role=combobox]", ("SearchFunc", SearchFruits)),
		["Cascader"] = new(typeof(MokaCascader), "[role=combobox]"),
		["CountrySelect"] = new(typeof(MokaCountrySelect), "[role=combobox]"),
		["EmojiPicker"] = new(typeof(MokaEmojiPicker), "button[aria-labelledby]"),
		["TagInput"] = new(typeof(MokaTagInput), "input"),
		["CodeEditor"] = new(typeof(MokaCodeEditor), "textarea"),
		["FileUpload"] = new(typeof(MokaFileUpload), "input[type=file]"),
		["Slider"] = new(typeof(MokaSlider), "input[type=range]"),
		["RangeBar"] = new(typeof(MokaRangeBar), "[data-moka-handle=start]"),
		["Checkbox"] = new(typeof(MokaCheckbox), "input[type=checkbox]"),
		["Switch"] = new(typeof(MokaSwitch), "input[type=checkbox]"),
		["RadioGroup"] = new(typeof(MokaRadioGroup<string>), "[role=radiogroup]")
	};

	// The fields with a one-line box, which take content beside it.
	private static readonly string[] Boxed =
	[
		"TextField", "PasswordField", "NumericField", "PhoneInput", "ColorInput", "CurrencyInput", "DatePicker",
		"TimePicker", "Select", "NativeSelect", "AutoComplete", "Cascader", "CountrySelect", "EmojiPicker", "TagInput"
	];

	private static readonly RenderFragment UseIt = builder =>
	{
		builder.OpenElement(0, "button");
		builder.AddAttribute(1, "type", "button");
		builder.AddAttribute(2, "class", "probe-action");
		builder.AddContent(3, "Use it");
		builder.CloseElement();
	};

	public FieldHelperAndActionsTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	public static TheoryData<string> AllFields => [.. Fields.Keys];

	public static TheoryData<string> BoxedFields => [.. Boxed];

	[Theory]
	[MemberData(nameof(AllFields))]
	public void TheHelperLine_DescribesTheControl(string name)
	{
		Field field = Fields[name];
		IRenderedComponent<ContainerFragment> cut = Render(field.Render(("HelperText", Help)));

		IElement control = cut.Find(field.Control);
		Assert.Equal(Help, DescriptionOf(cut, control));
	}

	[Theory]
	[MemberData(nameof(AllFields))]
	public void WithNoHelperLine_TheControlNamesNoMissingElement(string name)
	{
		Field field = Fields[name];
		IRenderedComponent<ContainerFragment> cut = Render(field.Render());

		IElement control = cut.Find(field.Control);
		string? ids = control.GetAttribute("aria-describedby");
		foreach (string id in (ids ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries))
		{
			Assert.NotEmpty(cut.FindAll($"[id='{id}']"));
		}

		Assert.Empty(cut.FindAll(".moka-field-helper"));
	}

	[Fact]
	public void TheErrorLine_DescribesTheControlInPlaceOfTheHelper()
	{
		IRenderedComponent<MokaTextField> cut = Render<MokaTextField>(p => p
			.Add(x => x.HelperText, Help)
			.Add(x => x.ErrorText, "Use one letter."));

		Assert.Equal("Use one letter.", DescriptionOf(cut, cut.Find("input")));
	}

	[Fact]
	public void TheConsumersOwnDescription_ComesFirst_AndTheAttributeIsWrittenOnce()
	{
		IRenderedComponent<MokaTextField> cut = Render<MokaTextField>(p => p
			.Add(x => x.Id, "letter")
			.Add(x => x.HelperText, Help)
			.AddUnmatched("aria-describedby", "intro"));

		IElement input = cut.Find("input");
		Assert.Equal($"intro {MokaFieldWrapper.HelperIdFor("letter")}", input.GetAttribute("aria-describedby"));
		Assert.Single(input.Attributes, a => a.Name == "aria-describedby");
	}

	[Fact]
	public void TheSuffix_AndTheHelperLine_BothDescribeTheInput()
	{
		IRenderedComponent<MokaNumericField<int>> cut = Render<MokaNumericField<int>>(p => p
			.Add(x => x.Id, "speed")
			.Add(x => x.Suffix, "Mbit/s")
			.Add(x => x.HelperText, "Up to 1000."));

		Assert.Equal($"speed-suffix {MokaFieldWrapper.HelperIdFor("speed")}", cut.Find("input").GetAttribute("aria-describedby"));
	}

	[Fact]
	public void WithoutActions_TheBoxIsTheRowsOnlyContent()
	{
		IRenderedComponent<MokaTextField> cut = Render<MokaTextField>(p => p.Add(x => x.Label, "Letter"));

		IElement row = cut.Find(".moka-field-input-wrapper");
		Assert.Equal("moka-field-input-wrapper", row.ClassName);
		Assert.Empty(cut.FindAll(".moka-field-box"));
		Assert.Empty(cut.FindAll(".moka-field-actions"));
	}

	[Fact]
	public void Actions_SitInTheBoxsRow_BetweenTheLabelAndTheHelperLine()
	{
		IRenderedComponent<MokaTextField> cut = Render<MokaTextField>(p => p
			.Add(x => x.Id, "letter")
			.Add(x => x.Label, "Letter")
			.Add(x => x.HelperText, Help)
			.Add(x => x.Actions, UseIt));

		IElement field = cut.Find(".moka-field");
		Assert.Equal(["moka-field-label", "moka-field-input-wrapper", "moka-field-helper"],
			field.Children.Select(child => child.ClassList[0]));
		IElement row = field.Children[1];
		Assert.Contains("moka-field-input-wrapper--actions", row.ClassList);
		Assert.Equal(["moka-field-box", "moka-field-actions"], row.Children.Select(child => child.ClassName));
		Assert.NotNull(row.Children[0].QuerySelector("input#letter"));
		Assert.NotNull(row.Children[1].QuerySelector("button.probe-action"));
		Assert.Equal(Help, DescriptionOf(cut, cut.Find("input")));
	}

	[Theory]
	[MemberData(nameof(BoxedFields))]
	public void EveryBoxedField_DrawsItsActionsBesideTheBox(string name)
	{
		Field field = Fields[name];
		IRenderedComponent<ContainerFragment> cut = Render(field.Render(("Label", "Probe"), ("Actions", UseIt)));

		IElement actions = cut.Find(".moka-field-actions");
		Assert.NotNull(actions.QuerySelector("button.probe-action"));
		Assert.NotNull(actions.ParentElement!.QuerySelector(".moka-field-box " + field.Control));
	}

	[Fact]
	public void OnTheNewDesign_AButtonBesideTheBox_IsAsTallAsTheBox()
	{
		IReadOnlyDictionary<string, string> actions =
			ScopedCss.Declarations("MokaFieldWrapper.razor.css", ".moka-gate .moka-field-actions");

		Assert.Equal("calc(var(--moka-control-height) - var(--moka-spacing-md))", actions["--moka-button-height-sm"]);
		Assert.Equal("var(--moka-control-height)", actions["--moka-button-height"]);
		Assert.Equal("calc(var(--moka-control-height) + var(--moka-spacing-md))", actions["--moka-button-height-lg"]);
		Assert.Equal("center", ScopedCss.Declarations("MokaFieldWrapper.razor.css", ".moka-field-input-wrapper--actions")["align-items"]);
	}

	private static string? DescriptionOf<T>(IRenderedComponent<T> cut, IElement control) where T : IComponent
	{
		string? ids = control.GetAttribute("aria-describedby");
		Assert.False(string.IsNullOrWhiteSpace(ids), "The control has no aria-describedby.");
		return string.Join(" ", ids!.Split(' ', StringSplitOptions.RemoveEmptyEntries)
			.Select(id => cut.Find($"[id='{id}']").TextContent.Trim()));
	}

	private sealed class Field(Type component, string control, params (string Name, object? Value)[] parameters)
	{
		public string Control { get; } = control;

		public RenderFragment Render(params (string Name, object? Value)[] extra) => builder =>
		{
			builder.OpenComponent(0, component);
			int sequence = 1;
			foreach ((string name, object? value) in parameters.Concat(extra))
			{
				builder.AddAttribute(sequence++, name, value);
			}

			builder.CloseComponent();
		};
	}
}
