using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Moka.Red.Forms.Cascader;
using Moka.Red.Forms.Checkbox;
using Moka.Red.Forms.ColorInput;
using Moka.Red.Forms.ColorPicker;
using Moka.Red.Forms.CurrencyInput;
using Moka.Red.Forms.DatePicker;
using Moka.Red.Forms.NativeSelect;
using Moka.Red.Forms.NumericField;
using Moka.Red.Forms.PasswordField;
using Moka.Red.Forms.PhoneInput;
using Moka.Red.Forms.RadioGroup;
using Moka.Red.Forms.SelectField;
using Moka.Red.Forms.Switch;
using Moka.Red.Forms.TextArea;
using Moka.Red.Forms.TextField;
using Moka.Red.Forms.TimePicker;

namespace Moka.Red.Forms.Tests.Components;

// A page with no interactivity posts what the browser posts, and the browser only posts named fields.
// With no name on any input, a login form sent nothing and failed its own validation with every field
// typed in. On a real server these names bind through [SupplyParameterFromForm].
public class StaticFormPostTests : BunitContext
{
	private readonly Fields _fields = new();

	public StaticFormPostTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	private static string? Name(IElement element) => element.GetAttribute("name");

	private static Fields Broken() => new();

	[Fact]
	public void TextField_PostsUnderTheFieldItsBoundTo()
	{
		_fields.Email = "a@b.se";

		var cut = Render<MokaTextField>(p => p
			.Add(x => x.Value, _fields.Email)
			.Add(x => x.ValueExpression, () => _fields.Email!));

		Assert.Equal("_fields.Email", Name(cut.Find("input")));
		Assert.Equal("a@b.se", cut.Find("input").GetAttribute("value"));
	}

	[Fact]
	public void PasswordField_HasAName()
	{
		var cut = Render<MokaPasswordField>(p => p.Add(x => x.ValueExpression, () => _fields.Password!));

		Assert.Equal("_fields.Password", Name(cut.Find("input")));
	}

	[Fact]
	public void TextArea_HasAName()
	{
		var cut = Render<MokaTextArea>(p => p.Add(x => x.ValueExpression, () => _fields.Note!));

		Assert.Equal("_fields.Note", Name(cut.Find("textarea")));
	}

	[Fact]
	public void NumericField_HasAName()
	{
		var cut = Render<MokaNumericField<int>>(p => p.Add(x => x.ValueExpression, () => _fields.Count));

		Assert.Equal("_fields.Count", Name(cut.Find("input")));
	}

	[Fact]
	public void CurrencyInput_HasAName()
	{
		var cut = Render<MokaCurrencyInput>(p => p.Add(x => x.ValueExpression, () => _fields.Price));

		Assert.Equal("_fields.Price", Name(cut.Find("input")));
	}

	[Fact]
	public void PhoneInput_HasAName()
	{
		var cut = Render<MokaPhoneInput>(p => p.Add(x => x.ValueExpression, () => _fields.Phone!));

		Assert.Equal("_fields.Phone", Name(cut.Find("input")));
	}

	[Fact]
	public void ColorInput_NamesTheTextBoxAndNotTheSwatch()
	{
		var cut = Render<MokaColorInput>(p => p
			.Add(x => x.ShowNativeInput, true)
			.Add(x => x.ValueExpression, () => _fields.Color!));

		Assert.Equal("_fields.Color", Name(cut.Find("input[type=text]")));
		Assert.Null(Name(cut.Find("input[type=color]")));
	}

	[Fact]
	public void ColorPicker_NamesItsReadOnlyBox()
	{
		var cut = Render<MokaColorPicker>(p => p
			.Add(x => x.Value, "#ff0000")
			.Add(x => x.ValueExpression, () => _fields.Color!));

		IElement box = cut.Find("input[type=text]");
		Assert.Equal("_fields.Color", Name(box));
		Assert.Equal("#ff0000", box.GetAttribute("value"));
	}

