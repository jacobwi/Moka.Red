using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using AngleSharp.Dom;
using Bunit;
using Bunit.Rendering;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Forms.AutoComplete;
using Moka.Red.Forms.Checkbox;
using Moka.Red.Forms.ColorInput;
using Moka.Red.Forms.ColorPicker;
using Moka.Red.Forms.CurrencyInput;
using Moka.Red.Forms.DatePicker;
using Moka.Red.Forms.DateRangePicker;
using Moka.Red.Forms.FileUpload;
using Moka.Red.Forms.NumericField;
using Moka.Red.Forms.PasswordField;
using Moka.Red.Forms.PhoneInput;
using Moka.Red.Forms.RadioGroup;
using Moka.Red.Forms.Rating;
using Moka.Red.Forms.SearchInput;
using Moka.Red.Forms.SelectField;
using Moka.Red.Forms.Slider;
using Moka.Red.Forms.Switch;
using Moka.Red.Forms.TagInput;
using Moka.Red.Forms.TextArea;
using Moka.Red.Forms.TextField;
using Moka.Red.Forms.TimePicker;
using Moka.Red.Forms.TreeSelect;

namespace Moka.Red.Forms.Tests.Components;

// The field inputs ignored Id, six components ignored Class, seven dropped the unmatched attributes,
// and two pickers put Id and the attributes on their outer element. Id now names the control, and
// everything that points at the control follows it. Class goes on the component's own element, and
// the unmatched attributes on the control (the group for a radio group).
public class IdClassAttributeTests : BunitContext
{
	private const string ProbeId = "probe-id";

	private static readonly string[] Fruits = ["Apple", "Banana"];

	private static readonly Func<string, Task<IEnumerable<string>>> SearchFruits = query =>
		Task.FromResult(Fruits.Where(f => f.StartsWith(query, StringComparison.OrdinalIgnoreCase)));

	private static readonly MokaTreeSelectItem<string>[] Tree = [new("a", "A")];

	// Every input, with the selector of its control and the parameters it needs to render.
	private static readonly Dictionary<string, Field> Fields = new()
	{
		["TextField"] = new(typeof(MokaTextField), "input"),
		["TextArea"] = new(typeof(MokaTextArea), "textarea"),
		["PasswordField"] = new(typeof(MokaPasswordField), "input"),
		["NumericField"] = new(typeof(MokaNumericField<int>), "input"),
		["PhoneInput"] = new(typeof(MokaPhoneInput), "input"),
		["ColorInput"] = new(typeof(MokaColorInput), "input:not([type=color])"),
		["CurrencyInput"] = new(typeof(MokaCurrencyInput), "input"),
		["DatePicker"] = new(typeof(MokaDatePicker), "input"),
		["TimePicker"] = new(typeof(MokaTimePicker), "input"),
		["ColorPicker"] = new(typeof(MokaColorPicker), "input[aria-haspopup]"),
		["Select"] = new(typeof(MokaSelect<string>), "[role=combobox]", ("Items", Fruits)),
		["AutoComplete"] = new(typeof(MokaAutoComplete<string>), "input[role=combobox]", ("SearchFunc", SearchFruits)),
		["Rating"] = new(typeof(MokaRating), "[role=slider]"),
		["FileUpload"] = new(typeof(MokaFileUpload), "input[type=file]"),
		["Slider"] = new(typeof(MokaSlider), "input[type=range]"),
		["TagInput"] = new(typeof(MokaTagInput), "input"),
		["SearchInput"] = new(typeof(MokaSearchInput), "input"),
		["DateRangePicker"] = new(typeof(MokaDateRangePicker), "input"),
		["Checkbox"] = new(typeof(MokaCheckbox), "input[type=checkbox]"),
		["Switch"] = new(typeof(MokaSwitch), "input[type=checkbox]"),
		["RadioGroup"] = new(typeof(MokaRadioGroup<string>), "[role=radiogroup]"),
		["TreeSelect"] = new(typeof(MokaTreeSelect<string>), "button.moka-tree-select-trigger", ("Items", Tree))
	};

