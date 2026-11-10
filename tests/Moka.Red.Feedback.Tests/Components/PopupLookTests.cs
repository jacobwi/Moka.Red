using System.Text.RegularExpressions;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Theming;
using Moka.Red.Feedback.Dropdown;
using Moka.Red.Feedback.HoverCard;
using Moka.Red.Feedback.Popover;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Feedback.Tests.Components;

// The theme reaches every floating panel through the provider's moka-popups-* class, which only sets CSS variables. A
// panel with a Look of its own carries that class itself, so its variables win over the root's.
public class PopupLookTests : BunitContext
{
	public PopupLookTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	public static TheoryData<MokaPopupLook, string> Looks => new()
	{
		{ MokaPopupLook.Standard, "moka-popups-standard" },
		{ MokaPopupLook.Flat, "moka-popups-flat" },
		{ MokaPopupLook.Elevated, "moka-popups-elevated" },
		{ MokaPopupLook.Glass, "moka-popups-glass" },
		{ MokaPopupLook.AccentTint, "moka-popups-accent-tint" },
		{ MokaPopupLook.Contrast, "moka-popups-contrast" },
		{ MokaPopupLook.Terminal, "moka-popups-terminal" }
	};

	[Theory]
	[MemberData(nameof(Looks))]
	public void APopoversLook_IsAClassOnItsPanel(MokaPopupLook look, string expected)
	{
		IRenderedComponent<MokaPopover> cut = Render<MokaPopover>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Look, look)
			.Add(x => x.PopoverContent, "Details"));

		Assert.Equal([expected], LookClasses(cut.Find(".moka-popover-popup")));
		Assert.Empty(LookClasses(cut.Find(".moka-popover")));
	}

	[Fact]
	public void APopoverWithoutALook_LeavesItToTheTheme()
	{
		IRenderedComponent<MokaPopover> cut = Render<MokaPopover>(p => p
			.AddCascadingValue(MokaTheme.Default.WithPopups(MokaPopupLook.Glass))
			.Add(x => x.Open, true)
			.Add(x => x.PopoverContent, "Details"));

		Assert.Empty(LookClasses(cut.Find(".moka-popover-popup")));
	}

	[Fact]
	public void ADropdownsLook_ReachesItsMenuPanel()
	{
		IRenderedComponent<MokaDropdown> cut = Render<MokaDropdown>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Look, MokaPopupLook.Terminal)
			.Add(x => x.ChildContent, Trigger)
			.Add(x => x.Items, Item));

		Assert.Equal(["moka-popups-terminal"], LookClasses(cut.Find(".moka-popover-popup")));
	}

	[Fact]
	public void ADropdownWithoutALook_PassesNone()
	{
		IRenderedComponent<MokaDropdown> cut = Render<MokaDropdown>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.ChildContent, Trigger)
			.Add(x => x.Items, Item));

		Assert.Empty(LookClasses(cut.Find(".moka-popover-popup")));
	}

	[Theory]
	[InlineData(MokaPopupLook.Elevated, "moka-popups-elevated")]
	[InlineData(MokaPopupLook.AccentTint, "moka-popups-accent-tint")]
	public async Task AHoverCardsLook_IsAClassOnTheCard(MokaPopupLook look, string expected)
	{
		IRenderedComponent<MokaHoverCard> cut = RenderHoverCard(look);

		await cut.Find(".moka-hover-card__trigger").TriggerEventAsync("onmouseenter", new MouseEventArgs());

		cut.WaitForAssertion(() => Assert.Equal([expected], LookClasses(cut.Find(".moka-hover-card__card"))));
		Assert.Empty(LookClasses(cut.Find(".moka-hover-card")));
	}

	[Fact]
	public async Task AHoverCardWithoutALook_LeavesItToTheTheme()
	{
		IRenderedComponent<MokaHoverCard> cut = RenderHoverCard(null);

		await cut.Find(".moka-hover-card__trigger").TriggerEventAsync("onmouseenter", new MouseEventArgs());

		cut.WaitForAssertion(() => Assert.Empty(LookClasses(cut.Find(".moka-hover-card__card"))));
	}

	private IRenderedComponent<MokaHoverCard> RenderHoverCard(MokaPopupLook? look) =>
		Render<MokaHoverCard>(p => p
			.Add(x => x.Delay, 0)
			.Add(x => x.Look, look)
			.Add(x => x.ChildContent, "Ada")
			.Add(x => x.CardContent, "Ada Lovelace"));

	private static List<string> LookClasses(IElement element) =>
		[.. element.ClassList.Where(c => c.StartsWith("moka-popups-", StringComparison.Ordinal))];

	private static readonly RenderFragment Trigger = builder =>
	{
		builder.OpenElement(0, "button");
		builder.AddAttribute(1, "type", "button");
		builder.AddContent(2, "Actions");
		builder.CloseElement();
	};

	private static readonly RenderFragment Item = builder =>
	{
		builder.OpenComponent<MokaDropdownItem>(0);
		builder.AddComponentParameter(1, nameof(MokaDropdownItem.Text), "Rename");
		builder.CloseComponent();
	};
}

