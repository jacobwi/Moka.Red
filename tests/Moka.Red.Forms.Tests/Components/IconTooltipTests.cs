using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Feedback.Tooltip;
using Moka.Red.Forms.AutoComplete;
using Moka.Red.Forms.Calendar;
using Moka.Red.Forms.ColorPicker;
using Moka.Red.Forms.DatePicker;
using Moka.Red.Forms.DateRangePicker;
using Moka.Red.Forms.FileUpload;
using Moka.Red.Forms.FormBuilder;
using Moka.Red.Forms.PasswordField;
using Moka.Red.Forms.SearchInput;
using Moka.Red.Forms.SelectField;
using Moka.Red.Forms.TagInput;
using Moka.Red.Forms.TextField;
using Moka.Red.Forms.TimePicker;
using Moka.Red.Forms.TreeSelect;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Forms.Tests.Components;

// Every control here shows only an icon, an arrow glyph or a colour. Those that had a title showed the browser's own
// pale, late hint instead of Moka's; the rest had no hover name, and some had no name at all. Each is named by
// aria-label and by a MokaTooltip on the side its place calls for.
public class IconTooltipTests : BunitContext
{
	private static readonly string[] Fruits = ["Apple", "Banana", "Cherry"];
	private static readonly string[] Swatches = ["#ef5350", "#42a5f5"];

	public IconTooltipTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	// A clear button sits at the right end of its input, so its name opens below and lines up with that end.
	[Fact]
	public void TextField_NamesItsClearButton()
	{
		IRenderedComponent<MokaTextField> cut = Render<MokaTextField>(p => p
			.Add(x => x.Value, "draft")
			.Add(x => x.Clearable, true));

		IconTooltip.AssertNamed(cut.Find(".moka-textfield-clear"), "Clear", MokaTooltipPosition.BottomEnd);
	}

	[Fact]
	public void SearchInput_NamesItsClearButton()
	{
		IRenderedComponent<MokaSearchInput> cut = Render<MokaSearchInput>(p => p.Add(x => x.Value, "moka"));

		IconTooltip.AssertNamed(cut.Find(".moka-search-clear"), "Clear search", MokaTooltipPosition.BottomEnd);
	}

	[Fact]
	public async Task PasswordField_NamesItsToggleByWhatItDoesNext()
	{
		IRenderedComponent<MokaPasswordField> cut = Render<MokaPasswordField>(p => p.Add(x => x.Value, "secret"));

		IconTooltip.AssertNamed(cut.Find(".moka-passwordfield-toggle"), "Show password", MokaTooltipPosition.BottomEnd);

		await cut.Find(".moka-passwordfield-toggle").ClickAsync(new MouseEventArgs());

		IconTooltip.AssertNamed(cut.Find(".moka-passwordfield-toggle"), "Hide password", MokaTooltipPosition.BottomEnd);
	}

	[Fact]
	public void AutoComplete_NamesItsClearButton()
	{
		IRenderedComponent<MokaAutoComplete<string>> cut = Render<MokaAutoComplete<string>>(p => p
			.Add(x => x.Value, "Paris")
			.Add(x => x.SearchFunc, _ => Task.FromResult(Enumerable.Empty<string>())));

		IconTooltip.AssertNamed(cut.Find(".moka-autocomplete-clear"), "Clear", MokaTooltipPosition.BottomEnd);
	}

	// The clear button and the chips' remove buttons sit above the list, so they show no tooltip while it is open;
	// their names stay.
	[Fact]
	public async Task Select_NamesItsClearAndRemoveButtons_UntilTheListOpens()
	{
		IRenderedComponent<MokaSelect<string>> cut = Render<MokaSelect<string>>(p => p
			.Add(x => x.Items, Fruits)
			.Add(x => x.Multiple, true)
			.Add(x => x.Clearable, true)
			.Add(x => x.SelectedValues, new List<string> { "Apple" }));

		IconTooltip.AssertNamed(cut.Find(".moka-select-clear"), "Clear selection", MokaTooltipPosition.Bottom);
		IconTooltip.AssertNamed(cut.Find(".moka-select-chip-remove"), "Remove Apple", MokaTooltipPosition.Bottom);

		await cut.Find("[role=combobox]").ClickAsync(new MouseEventArgs());

		Assert.Equal("Clear selection", cut.Find(".moka-select-clear").GetAttribute("aria-label"));
		Assert.Null(cut.Find(".moka-select-clear").ParentElement!.QuerySelector(":scope > .moka-tooltip-popup"));
		Assert.Null(cut.Find(".moka-select-chip-remove").ParentElement!.QuerySelector(":scope > .moka-tooltip-popup"));
	}

