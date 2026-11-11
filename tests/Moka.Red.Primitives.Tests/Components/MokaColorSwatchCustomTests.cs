using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Feedback.Tooltip;
using Moka.Red.Primitives.ColorSwatch;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Primitives.Tests.Components;

// CustomLook: the rainbow + and the Custom pill open a panel in the top layer; the hex field takes six digits and Enter.
public class MokaColorSwatchCustomTests : BunitContext
{
	private const string SwatchModule = "./_content/Moka.Red.Primitives/ColorSwatch/MokaColorSwatch.razor.js";

	private static readonly string[] Palette = ["#ef5350", "#42a5f5", "#66bb6a"];

	private readonly List<string?> _changes = [];
	private readonly BunitJSModuleInterop _popup;
	private readonly BunitJSModuleInterop _swatch;

	public MokaColorSwatchCustomTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_popup = TopLayerPopup.Setup(JSInterop);
		_swatch = JSInterop.SetupModule(SwatchModule);
		_swatch.Mode = JSRuntimeMode.Loose;
	}

	private static string?[] Checked(IRenderedComponent<MokaColorSwatch> cut) =>
		[.. cut.FindAll("input.moka-color-swatch-input").Where(radio => radio.HasAttribute("checked")).Select(radio => radio.GetAttribute("value"))];

	private IRenderedComponent<MokaColorSwatch> Swatch(MokaSwatchCustomLook custom,
		Action<ComponentParameterCollectionBuilder<MokaColorSwatch>>? more = null) =>
		Render<MokaColorSwatch>(p =>
		{
			p.Add(x => x.Colors, Palette)
				.Add(x => x.SelectedColor, "#42a5f5")
				.Add(x => x.AllowCustom, true)
				.Add(x => x.CustomLook, custom)
				.Add(x => x.SelectedColorChanged, color => _changes.Add(color));
			more?.Invoke(p);
		});

	[Fact]
	public void Rainbow_IsAnIconButton_NamedInATooltip_ThatOpensADialog()
	{
		IRenderedComponent<MokaColorSwatch> cut = Swatch(MokaSwatchCustomLook.Rainbow);

		IElement rainbow = cut.Find("button.moka-color-swatch-rainbow");
		IconTooltip.AssertNamed(rainbow, "Custom color", MokaTooltipPosition.Bottom);
		Assert.Equal("dialog", rainbow.GetAttribute("aria-haspopup"));
		Assert.Equal("false", rainbow.GetAttribute("aria-expanded"));
		Assert.False(rainbow.HasAttribute("aria-controls"));
		Assert.Contains("moka-color-swatch-item--circle", rainbow.ClassList);
		Assert.Empty(cut.FindAll(".moka-color-swatch-add, .moka-color-swatch-panel"));
	}

	[Fact]
	public void Pill_SaysCustom_SoItNeedsNoTooltip()
	{
		IRenderedComponent<MokaColorSwatch> cut = Swatch(MokaSwatchCustomLook.Pill);

		IElement pill = cut.Find("button.moka-color-swatch-pill");
		Assert.Equal("Custom", pill.TextContent.Trim());
		Assert.Null(pill.Closest(".moka-tooltip-trigger"));
		Assert.Equal("true", pill.QuerySelector(".moka-color-swatch-pill-dot")!.GetAttribute("aria-hidden"));
		Assert.Contains("moka-color-swatch-grid--flow", cut.Find(".moka-color-swatch-grid").ClassList);
	}

	[Theory]
	[InlineData(MokaSwatchCustomLook.Rainbow, "button.moka-color-swatch-rainbow")]
	[InlineData(MokaSwatchCustomLook.Pill, "button.moka-color-swatch-pill")]
	public async Task TheOpener_OpensAPanel_InTheTopLayer_AgainstItsWrapper(MokaSwatchCustomLook custom, string opener)
	{
		IRenderedComponent<MokaColorSwatch> cut = Swatch(custom);
		string? anchorId = cut.Find(".moka-color-swatch-anchor").GetAttribute("blazor:elementReference");

		await cut.Find(opener).ClickAsync(new MouseEventArgs());

		IElement panel = cut.Find(".moka-color-swatch-panel");
		Assert.Equal("dialog", panel.GetAttribute("role"));
		Assert.Equal("Custom color", panel.GetAttribute("aria-label"));
		Assert.Equal(panel.Id, cut.Find(opener).GetAttribute("aria-controls"));
		Assert.Equal("true", cut.Find(opener).GetAttribute("aria-expanded"));
		TopLayerPopup.AssertOpened(_popup, panel, anchorId, "bottom-start");

		Assert.Equal("Pick a color", panel.QuerySelector("input[type=color]")!.GetAttribute("aria-label"));
		Assert.Equal("#42a5f5", panel.QuerySelector("input[type=color]")!.GetAttribute("value"));
		IElement hex = panel.QuerySelector(".moka-color-swatch-hex-input")!;
		Assert.Equal("Hex", hex.GetAttribute("aria-label"));
		Assert.Equal("#42A5F5", hex.GetAttribute("value"));
		Assert.Equal(["Cancel", "Add"], panel.QuerySelectorAll(".moka-color-swatch-panel-actions button").Select(b => b.TextContent.Trim()));

		JSRuntimeInvocation focus = _swatch.Invocations["focusHexField"][^1];
		Assert.Equal(hex.GetAttribute("blazor:elementReference"), Assert.IsType<ElementReference>(focus.Arguments[0]).Id);
	}

	[Fact]
	public async Task ThePanel_AddsTheHexTyped_AsASwatchOfItsOwn()
	{
		IRenderedComponent<MokaColorSwatch> cut = Swatch(MokaSwatchCustomLook.Rainbow);
		await cut.Find("button.moka-color-swatch-rainbow").ClickAsync(new MouseEventArgs());

		await cut.Find(".moka-color-swatch-panel .moka-color-swatch-hex-input").InputAsync(new ChangeEventArgs { Value = "AB12CD" });
		Assert.Equal("--_swatch-try: #ab12cd", cut.Find(".moka-color-swatch-panel .moka-color-swatch-hex-dot").GetAttribute("style"));
		Assert.Equal("#ab12cd", cut.Find(".moka-color-swatch-panel input[type=color]").GetAttribute("value"));

		await cut.Find(".moka-color-swatch-panel .moka-color-swatch-hex-input").KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

		Assert.Equal(["#ab12cd"], _changes);
		Assert.Equal(["#ab12cd"], Checked(cut));
		Assert.Contains("moka-color-swatch-item--custom", cut.FindAll(".moka-color-swatch-item")[3].ClassList);
		Assert.Empty(cut.FindAll(".moka-color-swatch-panel"));
		TopLayerPopup.AssertClosed(_popup);
		Assert.Equal("false", cut.Find("button.moka-color-swatch-rainbow").GetAttribute("aria-expanded"));
		JSInterop.VerifyFocusAsyncInvoke();
	}

	[Fact]
	public async Task TheBrowsersColorButton_WritesTheHex_AndAddTakesIt()
	{
		IRenderedComponent<MokaColorSwatch> cut = Swatch(MokaSwatchCustomLook.Pill);
		await cut.Find("button.moka-color-swatch-pill").ClickAsync(new MouseEventArgs());

		await cut.Find(".moka-color-swatch-panel input[type=color]").InputAsync(new ChangeEventArgs { Value = "#123456" });
		Assert.Equal("#123456", cut.Find(".moka-color-swatch-panel .moka-color-swatch-hex-input").GetAttribute("value"));

		await cut.FindAll(".moka-color-swatch-panel-actions button")[1].ClickAsync(new MouseEventArgs());

		Assert.Equal(["#123456"], _changes);
		Assert.Equal(["#123456"], Checked(cut));
	}

	[Fact]
	public async Task AColorThePaletteHas_PicksThatSwatch()
	{
		IRenderedComponent<MokaColorSwatch> cut = Swatch(MokaSwatchCustomLook.Pill);
		await cut.Find("button.moka-color-swatch-pill").ClickAsync(new MouseEventArgs());
		await cut.Find(".moka-color-swatch-panel .moka-color-swatch-hex-input").InputAsync(new ChangeEventArgs { Value = "#66BB6A" });
		await cut.FindAll(".moka-color-swatch-panel-actions button")[1].ClickAsync(new MouseEventArgs());

		Assert.Equal(3, cut.FindAll("input.moka-color-swatch-input").Count);
		Assert.Equal(["#66bb6a"], Checked(cut));
	}

	[Fact]
	public async Task AddWithNoColor_MarksTheHexField_AndKeepsThePanelOpen()
	{
		IRenderedComponent<MokaColorSwatch> cut = Swatch(MokaSwatchCustomLook.Rainbow);
		await cut.Find("button.moka-color-swatch-rainbow").ClickAsync(new MouseEventArgs());
		await cut.Find(".moka-color-swatch-panel .moka-color-swatch-hex-input").InputAsync(new ChangeEventArgs { Value = "#12345" });

		await cut.FindAll(".moka-color-swatch-panel-actions button")[1].ClickAsync(new MouseEventArgs());

		IElement hex = cut.Find(".moka-color-swatch-panel .moka-color-swatch-hex-input");
		Assert.Equal("true", hex.GetAttribute("aria-invalid"));
		Assert.Equal("Six hex digits", cut.Find($"#{hex.GetAttribute("aria-describedby")}").TextContent);
		Assert.Contains("moka-color-swatch-hex--invalid", hex.ParentElement!.ClassList);
		Assert.Empty(_changes);
		Assert.Single(cut.FindAll(".moka-color-swatch-panel"));

		await hex.InputAsync(new ChangeEventArgs { Value = "#123456" });

		Assert.False(cut.Find(".moka-color-swatch-panel .moka-color-swatch-hex-input").HasAttribute("aria-invalid"));
	}

	[Theory]
	[InlineData("Escape")]
	[InlineData("Cancel")]
	public async Task EscapeOrCancel_Close_WithNoChange_AndFocusGoesBack(string how)
	{
		IRenderedComponent<MokaColorSwatch> cut = Swatch(MokaSwatchCustomLook.Pill);
		await cut.Find("button.moka-color-swatch-pill").ClickAsync(new MouseEventArgs());
		await cut.Find(".moka-color-swatch-panel .moka-color-swatch-hex-input").InputAsync(new ChangeEventArgs { Value = "#123456" });

		if (how == "Escape")
		{
			await cut.Find(".moka-color-swatch-panel").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });
		}
		else
		{
			await cut.FindAll(".moka-color-swatch-panel-actions button")[0].ClickAsync(new MouseEventArgs());
		}

		Assert.Empty(cut.FindAll(".moka-color-swatch-panel"));
		Assert.Empty(_changes);
		Assert.Equal(["#42a5f5"], Checked(cut));
		TopLayerPopup.AssertClosed(_popup);
		JSInterop.VerifyFocusAsyncInvoke();
	}

	// Escape in the panel must not also close a MokaDialog around the swatch.
	[Fact]
	public async Task ThePanelsKeys_StopAtThePanel()
	{
		IRenderedComponent<MokaColorSwatch> cut = Swatch(MokaSwatchCustomLook.Rainbow);
		await cut.Find("button.moka-color-swatch-rainbow").ClickAsync(new MouseEventArgs());

		Assert.True(cut.Find(".moka-color-swatch-panel").HasAttribute("blazor:onkeydown:stopPropagation"));
	}

	[Fact]
	public async Task TheBackdrop_ClosesThePanel()
	{
		IRenderedComponent<MokaColorSwatch> cut = Swatch(MokaSwatchCustomLook.Rainbow);
		await cut.Find("button.moka-color-swatch-rainbow").ClickAsync(new MouseEventArgs());

		await cut.Find(".moka-color-swatch-backdrop").ClickAsync(new MouseEventArgs());

		Assert.Empty(cut.FindAll(".moka-color-swatch-panel"));
		Assert.Empty(_changes);
	}

	[Fact]
	public void Disabled_DisablesTheOpeners()
	{
		Assert.True(Swatch(MokaSwatchCustomLook.Rainbow, p => p.Add(x => x.Disabled, true))
			.Find("button.moka-color-swatch-rainbow").HasAttribute("disabled"));
		Assert.True(Swatch(MokaSwatchCustomLook.Pill, p => p.Add(x => x.Disabled, true))
			.Find("button.moka-color-swatch-pill").HasAttribute("disabled"));
		Assert.True(Swatch(MokaSwatchCustomLook.Hex, p => p.Add(x => x.Disabled, true))
			.Find(".moka-color-swatch-hex-input").HasAttribute("disabled"));
	}

	[Fact]
	public void Hex_IsAFieldAfterTheSwatches_BoundToWhatIsTyped()
	{
		IRenderedComponent<MokaColorSwatch> cut = Swatch(MokaSwatchCustomLook.Hex);

		IElement field = cut.Find(".moka-color-swatch-grid > .moka-color-swatch-hex .moka-color-swatch-hex-input");
		Assert.Equal("Custom color, as hex", field.GetAttribute("aria-label"));
		Assert.Equal("#hex", field.GetAttribute("placeholder"));
		Assert.Equal("7", field.GetAttribute("maxlength"));
		Assert.Equal("off", field.GetAttribute("autocomplete"));
		Assert.Equal("false", field.GetAttribute("spellcheck"));
		Assert.True(field.HasAttribute("blazor:oninput"));
		Assert.False(field.HasAttribute("aria-invalid"));

		JSRuntimeInvocation bound = Assert.Single(_swatch.Invocations["bindHexField"]);
		Assert.Equal(field.GetAttribute("blazor:elementReference"), Assert.IsType<ElementReference>(bound.Arguments[0]).Id);
	}

	[Theory]
	[InlineData("123abc", "#123abc")]
	[InlineData("#123ABC", "#123abc")]
	[InlineData(" #a1B2c3 ", "#a1b2c3")]
	public async Task Hex_EnterAddsSixDigits_WithOrWithoutTheHash(string typed, string expected)
	{
		IRenderedComponent<MokaColorSwatch> cut = Swatch(MokaSwatchCustomLook.Hex);

		await cut.Find(".moka-color-swatch-hex-input").InputAsync(new ChangeEventArgs { Value = typed });
		Assert.Equal($"--_swatch-try: {expected}", cut.Find(".moka-color-swatch-hex-dot").GetAttribute("style"));
		await cut.Find(".moka-color-swatch-hex-input").KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

		Assert.Equal([expected], _changes);
		Assert.Equal([expected], Checked(cut));
		Assert.True(string.IsNullOrEmpty(cut.Find(".moka-color-swatch-hex-input").GetAttribute("value")));
		Assert.False(cut.Find(".moka-color-swatch-hex-dot").HasAttribute("style"));
	}

	[Theory]
	[InlineData("12345")]
	[InlineData("#1234567")]
	[InlineData("red")]
	[InlineData("#ggg000")]
	public async Task Hex_AnythingElse_IsMarkedInvalid_AndAddsNothing(string typed)
	{
		IRenderedComponent<MokaColorSwatch> cut = Swatch(MokaSwatchCustomLook.Hex);

		await cut.Find(".moka-color-swatch-hex-input").InputAsync(new ChangeEventArgs { Value = typed });
		await cut.Find(".moka-color-swatch-hex-input").KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

		IElement field = cut.Find(".moka-color-swatch-hex-input");
		Assert.Empty(_changes);
		Assert.Equal("true", field.GetAttribute("aria-invalid"));
		Assert.Equal("Six hex digits", cut.Find($"#{field.GetAttribute("aria-describedby")}").TextContent);
		Assert.Equal(typed, field.GetAttribute("value"));
	}

	[Fact]
	public async Task Hex_EnterOnAnEmptyField_DoesNothing()
	{
		IRenderedComponent<MokaColorSwatch> cut = Swatch(MokaSwatchCustomLook.Hex);

		await cut.Find(".moka-color-swatch-hex-input").KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

		Assert.Empty(_changes);
		Assert.False(cut.Find(".moka-color-swatch-hex-input").HasAttribute("aria-invalid"));
	}

	[Fact]
	public void Hex_TakesItsWordsFromText()
	{
		IRenderedComponent<MokaColorSwatch> cut = Swatch(MokaSwatchCustomLook.Hex, p => p.Add(x => x.Text, new MokaColorSwatchText
		{
			HexField = "Couleur, en hexa",
			HexPlaceholder = "#rvb"
		}));

		IElement field = cut.Find(".moka-color-swatch-hex-input");
		Assert.Equal("Couleur, en hexa", field.GetAttribute("aria-label"));
		Assert.Equal("#rvb", field.GetAttribute("placeholder"));
	}

	[Fact]
	public async Task Disposing_ClosesAnOpenPanel()
	{
		IRenderedComponent<MokaColorSwatch> cut = Swatch(MokaSwatchCustomLook.Rainbow);
		await cut.Find("button.moka-color-swatch-rainbow").ClickAsync(new MouseEventArgs());
		int closes = TopLayerPopup.Count(_popup, "close");

		await DisposeComponentsAsync();

		Assert.Equal(closes + 1, TopLayerPopup.Count(_popup, "close"));
	}

	// The default look's + keeps its inline color input, and loads no script for it.
	[Fact]
	public async Task TheDefaultEntry_LoadsNoScript()
	{
		IRenderedComponent<MokaColorSwatch> cut = Swatch(MokaSwatchCustomLook.Standard);
		await cut.Find(".moka-color-swatch-add").ClickAsync(new MouseEventArgs());

		Assert.Single(cut.FindAll(".moka-color-swatch-custom input[type=color]"));
		Assert.Empty(_swatch.Invocations);
		Assert.Empty(_popup.Invocations);
	}
}
