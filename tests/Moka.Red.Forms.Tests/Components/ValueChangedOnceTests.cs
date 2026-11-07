using Bunit;
using Moka.Red.Forms.ColorPicker;
using Moka.Red.Forms.DatePicker;
using Moka.Red.Forms.SelectField;
using Moka.Red.Forms.TimePicker;

namespace Moka.Red.Forms.Tests.Components;

// Setting CurrentValue already raises ValueChanged. These called it a second time, so a consumer's handler
// ran twice per change. Under @bind-Value the second call changed nothing, which hid it.
public class ValueChangedOnceTests : BunitContext
{
	private static readonly DateTime SomeDay = new(2020, 1, 15);
	private static readonly TimeSpan ThreeOSeven = new(3, 7, 30);

	public ValueChangedOnceTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public void DatePicker_PickingADay()
	{
		List<DateTime?> values = [];
		IRenderedComponent<MokaDatePicker> cut = RenderDatePicker(values);

		cut.Find("input").Click();
		cut.FindAll(".moka-datepicker-days button:not(.moka-datepicker-day--selected):not(.moka-datepicker-day--other-month)")[0].Click();

		Assert.Single(values);
	}

	[Fact]
	public void DatePicker_Today()
	{
		List<DateTime?> values = [];
		IRenderedComponent<MokaDatePicker> cut = RenderDatePicker(values);

		cut.Find("input").Click();
		cut.Find(".moka-datepicker-today").Click();

		Assert.Equal([DateTime.Today], values);
	}

	[Fact]
	public void DatePicker_Clear()
	{
		List<DateTime?> values = [];
		IRenderedComponent<MokaDatePicker> cut = RenderDatePicker(values);

		cut.Find("input").Click();
		cut.Find(".moka-datepicker-clear").Click();

		Assert.Equal([null], values);
	}

	[Fact]
	public void TimePicker_PickingAnHour()
	{
		List<TimeSpan?> values = [];
		IRenderedComponent<MokaTimePicker> cut = RenderTimePicker(values);

		cut.Find("input").Click();
		cut.FindAll(".moka-timepicker-column")[0].QuerySelector("button:not(.moka-timepicker-item--selected)")!.Click();

		Assert.Single(values);
	}

	[Fact]
	public void TimePicker_Now()
	{
		List<TimeSpan?> values = [];
		IRenderedComponent<MokaTimePicker> cut = RenderTimePicker(values);

		cut.Find("input").Click();
		cut.Find(".moka-timepicker-now").Click();

		Assert.Single(values);
	}

	[Fact]
	public void TimePicker_Clear()
	{
		List<TimeSpan?> values = [];
		IRenderedComponent<MokaTimePicker> cut = RenderTimePicker(values);

		cut.Find("input").Click();
		cut.Find(".moka-timepicker-clear").Click();

		Assert.Equal([null], values);
	}

	[Fact]
	public void ColorPicker_Hex()
	{
		List<string> values = [];
		IRenderedComponent<MokaColorPicker> cut = RenderColorPicker(values);

		cut.Find(".moka-colorpicker-swatch-button").Click();
		cut.Find(".moka-colorpicker-hex-input").Change("#112233");

		Assert.Equal(["#112233"], values);
	}

	[Fact]
	public void ColorPicker_Rgb()
	{
		List<string> values = [];
		IRenderedComponent<MokaColorPicker> cut = RenderColorPicker(values);

		cut.Find(".moka-colorpicker-swatch-button").Click();
		cut.FindAll(".moka-colorpicker-rgb-input")[0].Change("17");

		Assert.Equal(["#116699"], values);
	}

	[Fact]
	public void ColorPicker_Preset()
	{
		List<string> values = [];
		IRenderedComponent<MokaColorPicker> cut = RenderColorPicker(values);

		cut.Find(".moka-colorpicker-swatch-button").Click();
		cut.Find(".moka-colorpicker-presets .moka-colorpicker-preset").Click();

		Assert.Single(values);
	}

	[Fact]
	public void Select_Clear()
	{
		List<string> values = [];
		IRenderedComponent<MokaSelect<string>> cut = Render<MokaSelect<string>>(p => p
			.Add(x => x.Items, new List<string> { "Apple", "Pear" })
			.Add(x => x.Clearable, true)
			.Add(x => x.Value, "Apple")
			.Add(x => x.ValueChanged, value => values.Add(value)));

		cut.Find(".moka-select-clear").Click();

		Assert.Single(values);
	}

	private IRenderedComponent<MokaDatePicker> RenderDatePicker(List<DateTime?> values) =>
		Render<MokaDatePicker>(p => p
			.Add(x => x.Value, SomeDay)
			.Add(x => x.ShowClearButton, true)
			.Add(x => x.ShowTodayButton, true)
			.Add(x => x.ValueChanged, value => values.Add(value)));

	private IRenderedComponent<MokaTimePicker> RenderTimePicker(List<TimeSpan?> values) =>
		Render<MokaTimePicker>(p => p
			.Add(x => x.Value, ThreeOSeven)
			.Add(x => x.ShowClearButton, true)
			.Add(x => x.ValueChanged, value => values.Add(value)));

	private IRenderedComponent<MokaColorPicker> RenderColorPicker(List<string> values) =>
		Render<MokaColorPicker>(p => p
			.Add(x => x.Value, "#336699")
			.Add(x => x.ShowHexInput, true)
			.Add(x => x.ShowRgbInputs, true)
			.Add(x => x.Presets, new List<string> { "#aabbcc", "#ddeeff" })
			.Add(x => x.ValueChanged, value => values.Add(value)));
}