	// The ones whose label points at the control with <label for>. They make up an id when the
	// consumer gives none.
	private static readonly string[] Labelled =
	[
		"TextField", "TextArea", "PasswordField", "NumericField", "PhoneInput", "ColorInput", "CurrencyInput",
		"DatePicker", "TimePicker", "ColorPicker", "AutoComplete", "FileUpload", "Slider", "TagInput",
		"SearchInput", "DateRangePicker"
	];

	// The custom controls. <label for> may only point at a native control, so these point
	// aria-labelledby at the label instead, and the label keeps its id but has no for.
	private static readonly string[] LabelledBy = ["Select", "Rating", "TreeSelect"];

	// The element that takes Class, for the components that dropped it.
	private static readonly Dictionary<string, string> ClassHolders = new()
	{
		["DatePicker"] = ".moka-datepicker",
		["TimePicker"] = ".moka-timepicker",
		["ColorPicker"] = ".moka-colorpicker",
		["PhoneInput"] = ".moka-phone-wrapper",
		["CurrencyInput"] = ".moka-currency-wrapper",
		["FileUpload"] = ".moka-field"
	};

	public IdClassAttributeTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	public static TheoryData<string> AllFields => [.. Fields.Keys];

	public static TheoryData<string> LabelledFields => [.. Labelled];

	public static TheoryData<string> LabelledByFields => [.. LabelledBy];

	public static TheoryData<string> ClassFields => [.. ClassHolders.Keys];

	public static TheoryData<string> AttributeFields =>
	[
		"DatePicker", "TimePicker", "ColorPicker", "RadioGroup", "FileUpload", "Select", "Rating",
		"DateRangePicker", "TreeSelect"
	];

	[Theory]
	[MemberData(nameof(AllFields))]
	public void Id_NamesTheControl(string name)
	{
		Field field = Fields[name];
		IRenderedComponent<ContainerFragment> cut = Render(field.Render(("Id", ProbeId), ("Label", "Probe")));

		IElement named = Assert.Single(cut.FindAll($"#{ProbeId}"));
		Assert.True(named.Matches(field.Control), $"The id is on {named.OuterHtml}");
		IReadOnlyList<IElement> labels = cut.FindAll("label[for]");
		Assert.Equal(Labelled.Contains(name), labels.Count > 0);
		Assert.All(labels, label => Assert.Equal(ProbeId, label.GetAttribute("for")));
		AssertReferencesResolve(cut);
	}

	[Theory]
	[MemberData(nameof(LabelledFields))]
	public void WithoutId_TheLabelNamesTheControlsOwnId(string name)
	{
		Field field = Fields[name];
		IRenderedComponent<ContainerFragment> cut = Render(field.Render(("Label", "Probe")));

		string? id = cut.Find(field.Control).Id;
		Assert.False(string.IsNullOrEmpty(id));
		Assert.NotEmpty(cut.FindAll("label[for]"));
		Assert.All(cut.FindAll("label[for]"), label => Assert.Equal(id, label.GetAttribute("for")));
		AssertReferencesResolve(cut);
	}

	// Rating's and Select's labels pointed for at a div, which HTML does not allow, and TreeSelect's
	// label was tied to nothing, so its button was named by its content alone.
	[Theory]
	[MemberData(nameof(LabelledByFields))]
	public void ACustomControl_IsNamedByTheLabelsId(string name)
	{
		Field field = Fields[name];
		IRenderedComponent<ContainerFragment> cut = Render(field.Render(("Label", "Probe")));

		IElement control = cut.Find(field.Control);
		IElement label = Assert.Single(cut.FindAll("label"));
		Assert.False(string.IsNullOrEmpty(control.Id));
		Assert.Equal($"{control.Id}-label", label.Id);
		Assert.False(label.HasAttribute("for"));
		Assert.Equal(label.Id, control.GetAttribute("aria-labelledby")?.Split(' ')[0]);
		AssertReferencesResolve(cut);
	}