	[Fact]
	public void TagInput_NamesItsRemoveAndClearButtons()
	{
		IRenderedComponent<MokaTagInput> cut = Render<MokaTagInput>(p => p
			.Add(x => x.Values, new List<string> { "blazor" }));

		IconTooltip.AssertNamed(cut.Find(".moka-taginput-tag-dismiss"), "Remove blazor", MokaTooltipPosition.Bottom);
		IconTooltip.AssertNamed(cut.Find(".moka-taginput-clear"), "Clear all tags", MokaTooltipPosition.BottomEnd);
	}

	// The arrows end the header row, so their names open below and line up with its edges.
	[Fact]
	public void Calendar_NamesItsMonthArrows()
	{
		IRenderedComponent<MokaCalendar> cut = Render<MokaCalendar>();

		IReadOnlyList<IElement> arrows = cut.FindAll(".moka-calendar__nav");
		IconTooltip.AssertNamed(arrows[0], "Previous month", MokaTooltipPosition.BottomStart);
		IconTooltip.AssertNamed(arrows[1], "Next month", MokaTooltipPosition.BottomEnd);
	}

	// The calendar opens where the toggle's tooltip would, so the toggle shows none while it is open. The month
	// arrows inside were bare "<" and ">" with no name at all.
	[Fact]
	public async Task DatePicker_NamesItsToggleAndMonthArrows()
	{
		IRenderedComponent<MokaDatePicker> cut = Render<MokaDatePicker>();

		IconTooltip.AssertNamed(cut.Find(".moka-datepicker-toggle"), "Choose date", MokaTooltipPosition.BottomEnd);

		await cut.Find(".moka-datepicker-toggle").ClickAsync(new MouseEventArgs());

		Assert.Null(cut.Find(".moka-datepicker-toggle").ParentElement!.QuerySelector(":scope > .moka-tooltip-popup"));
		IconTooltip.AssertNamed(cut.Find("button[aria-label='Previous month']"), "Previous month", MokaTooltipPosition.BottomStart);
		IconTooltip.AssertNamed(cut.Find("button[aria-label='Next month']"), "Next month", MokaTooltipPosition.BottomEnd);
	}

	[Fact]
	public async Task DateRangePicker_NamesItsMonthArrows()
	{
		IRenderedComponent<MokaDateRangePicker> cut = Render<MokaDateRangePicker>();

		await cut.Find("input").ClickAsync(new MouseEventArgs());

		IReadOnlyList<IElement> arrows = cut.FindAll(".moka-daterange__nav");
		IconTooltip.AssertNamed(arrows[0], "Previous month", MokaTooltipPosition.BottomStart);
		IconTooltip.AssertNamed(arrows[1], "Next month", MokaTooltipPosition.BottomEnd);
	}

	[Fact]
	public async Task TimePicker_NamesItsToggleUntilThePickerOpens()
	{
		IRenderedComponent<MokaTimePicker> cut = Render<MokaTimePicker>();

		IconTooltip.AssertNamed(cut.Find(".moka-timepicker-toggle"), "Choose time", MokaTooltipPosition.BottomEnd);

		await cut.Find(".moka-timepicker-toggle").ClickAsync(new MouseEventArgs());

		Assert.Null(cut.Find(".moka-timepicker-toggle").ParentElement!.QuerySelector(":scope > .moka-tooltip-popup"));
	}

	[Fact]
	public async Task ColorPicker_NamesItsButtonsAndSwatches()
	{
		IRenderedComponent<MokaColorPicker> cut = Render<MokaColorPicker>(p => p
			.Add(x => x.Value, "#ef5350")
			.Add(x => x.ShowEyeDropper, true)
			.Add(x => x.Presets, Swatches));

		IconTooltip.AssertNamed(cut.Find(".moka-colorpicker-swatch-button"), "Choose colour", MokaTooltipPosition.BottomStart);
		IconTooltip.AssertNamed(cut.Find(".moka-colorpicker-copy-btn"), "Copy hex value", MokaTooltipPosition.BottomEnd);

		await cut.Find(".moka-colorpicker-swatch-button").ClickAsync(new MouseEventArgs());

		IconTooltip.AssertNamed(cut.Find(".moka-colorpicker-eyedropper-btn"), "Pick from screen", MokaTooltipPosition.Bottom);
		IconTooltip.AssertNamed(cut.FindAll(".moka-colorpicker-presets .moka-colorpicker-preset")[1], "#42a5f5", MokaTooltipPosition.Bottom);

		// Closing keeps the colour in the recent row, whose swatches are named the same way.
		await cut.Find(".moka-colorpicker-backdrop").ClickAsync(new MouseEventArgs());
		await cut.Find(".moka-colorpicker-swatch-button").ClickAsync(new MouseEventArgs());

		IconTooltip.AssertNamed(cut.Find(".moka-colorpicker-recent .moka-colorpicker-preset"), "#ef5350", MokaTooltipPosition.Bottom);
	}

