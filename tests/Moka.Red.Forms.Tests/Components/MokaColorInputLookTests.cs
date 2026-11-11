using System.Linq.Expressions;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Feedback.Tooltip;
using Moka.Red.Forms.ColorInput;
using Moka.Red.Forms.ColorPicker;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Forms.Tests.Components;

// MokaColorInput's looks. Standard, the default, is the original text field; the others open a MokaColorPicker
// panel in a popup in the top layer, except Terminal, which is typed into.
public class MokaColorInputLookTests : BunitContext
{
	private const string DragModule = "./_content/Moka.Red.Core/moka-drag.js";

	private static readonly string[] OneRecent = ["#111111"];
	private static readonly string[] InkAndPaper = ["#000000", "#ffffff"];
	private const string PickerModule = "./_content/Moka.Red.Forms/ColorPicker/MokaColorPicker.razor.js";

	private readonly BunitJSModuleInterop _picker;
	private readonly BunitJSModuleInterop _popup;
	private readonly List<IReadOnlyList<string>> _recents = [];
	private readonly List<string> _values = [];

	public MokaColorInputLookTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_popup = TopLayerPopup.Setup(JSInterop);
		_picker = JSInterop.SetupModule(PickerModule);
		_picker.Mode = JSRuntimeMode.Loose;
	}

	private static IReadOnlyList<IElement> Presets(IRenderedComponent<MokaColorInput> cut) =>
		cut.FindAll(".moka-color-input__palette input.moka-color-swatch-input");

	public static TheoryData<MokaColorInputLook, string> PopupLooks => new()
	{
		{ MokaColorInputLook.SwatchButton, ".moka-color-input__swatch-btn" },
		{ MokaColorInputLook.Split, ".moka-color-input__split-swatch" },
		{ MokaColorInputLook.Chip, ".moka-color-input__chip" },
		{ MokaColorInputLook.Palette, ".moka-color-input__custom" }
	};

	[Fact]
	public void Standard_IsTheDefault_AndDrawsTheOriginalField()
	{
		IRenderedComponent<MokaColorInput> cut = Input(p => p.Add(x => x.Value, "#ef5350"));

		Assert.Equal(MokaColorInputLook.Standard, cut.Instance.Look);
		IElement box = cut.Find(".moka-color-input");
		Assert.DoesNotContain("moka-color-input--look", box.ClassList);
		Assert.Contains("moka-color-input--md", box.ClassList);
		Assert.Single(cut.FindAll(".moka-color-input__swatch"));
		Assert.Single(cut.FindAll("input[type=color]"));
		Assert.Empty(cut.FindAll("button"));
	}

	// Standard still takes any text, as it always has.
	[Fact]
	public void Standard_StillTakesAnyText()
	{
		IRenderedComponent<MokaColorInput> cut = Input();

		cut.Find("input[type=text]").Input("not a color");

		Assert.Equal(["not a color"], _values);
		Assert.Empty(cut.FindAll(".moka-field-helper--error"));
	}

	[Theory]
	[MemberData(nameof(PopupLooks))]
	public async Task ALook_OpensThePickerInTheTopLayer_AgainstTheField(MokaColorInputLook look, string trigger)
	{
		IRenderedComponent<MokaColorInput> cut = Input(p => p.Add(x => x.Look, look));
		string? anchorId = cut.Find(".moka-color-input").GetAttribute("blazor:elementReference");

		IElement button = cut.Find(trigger);
		Assert.Equal("dialog", button.GetAttribute("aria-haspopup"));
		Assert.Equal("false", button.GetAttribute("aria-expanded"));
		Assert.Null(button.GetAttribute("aria-controls"));

		await button.ClickAsync(new MouseEventArgs());

		IElement popup = cut.Find(".moka-color-input__popup");
		TopLayerPopup.AssertOpened(_popup, popup, anchorId, "bottom-start");
		Assert.Equal("dialog", popup.GetAttribute("role"));
		Assert.Equal("Choose a color", popup.GetAttribute("aria-label"));
		Assert.Equal("true", cut.Find(trigger).GetAttribute("aria-expanded"));
		Assert.Equal(popup.Id, cut.Find(trigger).GetAttribute("aria-controls"));
		Assert.Contains("moka-colorpicker-panel--compact", cut.Find(".moka-colorpicker-panel").ClassList);
		Assert.Contains("moka-colorpicker-panel--inline", cut.Find(".moka-colorpicker-panel").ClassList);
		JSRuntimeInvocation focus = Assert.Single(_picker.Invocations, call => call.Identifier == "focusFirst");
		Assert.Equal(popup.GetAttribute("blazor:elementReference"), Assert.IsType<ElementReference>(focus.Arguments[0]).Id);
	}

	[Fact]
	public async Task Escape_ClosesThePopup_AndPutsFocusBackOnTheTrigger()
	{
		IRenderedComponent<MokaColorInput> cut = Input(p => p.Add(x => x.Look, MokaColorInputLook.SwatchButton));
		await cut.Find(".moka-color-input__swatch-btn").ClickAsync(new MouseEventArgs());

		await cut.Find(".moka-color-input__popup").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

		Assert.Empty(cut.FindAll(".moka-color-input__popup"));
		TopLayerPopup.AssertClosed(_popup);
		JSInterop.VerifyFocusAsyncInvoke(1);
	}

	[Fact]
	public async Task TheBackdrop_ClosesThePopup()
	{
		IRenderedComponent<MokaColorInput> cut = Input(p => p.Add(x => x.Look, MokaColorInputLook.Chip));
		await cut.Find(".moka-color-input__chip").ClickAsync(new MouseEventArgs());

		await cut.Find(".moka-color-input__backdrop").ClickAsync(new MouseEventArgs());

		Assert.Empty(cut.FindAll(".moka-color-input__popup"));
		TopLayerPopup.AssertClosed(_popup);
	}

	[Fact]
	public async Task ThePopup_ClosesWhenTheFieldGoes()
	{
		IRenderedComponent<MokaColorInput> cut = Input(p => p.Add(x => x.Look, MokaColorInputLook.SwatchButton));
		await cut.Find(".moka-color-input__swatch-btn").ClickAsync(new MouseEventArgs());

		await DisposeComponentsAsync();

		TopLayerPopup.AssertClosed(_popup);
	}

	[Fact]
	public async Task APick_ReachesTheField()
	{
		IRenderedComponent<MokaColorInput> cut = Input(p => p.Add(x => x.Look, MokaColorInputLook.SwatchButton));
		await cut.Find(".moka-color-input__swatch-btn").ClickAsync(new MouseEventArgs());

		MokaColorPicker picker = cut.FindComponent<MokaColorPicker>().Instance;
		await cut.InvokeAsync(() => picker.OnPanelPicked(0, 1, 1, 1));

		Assert.Equal(["#ff0000"], _values);
		Assert.Equal("#FF0000", cut.Find(".moka-color-input__hex").TextContent);
	}

	[Fact]
	public async Task PickerLayout_ChoosesThePanel_AndTheExtrasPassThrough()
	{
		IRenderedComponent<MokaColorInput> cut = Input(p => p
			.Add(x => x.Look, MokaColorInputLook.SwatchButton)
			.Add(x => x.PickerLayout, MokaColorPickerLayout.Tabbed)
			.Add(x => x.ShowContrast, true)
			.Add(x => x.ShowFormats, true)
			.Add(x => x.Format, MokaColorFormat.Hsl)
			.Add(x => x.ShowThemeColors, true)
			.Add(x => x.ClearableRecentColors, true)
			.Add(x => x.Value, "#ef5350"));
		await cut.Find(".moka-color-input__swatch-btn").ClickAsync(new MouseEventArgs());

		MokaColorPicker picker = cut.FindComponent<MokaColorPicker>().Instance;
		Assert.True(picker.Inline);
		Assert.Equal(MokaColorPickerLayout.Tabbed, picker.Layout);
		Assert.True(picker.ShowContrast);
		Assert.True(picker.ShowFormats);
		Assert.Equal(MokaColorFormat.Hsl, picker.Format);
		Assert.True(picker.ShowThemeColors);
		Assert.True(picker.ClearableRecentColors);
		Assert.Single(cut.FindAll(".moka-colorpicker-contrast"));
	}

	// The panel belongs to the field, so an EditForm sees one field, not two.
	[Fact]
	public async Task ThePanel_IsNotAFieldOfTheForm()
	{
		Model model = new() { Color = "#000000" };
		EditContext context = new(model);
		IRenderedComponent<MokaColorInput> cut = Render<MokaColorInput>(p => p
			.AddCascadingValue(context)
			.Add(x => x.Look, MokaColorInputLook.SwatchButton)
			.Add(x => x.Value, model.Color)
			.Add(x => x.ValueChanged, value => model.Color = value)
			.Add(x => x.ValueExpression, (Expression<Func<string>>)(() => model.Color)));
		await cut.Find(".moka-color-input__swatch-btn").ClickAsync(new MouseEventArgs());

		MokaColorPicker picker = cut.FindComponent<MokaColorPicker>().Instance;
		await cut.InvokeAsync(() => picker.OnPanelPicked(120, 1, 1, 1));

		Assert.Equal("#00ff00", model.Color);
		Assert.True(context.IsModified(() => model.Color));
		Assert.Empty(cut.FindAll(".moka-colorpicker input[type=hidden]"));
	}

	[Fact]
	public async Task ClosingAfterAChange_AddsTheColorToTheRecentColors()
	{
		IRenderedComponent<MokaColorInput> cut = Input(p => p
			.Add(x => x.Look, MokaColorInputLook.SwatchButton)
			.Add(x => x.RecentColors, OneRecent)
			.Add(x => x.RecentColorsChanged, list => _recents.Add(list)));
		await cut.Find(".moka-color-input__swatch-btn").ClickAsync(new MouseEventArgs());
		await cut.InvokeAsync(() => cut.FindComponent<MokaColorPicker>().Instance.OnPanelPicked(0, 1, 1, 1));

		await cut.Find(".moka-color-input__backdrop").ClickAsync(new MouseEventArgs());

		Assert.Equal(["#ff0000", "#111111"], _recents[^1]);
	}

	// C1A
	[Fact]
	public void SwatchButton_ShowsTheHexInMono_NamedByTheLabelAndTheValue()
	{
		IRenderedComponent<MokaColorInput> cut = Input(p => p
			.Add(x => x.Look, MokaColorInputLook.SwatchButton)
			.Add(x => x.Label, "Color")
			.Add(x => x.Id, "tag-color")
			.Add(x => x.Value, "#ef5350"));

		IElement button = cut.Find(".moka-color-input__swatch-btn");
		Assert.Equal("tag-color", button.Id);
		Assert.Equal("tag-color-label tag-color-text", button.GetAttribute("aria-labelledby"));
		Assert.Equal("#EF5350", cut.Find("#tag-color-text").TextContent);
		Assert.Equal("background-color: #ef5350", cut.Find(".moka-color-input__dot").GetAttribute("style"));
		Assert.Null(cut.Find("label.moka-field-label").GetAttribute("for"));
		Assert.Contains("moka-color-input--swatch-button", cut.Find(".moka-color-input").ClassList);
	}

	[Fact]
	public void SwatchButton_NamesAThemeTokenByItsColor()
	{
		IRenderedComponent<MokaColorInput> cut = Input(p => p
			.Add(x => x.Look, MokaColorInputLook.SwatchButton)
			.Add(x => x.Value, "var(--moka-color-primary)"));

		Assert.Equal("Primary", cut.Find(".moka-color-input__hex").TextContent);
		Assert.Equal("background-color: var(--moka-color-primary)", cut.Find(".moka-color-input__dot").GetAttribute("style"));
	}

	[Theory]
	[InlineData(MokaColorInputLook.SwatchButton)]
	[InlineData(MokaColorInputLook.Chip)]
	[InlineData(MokaColorInputLook.Palette)]
	public void ALookWithNoTextBox_PostsItsValueThroughAHiddenField(MokaColorInputLook look)
	{
		Model model = new() { Color = "#123456" };
		IRenderedComponent<MokaColorInput> cut = Render<MokaColorInput>(p => p
			.Add(x => x.Look, look)
			.Add(x => x.Value, model.Color)
			.Add(x => x.ValueExpression, (Expression<Func<string>>)(() => model.Color)));

		IElement hidden = cut.Find("input[type=hidden]");
		Assert.Equal("model.Color", hidden.GetAttribute("name"));
		Assert.Equal("#123456", hidden.GetAttribute("value"));
	}

	// C1B
	[Fact]
	public void Split_TypesIntoATextBox_AndOpensThePickerFromItsSwatch()
	{
		Model model = new() { Color = "#ef5350" };
		IRenderedComponent<MokaColorInput> cut = Render<MokaColorInput>(p => p
			.Add(x => x.Look, MokaColorInputLook.Split)
			.Add(x => x.Label, "Color")
			.Add(x => x.Value, model.Color)
			.Add(x => x.ValueExpression, (Expression<Func<string>>)(() => model.Color)));

		IElement box = cut.Find("input[type=text]");
		Assert.Equal(cut.Find("label.moka-field-label").GetAttribute("for"), box.Id);
		Assert.Equal("model.Color", box.GetAttribute("name"));
		Assert.Equal("#ef5350", box.GetAttribute("value"));
		Assert.Empty(cut.FindAll("input[type=hidden]"));
		IconTooltip.AssertNamed(cut.Find(".moka-color-input__split-swatch"), "Open the color picker", MokaTooltipPosition.BottomStart);
	}

	[Fact]
	public void Split_HasNoEyedropper_WhereTheBrowserHasNone()
	{
		IRenderedComponent<MokaColorInput> cut = Input(p => p.Add(x => x.Look, MokaColorInputLook.Split));

		Assert.Empty(cut.FindAll(".moka-color-input__eye"));
	}

	[Fact]
	public async Task Split_HasAnEyedropper_WhereTheBrowserHasOne()
	{
		BunitJSModuleInterop drag = JSInterop.SetupModule(DragModule);
		drag.Mode = JSRuntimeMode.Loose;
		drag.Setup<bool>("isEyeDropperSupported").SetResult(true);
		drag.Setup<string?>("pickColor").SetResult("#42a5f5");

		IRenderedComponent<MokaColorInput> cut = Input(p => p.Add(x => x.Look, MokaColorInputLook.Split));

		IElement eye = cut.WaitForElement(".moka-color-input__eye");
		IconTooltip.AssertNamed(eye, "Pick from screen", MokaTooltipPosition.BottomEnd);

		await eye.ClickAsync(new MouseEventArgs());

		Assert.Equal(["#42a5f5"], _values);
	}

	[Theory]
	[InlineData(MokaColorInputLook.Split)]
	[InlineData(MokaColorInputLook.Terminal)]
	public void ATypedColor_TakesEffectOnceWhole(MokaColorInputLook look)
	{
		IRenderedComponent<MokaColorInput> cut = Input(p => p.Add(x => x.Look, look));

		cut.Find("input[type=text]").Input("#42a");
		cut.Find("input[type=text]").Input("#42a5f");
		Assert.Empty(_values);
		Assert.Null(cut.Find("input[type=text]").GetAttribute("aria-invalid"));

		cut.Find("input[type=text]").Input("42a5f5");
		Assert.Equal(["#42a5f5"], _values);
	}

	[Theory]
	[InlineData(MokaColorInputLook.Split)]
	[InlineData(MokaColorInputLook.Terminal)]
	public void TextThatCannotBeAColor_ShowsTheError(MokaColorInputLook look)
	{
		IRenderedComponent<MokaColorInput> cut = Input(p => p.Add(x => x.Look, look));

		cut.Find("input[type=text]").Input("#zz");

		Assert.Equal("true", cut.Find("input[type=text]").GetAttribute("aria-invalid"));
		Assert.Equal("Six hex digits", cut.Find(".moka-field-helper--error").TextContent.Trim());
		Assert.Contains("moka-color-input--error", cut.Find(".moka-color-input").ClassList);
		Assert.Empty(_values);
	}

	[Fact]
	public void AShortHex_TakesEffectWhenTheFieldIsLeft()
	{
		IRenderedComponent<MokaColorInput> cut = Input(p => p.Add(x => x.Look, MokaColorInputLook.Split));

		cut.Find("input[type=text]").Change("#abc");

		Assert.Equal(["#abc"], _values);
	}

	[Fact]
	public void AnInvalidColor_IsAValidationMessage_InAnEditForm()
	{
		Model model = new() { Color = "#ef5350" };
		EditContext context = new(model);
		IRenderedComponent<MokaColorInput> cut = Render<MokaColorInput>(p => p
			.AddCascadingValue(context)
			.Add(x => x.Look, MokaColorInputLook.Terminal)
			.Add(x => x.InvalidText, "Use a hex color")
			.Add(x => x.Value, model.Color)
			.Add(x => x.ValueChanged, value => model.Color = value)
			.Add(x => x.ValueExpression, (Expression<Func<string>>)(() => model.Color)));

		cut.Find("input[type=text]").Change("orange");

		Assert.Equal("#ef5350", model.Color);
		Assert.Equal(["Use a hex color"], context.GetValidationMessages());
		Assert.Equal("Use a hex color", cut.Find(".moka-field-helper--error").TextContent.Trim());

		cut.Find("input[type=text]").Change("var(--moka-color-warning)");

		Assert.Equal("var(--moka-color-warning)", model.Color);
		Assert.Empty(context.GetValidationMessages());
	}

	// C1C
	[Fact]
	public void Chip_HasNoFieldAroundIt_AndCarriesItsLabelInItsName()
	{
		IRenderedComponent<MokaColorInput> cut = Input(p => p
			.Add(x => x.Look, MokaColorInputLook.Chip)
			.Add(x => x.Label, "Fill color")
			.Add(x => x.Value, "#ffb547")
			.Add(x => x.MarginValue, "4px"));

		Assert.Empty(cut.FindAll(".moka-field"));
		IElement root = cut.Find(".moka-color-input");
		Assert.Equal("SPAN", root.TagName);
		Assert.Equal("margin: 4px", root.GetAttribute("style"));
		IElement chip = cut.Find(".moka-color-input__chip");
		Assert.Equal("Fill color", chip.QuerySelector(".moka-visually-hidden")!.TextContent.Trim());
		Assert.Equal("#FFB547", chip.QuerySelector(".moka-color-input__hex")!.TextContent);
	}

	// C1D
	[Fact]
	public void Palette_ShowsEightNamedPresets_AndCustom()
	{
		IRenderedComponent<MokaColorInput> cut = Input(p => p
			.Add(x => x.Look, MokaColorInputLook.Palette)
			.Add(x => x.Label, "Color")
			.Add(x => x.Value, "#42a5f5"));

		IElement group = cut.Find(".moka-color-input__palette");
		Assert.Equal("radiogroup", group.GetAttribute("role"));
		Assert.Equal(cut.Find("label.moka-field-label").Id, group.GetAttribute("aria-labelledby"));
		Assert.False(group.HasAttribute("aria-label"));

		IReadOnlyList<IElement> presets = Presets(cut);
		Assert.Equal(["Red", "Orange", "Amber", "Green", "Teal", "Blue", "Violet", "Gray"], presets.Select(b => b.GetAttribute("aria-label")));
		IconTooltip.AssertNamed(presets[5], "Blue", MokaTooltipPosition.Bottom);
		Assert.Equal([false, false, false, false, false, true, false, false], presets.Select(radio => radio.HasAttribute("checked")));
		Assert.Equal("Custom", cut.Find(".moka-color-input__custom").TextContent.Trim());
		Assert.DoesNotContain("moka-color-input__custom--on", cut.Find(".moka-color-input__custom").ClassList);
	}

	// The presets were a button each, so Tab stopped at all eight on the way to Custom. As one radio group Tab reaches
	// them once, the arrows move the choice, and Custom comes after them in the same row.
	[Fact]
	public void Palette_IsOneRadioGroup_WithCustomAtItsEnd()
	{
		IRenderedComponent<MokaColorInput> cut = Input(p => p
			.Add(x => x.Look, MokaColorInputLook.Palette)
			.Add(x => x.Value, "#42a5f5"));

		IElement group = cut.Find(".moka-color-input__palette");
		Assert.Equal("Color", group.GetAttribute("aria-label"));
		Assert.Contains("moka-color-swatch--ring", group.ClassList);
		Assert.Contains("moka-color-swatch--mark-ring", group.ClassList);
		Assert.Single(Presets(cut).Select(radio => radio.GetAttribute("name")).Distinct());
		Assert.All(Presets(cut), radio => Assert.Equal("radio", radio.GetAttribute("type")));
		Assert.Empty(cut.FindAll("button.moka-color-input__preset"));
		Assert.Empty(cut.FindAll(".moka-color-input__palette .moka-color-swatch-check"));
		Assert.Empty(cut.FindAll(".moka-color-input__palette .moka-color-swatch-preview"));

		IElement end = cut.Find(".moka-color-input__palette .moka-color-swatch-end");
		Assert.NotNull(end.QuerySelector("button.moka-color-input__custom"));
	}

	[Fact]
	public void Palette_Disabled_DisablesEveryPresetAndCustom()
	{
		IRenderedComponent<MokaColorInput> cut = Input(p => p
			.Add(x => x.Look, MokaColorInputLook.Palette)
			.Add(x => x.Disabled, true));

		Assert.All(Presets(cut), radio => Assert.True(radio.HasAttribute("disabled")));
		Assert.True(cut.Find(".moka-color-input__custom").HasAttribute("disabled"));
	}

	[Fact]
	public async Task Palette_ChoosingAPreset_SetsTheValue()
	{
		IRenderedComponent<MokaColorInput> cut = Input(p => p.Add(x => x.Look, MokaColorInputLook.Palette));

		await Presets(cut)[3].ChangeAsync(new ChangeEventArgs { Value = true });

		Assert.Equal(["#66bb6a"], _values);
		Assert.True(Presets(cut)[3].HasAttribute("checked"));
	}

	[Fact]
	public void Palette_MarksCustom_WhenTheValueIsNoPreset()
	{
		IRenderedComponent<MokaColorInput> cut = Input(p => p
			.Add(x => x.Look, MokaColorInputLook.Palette)
			.Add(x => x.Value, "#123456"));

		IElement custom = cut.Find(".moka-color-input__custom");
		Assert.Contains("moka-color-input__custom--on", custom.ClassList);
		Assert.Equal("background-color: #123456", custom.QuerySelector(".moka-color-input__custom-dot")!.GetAttribute("style"));
		Assert.Equal(", #123456", custom.QuerySelector(".moka-visually-hidden")!.TextContent);
	}

	[Fact]
	public void Palette_TakesItsOwnPresetsAndNames()
	{
		IRenderedComponent<MokaColorInput> cut = Input(p => p
			.Add(x => x.Look, MokaColorInputLook.Palette)
			.Add(x => x.Presets, InkAndPaper)
			.Add(x => x.ColorLabel, color => color == "#000000" ? "Ink" : null)
			.Add(x => x.CustomText, "Other"));

		Assert.Equal(["Ink", "#ffffff"], Presets(cut).Select(b => b.GetAttribute("aria-label")));
		Assert.Equal("Other", cut.Find(".moka-color-input__custom").TextContent.Trim());
	}

	// C1E
	[Fact]
	public void Terminal_TagsItsValue_AndGlowsInTheColor()
	{
		IRenderedComponent<MokaColorInput> cut = Input(p => p
			.Add(x => x.Look, MokaColorInputLook.Terminal)
			.Add(x => x.Value, "#ef5350"));

		IElement box = cut.Find("input[type=text]");
		Assert.Contains("moka-color-input__text--terminal", box.ClassList);
		IElement tag = cut.Find(".moka-color-input__term-tag");
		Assert.Equal("HEX", tag.TextContent);
		Assert.Equal(tag.Id, box.GetAttribute("aria-describedby"));
		Assert.Equal("--_ci-color: #ef5350", cut.Find(".moka-color-input__term-square").GetAttribute("style"));
		Assert.Empty(cut.FindAll("button"));

		cut.Find("input[type=text]").Input("#qq");
		Assert.Equal("INVALID", cut.Find(".moka-color-input__term-tag").TextContent);

		cut.Find("input[type=text]").Input("var(--moka-color-info)");
		Assert.Equal("VAR", cut.Find(".moka-color-input__term-tag").TextContent);
	}

	// C3F: a token is a color the swatch can show.
	[Fact]
	public void Standard_ShowsAThemeTokenInItsSwatch()
	{
		IRenderedComponent<MokaColorInput> cut = Input(p => p.Add(x => x.Value, "var(--moka-color-primary)"));

		Assert.Equal("background-color: var(--moka-color-primary)", cut.Find(".moka-color-input__swatch").GetAttribute("style"));
	}

	[Fact]
	public void ADisabledLook_CannotOpen()
	{
		IRenderedComponent<MokaColorInput> cut = Input(p => p
			.Add(x => x.Look, MokaColorInputLook.SwatchButton)
			.Add(x => x.Disabled, true));

		Assert.True(cut.Find(".moka-color-input__swatch-btn").HasAttribute("disabled"));
		Assert.Contains("moka-color-input--disabled", cut.Find(".moka-color-input").ClassList);
	}

	private IRenderedComponent<MokaColorInput> Input(Action<ComponentParameterCollectionBuilder<MokaColorInput>>? configure = null) =>
		Render<MokaColorInput>(p =>
		{
			p.Add(x => x.ValueChanged, value => _values.Add(value));
			configure?.Invoke(p);
		});

	private sealed class Model
	{
		public string Color { get; set; } = "";
	}
}
