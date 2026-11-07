using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Forms.AutoComplete;
using Moka.Red.Forms.Cascader;
using Moka.Red.Forms.ColorPicker;
using Moka.Red.Forms.DatePicker;
using Moka.Red.Forms.DateRangePicker;
using Moka.Red.Forms.SelectField;
using Moka.Red.Forms.SettingsForm;
using Moka.Red.Forms.TagInput;
using Moka.Red.Forms.TimePicker;
using Moka.Red.Forms.TreeSelect;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Forms.Tests.Components;

// A picker's panel drawn inside the field's wrapper was cut off by a card's overflow and painted under any later
// element with a stacking context of its own. The panels now go to the top layer through Core's moka-popup.js;
// what that does in the browser can't run here, so these tests cover what the components render for it and when
// they call it.
public class TopLayerPanelTests : BunitContext
{
	private static readonly string[] Colours = ["Red", "Green", "Blue"];
	private static readonly string[] Cities = ["Berlin", "Bern", "Bergen", "Oslo"];

	private static readonly IReadOnlyList<MokaCascaderOption> Places =
	[
		new MokaCascaderOption { Value = "europe", Label = "Europe", Children = [new MokaCascaderOption { Value = "oslo", Label = "Oslo" }] }
	];

	private static readonly MokaTreeSelectItem<string>[] Tree = [new("a", "Alpha"), new("b", "Beta")];

	private static readonly IReadOnlyList<MokaSettingsSection> Sections = [new("general", "General")];

	private static readonly IReadOnlyList<MokaSettingsEntry> Entries =
		[new("Time zone", "clock region", "time-zone") { Section = "General" }];

	private readonly BunitJSModuleInterop _popup;