// bUnit applies no CSS, so the popup looks are read as text: every floating panel reads each --moka-popup-* variable
// with what it drew before as the fallback, and only in its Gate block, so Dark and Light never see them.
public partial class PopupLookStylesheetTests
{
	private const string GateMarker = "/* Gate look.";

	private static readonly string[] PanelProperties =
	[
		"background", "color", "border-color", "border-radius", "box-shadow", "backdrop-filter", "font-family"
	];

	private static readonly Dictionary<string, string> Variables = new(StringComparer.Ordinal)
	{
		["background"] = "--moka-popup-bg",
		["color"] = "--moka-popup-color",
		["border-color"] = "--moka-popup-border",
		["border-radius"] = "--moka-popup-radius",
		["box-shadow"] = "--moka-popup-shadow",
		["backdrop-filter"] = "--moka-popup-blur",
		["font-family"] = "--moka-popup-font"
	};

	public static TheoryData<string, string> Panels => new()
	{
		{ "MokaPopover.razor.css", ".moka-gate .moka-popover-popup" },
		{ "MokaHoverCard.razor.css", ".moka-gate .moka-hover-card__card" },
		{ "MokaSlashMenu.razor.css", ".moka-gate .moka-slash-menu" },
		{ "MokaContextMenu.razor.css", ".moka-gate .moka-ctx-menu" },
		{ "MokaSplitButton.razor.css", ".moka-gate .moka-split-btn__dropdown" },
		{ "MokaThemeSwitcher.razor.css", ".moka-gate .moka-theme-switcher__dropdown" },
		{ "MokaMenuBarMenu.razor.css", ".moka-gate .moka-menubar-menu__popup" },
		{ "MokaAccountCard.razor.css", ".moka-gate .moka-account-card__menu" },
		{ "MokaTabContextMenu.razor.css", ".moka-gate .moka-context-menu" },
		{ "MokaDatePicker.razor.css", ".moka-gate .moka-datepicker-dropdown" },
		{ "MokaDateRangePicker.razor.css", ".moka-gate .moka-daterange__popup" },
		{ "MokaTimePicker.razor.css", ".moka-gate .moka-timepicker-dropdown" },
		{ "MokaColorPicker.razor.css", ".moka-gate .moka-colorpicker-dropdown" },
		{ "MokaTagInput.razor.css", ".moka-gate .moka-taginput-suggestions" },
		{ "MokaCascader.razor.css", ".moka-gate .moka-cascader-panel" },
		{ "MokaSettingsLayout.razor.css", ".moka-gate .moka-settings-layout__results" },
		{ "MokaTable.razor.css", ".moka-gate .moka-table-column-menu" }
	};

