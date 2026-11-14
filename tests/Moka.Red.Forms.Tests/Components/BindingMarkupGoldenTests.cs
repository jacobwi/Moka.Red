using System.Linq.Expressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Moka.Red.Forms.Cascader;
using Moka.Red.Forms.Checkbox;
using Moka.Red.Forms.ColorInput;
using Moka.Red.Forms.ColorPicker;
using Moka.Red.Forms.ColorWheel;
using Moka.Red.Forms.CountrySelect;
using Moka.Red.Forms.DatePicker;
using Moka.Red.Forms.FormBuilder;
using Moka.Red.Forms.NativeSelect;
using Moka.Red.Forms.RadioGroup;
using Moka.Red.Forms.RangeBar;
using Moka.Red.Forms.SelectField;
using Moka.Red.Forms.Switch;
using Moka.Red.Forms.TextField;
using Moka.Red.Forms.TimePicker;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Forms.Tests.Components;

// The whole markup of the inputs whose native elements bind their value, each unbound and bound to a field. The files
// were written before the inputs moved to @bind:get and @bind:set, so a change here is either one of those or a
// mistake. Set MOKA_RECORD_GOLDENS=1 to write them again after a change that is meant to show.
public sealed class BindingMarkupGoldenTests : BunitContext
{
	private static readonly string[] Letters = ["a", "b", "c"];

	private static readonly IReadOnlyList<MokaCascaderOption> Places =
	[
		new() { Value = "eu", Label = "Europe", Children = [new() { Value = "se", Label = "Sweden" }] },
		new() { Value = "as", Label = "Asia" }
	];

	private readonly Fields _fields = new();