	public TopLayerPanelTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_popup = TopLayerPopup.Setup(JSInterop);
	}

	[Fact]
	public async Task Select_OpensItsListAgainstTheField_WithTheBackdropUnderIt_AndClosesIt()
	{
		IRenderedComponent<MokaSelect<string>> cut = Render<MokaSelect<string>>(p => p.Add(x => x.Items, Colours));
		string? anchorId = AnchorId(cut, ".moka-select");

		await cut.Find(".moka-select-trigger").ClickAsync(new MouseEventArgs());

		IReadOnlyDictionary<string, object?> options =
			TopLayerPopup.AssertOpened(_popup, cut.Find(".moka-select-dropdown"), anchorId, "bottom-start");
		Assert.Equal("min", options["width"]);
		AssertBackdrop(cut.Find(".moka-select-backdrop"));

		await cut.Find(".moka-select-backdrop").ClickAsync(new MouseEventArgs());

		Assert.Empty(cut.FindAll(".moka-select-dropdown"));
		TopLayerPopup.AssertClosed(_popup);
	}

	[Fact]
	public async Task Select_ClosesItsList_WhenDisposedOpen()
	{
		IRenderedComponent<MokaSelect<string>> cut = Render<MokaSelect<string>>(p => p.Add(x => x.Items, Colours));
		await cut.Find(".moka-select-trigger").ClickAsync(new MouseEventArgs());

		await DisposeComponentsAsync();

		TopLayerPopup.AssertClosed(_popup);
	}

	[Fact]
	public void Select_ThatNeverOpens_CallsNothing()
	{
		IRenderedComponent<MokaSelect<string>> cut = Render<MokaSelect<string>>(p => p.Add(x => x.Items, Colours));
		cut.Render(p => p.Add(x => x.Placeholder, "Pick one"));

		Assert.Equal(0, TopLayerPopup.Count(_popup, "open"));
		Assert.Equal(0, TopLayerPopup.Count(_popup, "close"));
	}

	[Fact]
	public async Task AutoComplete_OpensItsSuggestionsAsWideAsTheField_AndClosesThem()
	{
		IRenderedComponent<MokaAutoComplete<string>> cut = Render<MokaAutoComplete<string>>(p => p
			.Add(x => x.Debounce, 0)
			.Add(x => x.SearchFunc, query => Task.FromResult(
				Cities.Where(c => c.StartsWith(query, StringComparison.OrdinalIgnoreCase)))));
		string? anchorId = AnchorId(cut, ".moka-autocomplete");

		await cut.Find("input[role=combobox]").InputAsync(new ChangeEventArgs { Value = "Ber" });
		cut.WaitForAssertion(() => Assert.Equal(3, cut.FindAll("[role=option]").Count), TimeSpan.FromSeconds(10));

		Assert.Equal(1, TopLayerPopup.Count(_popup, "open"));
		JSRuntimeInvocation open = _popup.Invocations["open"][^1];
		Assert.Equal(anchorId, Assert.IsType<ElementReference>(open.Arguments[2]).Id);
		IElement panel = cut.Find(".moka-autocomplete-dropdown");
		Assert.Equal("manual", panel.GetAttribute("popover"));
		IReadOnlyDictionary<string, object?> options = Options();
		Assert.Equal("bottom-start", options["placement"]);
		Assert.Equal("match", options["width"]);

		await cut.Find(".moka-autocomplete-backdrop").ClickAsync(new MouseEventArgs());

		TopLayerPopup.AssertClosed(_popup);
	}

	[Fact]
	public async Task DatePicker_OpensItsCalendarAgainstTheField_AndClosesIt()
	{
		IRenderedComponent<MokaDatePicker> cut = Render<MokaDatePicker>();
		string? anchorId = AnchorId(cut, ".moka-datepicker");

		await cut.Find("input").ClickAsync(new MouseEventArgs());

		IReadOnlyDictionary<string, object?> options =
			TopLayerPopup.AssertOpened(_popup, cut.Find(".moka-datepicker-dropdown"), anchorId, "bottom-start");
		Assert.False(options.ContainsKey("width"));
		AssertBackdrop(cut.Find(".moka-datepicker-backdrop"));

		await cut.Find(".moka-datepicker-backdrop").ClickAsync(new MouseEventArgs());

		TopLayerPopup.AssertClosed(_popup);
	}

	[Fact]
	public async Task DatePicker_ClosesItsCalendar_WhenDisposedOpen()
	{
		IRenderedComponent<MokaDatePicker> cut = Render<MokaDatePicker>();
		await cut.Find("input").ClickAsync(new MouseEventArgs());

		await DisposeComponentsAsync();

		TopLayerPopup.AssertClosed(_popup);
	}

	[Fact]
	public async Task DateRangePicker_OpensItsCalendarsAgainstTheField_AndClosesThem()
	{
		IRenderedComponent<MokaDateRangePicker> cut = Render<MokaDateRangePicker>();
		string? anchorId = AnchorId(cut, ".moka-daterange");

		await cut.Find("input").ClickAsync(new MouseEventArgs());

		TopLayerPopup.AssertOpened(_popup, cut.Find(".moka-daterange__popup"), anchorId, "bottom-start");
		AssertBackdrop(cut.Find(".moka-daterange__backdrop"));

		await cut.Find(".moka-daterange__backdrop").ClickAsync(new MouseEventArgs());

		TopLayerPopup.AssertClosed(_popup);
	}

	[Fact]
	public async Task DateRangePicker_ClosesItsCalendars_WhenDisposedOpen()
	{
		IRenderedComponent<MokaDateRangePicker> cut = Render<MokaDateRangePicker>();
		await cut.Find("input").ClickAsync(new MouseEventArgs());

		await DisposeComponentsAsync();

		TopLayerPopup.AssertClosed(_popup);
	}

	[Fact]
	public async Task TimePicker_OpensItsColumnsAgainstTheField_AndClosesThem()
	{
		IRenderedComponent<MokaTimePicker> cut = Render<MokaTimePicker>();
		string? anchorId = AnchorId(cut, ".moka-timepicker");

		await cut.Find(".moka-timepicker-toggle").ClickAsync(new MouseEventArgs());

		TopLayerPopup.AssertOpened(_popup, cut.Find(".moka-timepicker-dropdown"), anchorId, "bottom-start");
		AssertBackdrop(cut.Find(".moka-timepicker-backdrop"));

		await cut.Find(".moka-timepicker-backdrop").ClickAsync(new MouseEventArgs());

		TopLayerPopup.AssertClosed(_popup);
	}

	[Fact]
	public async Task TimePicker_ClosesItsColumns_WhenDisposedOpen()
	{
		IRenderedComponent<MokaTimePicker> cut = Render<MokaTimePicker>();
		await cut.Find(".moka-timepicker-toggle").ClickAsync(new MouseEventArgs());

		await DisposeComponentsAsync();

		TopLayerPopup.AssertClosed(_popup);
	}

	[Fact]
	public async Task ColorPicker_OpensItsPickerAgainstTheField_AndClosesIt()
	{
		IRenderedComponent<MokaColorPicker> cut = Render<MokaColorPicker>();
		string? anchorId = AnchorId(cut, ".moka-colorpicker");

		await cut.Find(".moka-colorpicker-swatch-button").ClickAsync(new MouseEventArgs());

		TopLayerPopup.AssertOpened(_popup, cut.Find(".moka-colorpicker-dropdown"), anchorId, "bottom-start");
		AssertBackdrop(cut.Find(".moka-colorpicker-backdrop"));

		await cut.Find(".moka-colorpicker-backdrop").ClickAsync(new MouseEventArgs());

		TopLayerPopup.AssertClosed(_popup);
	}

	[Fact]
	public async Task ColorPicker_ClosesItsPicker_WhenDisposedOpen()
	{
		IRenderedComponent<MokaColorPicker> cut = Render<MokaColorPicker>();
		await cut.Find(".moka-colorpicker-swatch-button").ClickAsync(new MouseEventArgs());

		await DisposeComponentsAsync();

		TopLayerPopup.AssertClosed(_popup);
	}

	[Fact]
	public async Task Cascader_OpensItsPanelAtLeastAsWideAsTheField_AndClosesIt()
	{
		IRenderedComponent<MokaCascader> cut = Render<MokaCascader>(p => p.Add(x => x.Options, Places));
		string? anchorId = AnchorId(cut, ".moka-cascader");

		await cut.Find(".moka-cascader-trigger").ClickAsync(new MouseEventArgs());

		IReadOnlyDictionary<string, object?> options =
			TopLayerPopup.AssertOpened(_popup, cut.Find(".moka-cascader-panel"), anchorId, "bottom-start");
		Assert.Equal("min", options["width"]);
		AssertBackdrop(cut.Find(".moka-cascader-backdrop"));

		await cut.Find(".moka-cascader-backdrop").ClickAsync(new MouseEventArgs());

		TopLayerPopup.AssertClosed(_popup);
	}

	[Fact]
	public async Task Cascader_ClosesItsPanel_WhenDisposedOpen()
	{
		IRenderedComponent<MokaCascader> cut = Render<MokaCascader>(p => p.Add(x => x.Options, Places));
		await cut.Find(".moka-cascader-trigger").ClickAsync(new MouseEventArgs());

		await DisposeComponentsAsync();

		TopLayerPopup.AssertClosed(_popup);
	}

	[Fact]
	public async Task TreeSelect_OpensItsTreeAsWideAsTheField_AndClosesIt()
	{
		IRenderedComponent<MokaTreeSelect<string>> cut = Render<MokaTreeSelect<string>>(p => p.Add(x => x.Items, Tree));
		string? anchorId = AnchorId(cut, ".moka-tree-select");

		await cut.Find("button.moka-tree-select-trigger").ClickAsync(new MouseEventArgs());

		IReadOnlyDictionary<string, object?> options =
			TopLayerPopup.AssertOpened(_popup, cut.Find(".moka-tree-select-dropdown"), anchorId, "bottom-start");
		Assert.Equal("match", options["width"]);
		AssertBackdrop(cut.Find(".moka-tree-select-backdrop"));

		await cut.Find(".moka-tree-select-backdrop").ClickAsync(new MouseEventArgs());

		TopLayerPopup.AssertClosed(_popup);
	}

	[Fact]
	public async Task TreeSelect_ClosesItsTree_WhenDisposedOpen()
	{
		IRenderedComponent<MokaTreeSelect<string>> cut = Render<MokaTreeSelect<string>>(p => p.Add(x => x.Items, Tree));
		await cut.Find("button.moka-tree-select-trigger").ClickAsync(new MouseEventArgs());

		await DisposeComponentsAsync();

		TopLayerPopup.AssertClosed(_popup);
	}

	[Fact]
	public async Task TagInput_OpensItsSuggestionsAsWideAsTheField_WithNoBackdrop_AndClosesThem()
	{
		IRenderedComponent<MokaTagInput> cut = Render<MokaTagInput>(p => p.Add(x => x.Suggestions, Cities));
		string? anchorId = AnchorId(cut, ".moka-taginput");

		await cut.Find("input").InputAsync(new ChangeEventArgs { Value = "Be" });

		IReadOnlyDictionary<string, object?> options =
			TopLayerPopup.AssertOpened(_popup, cut.Find(".moka-taginput-suggestions"), anchorId, "bottom-start");
		Assert.Equal("match", options["width"]);
		Assert.Null(_popup.Invocations["open"][^1].Arguments[3]);

		await cut.Find("input").InputAsync(new ChangeEventArgs { Value = "" });

		Assert.Empty(cut.FindAll(".moka-taginput-suggestions"));
		TopLayerPopup.AssertClosed(_popup);
	}

	[Fact]
	public async Task TagInput_ClosesItsSuggestions_WhenDisposedOpen()
	{
		IRenderedComponent<MokaTagInput> cut = Render<MokaTagInput>(p => p.Add(x => x.Suggestions, Cities));
		await cut.Find("input").InputAsync(new ChangeEventArgs { Value = "Be" });

		await DisposeComponentsAsync();

		TopLayerPopup.AssertClosed(_popup);
	}

	[Fact]
	public void TagInput_WithNoMatchingSuggestion_CallsNothing()
	{
		IRenderedComponent<MokaTagInput> cut = Render<MokaTagInput>(p => p.Add(x => x.Suggestions, Cities));

		cut.Find("input").Input("zz");

		Assert.Equal(0, TopLayerPopup.Count(_popup, "open"));
	}

	[Fact]
	public void SettingsLayout_OpensItsResultsAsWideAsTheSearchBox_AndClosesThem()
	{
		IRenderedComponent<MokaSettingsLayout> cut = Render<MokaSettingsLayout>(p => p
			.Add(x => x.Sections, Sections)
			.Add(x => x.Active, "general")
			.Add(x => x.Entries, Entries));
		string? anchorId = AnchorId(cut, ".moka-settings-layout__search");

		cut.Find("input").Focus();
		cut.Find("input").Input("time");

		IReadOnlyDictionary<string, object?> options =
			TopLayerPopup.AssertOpened(_popup, cut.Find(".moka-settings-layout__results"), anchorId, "bottom-start");
		Assert.Equal("match", options["width"]);
		Assert.Null(_popup.Invocations["open"][^1].Arguments[3]);

		cut.Find("input").Input("");

		Assert.Empty(cut.FindAll(".moka-settings-layout__results"));
		TopLayerPopup.AssertClosed(_popup);
	}

	[Fact]
	public async Task SettingsLayout_ClosesItsResults_WhenDisposedOpen()
	{
		IRenderedComponent<MokaSettingsLayout> cut = Render<MokaSettingsLayout>(p => p
			.Add(x => x.Sections, Sections)
			.Add(x => x.Active, "general")
			.Add(x => x.Entries, Entries));
		await cut.Find("input").FocusAsync(new FocusEventArgs());
		await cut.Find("input").InputAsync(new ChangeEventArgs { Value = "time" });

		await DisposeComponentsAsync();

		TopLayerPopup.AssertClosed(_popup);
	}

	private static string? AnchorId<TComponent>(IRenderedComponent<TComponent> cut, string selector)
		where TComponent : IComponent =>
		cut.Find(selector).GetAttribute("blazor:elementReference");

	private void AssertBackdrop(IElement backdrop)
	{
		Assert.Equal("manual", backdrop.GetAttribute("popover"));
		Assert.True(backdrop.HasAttribute("data-moka-popup"));
		Assert.Equal(backdrop.GetAttribute("blazor:elementReference"),
			Assert.IsType<ElementReference>(_popup.Invocations["open"][^1].Arguments[3]).Id);
	}

	private IReadOnlyDictionary<string, object?> Options() =>
		Assert.IsAssignableFrom<IReadOnlyDictionary<string, object?>>(_popup.Invocations["open"][^1].Arguments[4]);
}