	// The remove buttons make a column down the list, so the name opens to the left, over its own row.
	[Fact]
	public void FileUpload_NamesEachRemoveButton()
	{
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>();

		cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("a", "report.pdf"));

		IconTooltip.AssertNamed(cut.Find(".moka-fileupload-remove"), "Remove report.pdf", MokaTooltipPosition.Left);
	}

	// Moving and removing buttons make columns at the ends of the rows, so their names open beside them.
	[Fact]
	public void FormBuilder_NamesItsRowButtons()
	{
		List<MokaFormField> fields =
		[
			new() { Id = "a", Type = MokaFormFieldType.TextField, Label = "Name" },
			new() { Id = "b", Type = MokaFormFieldType.TextField, Label = "Email" }
		];
		IRenderedComponent<MokaFormBuilder> cut = Render<MokaFormBuilder>(p => p.Add(x => x.Fields, fields));

		IReadOnlyList<IElement> rows = cut.FindAll(".moka-form-builder__field-item");
		IconTooltip.AssertNamed(rows[0].QuerySelector("button[aria-label='Move down']")!, "Move down", MokaTooltipPosition.Left);
		IconTooltip.AssertNamed(rows[1].QuerySelector("button[aria-label='Move up']")!, "Move up", MokaTooltipPosition.Left);
		IconTooltip.AssertNamed(rows[1].QuerySelector(".moka-form-builder__remove-btn")!, "Remove field", MokaTooltipPosition.Left);
		Assert.All(cut.FindAll(".moka-form-builder__move-btn, .moka-form-builder__remove-btn"),
			button => Assert.Equal("button", button.GetAttribute("type")));
	}

	// The toggle had neither a name nor a state; it has both now, and its name opens beside the tree.
	[Fact]
	public async Task TreeSelect_NamesItsExpandToggle()
	{
		MokaTreeSelectItem<string>[] items = [new("src", "src", Children: [new MokaTreeSelectItem<string>("app", "app.cs")])];
		IRenderedComponent<MokaTreeSelect<string>> cut = Render<MokaTreeSelect<string>>(p => p.Add(x => x.Items, items));

		await cut.Find(".moka-tree-select-trigger").ClickAsync(new MouseEventArgs());

		IElement toggle = cut.Find(".moka-tree-select-expand");
		IconTooltip.AssertNamed(toggle, "Expand", MokaTooltipPosition.Right);
		Assert.Equal("false", toggle.GetAttribute("aria-expanded"));
	}

	// The adornments sit in tooltips that take their places inside the input. A transform on an element that holds a
	// tooltip makes it the box the fixed popup is placed in, so they are centred with auto margins instead.
	[Theory]
	[InlineData("MokaTextField.razor.css", ".moka-textfield ::deep .moka-textfield-clear-tip", ".moka-textfield-clear")]
	[InlineData("MokaSearchInput.razor.css", ".moka-search-wrapper ::deep .moka-search-clear-tip", ".moka-search-clear")]
	[InlineData("MokaPasswordField.razor.css", ".moka-passwordfield ::deep .moka-passwordfield-toggle-tip", ".moka-passwordfield-toggle")]
	[InlineData("MokaAutoComplete.razor.css", ".moka-autocomplete ::deep .moka-autocomplete-clear-tip", ".moka-autocomplete-clear")]
	[InlineData("MokaTagInput.razor.css", ".moka-taginput ::deep .moka-taginput-clear-tip", ".moka-taginput-clear")]
	[InlineData("MokaDatePicker.razor.css", ".moka-datepicker-input-wrapper ::deep .moka-datepicker-toggle-tip", ".moka-datepicker-toggle")]
	[InlineData("MokaTimePicker.razor.css", ".moka-timepicker-input-wrapper ::deep .moka-timepicker-toggle-tip", ".moka-timepicker-toggle")]
	public void AnAdornment_IsPlacedByItsTooltipWithoutATransform(string file, string tip, string button)
	{
		IReadOnlyDictionary<string, string> place = ScopedCss.Declarations(file, tip);
		Assert.Equal("absolute", place["position"]);
		Assert.Equal("auto", place["margin-block"]);
		Assert.False(place.ContainsKey("transform"));

		IReadOnlyDictionary<string, string> look = ScopedCss.Declarations(file, button);
		Assert.False(look.ContainsKey("position"));
		Assert.False(look.ContainsKey("transform"));
	}
}