	[Fact]
	public void TreeSelect_TheNameReadsTheLabelThenTheChoice()
	{
		IRenderedComponent<ContainerFragment> cut = Render(Fields["TreeSelect"].Render(
			("Id", ProbeId), ("Label", "Department"), ("Value", "a")));

		IElement trigger = cut.Find("button.moka-tree-select-trigger");
		Assert.Equal($"{ProbeId}-label {ProbeId}", trigger.GetAttribute("aria-labelledby"));
		Assert.Equal("Department", cut.Find($"#{ProbeId}-label").TextContent.Trim());
		Assert.Contains("A", trigger.TextContent, StringComparison.Ordinal);
	}

	[Fact]
	public void TreeSelect_AnAriaLabelNamesItOnlyWithoutALabel()
	{
		IElement unlabelled = Render(Fields["TreeSelect"].Render(("aria-label", "Department")))
			.Find("button.moka-tree-select-trigger");
		Assert.Equal("Department", unlabelled.GetAttribute("aria-label"));
		Assert.False(unlabelled.HasAttribute("aria-labelledby"));

		IRenderedComponent<ContainerFragment> cut =
			Render(Fields["TreeSelect"].Render(("Label", "Team"), ("aria-label", "Department")));
		IElement labelled = cut.Find("button.moka-tree-select-trigger");
		Assert.False(labelled.HasAttribute("aria-label"));
		Assert.StartsWith(cut.Find("label").Id, labelled.GetAttribute("aria-labelledby"), StringComparison.Ordinal);
	}

	[Fact]
	public async Task Select_TheListboxAndOptionIdsFollowTheId()
	{
		IRenderedComponent<ContainerFragment> cut = Render(Fields["Select"].Render(("Id", ProbeId), ("Label", "Fruit")));

		Assert.Equal($"{ProbeId}-label", cut.Find("label.moka-field-label").Id);
		Assert.Equal($"{ProbeId}-label", cut.Find("[role=combobox]").GetAttribute("aria-labelledby"));

		await cut.Find("[role=combobox]").ClickAsync(new MouseEventArgs());
		await cut.Find("[role=combobox]").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown" });

		IElement combobox = cut.Find("[role=combobox]");
		Assert.Equal($"{ProbeId}-listbox", cut.Find("[role=listbox]").Id);
		Assert.Equal($"{ProbeId}-listbox", combobox.GetAttribute("aria-controls"));
		Assert.Equal($"{ProbeId}-listbox-option-0", combobox.GetAttribute("aria-activedescendant"));
		AssertReferencesResolve(cut);
	}

	[Fact]
	public async Task AutoComplete_TheListboxAndOptionIdsFollowTheId()
	{
		IRenderedComponent<ContainerFragment> cut =
			Render(Fields["AutoComplete"].Render(("Id", ProbeId), ("Label", "Fruit"), ("Debounce", 0)));

		await cut.Find("input[role=combobox]").InputAsync(new ChangeEventArgs { Value = "A" });
		cut.WaitForAssertion(() => Assert.Single(cut.FindAll("[role=option]")));
		await cut.Find("input[role=combobox]").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown" });

		IElement input = cut.Find("input[role=combobox]");
		IElement listbox = cut.Find("[role=listbox]");
		Assert.Equal($"{ProbeId}-listbox", listbox.Id);
		Assert.Equal($"{ProbeId}-listbox", input.GetAttribute("aria-controls"));
		Assert.Equal($"{ProbeId}-label", listbox.GetAttribute("aria-labelledby"));
		Assert.Equal($"{ProbeId}-listbox-option-0", input.GetAttribute("aria-activedescendant"));
		AssertReferencesResolve(cut);
	}