	// Without value="True" a browser sends "on", which a bool can't bind.
	[Fact]
	public void Checkbox_PostsTrueWhenChecked()
	{
		var cut = Render<MokaCheckbox>(p => p.Add(x => x.ValueExpression, () => _fields.Remember));

		IElement box = cut.Find("input[type=checkbox]");
		Assert.Equal("_fields.Remember", Name(box));
		Assert.Equal(bool.TrueString, box.GetAttribute("value"));
	}

	[Fact]
	public void Switch_PostsTrueWhenChecked()
	{
		var cut = Render<MokaSwitch>(p => p.Add(x => x.ValueExpression, () => _fields.Remember));

		IElement box = cut.Find("input[type=checkbox]");
		Assert.Equal("_fields.Remember", Name(box));
		Assert.Equal(bool.TrueString, box.GetAttribute("value"));
	}

	[Fact]
	public void RadioGroup_InAForm_SharesTheFieldNameAndPostsEachValue()
	{
		var cut = Render<EditForm>(p => p
			.Add(x => x.Model, _fields)
			.Add(x => x.ChildContent, _ => builder => BuildPlanGroup(builder)));

		var radios = cut.FindAll("input[type=radio]");
		Assert.All(radios, radio => Assert.Equal("_fields.Plan", Name(radio)));
		Assert.Equal(["free", "pro"], radios.Select(radio => radio.GetAttribute("value")));
	}

	// Two groups bound through a loop variable would both be "row.Plan", and the browser would make them one.
	[Fact]
	public void RadioGroup_OutsideAForm_KeepsItsGeneratedName()
	{
		var cut = Render(BuildPlanGroup);

		Assert.All(cut.FindAll("input[type=radio]"),
			radio => Assert.StartsWith("moka-radiogroup-", Name(radio), StringComparison.Ordinal));
	}

	[Fact]
	public void RadioGroup_WithANameAttribute_UsesItOutsideAForm()
	{
		var cut = Render<MokaRadioGroup<string>>(p => p
			.AddUnmatched("name", "plan")
			.Add(x => x.ChildContent, builder => BuildPlanItems(builder)));

		Assert.All(cut.FindAll("input[type=radio]"), radio => Assert.Equal("plan", Name(radio)));
	}

	[Fact]
	public void RadioValues_AreWrittenInTheInvariantCulture()
	{
		CultureInfo previous = CultureInfo.CurrentCulture;
		CultureInfo.CurrentCulture = new CultureInfo("sv-SE");
		try
		{
			var cut = Render<MokaRadioGroup<double>>(p => p
				.Add(x => x.ChildContent, builder =>
				{
					builder.OpenComponent<MokaRadioItem<double>>(0);
					builder.AddComponentParameter(1, nameof(MokaRadioItem<double>.Value), 1.5);
					builder.CloseComponent();
				}));

			Assert.Equal("1.5", cut.Find("input[type=radio]").GetAttribute("value"));
		}
		finally
		{
			CultureInfo.CurrentCulture = previous;
		}
	}

	[Fact]
	public void Select_PostsItsValueInAHiddenInput()
	{
		var cut = Render<MokaSelect<string>>(p => p
			.Add(x => x.Value, "SE")
			.Add(x => x.Items, ["SE", "US"])
			.Add(x => x.ValueExpression, () => _fields.Country!));

		IElement hidden = cut.Find("input[type=hidden]");
		Assert.Equal("_fields.Country", Name(hidden));
		Assert.Equal("SE", hidden.GetAttribute("value"));
	}

	[Fact]
	public void Select_WhenDisabled_PostsNothing()
	{
		var cut = Render<MokaSelect<string>>(p => p
			.Add(x => x.Value, "SE")
			.Add(x => x.Disabled, true)
			.Add(x => x.ValueExpression, () => _fields.Country!));

		Assert.True(cut.Find("input[type=hidden]").HasAttribute("disabled"));
	}