	public static TheoryData<string, string> LitRows => new()
	{
		{ "MokaDropdownItem.razor.css", ".moka-gate .moka-dropdown-item:hover:not(.moka-dropdown-item--disabled)" },
		{ "MokaSlashMenu.razor.css", ".moka-gate .moka-slash-menu-list[data-moka-pointer] .moka-slash-menu-row:hover" },
		{ "MokaSlashMenu.razor.css", ".moka-gate .moka-slash-menu-list:not([data-moka-pointer]) .moka-slash-menu-row--active" },
		{ "MokaContextMenu.razor.css", ".moka-gate .moka-ctx-menu[data-moka-pointer] .moka-ctx-item:hover:not(.moka-ctx-item--disabled)" },
		{ "MokaContextMenu.razor.css", ".moka-gate .moka-ctx-menu:not([data-moka-pointer]) .moka-ctx-item--focused:not(.moka-ctx-item--disabled)" },
		{ "MokaMenuBarItem.razor.css", ".moka-gate .moka-menubar-item--highlighted:not(:disabled)" },
		{ "MokaAccountCard.razor.css", ".moka-gate .moka-account-card__item:hover" },
		{ "MokaTabContextMenu.razor.css", ".moka-gate .moka-context-menu-item:hover:not(:disabled)" },
		{ "MokaThemeSwitcher.razor.css", ".moka-gate .moka-theme-switcher__item:hover" },
		{ "MokaDatePicker.razor.css", ".moka-gate .moka-datepicker-day:hover:not(:disabled)" },
		{ "MokaTimePicker.razor.css", ".moka-gate .moka-timepicker-item:hover" },
		{ "MokaTagInput.razor.css", ".moka-gate .moka-taginput-suggestion:hover" },
		{ "MokaCascader.razor.css", ".moka-gate .moka-cascader-panel[data-moka-pointer] .moka-cascader-option:hover" },
		{ "MokaSettingsLayout.razor.css", ".moka-gate .moka-settings-layout__results[data-moka-pointer] .moka-settings-layout__result:hover" },
		{ "MokaTable.razor.css", ".moka-gate .moka-table-column-menu-item:hover" }
	};

	public static TheoryData<string> Files =>
	[
		"MokaPopover.razor.css", "MokaDropdownItem.razor.css", "MokaHoverCard.razor.css", "MokaSlashMenu.razor.css",
		"MokaContextMenu.razor.css", "MokaSplitButton.razor.css", "MokaThemeSwitcher.razor.css",
		"MokaMenuBarMenu.razor.css", "MokaMenuBarItem.razor.css", "MokaMenuBarDivider.razor.css",
		"MokaAccountCard.razor.css", "MokaTabContextMenu.razor.css", "MokaDatePicker.razor.css",
		"MokaDateRangePicker.razor.css", "MokaTimePicker.razor.css", "MokaColorPicker.razor.css",
		"MokaTagInput.razor.css", "MokaCascader.razor.css", "MokaSettingsLayout.razor.css", "MokaTable.razor.css"
	];

	[Theory]
	[MemberData(nameof(Panels))]
	public void APanel_ReadsEveryPopupVariable_WithAFallback(string file, string selector)
	{
		IReadOnlyDictionary<string, string> rule = ScopedCss.Declarations(file, selector);

		foreach (string property in PanelProperties)
		{
			Assert.True(rule.TryGetValue(property, out string? value), $"{file}: {selector} sets no {property}.");
			Assert.Matches(WithFallback(Variables[property]), value);
		}

		Assert.Equal(rule["backdrop-filter"], rule["-webkit-backdrop-filter"]);
	}

	[Theory]
	[MemberData(nameof(LitRows))]
	public void ALitRow_ReadsTheRowHoverVariable_WithAFallback(string file, string selector)
	{
		IReadOnlyDictionary<string, string> rule = ScopedCss.Declarations(file, selector);

		string background = rule.TryGetValue("background", out string? shorthand) ? shorthand : rule["background-color"];
		Assert.Matches(WithFallback("--moka-popup-row-hover"), background);
		Assert.Matches(WithFallback("--moka-popup-row-hover-shadow"), rule["box-shadow"]);
	}

	// The original design draws its panels from the rules above the Gate block, which must never read a popup
	// variable: a Look on a panel under MokaTheme.Dark would otherwise restyle it.
	[Theory]
	[MemberData(nameof(Files))]
	public void TheOriginalDesign_NeverReadsAPopupVariable(string file)
	{
		string css = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Css", file));
		int marker = css.IndexOf(GateMarker, StringComparison.Ordinal);

		Assert.True(marker >= 0, $"{file} has no Gate block.");
		Assert.DoesNotContain("--moka-popup-", css[..marker], StringComparison.Ordinal);
	}

	private static Regex WithFallback(string variable) =>
		new($@"var\({Regex.Escape(variable)},\s*[^\s)]");
}
