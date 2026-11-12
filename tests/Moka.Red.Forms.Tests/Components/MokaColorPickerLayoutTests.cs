using System.Linq.Expressions;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Clipboard;
using Moka.Red.Core.Theming;
using Moka.Red.Feedback.Tooltip;
using Moka.Red.Forms.ColorPicker;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Forms.Tests.Components;

// MokaColorPicker's panel layouts and the extras any panel can show. Standard, the default, must render exactly as it
// did before they existed.
public class MokaColorPickerLayoutTests : BunitContext
{
	private const string PanelModule = "./_content/Moka.Red.Forms/ColorPicker/MokaColorPicker.razor.js";

	private static readonly string[] OneRecent = ["#111111"];
	private static readonly string[] TwoRecent = ["#111111", "#222222"];
	private static readonly string[] ThreeRecent = ["#111111", "#222222", "#333333"];
	private static readonly string[] TwoPresets = ["#112233", "#445566"];

	private readonly BunitJSModuleInterop _panel;
	private readonly List<IReadOnlyList<string>> _recents = [];
	private readonly List<string> _values = [];

	public MokaColorPickerLayoutTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		TopLayerPopup.Setup(JSInterop);
		_panel = JSInterop.SetupModule(PanelModule);
		_panel.Mode = JSRuntimeMode.Loose;
	}

	public static TheoryData<MokaColorPickerLayout, string> Layouts => new()
	{
		{ MokaColorPickerLayout.Tabbed, "tabbed" },
		{ MokaColorPickerLayout.Wheel, "wheel" },
		{ MokaColorPickerLayout.Sliders, "sliders" },
		{ MokaColorPickerLayout.PaletteFirst, "palette-first" },
		{ MokaColorPickerLayout.Compact, "compact" }
	};

	[Fact]
	public async Task Standard_IsTheDefault_AndDrawsTheOriginalPanel()
	{
		IRenderedComponent<MokaColorPicker> cut = Picker();
		await Open(cut);

		Assert.Equal(MokaColorPickerLayout.Standard, cut.Instance.Layout);
		Assert.Equal("moka-colorpicker-dropdown", cut.Find(".moka-colorpicker-dropdown").GetAttribute("class"));
		Assert.Empty(cut.FindAll(".moka-colorpicker-panel"));
		Assert.Empty(cut.FindAll(".moka-colorpicker-sec"));
		Assert.Empty(cut.FindAll(".moka-colorpicker-note"));
		Assert.Single(cut.FindAll(".moka-colorpicker-gradient-area"));
		Assert.Single(cut.FindAll(".moka-colorpicker-hue-slider"));
		Assert.Single(cut.FindAll(".moka-colorpicker-hex-input"));
		Assert.Equal(3, cut.FindAll(".moka-colorpicker-rgb-input").Count);
		Assert.Equal(20, cut.FindAll(".moka-colorpicker-presets .moka-colorpicker-preset").Count);
		Assert.DoesNotContain(_panel.Invocations, call => call.Identifier == "bind");
	}

	[Fact]
	public async Task Standard_WithAlpha_KeepsItsBarAsItWas()
	{
		IRenderedComponent<MokaColorPicker> cut = Picker(p => p.Add(x => x.ShowAlpha, true));
		await Open(cut);

		IElement bar = cut.Find(".moka-colorpicker-alpha-slider");
		Assert.Equal("moka-colorpicker-alpha-slider", bar.GetAttribute("class"));
		Assert.StartsWith("background: linear-gradient(to right, transparent, hsl(", bar.GetAttribute("style"), StringComparison.Ordinal);
		Assert.Empty(cut.FindAll(".moka-colorpicker-alpha-head"));
	}

	[Theory]
	[MemberData(nameof(Layouts))]
	public async Task ALayout_DrawsItsPanelInThePopup(MokaColorPickerLayout layout, string modifier)
	{
		IRenderedComponent<MokaColorPicker> cut = Picker(p => p.Add(x => x.Layout, layout));
		await Open(cut);

		Assert.Contains("moka-colorpicker-dropdown--panel", cut.Find(".moka-colorpicker-dropdown").ClassList);
		IElement panel = cut.Find(".moka-colorpicker-panel");
		Assert.Contains($"moka-colorpicker-panel--{modifier}", panel.ClassList);
		Assert.True(panel.HasAttribute("data-cp-state"));
		Assert.Empty(cut.FindAll(".moka-colorpicker-gradient-area"));
	}

	[Theory]
	[MemberData(nameof(Layouts))]
	public async Task ALayout_BindsItsScriptWhileOpen_AndLetsGoOnClose(MokaColorPickerLayout layout, string modifier)
	{
		_ = modifier;
		IRenderedComponent<MokaColorPicker> cut = Picker(p => p.Add(x => x.Layout, layout));
		await Open(cut);

		JSRuntimeInvocation bind = Assert.Single(_panel.Invocations, call => call.Identifier == "bind");
		string key = Assert.IsType<string>(bind.Arguments[0]);
		Assert.IsType<ElementReference>(bind.Arguments[1]);

		await cut.Find(".moka-colorpicker-backdrop").ClickAsync(new MouseEventArgs());

		JSRuntimeInvocation unbind = Assert.Single(_panel.Invocations, call => call.Identifier == "unbind");
		Assert.Equal(key, unbind.Arguments[0]);
	}

	[Fact]
	public async Task TheSquare_AndTheHueBar_AreSliders()
	{
		IRenderedComponent<MokaColorPicker> cut = Picker(p => p
			.Add(x => x.Layout, MokaColorPickerLayout.Compact)
			.Add(x => x.Value, "#7e57c2"));
		await Open(cut);

		IElement square = cut.Find("[data-cp-drag=sv]");
		Assert.Equal("slider", square.GetAttribute("role"));
		Assert.Equal("0", square.GetAttribute("tabindex"));
		Assert.Equal("Saturation and brightness", square.GetAttribute("aria-label"));
		Assert.Equal("55", square.GetAttribute("aria-valuenow"));
		Assert.Equal("Saturation 55%, brightness 76%", square.GetAttribute("aria-valuetext"));

		IElement hue = cut.Find("[data-cp-drag=hue]");
		Assert.Equal("slider", hue.GetAttribute("role"));
		Assert.Equal("Hue", hue.GetAttribute("aria-label"));
		Assert.Equal("0", hue.GetAttribute("aria-valuemin"));
		Assert.Equal("360", hue.GetAttribute("aria-valuemax"));
		Assert.Equal("262", hue.GetAttribute("aria-valuenow"));
	}

	[Fact]
	public async Task ThePanel_CarriesItsColorForTheScript_InInvariantNumbers()
	{
		System.Globalization.CultureInfo culture = System.Globalization.CultureInfo.CurrentCulture;
		try
		{
			System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("sv-SE");
			IRenderedComponent<MokaColorPicker> cut = Picker(p => p
				.Add(x => x.Layout, MokaColorPickerLayout.Compact)
				.Add(x => x.Value, "#7e57c2"));
			await Open(cut);

			IElement panel = cut.Find(".moka-colorpicker-panel");
			Assert.Matches(@"^261\.\d+$", panel.GetAttribute("data-h"));
			Assert.Matches(@"^0\.\d+$", panel.GetAttribute("data-s"));
			Assert.Equal("1", panel.GetAttribute("data-a"));
			string style = panel.GetAttribute("style")!;
			Assert.Contains("--_cp-color: #7e57c2", style, StringComparison.Ordinal);
			Assert.Matches(@"--_cp-hue: 261\.\d+;", style);
			Assert.DoesNotContain(",", style.Replace(", ", "", StringComparison.Ordinal), StringComparison.Ordinal);
		}
		finally
		{
			System.Globalization.CultureInfo.CurrentCulture = culture;
		}
	}

	[Fact]
	public async Task ADrag_ReportsOnce_WhereItWasLetGo()
	{
		IRenderedComponent<MokaColorPicker> cut = Picker(p => p
			.Add(x => x.Layout, MokaColorPickerLayout.Compact)
			.Add(x => x.Value, "#336699"));
		await Open(cut);

		await cut.InvokeAsync(() => cut.Instance.OnPanelPicked(0, 1, 1, 1));

		Assert.Equal(["#ff0000"], _values);
		Assert.Equal("#ff0000", cut.Find("input[data-cp-hex]").GetAttribute("value"));
	}

	// Dragged to grey, the hue stays where it was: grey has none of its own to come back with.
	[Fact]
	public async Task AGreyFromTheSquare_KeepsItsHue()
	{
		IRenderedComponent<MokaColorPicker> cut = Picker(p => p.Add(x => x.Layout, MokaColorPickerLayout.Compact));
		await Open(cut);

		await cut.InvokeAsync(() => cut.Instance.OnPanelPicked(200, 0, 0.5, 1));
		cut.Render(p => p.Add(x => x.Value, _values[^1]));

		Assert.Equal("#808080", _values[^1]);
		Assert.Equal("200", cut.Find("[data-cp-drag=hue]").GetAttribute("aria-valuenow"));
	}

	[Fact]
	public async Task WithAlpha_ADragWritesEightDigitHex()
	{
		IRenderedComponent<MokaColorPicker> cut = Picker(p => p
			.Add(x => x.Layout, MokaColorPickerLayout.Compact)
			.Add(x => x.ShowAlpha, true));
		await Open(cut);

		Assert.Equal("Opacity", cut.Find("[data-cp-drag=alpha]").GetAttribute("aria-label"));
		await cut.InvokeAsync(() => cut.Instance.OnPanelPicked(0, 1, 1, 0.5));

		Assert.Equal(["#ff000080"], _values);
	}

	[Fact]
	public async Task TheHexField_TakesAHexOrAToken_AndMarksAnythingElse()
	{
		IRenderedComponent<MokaColorPicker> cut = Picker(p => p.Add(x => x.Layout, MokaColorPickerLayout.Compact));
		await Open(cut);

		await cut.Find("input[data-cp-hex]").ChangeAsync(new ChangeEventArgs { Value = "42A5F5" });
		Assert.Equal("#42a5f5", _values[^1]);

		await cut.Find("input[data-cp-hex]").ChangeAsync(new ChangeEventArgs { Value = "var(--moka-color-primary)" });
		Assert.Equal("var(--moka-color-primary)", _values[^1]);

		await cut.Find("input[data-cp-hex]").ChangeAsync(new ChangeEventArgs { Value = "nope" });
		Assert.Equal(2, _values.Count);
		Assert.Equal("true", cut.Find("input[data-cp-hex]").GetAttribute("aria-invalid"));
	}

	[Fact]
	public async Task Compact_HasAnEyedropperInItsHexRow_NamedAndInATooltip()
	{
		IRenderedComponent<MokaColorPicker> cut = Picker(p => p.Add(x => x.Layout, MokaColorPickerLayout.Compact));
		await Open(cut);

		IElement eye = cut.Find(".moka-colorpicker-hexrow [data-cp-eye]");
		IconTooltip.AssertNamed(eye, "Pick from screen", MokaTooltipPosition.BottomEnd);
		Assert.Contains("moka-colorpicker-eye-tip", eye.Closest(".moka-tooltip-trigger")!.ClassList);
	}

	[Fact]
	public async Task Tabbed_FollowsTheTabsPattern()
	{
		IRenderedComponent<MokaColorPicker> cut = Picker(p => p.Add(x => x.Layout, MokaColorPickerLayout.Tabbed));
		await Open(cut);

		IElement list = cut.Find("[role=tablist]");
		Assert.Equal("Way to pick", list.GetAttribute("aria-label"));
		IReadOnlyList<IElement> tabs = cut.FindAll("[role=tab]");
		Assert.Equal(["Spectrum", "Swatches"], tabs.Select(tab => tab.TextContent.Trim()));
		Assert.Equal("true", tabs[0].GetAttribute("aria-selected"));
		Assert.Equal("0", tabs[0].GetAttribute("tabindex"));
		Assert.Equal("false", tabs[1].GetAttribute("aria-selected"));
		Assert.Equal("-1", tabs[1].GetAttribute("tabindex"));

		IElement spectrum = cut.Find($"#{tabs[0].GetAttribute("aria-controls")}");
		Assert.Equal("tabpanel", spectrum.GetAttribute("role"));
		Assert.Equal(tabs[0].Id, spectrum.GetAttribute("aria-labelledby"));
		Assert.False(spectrum.HasAttribute("hidden"));
		Assert.True(cut.Find($"#{tabs[1].GetAttribute("aria-controls")}").HasAttribute("hidden"));

		await cut.FindAll("[role=tab]")[1].ClickAsync(new MouseEventArgs());

		Assert.Equal("true", cut.FindAll("[role=tab]")[1].GetAttribute("aria-selected"));
		Assert.Empty(cut.FindAll("[data-cp-drag=sv]"));
		Assert.Equal(24, cut.FindAll(".moka-colorpicker-swatches--grid input.moka-color-swatch-input").Count);
		Assert.Single(cut.FindAll("input[data-cp-hex]"));
	}

	[Fact]
	public async Task Tabbed_SwatchesHoldTheThemesColorsAsHex()
	{
		IRenderedComponent<MokaColorPicker> cut = Picker(p => p.Add(x => x.Layout, MokaColorPickerLayout.Tabbed), MokaTheme.Dark);
		await Open(cut);
		await cut.FindAll("[role=tab]")[1].ClickAsync(new MouseEventArgs());

		IElement theme = cut.FindAll(".moka-colorpicker-swatches")[0];
		Assert.Equal("radiogroup", theme.GetAttribute("role"));
		Assert.Equal("Theme", cut.Find($"#{theme.GetAttribute("aria-labelledby")}").TextContent);
		IElement primary = cut.FindAll(".moka-colorpicker-swatches input.moka-color-swatch-input")
			.First(b => b.GetAttribute("aria-label")!.StartsWith("Primary", StringComparison.Ordinal));
		Assert.Equal("Primary #EF5350", primary.GetAttribute("aria-label"));
		IconTooltip.AssertNamed(primary, "Primary #EF5350", MokaTooltipPosition.Bottom);

		await primary.ChangeAsync(new ChangeEventArgs { Value = true });

		Assert.Equal("#ef5350", _values[^1]);
	}

	[Fact]
	public async Task Wheel_HasAHueRing_ACopyButton_AndAReadout()
	{
		IRenderedComponent<MokaColorPicker> cut = Picker(p => p
			.Add(x => x.Layout, MokaColorPickerLayout.Wheel)
			.Add(x => x.Value, "#7e57c2"));
		await Open(cut);

		IElement ring = cut.Find("[data-cp-drag=ring]");
		Assert.Equal("slider", ring.GetAttribute("role"));
		Assert.Equal("Hue", ring.GetAttribute("aria-label"));
		Assert.Contains("moka-colorpicker-square--wheel", cut.Find("[data-cp-drag=sv]").ClassList);
		IconTooltip.AssertNamed(cut.Find(".moka-colorpicker-hexrow .moka-colorpicker-icon-btn"), "Copy hex", MokaTooltipPosition.BottomEnd);
		Assert.Equal("H 262°  S 55%  B 76%", cut.Find(".moka-colorpicker-readout").TextContent);
	}

	[Fact]
	public async Task Sliders_HaveARangeAndANumberForEachOfHueSaturationAndLightness()
	{
		IRenderedComponent<MokaColorPicker> cut = Picker(p => p
			.Add(x => x.Layout, MokaColorPickerLayout.Sliders)
			.Add(x => x.Value, "#26a69a"));
		await Open(cut);

		IReadOnlyList<IElement> ranges = cut.FindAll("input[type=range]");
		Assert.Equal(["Hue", "Saturation", "Lightness"], ranges.Select(r => r.GetAttribute("aria-label")));
		Assert.Equal(["174", "63", "40"], ranges.Select(r => r.GetAttribute("value")));
		Assert.Equal(["360", "100", "100"], ranges.Select(r => r.GetAttribute("max")));
		Assert.Equal(["h", "s", "l"], ranges.Select(r => r.GetAttribute("data-cp-range")));
		Assert.Equal(["Hue in degrees", "Saturation in percent", "Lightness in percent"],
			cut.FindAll("input[type=number]").Select(n => n.GetAttribute("aria-label")));
		Assert.Equal("#26A69A", cut.Find(".moka-colorpicker-sl-hex").TextContent);
		Assert.Equal("hsl(174 63% 40%)", cut.Find(".moka-colorpicker-sl-hsl").TextContent);

		await cut.FindAll("input[type=range]")[2].ChangeAsync(new ChangeEventArgs { Value = "100" });

		Assert.Equal("#ffffff", _values[^1]);
	}

	[Fact]
	public async Task Sliders_ReportOnChange_NotOnEveryInput()
	{
		IRenderedComponent<MokaColorPicker> cut = Picker(p => p.Add(x => x.Layout, MokaColorPickerLayout.Sliders));
		await Open(cut);

		foreach (IElement range in cut.FindAll("input[type=range]"))
		{
			Assert.False(range.HasAttribute("blazor:oninput"));
			Assert.True(range.HasAttribute("blazor:onchange"));
		}
	}

	[Fact]
	public async Task PaletteFirst_PutsThePaletteFirst_AndFoldsTheSpectrumUnderMoreColors()
	{
		IRenderedComponent<MokaColorPicker> cut = Picker(p => p.Add(x => x.Layout, MokaColorPickerLayout.PaletteFirst));
		await Open(cut);

		Assert.Equal("Palette", cut.Find(".moka-colorpicker-sec .moka-micro-label").TextContent);
		IElement more = cut.Find(".moka-colorpicker-more");
		Assert.Equal("true", more.GetAttribute("aria-expanded"));
		Assert.Equal(more.GetAttribute("aria-controls"), cut.Find(".moka-colorpicker-more-body").Id);
		Assert.Single(cut.FindAll(".moka-colorpicker-more-body [data-cp-drag=sv]"));

		await more.ClickAsync(new MouseEventArgs());

		Assert.Equal("false", cut.Find(".moka-colorpicker-more").GetAttribute("aria-expanded"));
		Assert.True(cut.Find(".moka-colorpicker-more-body").HasAttribute("hidden"));
		Assert.Empty(cut.FindAll("[data-cp-drag=sv]"));
	}

	[Fact]
	public async Task PaletteSwatches_AreNamedCheckedAndInTooltips()
	{
		IRenderedComponent<MokaColorPicker> cut = Picker(p => p
			.Add(x => x.Layout, MokaColorPickerLayout.PaletteFirst)
			.Add(x => x.Presets, TwoPresets)
			.Add(x => x.Value, "#445566"));
		await Open(cut);

		IReadOnlyList<IElement> swatches = cut.FindAll(".moka-colorpicker-swatches--grid input.moka-color-swatch-input");
		IconTooltip.AssertNamed(swatches[0], "#112233", MokaTooltipPosition.Bottom);
		Assert.False(swatches[0].HasAttribute("checked"));
		Assert.True(swatches[1].HasAttribute("checked"));

		await swatches[0].ChangeAsync(new ChangeEventArgs { Value = true });

		Assert.Equal("#112233", _values[^1]);
	}

	// Each swatch was a toggle button of its own, so Tab stopped at all twenty-four. Each row is now one radio group,
	// named by its heading: Tab reaches it once and the arrows move the choice.
	[Fact]
	public async Task EachRowOfColors_IsOneRadioGroup_NamedByItsHeading()
	{
		IRenderedComponent<MokaColorPicker> cut = Picker(p => p
			.Add(x => x.Layout, MokaColorPickerLayout.PaletteFirst)
			.Add(x => x.RecentColors, TwoRecent)
			.Add(x => x.Value, "#111111"));
		await Open(cut);

		IReadOnlyList<IElement> rows = cut.FindAll(".moka-colorpicker-swatches");
		Assert.Equal(2, rows.Count);
		Assert.Equal(["Palette", "Recent"], rows.Select(row => cut.Find($"#{row.GetAttribute("aria-labelledby")}").TextContent));
		foreach (IElement row in rows)
		{
			Assert.Equal("radiogroup", row.GetAttribute("role"));
			Assert.Contains("moka-color-swatch--ring", row.ClassList);
			Assert.Single(row.QuerySelectorAll("input.moka-color-swatch-input").Select(radio => radio.GetAttribute("name")).Distinct());
			Assert.Empty(row.QuerySelectorAll(".moka-color-swatch-check"));
		}

		Assert.NotEqual(rows[0].QuerySelector("input")!.GetAttribute("name"), rows[1].QuerySelector("input")!.GetAttribute("name"));
		Assert.Empty(cut.FindAll("button[aria-pressed].moka-colorpicker-swatch-btn"));
		Assert.True(cut.Find(".moka-colorpicker-recent-sec input[value='#111111']").HasAttribute("checked"));
	}

	[Fact]
	public void ReadOnly_DisablesTheRowsOfColors()
	{
		IRenderedComponent<MokaColorPicker> cut = Picker(p => p
			.Add(x => x.Layout, MokaColorPickerLayout.PaletteFirst)
			.Add(x => x.ReadOnly, true)
			.Add(x => x.Inline, true));

		Assert.NotEmpty(cut.FindAll(".moka-colorpicker-swatches input.moka-color-swatch-input"));
		Assert.All(cut.FindAll(".moka-colorpicker-swatches input.moka-color-swatch-input"), radio => Assert.True(radio.HasAttribute("disabled")));
	}

	[Fact]
	public void Inline_DrawsThePanelInPlace_WithNoFieldOrPopup()
	{
		IRenderedComponent<MokaColorPicker> cut = Picker(p => p
			.Add(x => x.Inline, true)
			.Add(x => x.Layout, MokaColorPickerLayout.Compact));

		Assert.Empty(cut.FindAll(".moka-colorpicker-input-wrapper"));
		Assert.Empty(cut.FindAll("[popover]"));
		Assert.Contains("moka-colorpicker-panel--inline", cut.Find(".moka-colorpicker-panel").ClassList);

		JSRuntimeInvocation bind = Assert.Single(_panel.Invocations, call => call.Identifier == "bind");
		IReadOnlyDictionary<string, object?> options = Assert.IsAssignableFrom<IReadOnlyDictionary<string, object?>>(bind.Arguments[3]);
		Assert.Equal(true, options["reportLeave"]);
	}

	[Fact]
	public void Inline_Standard_DrawsTheOriginalPanelInPlace()
	{
		IRenderedComponent<MokaColorPicker> cut = Picker(p => p.Add(x => x.Inline, true));

		IElement dropdown = cut.Find(".moka-colorpicker-dropdown");
		Assert.Contains("moka-colorpicker-dropdown--inline", dropdown.ClassList);
		Assert.False(dropdown.HasAttribute("popover"));
		Assert.Single(cut.FindAll(".moka-colorpicker-gradient-area"));
	}

	[Fact]
	public void Inline_PostsItsValueThroughAHiddenField()
	{
		Model model = new() { Color = "#ff0000" };
		IRenderedComponent<MokaColorPicker> cut = Render<MokaColorPicker>(p => p
			.Add(x => x.Inline, true)
			.Add(x => x.Value, model.Color)
			.Add(x => x.ValueExpression, (Expression<Func<string>>)(() => model.Color)));

		IElement hidden = cut.Find("input[type=hidden]");
		Assert.Equal("model.Color", hidden.GetAttribute("name"));
		Assert.Equal("#ff0000", hidden.GetAttribute("value"));
	}

	[Fact]
	public async Task LeavingAnInlinePanel_AddsItsColorToTheRecentColors()
	{
		IRenderedComponent<MokaColorPicker> cut = Picker(p => p
			.Add(x => x.Inline, true)
			.Add(x => x.Value, "#123456")
			.Add(x => x.RecentColorsChanged, list => _recents.Add(list)));

		await cut.InvokeAsync(cut.Instance.OnPanelLeft);

		cut.WaitForAssertion(() => Assert.Equal(["#123456"], Assert.Single(_recents)));
	}

	// C3A
	[Fact]
	public async Task ShowContrast_MeasuresTheColorOnTheGivenBackground()
	{
		IRenderedComponent<MokaColorPicker> cut = Picker(p => p
			.Add(x => x.Layout, MokaColorPickerLayout.Compact)
			.Add(x => x.ShowContrast, true)
			.Add(x => x.ContrastBackground, "#ffffff")
			.Add(x => x.Value, "#000000"));
		await Open(cut);

		Assert.Equal("21.00:1", cut.Find(".moka-colorpicker-contrast-ratio").TextContent);
		IElement grade = cut.Find(".moka-colorpicker-grade");
		Assert.Equal("AAA", grade.TextContent);
		Assert.Contains("moka-colorpicker-grade--ok", grade.ClassList);
		Assert.Equal("#FFFFFF", cut.Find(".moka-colorpicker-contrast-head .moka-colorpicker-mono").TextContent);
		Assert.Equal(["Pass", "Pass", "Pass"], cut.FindAll(".moka-colorpicker-need-result").Select(r => r.TextContent));
		Assert.Equal("background: #ffffff; color: #000000", cut.Find(".moka-colorpicker-contrast-sample").GetAttribute("style"));
	}

	[Theory]
	[InlineData("#767676", "4.54:1", "AA", "moka-colorpicker-grade--ok")]
	[InlineData("#777777", "4.47:1", "Large text only", "moka-colorpicker-grade--warn")]
	[InlineData("#cccccc", "1.60:1", "Fails", "moka-colorpicker-grade--fail")]
	public async Task ShowContrast_GivesTheLevel(string color, string ratio, string level, string tone)
	{
		IRenderedComponent<MokaColorPicker> cut = Picker(p => p
			.Add(x => x.ShowContrast, true)
			.Add(x => x.ContrastBackground, "#ffffff")
			.Add(x => x.Value, color));
		await Open(cut);

		Assert.Equal(ratio, cut.Find(".moka-colorpicker-contrast-ratio").TextContent);
		Assert.Equal(level, cut.Find(".moka-colorpicker-grade").TextContent);
		Assert.Contains(tone, cut.Find(".moka-colorpicker-grade").ClassList);
	}

	// With no background given it measures against the theme's page background, so light and dark differ.
	[Fact]
	public async Task ShowContrast_FollowsTheThemeBetweenLightAndDark()
	{
		IRenderedComponent<MokaColorPicker> dark = Picker(p => p
			.Add(x => x.ShowContrast, true)
			.Add(x => x.Value, "#ffffff"), MokaTheme.Red);
		await Open(dark);
		string darkRatio = dark.Find(".moka-colorpicker-contrast-ratio").TextContent;
		Assert.Equal("On the page background", dark.Find(".moka-colorpicker-contrast-head .moka-micro-label").TextContent);

		IRenderedComponent<MokaColorPicker> light = Picker(p => p
			.Add(x => x.ShowContrast, true)
			.Add(x => x.Value, "#ffffff"), MokaTheme.RedLight);
		await Open(light);

		Assert.Equal("AAA", dark.Find(".moka-colorpicker-grade").TextContent);
		Assert.Equal("Fails", light.Find(".moka-colorpicker-grade").TextContent);
		Assert.NotEqual(darkRatio, light.Find(".moka-colorpicker-contrast-ratio").TextContent);
	}

	[Fact]
	public async Task ShowContrast_CompositesATranslucentColorOverTheBackground()
	{
		IRenderedComponent<MokaColorPicker> cut = Picker(p => p
			.Add(x => x.ShowContrast, true)
			.Add(x => x.ContrastBackground, "#ffffff")
			.Add(x => x.Value, "#00000000"));
		await Open(cut);

		Assert.Equal("1.00:1", cut.Find(".moka-colorpicker-contrast-ratio").TextContent);
	}

	// C3B
	[Fact]
	public async Task ShowFormats_WritesTheValueInTheChosenFormat()
	{
		List<MokaColorFormat> formats = [];
		IRenderedComponent<MokaColorPicker> cut = Picker(p => p
			.Add(x => x.ShowFormats, true)
			.Add(x => x.Value, "#ef5350")
			.Add(x => x.FormatChanged, format => formats.Add(format)));
		await Open(cut);

		Assert.Equal("#EF5350", FormatValue(cut));
		Assert.Equal(["HEX", "RGB", "HSL", "OKLCH"], cut.FindAll(".moka-segment").Select(s => s.TextContent.Trim()));

		await cut.FindAll(".moka-colorpicker-formats input[type=radio]")[1].ChangeAsync(new ChangeEventArgs { Value = "on" });

		Assert.Equal("rgb(239 83 80)", FormatValue(cut));
		Assert.Equal([MokaColorFormat.Rgb], formats);
		IconTooltip.AssertNamed(cut.Find(".moka-colorpicker-formats .moka-colorpicker-icon-btn"), "Copy value", MokaTooltipPosition.BottomEnd);
	}

	// The original panel has no element carrying the color, so its extras get one, and only when one is on.
	[Fact]
	public async Task OnStandard_TheExtrasCarryTheColorForTheirPreviews()
	{
		IRenderedComponent<MokaColorPicker> cut = Picker(p => p
			.Add(x => x.ShowFormats, true)
			.Add(x => x.Value, "#ef535080"));
		await Open(cut);

		Assert.Equal("--_cp-color: #ef5350; --_cp-color-a: #ef535080", cut.Find(".moka-colorpicker-extras").GetAttribute("style"));
		Assert.Single(cut.FindAll(".moka-colorpicker-extras .moka-colorpicker-formats"));
	}

	[Fact]
	public async Task OnStandard_NoExtras_NoExtrasElement()
	{
		IRenderedComponent<MokaColorPicker> cut = Picker(p => p.Add(x => x.ClearableRecentColors, true));
		await Open(cut);

		Assert.Empty(cut.FindAll(".moka-colorpicker-extras"));
		Assert.Single(cut.FindAll(".moka-colorpicker-recent-sec"));
	}

	[Theory]
	[InlineData(MokaColorFormat.Hsl, "hsl(1 83% 63%)")]
	[InlineData(MokaColorFormat.Rgb, "rgb(239 83 80)")]
	public async Task ShowFormats_StartsOnTheFormatGiven(MokaColorFormat format, string text)
	{
		IRenderedComponent<MokaColorPicker> cut = Picker(p => p
			.Add(x => x.Layout, MokaColorPickerLayout.Compact)
			.Add(x => x.ShowFormats, true)
			.Add(x => x.Format, format)
			.Add(x => x.Value, "#ef5350"));
		await Open(cut);

		Assert.Equal(text, FormatValue(cut));
	}

	// The copy buttons carry their text and Core's click listener copies it inside the click: a .NET handler copied a
	// round trip later, which Safari, iOS and Firefox refuse.
	[Fact]
	public async Task ShowFormats_CopiesTheValueAsItIsShown()
	{
		IRenderedComponent<MokaColorPicker> cut = Picker(p => p
			.Add(x => x.ShowFormats, true)
			.Add(x => x.Format, MokaColorFormat.Oklch)
			.Add(x => x.Value, "#ef5350"));
		await Open(cut);
		IElement button = cut.Find(".moka-colorpicker-formats .moka-colorpicker-icon-btn");
		string? text = button.GetAttribute("data-moka-copy");

		Assert.Throws<MissingEventHandlerException>(() => button.Click());
		await button.TriggerEventAsync("onmokacopied", new MokaCopyEventArgs { Copied = true });

		Assert.StartsWith("oklch(", text, StringComparison.Ordinal);
		Assert.Equal(FormatValue(cut), text);
		Assert.Equal($"Copied {text}", cut.Find(".moka-colorpicker-formats [role=status]").TextContent);
	}

	[Fact]
	public async Task ShowFormats_SaysWhenTheBrowserRefused()
	{
		IRenderedComponent<MokaColorPicker> cut = Picker(p => p.Add(x => x.ShowFormats, true).Add(x => x.Value, "#ef5350"));
		await Open(cut);

		await cut.Find(".moka-colorpicker-formats .moka-colorpicker-icon-btn")
			.TriggerEventAsync("onmokacopied", new MokaCopyEventArgs { Copied = false });

		Assert.Equal("Press Ctrl+C to copy it.", cut.Find(".moka-colorpicker-formats [role=status]").TextContent);
	}

	[Fact]
	public async Task TheHexRowsCopy_CarriesTheHex_AndShowsACheckOnceCopied()
	{
		IRenderedComponent<MokaColorPicker> cut = Picker(p => p
			.Add(x => x.Layout, MokaColorPickerLayout.Wheel)
			.Add(x => x.Value, "#7e57c2"));
		await Open(cut);
		IElement button = cut.Find(".moka-colorpicker-hexrow .moka-colorpicker-icon-btn");

		Assert.Equal("#7e57c2", button.GetAttribute("data-moka-copy"));
		await button.TriggerEventAsync("onmokacopied", new MokaCopyEventArgs { Copied = false });
		Assert.DoesNotContain("moka-colorpicker-icon-btn--done", cut.Find(".moka-colorpicker-hexrow .moka-colorpicker-icon-btn").ClassList);

		await cut.Find(".moka-colorpicker-hexrow .moka-colorpicker-icon-btn")
			.TriggerEventAsync("onmokacopied", new MokaCopyEventArgs { Copied = true });
		Assert.Contains("moka-colorpicker-icon-btn--done", cut.Find(".moka-colorpicker-hexrow .moka-colorpicker-icon-btn").ClassList);
	}

	[Fact]
	public void TheFieldsCopyButton_CarriesTheHex()
	{
		IRenderedComponent<MokaColorPicker> cut = Picker(p => p.Add(x => x.Value, "#123456"));

		IElement button = cut.Find(".moka-colorpicker-copy-btn");
		Assert.Equal("#123456", button.GetAttribute("data-moka-copy"));
		Assert.Throws<MissingEventHandlerException>(() => button.Click());
	}

	// C3C
	[Fact]
	public async Task ShowAlphaPreview_OnStandard_HandsTheBarItsGradientToLayOverAChecker()
	{
		IRenderedComponent<MokaColorPicker> cut = Picker(p => p
			.Add(x => x.ShowAlpha, true)
			.Add(x => x.ShowAlphaPreview, true)
			.Add(x => x.Value, "#ef5350b8"));
		await Open(cut);

		IElement bar = cut.Find(".moka-colorpicker-alpha-slider");
		Assert.Contains("moka-colorpicker-alpha-slider--checker", bar.ClassList);
		Assert.StartsWith("--_cp-alpha-fill: linear-gradient(", bar.GetAttribute("style"), StringComparison.Ordinal);
		Assert.Equal("#EF5350B8", cut.Find(".moka-colorpicker-alpha-hex").TextContent);
		Assert.Equal("rgb(239 83 80 / 72%)", cut.Find(".moka-colorpicker-alpha-values .moka-colorpicker-dim").TextContent);
		Assert.Equal("--_cp-solid: #ef5350; --_cp-see: #ef5350b8", cut.Find(".moka-colorpicker-alpha-preview").GetAttribute("style"));
	}

	[Fact]
	public async Task ShowAlphaPreview_InALayout_AddsTheOpacityAsANumber()
	{
		IRenderedComponent<MokaColorPicker> cut = Picker(p => p
			.Add(x => x.Layout, MokaColorPickerLayout.Compact)
			.Add(x => x.ShowAlpha, true)
			.Add(x => x.ShowAlphaPreview, true)
			.Add(x => x.Value, "#ef5350b8"));
		await Open(cut);

		Assert.Contains("moka-colorpicker-track--checker", cut.Find("[data-cp-drag=alpha]").ClassList);
		IElement percent = cut.Find("input[aria-label='Opacity in percent']");
		Assert.Equal("72", percent.GetAttribute("value"));

		await percent.ChangeAsync(new ChangeEventArgs { Value = "50" });

		Assert.Equal("#ef535080", _values[^1]);
	}

	// C3D
	[Fact]
	public async Task LabelledEyeDropper_ReplacesTheBareIconWithANamedButton_KeyedToI()
	{
		IRenderedComponent<MokaColorPicker> cut = Picker(p => p
			.Add(x => x.ShowEyeDropper, true)
			.Add(x => x.LabelledEyeDropper, true));
		await Open(cut);

		Assert.Empty(cut.FindAll(".moka-colorpicker-eyedropper-btn"));
		IElement button = cut.Find(".moka-colorpicker-eye-btn");
		Assert.True(button.HasAttribute("data-cp-eye"));
		Assert.True(button.HasAttribute("data-cp-eye-key"));
		Assert.Contains("Pick from screen", button.TextContent, StringComparison.Ordinal);
		Assert.Equal("I", button.QuerySelector("kbd")!.TextContent.Trim());
		Assert.Single(_panel.Invocations, call => call.Identifier == "bind");
	}

	[Fact]
	public async Task LabelledEyeDropper_SaysWhatItPicked()
	{
		IRenderedComponent<MokaColorPicker> cut = Picker(p => p
			.Add(x => x.Layout, MokaColorPickerLayout.Compact)
			.Add(x => x.LabelledEyeDropper, true));
		await Open(cut);
		Assert.Empty(cut.FindAll(".moka-colorpicker-hexrow [data-cp-eye]"));

		await cut.InvokeAsync(() => cut.Instance.OnEyeDropped("#42a5f5"));
		Assert.Equal("#42a5f5", _values[^1]);
		Assert.Equal("Picked #42A5F5 from the screen.", cut.Find(".moka-colorpicker-eye-note").TextContent);

		await cut.InvokeAsync(() => cut.Instance.OnEyeDropped(null));
		Assert.Single(_values);
		Assert.Equal("Cancelled, nothing changed.", cut.Find(".moka-colorpicker-eye-note").TextContent);
	}

	// C3E
	[Fact]
	public async Task ClearableRecentColors_ShowsTheListGiven_AndMovesAChoiceToTheFront()
	{
		IRenderedComponent<MokaColorPicker> cut = Picker(p => p
			.Add(x => x.ClearableRecentColors, true)
			.Add(x => x.RecentColors, ThreeRecent)
			.Add(x => x.RecentColorsChanged, list => _recents.Add(list)));
		await Open(cut);

		Assert.Equal(["#111111", "#222222", "#333333"], RecentSwatches(cut));

		await cut.FindAll(".moka-colorpicker-recent-sec input.moka-color-swatch-input")[2].ChangeAsync(new ChangeEventArgs { Value = true });

		Assert.Equal("#333333", _values[^1]);
		Assert.Equal(["#333333", "#111111", "#222222"], _recents[^1]);
		Assert.Equal(["#333333", "#111111", "#222222"], RecentSwatches(cut));
	}

	[Fact]
	public async Task ClearableRecentColors_ClearsAndUndoes()
	{
		IRenderedComponent<MokaColorPicker> cut = Picker(p => p
			.Add(x => x.Layout, MokaColorPickerLayout.Compact)
			.Add(x => x.ClearableRecentColors, true)
			.Add(x => x.RecentColors, TwoRecent)
			.Add(x => x.RecentColorsChanged, list => _recents.Add(list)));
		await Open(cut);

		await cut.Find(".moka-colorpicker-rec-head .moka-colorpicker-textbtn").ClickAsync(new MouseEventArgs());

		Assert.Empty(_recents[^1]);
		Assert.Contains("Colors you choose show here.", cut.Find(".moka-colorpicker-rec-empty").TextContent, StringComparison.Ordinal);
		Assert.Empty(cut.FindAll(".moka-colorpicker-rec-head .moka-colorpicker-textbtn"));

		await cut.Find(".moka-colorpicker-rec-empty .moka-colorpicker-textbtn").ClickAsync(new MouseEventArgs());

		Assert.Equal(["#111111", "#222222"], _recents[^1]);
		Assert.Equal(["#111111", "#222222"], RecentSwatches(cut));
		Assert.Contains(_panel.Invocations, call => call.Identifier == "focusFirst");
	}

	[Fact]
	public async Task ClosingThePopup_AddsTheColorToTheFront_AndSaysSo()
	{
		IRenderedComponent<MokaColorPicker> cut = Picker(p => p
			.Add(x => x.Value, "#abcdef")
			.Add(x => x.RecentColors, OneRecent)
			.Add(x => x.RecentColorsChanged, list => _recents.Add(list)));
		await Open(cut);
		await cut.Find(".moka-colorpicker-backdrop").ClickAsync(new MouseEventArgs());

		Assert.Equal(["#abcdef", "#111111"], Assert.Single(_recents));
	}

	// C3F
	[Fact]
	public async Task ShowThemeColors_SetsTheValueToTheToken()
	{
		IRenderedComponent<MokaColorPicker> cut = Picker(p => p
			.Add(x => x.Layout, MokaColorPickerLayout.Compact)
			.Add(x => x.ShowThemeColors, true), MokaTheme.Dark);
		await Open(cut);

		IReadOnlyList<IElement> tokens = cut.FindAll(".moka-colorpicker-token");
		Assert.Equal(["Primary", "Success", "Warning", "Error", "Info", "Surface", "On surface", "Outline"],
			tokens.Select(t => t.TextContent.Trim()));
		Assert.Equal("background: var(--moka-color-primary)", tokens[0].QuerySelector(".moka-colorpicker-token-swatch")!.GetAttribute("style"));

		await tokens[0].ClickAsync(new MouseEventArgs());

		Assert.Equal("var(--moka-color-primary)", _values[^1]);
		Assert.Equal("true", cut.FindAll(".moka-colorpicker-token")[0].GetAttribute("aria-pressed"));
		Assert.Contains("--_cp-color: var(--moka-color-primary)", cut.Find(".moka-colorpicker-panel").GetAttribute("style"), StringComparison.Ordinal);

		// The panel shows the color the token stands for in this theme.
		Assert.Equal("0.9373", cut.Find(".moka-colorpicker-panel").GetAttribute("data-v"));
		Assert.Equal("0.6653", cut.Find(".moka-colorpicker-panel").GetAttribute("data-s"));
		Assert.Equal("var(--moka-color-primary)", cut.Find("input[data-cp-hex]").GetAttribute("value"));
	}

	[Fact]
	public async Task ShowThemeColors_InTabbed_GoesInTheSwatchesTab()
	{
		IRenderedComponent<MokaColorPicker> cut = Picker(p => p
			.Add(x => x.Layout, MokaColorPickerLayout.Tabbed)
			.Add(x => x.ShowThemeColors, true));
		await Open(cut);

		Assert.Empty(cut.FindAll(".moka-colorpicker-token"));
		await cut.FindAll("[role=tab]")[1].ClickAsync(new MouseEventArgs());
		Assert.Equal(8, cut.FindAll(".moka-colorpicker-token").Count);
	}

	[Fact]
	public void AThemeToken_IsAValidValue_AndShowsInTheClosedField()
	{
		IRenderedComponent<MokaColorPicker> cut = Picker(p => p.Add(x => x.Value, "var(--moka-color-success)"));

		Assert.Equal("background: var(--moka-color-success)", cut.Find(".moka-colorpicker-swatch-preview").GetAttribute("style"));
		Assert.Equal("var(--moka-color-success)", cut.Find("input[aria-haspopup]").GetAttribute("value"));
	}

	[Fact]
	public void AThemeToken_PassesValidation_InAnEditForm()
	{
		Model model = new() { Color = "#000000" };
		EditContext context = new(model);
		IRenderedComponent<MokaColorPicker> cut = Render<MokaColorPicker>(p => p
			.AddCascadingValue(context)
			.Add(x => x.Value, model.Color)
			.Add(x => x.ValueChanged, value => model.Color = value)
			.Add(x => x.ValueExpression, (Expression<Func<string>>)(() => model.Color))
			.Add(x => x.Inline, true)
			.Add(x => x.Layout, MokaColorPickerLayout.Compact));

		cut.Find("input[data-cp-hex]").Change("var(--moka-color-primary)");

		Assert.Equal("var(--moka-color-primary)", model.Color);
		Assert.Empty(context.GetValidationMessages());
	}

	[Fact]
	public async Task TheNewLayouts_NameEveryIconButtonInATooltip()
	{
		IRenderedComponent<MokaColorPicker> cut = Picker(p => p
			.Add(x => x.Layout, MokaColorPickerLayout.Sliders)
			.Add(x => x.ShowFormats, true));
		await Open(cut);

		foreach (IElement button in cut.FindAll(".moka-colorpicker-icon-btn"))
		{
			Assert.False(string.IsNullOrEmpty(button.GetAttribute("aria-label")));
			Assert.NotNull(button.Closest(".moka-tooltip-trigger"));
		}
	}

	[Fact]
	public void ADisabledInlinePanel_TakesNoKeysAndBindsNoScript()
	{
		IRenderedComponent<MokaColorPicker> cut = Picker(p => p
			.Add(x => x.Inline, true)
			.Add(x => x.Disabled, true)
			.Add(x => x.Layout, MokaColorPickerLayout.Compact));

		Assert.Equal("-1", cut.Find("[data-cp-drag=sv]").GetAttribute("tabindex"));
		Assert.Equal("true", cut.Find(".moka-colorpicker-panel").GetAttribute("data-disabled"));
		Assert.DoesNotContain(_panel.Invocations, call => call.Identifier == "bind");
	}

	private IRenderedComponent<MokaColorPicker> Picker(Action<ComponentParameterCollectionBuilder<MokaColorPicker>>? configure = null,
		MokaTheme? theme = null) =>
		Render<MokaColorPicker>(p =>
		{
			if (theme is not null)
			{
				p.AddCascadingValue(theme);
			}

			p.Add(x => x.ValueChanged, value => _values.Add(value));
			configure?.Invoke(p);
		});

	private static Task Open(IRenderedComponent<MokaColorPicker> cut) =>
		cut.Find(".moka-colorpicker-swatch-button").ClickAsync(new MouseEventArgs());

	private static string? FormatValue(IRenderedComponent<MokaColorPicker> cut) =>
		cut.Find(".moka-colorpicker-formats input[aria-label='Color value']").GetAttribute("value");

	private static IEnumerable<string?> RecentSwatches(IRenderedComponent<MokaColorPicker> cut) =>
		cut.FindAll(".moka-colorpicker-recent-sec input.moka-color-swatch-input").Select(b => b.GetAttribute("aria-label"));

	private sealed class Model
	{
		public string Color { get; set; } = "";
	}
}