	// A native select is the one list that works with no interactivity, as long as its options post the values.
	[Fact]
	public void NativeSelect_BoundToAField_PostsTheValuesThemselves()
	{
		var cut = Render<MokaNativeSelect<string>>(p => p
			.Add(x => x.Items, ["SE", "US"])
			.Add(x => x.Value, "US")
			.Add(x => x.ValueExpression, () => _fields.Country!));

		IElement select = cut.Find("select");
		Assert.Equal("_fields.Country", Name(select));
		Assert.Equal(["SE", "US"], cut.FindAll("option").Select(option => option.GetAttribute("value")));
		Assert.Equal("US", select.GetAttribute("value"));
	}

	[Fact]
	public void NativeSelect_BoundToAField_StillTakesAChange()
	{
		string? chosen = null;
		var cut = Render<MokaNativeSelect<string>>(p => p
			.Add(x => x.Items, ["SE", "US"])
			.Add(x => x.Value, "SE")
			.Add(x => x.ValueChanged, (string value) => chosen = value)
			.Add(x => x.ValueExpression, () => _fields.Country!));

		cut.Find("select").Change("US");

		Assert.Equal("US", chosen);
	}

	// An item the server can't read back from text keeps its position as the value, and isn't posted.
	[Fact]
	public void NativeSelect_OverItemsThatArentText_KeepsPositionsAndPostsNothing()
	{
		Plan[] plans = [new("Free"), new("Pro")];
		Plan? chosen = null;
		var cut = Render<MokaNativeSelect<Plan>>(p => p
			.Add(x => x.Items, plans)
			.Add(x => x.Value, plans[0])
			.Add(x => x.ValueChanged, (Plan value) => chosen = value)
			.Add(x => x.ValueExpression, () => _fields.Choice!));

		Assert.False(cut.Find("select").HasAttribute("name"));
		Assert.Equal(["0", "1"], cut.FindAll("option").Select(option => option.GetAttribute("value")));

		cut.Find("select").Change("1");
		Assert.Same(plans[1], chosen);
	}

	// The text box shows the date in the picker's Format, which the server couldn't read back.
	[Fact]
	public void DatePicker_PostsAnInvariantDateAndLeavesTheDisplayBoxUnnamed()
	{
		var cut = Render<MokaDatePicker>(p => p
			.Add(x => x.Value, new DateTime(2026, 3, 9, 0, 0, 0, DateTimeKind.Unspecified))
			.Add(x => x.Format, "dd MMM yyyy")
			.Add(x => x.ValueExpression, () => _fields.Day));

		IElement hidden = cut.Find("input[type=hidden]");
		Assert.Equal("_fields.Day", Name(hidden));
		Assert.Equal("2026-03-09T00:00:00", hidden.GetAttribute("value"));
		Assert.Null(Name(cut.Find("input[type=text]")));
	}

	[Fact]
	public void DatePicker_WithANameAttribute_PutsItOnlyOnTheHiddenInput()
	{
		var cut = Render<MokaDatePicker>(p => p.AddUnmatched("name", "day"));

		Assert.Equal("day", Name(cut.Find("input[type=hidden]")));
		Assert.Null(Name(cut.Find("input[type=text]")));
	}

	[Fact]
	public void TimePicker_PostsAnInvariantTime()
	{
		var cut = Render<MokaTimePicker>(p => p
			.Add(x => x.Value, new TimeSpan(9, 30, 0))
			.Add(x => x.ValueExpression, () => _fields.At));

		IElement hidden = cut.Find("input[type=hidden]");
		Assert.Equal("_fields.At", Name(hidden));
		Assert.Equal("09:30:00", hidden.GetAttribute("value"));
		Assert.Null(Name(cut.Find("input[type=text]")));
	}