	public BindingMarkupGoldenTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		TopLayerPopup.Setup(JSInterop);
	}

	public static TheoryData<bool> Bound => [false, true];

	[Theory]
	[MemberData(nameof(Bound))]
	public void Switch(bool bound) => Golden("switch", bound, () => Render<MokaSwitch>(p =>
	{
		p.Add(x => x.Id, "probe").Add(x => x.Label, "Start with the system").Add(x => x.Value, true);
		if (bound)
		{
			p.Add(x => x.ValueChanged, v => _fields.Flag = v).Add(x => x.ValueExpression, Expr(() => _fields.Flag));
		}
	}));

	[Theory]
	[MemberData(nameof(Bound))]
	public void Checkbox(bool bound) => Golden("checkbox", bound, () => Render<MokaCheckbox>(p =>
	{
		p.Add(x => x.Id, "probe").Add(x => x.Label, "Remember me").Add(x => x.Value, true);
		if (bound)
		{
			p.Add(x => x.ValueChanged, v => _fields.Flag = v).Add(x => x.ValueExpression, Expr(() => _fields.Flag));
		}
	}));

	[Theory]
	[MemberData(nameof(Bound))]
	public void NativeSelect(bool bound) => Golden("native-select", bound, () => Render<MokaNativeSelect<string>>(p =>
	{
		p.Add(x => x.Id, "probe").Add(x => x.Label, "Letter").Add(x => x.Items, Letters).Add(x => x.Value, "b");
		if (bound)
		{
			p.Add(x => x.ValueChanged, v => _fields.Text = v).Add(x => x.ValueExpression, Expr(() => _fields.Text));
		}
	}));

	[Theory]
	[MemberData(nameof(Bound))]
	public void TypedTimePicker(bool bound) => Golden("time-picker-typed", bound, () => Render<MokaTimePicker>(p =>
	{
		p.Add(x => x.Id, "probe").Add(x => x.Label, "Starts").Add(x => x.Typed, true).Add(x => x.Value, new TimeSpan(9, 30, 0));
		if (bound)
		{
			p.Add(x => x.ValueChanged, v => _fields.Time = v).Add(x => x.ValueExpression, Expr(() => _fields.Time));
		}
	}));

	[Theory]
	[MemberData(nameof(Bound))]
	public void ColorInputWithNativeInput(bool bound) => Golden("color-input-native", bound, () => Render<MokaColorInput>(p =>
	{
		p.Add(x => x.Id, "probe").Add(x => x.Label, "Accent").Add(x => x.ShowNativeInput, true).Add(x => x.Value, "#ff0000");
		if (bound)
		{
			p.Add(x => x.ValueChanged, v => _fields.Text = v).Add(x => x.ValueExpression, Expr(() => _fields.Text));
		}
	}));

	[Theory]
	[MemberData(nameof(Bound))]
	public void InlineColorPicker(bool bound) => Golden("color-picker-inline", bound, () => Render<MokaColorPicker>(p =>
	{
		p.Add(x => x.Id, "probe").Add(x => x.Inline, true).Add(x => x.ShowHexInput, true).Add(x => x.ShowRgbInputs, true)
			.Add(x => x.ShowAlpha, true).Add(x => x.Value, "#336699");
		if (bound)
		{
			p.Add(x => x.ValueChanged, v => _fields.Text = v).Add(x => x.ValueExpression, Expr(() => _fields.Text));
		}
	}));

	[Theory]
	[MemberData(nameof(Bound))]
	public void SlidersColorPicker(bool bound) => Golden("color-picker-sliders", bound, () => Render<MokaColorPicker>(p =>
	{
		p.Add(x => x.Id, "probe").Add(x => x.Inline, true).Add(x => x.Layout, MokaColorPickerLayout.Sliders)
			.Add(x => x.ShowAlpha, true).Add(x => x.Value, "#336699");
		if (bound)
		{
			p.Add(x => x.ValueChanged, v => _fields.Text = v).Add(x => x.ValueExpression, Expr(() => _fields.Text));
		}
	}));

	[Theory]
	[MemberData(nameof(Bound))]
	public void RadioGroup(bool bound) => Golden("radio-group", bound, () => Render<MokaRadioGroup<string>>(p =>
	{
		p.Add(x => x.Id, "probe").Add(x => x.Label, "Plan").Add(x => x.Value, "b")
			.AddChildContent<MokaRadioItem<string>>(item => item.Add(x => x.Value, "a").Add(x => x.Label, "A"))
			.AddChildContent<MokaRadioItem<string>>(item => item.Add(x => x.Value, "b").Add(x => x.Label, "B"));
		if (bound)
		{
			p.Add(x => x.ValueChanged, v => _fields.Text = v).Add(x => x.ValueExpression, Expr(() => _fields.Text));
		}
	}));

	[Theory]
	[MemberData(nameof(Bound))]
	public void TextField(bool bound) => Golden("text-field", bound, () => Render<MokaTextField>(p =>
	{
		p.Add(x => x.Id, "probe").Add(x => x.Label, "Name").Add(x => x.Value, "Ada");
		if (bound)
		{
			p.Add(x => x.ValueChanged, v => _fields.Text = v).Add(x => x.ValueExpression, Expr(() => _fields.Text));
		}
	}));

	[Theory]
	[MemberData(nameof(Bound))]
	public void DatePicker(bool bound) => Golden("date-picker", bound, () => Render<MokaDatePicker>(p =>
	{
		p.Add(x => x.Id, "probe").Add(x => x.Label, "Due").Add(x => x.Value, new DateTime(2026, 3, 14));
		if (bound)
		{
			p.Add(x => x.ValueChanged, v => _fields.Date = v).Add(x => x.ValueExpression, Expr(() => _fields.Date));
		}
	}));

	[Theory]
	[MemberData(nameof(Bound))]
	public void Select(bool bound) => Golden("select", bound, () => Render<MokaSelect<string>>(p =>
	{
		p.Add(x => x.Id, "probe").Add(x => x.Label, "Letter").Add(x => x.Items, Letters).Add(x => x.Value, "b");
		if (bound)
		{
			p.Add(x => x.ValueChanged, v => _fields.Text = v).Add(x => x.ValueExpression, Expr(() => _fields.Text));
		}
	}));

	[Theory]
	[MemberData(nameof(Bound))]
	public void CountrySelect(bool bound) => Golden("country-select", bound, () => Render<MokaCountrySelect>(p =>
	{
		p.Add(x => x.Id, "probe").Add(x => x.Label, "Country").Add(x => x.Value, "SE");
		if (bound)
		{
			p.Add(x => x.ValueChanged, v => _fields.Country = v).Add(x => x.ValueExpression, Expr(() => _fields.Country));
		}
	}));

	[Theory]
	[MemberData(nameof(Bound))]
	public void Cascader(bool bound) => Golden("cascader", bound, () => Render<MokaCascader>(p =>
	{
		p.Add(x => x.Id, "probe").Add(x => x.Label, "Place").Add(x => x.Options, Places).Add(x => x.Value, ["eu", "se"]);
		if (bound)
		{
			p.Add(x => x.ValueChanged, v => _fields.Path = v).Add(x => x.ValueExpression, Expr(() => _fields.Path));
		}
	}));

	[Theory]
	[MemberData(nameof(Bound))]
	public void RangeBar(bool bound) => Golden("range-bar", bound, () => Render<MokaRangeBar>(p =>
	{
		p.Add(x => x.Id, "probe").Add(x => x.Label, "Window").Add(x => x.Value, new MokaRange(20, 60));
		if (bound)
		{
			p.Add(x => x.ValueChanged, v => _fields.Range = v).Add(x => x.ValueExpression, Expr(() => _fields.Range));
		}
	}));

	[Theory]
	[MemberData(nameof(Bound))]
	public void ColorWheel(bool bound) => Golden("color-wheel", bound, () => Render<MokaColorWheel>(p =>
	{
		p.Add(x => x.Id, "probe").Add(x => x.Value, "#336699");
		if (bound)
		{
			p.Add(x => x.ValueChanged, v => _fields.Text = v);
		}
	}));

	// The property panel of each kind of field: text boxes, number boxes, checkboxes, the options and the span.
	[Theory]
	[MemberData(nameof(BuilderFields))]
	public void FormBuilderProperties(int field, bool bound) => Golden($"form-builder-{BuilderKinds[field]}", bound, () =>
	{
		IRenderedComponent<MokaFormBuilder> cut = Render<MokaFormBuilder>(p =>
		{
			p.Add(x => x.Fields, BuilderList()).Add(x => x.Columns, 2);
			if (bound)
			{
				p.Add(x => x.FieldsChanged, v => _fields.Form = v);
			}
		});
		cut.FindAll(".moka-form-builder__field-item")[field].Click();
		return cut;
	});

	public static TheoryData<int, bool> BuilderFields =>
		new() { { 0, false }, { 0, true }, { 1, false }, { 1, true }, { 2, false }, { 2, true } };

	private static readonly string[] BuilderKinds = ["text", "number", "select"];

	private static List<MokaFormField> BuilderList() =>
	[
		new()
		{
			Id = "name", Type = MokaFormFieldType.TextField, Label = "Name", Placeholder = "Ada", HelperText = "As on the card",
			MaxLength = 40, Required = true
		},
		new() { Id = "age", Type = MokaFormFieldType.NumericField, Label = "Age", Min = -5, Max = 10, Disabled = true },
		new()
		{
			Id = "plan", Type = MokaFormFieldType.Select, Label = "Plan", Options = ["One", "Two"], ColSpan = 2,
			DefaultValue = "One"
		}
	];

	private static Expression<Func<T>> Expr<T>(Expression<Func<T>> expression) => expression;

	private static void Golden<TComponent>(string name, bool bound, Func<IRenderedComponent<TComponent>> render)
		where TComponent : IComponent =>
		BindingGoldens.Check("Moka.Red.Forms.Tests", $"{name}-{(bound ? "bound" : "unbound")}", () => render().Markup);

	private sealed class Fields
	{
		public bool Flag { get; set; }

		public string Text { get; set; } = "";

		public string? Country { get; set; }

		public TimeSpan? Time { get; set; }

		public DateTime? Date { get; set; }

		public IReadOnlyList<string> Path { get; set; } = [];

		public MokaRange Range { get; set; }

		public IList<MokaFormField> Form { get; set; } = [];
	}
}
