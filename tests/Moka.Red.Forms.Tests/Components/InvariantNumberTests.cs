using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Forms.ColorPicker;
using Moka.Red.Forms.FormBuilder;
using Moka.Red.Forms.NumericField;

namespace Moka.Red.Forms.Tests.Components;

// Numbers written into CSS or attributes with the current culture broke in other locales
// (gotcha #4): a comma locale writes "37,5%", which CSS drops, Swedish writes the minus sign as
// U+2212 and Arabic puts a direction mark before it, and a number input shows either as empty.
public class InvariantNumberTests : BunitContext
{
	public InvariantNumberTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public async Task ColorPicker_PlacesItsThumbs_InACommaLocale()
	{
		CultureInfo culture = CultureInfo.CurrentCulture;
		CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
		try
		{
			// Hue 209.8, saturation 78.7, lightness 46.1 and alpha 0.502: none of them whole.
			IRenderedComponent<MokaColorPicker> cut = Render<MokaColorPicker>(p => p
				.Add(x => x.Value, "#1976d280")
				.Add(x => x.ShowAlpha, true));
			await cut.Find("input[aria-haspopup]").ClickAsync(new MouseEventArgs());

			Assert.Matches(@"^left: \d+\.\d+%; top: \d+\.\d+%$", Style(cut, ".moka-colorpicker-gradient-thumb"));
			Assert.Matches(@"^left: \d+\.\d+%$", Style(cut, ".moka-colorpicker-hue-thumb"));
			Assert.Matches(@"^left: \d+\.\d+%$", Style(cut, ".moka-colorpicker-alpha-thumb"));
			Assert.Matches(@"hsl\(\d+\.\d+, 100%, 50%\)", Style(cut, ".moka-colorpicker-gradient-area"));
		}
		finally
		{
			CultureInfo.CurrentCulture = culture;
		}
	}

	// The field reads its text in the invariant culture but wrote it in the current one, so a
	// German page showed 1.5 as "1,5", and typing one more digit gave 152.
	[Fact]
	public void NumericField_WritesItsValueTheWayItReadsIt()
	{
		CultureInfo culture = CultureInfo.CurrentCulture;
		CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
		try
		{
			double reported = 0;
			IRenderedComponent<MokaNumericField<double>> cut = Render<MokaNumericField<double>>(p => p
				.Add(x => x.Value, 1.5)
				.Add(x => x.ValueChanged, value => reported = value));

			IElement input = cut.Find("input");
			Assert.Equal("1.5", input.GetAttribute("value"));

			input.Input(input.GetAttribute("value") + "2");

			Assert.Equal(1.52, reported);
		}
		finally
		{
			CultureInfo.CurrentCulture = culture;
		}
	}

	[Theory]
	[InlineData("sv-SE")]
	[InlineData("ar-SA")]
	public async Task FormBuilder_ANegativeMinSurvivesTheRoundTrip(string cultureName)
	{
		CultureInfo culture = CultureInfo.CurrentCulture;
		CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
		try
		{
			List<MokaFormField> fields =
				[new() { Label = "Temperature", Type = MokaFormFieldType.NumericField, Min = -5, Max = 40 }];
			IRenderedComponent<MokaFormBuilder> cut = Render<MokaFormBuilder>(p => p.Add(x => x.Fields, fields));
			await cut.Find(".moka-form-builder__field-item").ClickAsync(new MouseEventArgs());

			Assert.Equal("-5", Property(cut, "Min").GetAttribute("value"));

			// The browser sends a number input's value in the invariant culture.
			await Property(cut, "Min").ChangeAsync(new ChangeEventArgs { Value = "-12" });

			Assert.Equal(-12, fields[0].Min);
			Assert.Equal("-12", Property(cut, "Min").GetAttribute("value"));
		}
		finally
		{
			CultureInfo.CurrentCulture = culture;
		}
	}

	private static string Style(IRenderedComponent<MokaColorPicker> cut, string selector) =>
		cut.Find(selector).GetAttribute("style") ?? string.Empty;

	private static IElement Property(IRenderedComponent<MokaFormBuilder> cut, string label) =>
		cut.FindAll(".moka-form-builder__property")
			.Single(property => property.QuerySelector("label")?.TextContent.Trim() == label)
			.QuerySelector("input")!;
}