	[Fact]
	public void Cascader_PostsEachLevelAsAnIndexedField()
	{
		var cut = Render<MokaCascader>(p => p
			.Add(x => x.Value, ["eu", "se"])
			.Add(x => x.ValueExpression, () => _fields.Path));

		var hidden = cut.FindAll("input[type=hidden]");
		Assert.Equal(["_fields.Path[0]", "_fields.Path[1]"], hidden.Select(Name));
		Assert.Equal(["eu", "se"], hidden.Select(input => input.GetAttribute("value")));
	}

	// The stand-in expression that lets an input render with no binding would name every one of them "Value".
	[Fact]
	public void AnInputWithNoBinding_HasNoName()
	{
		var cut = Render<MokaTextField>(p => p.Add(x => x.Value, "x"));

		Assert.False(cut.Find("input").HasAttribute("name"));
	}

	[Fact]
	public void ASelectWithNoBinding_HasNoHiddenInput()
	{
		var cut = Render<MokaSelect<string>>(p => p.Add(x => x.Value, "SE"));

		Assert.Empty(cut.FindAll("input[type=hidden]"));
	}

	[Fact]
	public void ANameAttribute_NamesAnInputWithNoBinding()
	{
		var cut = Render<MokaTextField>(p => p.AddUnmatched("name", "email"));

		Assert.Equal("email", Name(cut.Find("input")));
	}

	[Fact]
	public void ANameAttribute_WinsOverTheBinding()
	{
		var cut = Render<MokaTextField>(p => p
			.AddUnmatched("name", "email")
			.Add(x => x.ValueExpression, () => _fields.Email!));

		Assert.Equal("email", Name(cut.Find("input")));
	}

	// Blazor's own InputText throws on this binding. An interactive app runs it fine, so it has to keep rendering.
	[Fact]
	public void ABindingThatCantBeWrittenAsAName_StillRenders()
	{
		var cut = Render<MokaTextField>(p => p
			.Add(x => x.Value, "x")
			.Add(x => x.ValueExpression, () => Broken().Email!));

		Assert.False(cut.Find("input").HasAttribute("name"));
		Assert.Equal("x", cut.Find("input").GetAttribute("value"));

		cut.Render(p => p.Add(x => x.Value, "y"));
		Assert.Equal("y", cut.Find("input").GetAttribute("value"));
	}

	private void BuildPlanGroup(RenderTreeBuilder builder)
	{
		builder.OpenComponent<MokaRadioGroup<string>>(0);
		builder.AddComponentParameter(1, nameof(MokaRadioGroup<string>.ValueExpression),
			(System.Linq.Expressions.Expression<Func<string>>)(() => _fields.Plan!));
		builder.AddComponentParameter(2, nameof(MokaRadioGroup<string>.ChildContent), (RenderFragment)BuildPlanItems);
		builder.CloseComponent();
	}

	private static void BuildPlanItems(RenderTreeBuilder builder)
	{
		builder.OpenComponent<MokaRadioItem<string>>(0);
		builder.AddComponentParameter(1, nameof(MokaRadioItem<string>.Value), "free");
		builder.CloseComponent();
		builder.OpenComponent<MokaRadioItem<string>>(2);
		builder.AddComponentParameter(3, nameof(MokaRadioItem<string>.Value), "pro");
		builder.CloseComponent();
	}

	private sealed class Fields
	{
		public string? Email { get; set; }
		public string? Password { get; set; }
		public string? Note { get; set; }
		public int Count { get; set; }
		public decimal? Price { get; set; }
		public string? Phone { get; set; }
		public string? Color { get; set; }
		public bool Remember { get; set; }
		public string? Plan { get; set; }
		public string? Country { get; set; }
		public DateTime? Day { get; set; }
		public TimeSpan? At { get; set; }
		public IReadOnlyList<string> Path { get; set; } = [];
		public Plan? Choice { get; set; }
	}

	private sealed record Plan(string Name);
}