	[Fact]
	public void Rating_TheSliderIsLabelledThroughTheId()
	{
		IRenderedComponent<ContainerFragment> cut = Render(Fields["Rating"].Render(("Id", ProbeId), ("Label", "Score")));

		Assert.Equal($"{ProbeId}-label", cut.Find("[role=slider]").GetAttribute("aria-labelledby"));
		Assert.Equal($"{ProbeId}-label", cut.Find("label.moka-field-label").Id);
	}

	[Fact]
	public void TagInput_TheLabelLetsGoOnceTheInputIsGone()
	{
		IRenderedComponent<ContainerFragment> cut = Render(Fields["TagInput"].Render(
			("Id", ProbeId), ("Label", "Tags"), ("Values", new List<string> { "one" }), ("MaxTags", 1)));

		Assert.Empty(cut.FindAll("input"));
		Assert.Empty(cut.FindAll("label[for]"));
	}

	[Fact]
	public void Checkbox_DisplayOnly_TakesTheIdAndAttributesOnItsPicture()
	{
		IRenderedComponent<ContainerFragment> cut = Render(Fields["Checkbox"].Render(
			("Id", ProbeId), ("Label", "Archived"), ("DisplayOnly", true), ("data-probe", "x")));

		IElement picture = cut.Find("span.moka-checkbox");
		Assert.Equal(ProbeId, picture.Id);
		Assert.Equal("x", picture.GetAttribute("data-probe"));
		Assert.Equal("true", picture.GetAttribute("aria-hidden"));
	}

	[Theory]
	[MemberData(nameof(ClassFields))]
	public void Class_GoesOnTheComponentsOwnElement_Last(string name)
	{
		IRenderedComponent<ContainerFragment> cut = Render(Fields[name].Render(("Class", "probe-class")));

		IElement holder = Assert.Single(cut.FindAll(".probe-class"));
		Assert.True(holder.Matches(ClassHolders[name]), $"The class is on {holder.OuterHtml}");
		Assert.EndsWith(" probe-class", holder.GetAttribute("class"), StringComparison.Ordinal);
	}

	[Theory]
	[InlineData("PhoneInput", "moka-phone moka-phone-wrapper probe-class")]
	[InlineData("CurrencyInput", "moka-currency moka-currency-wrapper probe-class")]
	[InlineData("FileUpload", "moka-field moka-field--md moka-fileupload probe-class")]
	public void Class_FollowsTheRootClass(string name, string expected)
	{
		IRenderedComponent<ContainerFragment> cut = Render(Fields[name].Render(("Class", "probe-class")));

		Assert.Equal(expected, cut.Find(".probe-class").GetAttribute("class"));
	}

	[Theory]
	[MemberData(nameof(AttributeFields))]
	public void UnmatchedAttributes_GoOnTheControl(string name)
	{
		Field field = Fields[name];
		IRenderedComponent<ContainerFragment> cut =
			Render(field.Render(("data-probe", "x"), ("title", "Probe title")));

		IElement control = Assert.Single(cut.FindAll("[data-probe]"));
		Assert.True(control.Matches(field.Control), $"The attributes are on {control.OuterHtml}");
		Assert.Equal("Probe title", control.GetAttribute("title"));
	}

	[Theory]
	[InlineData("Select")]
	[InlineData("Rating")]
	public void AriaLabel_NamesTheControlOnce(string name)
	{
		Field field = Fields[name];
		IRenderedComponent<ContainerFragment> cut =
			Render(field.Render(("aria-label", "Probe name"), ("data-probe", "x")));

		IElement named = Assert.Single(cut.FindAll("[aria-label]"));
		Assert.True(named.Matches(field.Control));
		Assert.Equal("Probe name", named.GetAttribute("aria-label"));
		Assert.Equal("x", named.GetAttribute("data-probe"));
	}

	[Theory]
	[InlineData("Select")]
	[InlineData("Rating")]
	public void AriaLabel_GivesWayToTheLabel(string name)
	{
		Field field = Fields[name];
		IRenderedComponent<ContainerFragment> cut =
			Render(field.Render(("Label", "Visible"), ("aria-label", "Probe name"), ("data-probe", "x")));

		IElement control = cut.Find(field.Control);
		Assert.Equal("x", control.GetAttribute("data-probe"));
		Assert.Equal(cut.Find("label.moka-field-label").Id, control.GetAttribute("aria-labelledby"));
		Assert.Empty(cut.FindAll("[aria-label]"));
	}

	// InputBase adds aria-invalid to the unmatched attributes while its field fails validation. The
	// pickers, the select and the radio group dropped those attributes, so an invalid field was
	// never announced as invalid.
	[Theory]
	[InlineData("DatePicker")]
	[InlineData("TimePicker")]
	[InlineData("ColorPicker")]
	[InlineData("Select")]
	[InlineData("RadioGroup")]
	public void AFailedValidation_MarksTheControlInvalid(string name)
	{
		var booking = new Booking();
		Field field = Fields[name];
		IRenderedComponent<EditForm> cut = Render<EditForm>(p => p
			.Add(f => f.Model, booking)
			.Add(f => f.ChildContent, _ => builder =>
			{
				builder.OpenComponent<DataAnnotationsValidator>(0);
				builder.CloseComponent();
				builder.OpenComponent(1, field.Component);
				int sequence = 2;
				foreach ((string parameter, object? value) in field.Parameters)
				{
					builder.AddAttribute(sequence++, parameter, value);
				}

				builder.AddAttribute(10, "ValueExpression", name switch
				{
					"DatePicker" => (Expression<Func<DateTime?>>)(() => booking.Date),
					"TimePicker" => (Expression<Func<TimeSpan?>>)(() => booking.Time),
					"ColorPicker" => (Expression<Func<string?>>)(() => booking.Colour),
					"Select" => (Expression<Func<string?>>)(() => booking.Fruit),
					_ => (Expression<Func<string?>>)(() => booking.Plan)
				});
				builder.CloseComponent();
			}));
		Assert.False(cut.Find(field.Control).HasAttribute("aria-invalid"));

		cut.Find("form").Submit();

		Assert.Equal("true", cut.Find(field.Control).GetAttribute("aria-invalid"));
	}

	// Every id the rendering points at through for or an aria attribute is on exactly one element.
	private static void AssertReferencesResolve(IRenderedComponent<ContainerFragment> cut)
	{
		string[] references = ["for", "aria-labelledby", "aria-describedby", "aria-controls", "aria-activedescendant"];
		foreach (IElement element in cut.FindAll("*"))
		{
			foreach (string reference in references)
			{
				string? ids = element.GetAttribute(reference);
				if (string.IsNullOrEmpty(ids))
				{
					continue;
				}

				foreach (string id in ids.Split(' ', StringSplitOptions.RemoveEmptyEntries))
				{
					Assert.Single(cut.FindAll($"[id='{id}']"));
				}
			}
		}
	}

	private sealed class Field
	{
		public Field(Type component, string control, params (string Name, object? Value)[] parameters)
		{
			Component = component;
			Control = control;
			Parameters = parameters;
		}

		public Type Component { get; }

		public string Control { get; }

		public IReadOnlyList<(string Name, object? Value)> Parameters { get; }

		public RenderFragment Render(params (string Name, object? Value)[] extra) => builder =>
		{
			builder.OpenComponent(0, Component);
			int sequence = 1;
			foreach ((string name, object? value) in Parameters.Concat(extra))
			{
				builder.AddAttribute(sequence++, name, value);
			}

			builder.CloseComponent();
		};
	}

	private sealed class Booking
	{
		[Required]
		public DateTime? Date { get; set; }

		[Required]
		public TimeSpan? Time { get; set; }

		[Required]
		public string? Colour { get; set; }

		[Required]
		public string? Fruit { get; set; }

		[Required]
		public string? Plan { get; set; }
	}
}
