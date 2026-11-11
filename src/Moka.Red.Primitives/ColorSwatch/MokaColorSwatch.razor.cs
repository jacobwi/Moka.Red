using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Theming;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.ColorSwatch;

/// <summary>
///     A set of color swatches to pick one color from. It is a radio group: each swatch holds a native radio, so Tab
///     reaches the group once, the arrow keys move the choice, and screen readers hear "radio button, checked".
/// </summary>
public partial class MokaColorSwatch
{
	private const string ColorInputDefault = "#000000";
	private const string PopupModule = "./_content/Moka.Red.Core/moka-popup.js";
	private const string SwatchModule = "./_content/Moka.Red.Primitives/ColorSwatch/MokaColorSwatch.razor.js";

	private readonly List<string> _customColors = [];
	private readonly string _groupName = $"moka-swatch-{Guid.NewGuid():N}";
	private ElementReference _addButton;
	private ElementReference _anchor;
	private ElementReference _backdrop;
	private string? _boundHexId;
	private bool _customOpen;
	private ElementReference _customInput;
	private string _customValue = ColorInputDefault;
	private bool _focusAddButton;
	private bool _focusCustomInput;
	private bool _focusPanelHex;
	private ElementReference _hexInput;
	private bool _hexInvalid;
	private string _hexText = string.Empty;
	private string? _lastSelectedColor;
	private MokaLabelColor? _lastSelectedLabel;
	private ElementReference _panel;
	private string _panelHex = string.Empty;
	private ElementReference _panelHexInput;
	private bool _panelInvalid;
	private bool _panelShown;
	private int? _pickedSlot;
	private bool _refocusPanelHex;
	private bool _seededFromLabels;
	private string? _selectedColor;
	private bool _showCustomInput;

	private enum ShownMark
	{
		Nothing,
		Check,
		Dot,
		Badge
	}

	/// <summary>
	///     The colors to display as swatches: hex values, color keywords or color functions such as
	///     <c>rgb()</c> or <c>var()</c>. A value that is not a CSS color gets an empty swatch.
	/// </summary>
	[Parameter]
	public IEnumerable<string> Colors { get; set; } = [];

	/// <summary>
	///     Colors under headings, such as "Theme colors" and "Recent", in place of <see cref="Colors" />. All of them are
	///     one radio group. <see cref="Labels" /> wins over it.
	/// </summary>
	[Parameter]
	public IReadOnlyList<MokaSwatchGroup>? Groups { get; set; }

	/// <summary>Currently selected color. Two-way bindable.</summary>
	[Parameter]
	public string? SelectedColor { get; set; }

	/// <summary>Callback when selected color changes.</summary>
	[Parameter]
	public EventCallback<string?> SelectedColorChanged { get; set; }

	/// <summary>
	///     The most swatches in a row. A narrower container wraps them onto more rows. The custom color entry and
	///     <see cref="EndContent" /> don't count, and <see cref="MokaSwatchLook.Chips" /> and <see cref="MokaSwatchLook.Strip" />
	///     ignore it. Default 8.
	/// </summary>
	[Parameter]
	public int Columns { get; set; } = 8;

	/// <summary>
	///     Size of each swatch, as a CSS length. Wins over <see cref="SizeValue" /> and <see cref="Size" />,
	///     which give 16, 20, 24 or 32 px from Xs to Lg. Default 24 px.
	/// </summary>
	[Parameter]
	public string? SwatchSize { get; set; }

	/// <summary>Shape of the swatches. Default Circle.</summary>
	[Parameter]
	public MokaSwatchShape Shape { get; set; } = MokaSwatchShape.Circle;

	/// <summary>
	///     Show a + button that opens a color input for a color the palette doesn't have, drawn as
	///     <see cref="CustomLook" /> says. The color chosen there gets a swatch of its own after the palette, and so does
	///     a <see cref="SelectedColor" /> the palette doesn't have. Default false.
	/// </summary>
	[Parameter]
	public bool AllowCustom { get; set; }

	/// <summary>
	///     Whether a click on the chosen swatch, or Delete or Backspace while it has focus, clears the selection.
	///     Default false. <see cref="ShowNone" /> is the clearer way to offer the same.
	/// </summary>
	[Parameter]
	public bool AllowClear { get; set; }

	/// <summary>
	///     Adds a first swatch with a slash through it that chooses no color, named by <see cref="MokaColorSwatchText.None" />,
	///     or drawn as <see cref="NoneLook" /> says. It is checked while nothing is chosen. Default false.
	/// </summary>
	[Parameter]
	public bool ShowNone { get; set; }

	/// <summary>Whether the chosen color's name shows under the swatches. Default true.</summary>
	[Parameter]
	public bool ShowPreview { get; set; } = true;

	/// <summary>
	///     Accessible name of the group, such as "Folder color", announced when focus enters it. To use visible text as
	///     the name instead, pass <c>aria-labelledby</c>.
	/// </summary>
	[Parameter]
	public string? AriaLabel { get; set; }

	/// <summary>The words the swatch writes, for the no color swatch and the custom color controls.</summary>
	[Parameter]
	public MokaColorSwatchText Text { get; set; } = new();

	/// <summary>
	///     Names a color, such as "Blue" for <c>#42a5f5</c>. The name is the swatch's accessible name, its tooltip
	///     and the text beside the chosen color. A color it returns no name for is named by its value.
	/// </summary>
	[Parameter]
	public Func<string, string?>? ColorLabel { get; set; }

	/// <summary>Says which colors are drawn but can't be picked. Default none.</summary>
	[Parameter]
	public Func<string, bool>? ColorDisabled { get; set; }

	/// <summary>
	///     Label colors to offer in place of <see cref="Colors" />, such as <c>MokaLabelColors.Standard</c>. Each swatch is
	///     the label's solid color, so it follows the theme, light or dark, and <c>MokaTheme.WithLabelTone</c>, and is
	///     named by <see cref="LabelName" />. The pick comes back through <see cref="SelectedLabel" />, a name to store,
	///     never a color value. <see cref="AllowCustom" /> does nothing here, since a custom color has no name.
	/// </summary>
	[Parameter]
	public IReadOnlyList<MokaLabelColor>? Labels { get; set; }

	/// <summary>The chosen label color while <see cref="Labels" /> is set. Two-way bindable.</summary>
	[Parameter]
	public MokaLabelColor? SelectedLabel { get; set; }

	/// <summary>Raised with the chosen label color, or null when <see cref="AllowClear" /> clears it.</summary>
	[Parameter]
	public EventCallback<MokaLabelColor?> SelectedLabelChanged { get; set; }

	/// <summary>
	///     Names a label color in the app's own words, for the swatch's accessible name, its tooltip and the text beside
	///     the chosen color. A null or blank answer, or no function, gives the English name, such as "Teal".
	/// </summary>
	[Parameter]
	public Func<MokaLabelColor, string?>? LabelName { get; set; }

	/// <summary>Says which label colors are drawn but can't be picked. Default none.</summary>
	[Parameter]
	public Func<MokaLabelColor, bool>? LabelDisabled { get; set; }

	/// <summary>
	///     How the swatches are drawn: circles, a ring with a gap, tiles, named chips, a strip, a menu list or terminal
	///     squares. Each look has its own size at every <see cref="Size" />, and <see cref="SwatchSize" /> still wins.
	///     Default <see cref="MokaSwatchLook.Standard" />.
	/// </summary>
	[Parameter]
	public MokaSwatchLook Look { get; set; } = MokaSwatchLook.Standard;

	/// <summary>
	///     The mark on the chosen color. <see cref="MokaSwatchMark.Standard" /> (the default) is the look's own.
	///     <see cref="MokaSwatchLook.Chips" /> and <see cref="MokaSwatchLook.Menu" /> ignore it.
	/// </summary>
	[Parameter]
	public MokaSwatchMark Mark { get; set; } = MokaSwatchMark.Standard;

	/// <summary>
	///     How <see cref="AllowCustom" /> asks for a color: a dashed + (the default), a rainbow +, a Custom pill, or a hex
	///     field.
	/// </summary>
	[Parameter]
	public MokaSwatchCustomLook CustomLook { get; set; } = MokaSwatchCustomLook.Standard;

	/// <summary>
	///     How <see cref="ShowNone" /> draws the no color swatch: a slash (the default), a None pill, a dashed outline or a
	///     checkerboard.
	/// </summary>
	[Parameter]
	public MokaSwatchNoneLook NoneLook { get; set; } = MokaSwatchNoneLook.Standard;

	/// <summary>
	///     Content after the swatches, in the last group's row, such as a button of the app's own. It flows with the
	///     swatches and wraps with them.
	/// </summary>
	[Parameter]
	public RenderFragment? EndContent { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-color-swatch";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-color-swatch--look", Look != MokaSwatchLook.Standard)
		.AddClass($"moka-color-swatch--{MokaEnumHelpers.ToCssClass(Look)}", Look != MokaSwatchLook.Standard)
		.AddClass("moka-color-swatch--named", Named)
		.AddClass("moka-color-swatch--marked", MarkApplies)
		.AddClass($"moka-color-swatch--mark-{MokaEnumHelpers.ToCssClass(Mark)}", MarkApplies)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => SpacingStyle()
		.AddStyle("--moka-swatch-columns", Math.Max(1, Columns).ToString(CultureInfo.InvariantCulture))
		.AddStyle("--moka-swatch-size", ResolvedSwatchSize)
		.AddStyle(Style)
		.Build();

	private string ResolvedSwatchSize => SwatchSize is not null
		? CssValues.WithPixels(SwatchSize)
		: SizeValue is not null
			? ResolvedSize
			: LookSize(Look, Size);

	private bool LabelMode => Labels is not null;

	private bool Grouped => Groups is not null && !LabelMode;

	private bool CustomAllowed => AllowCustom && !LabelMode;

	// Chips and Menu show each color's name beside it, so they need no tooltip and draw their own mark.
	private bool Named => Look is MokaSwatchLook.Chips or MokaSwatchLook.Menu;

	private bool MarkApplies => Mark != MokaSwatchMark.Standard && !Named;

	private bool Strip => Look == MokaSwatchLook.Strip;

	private bool NoneIsChip => NoneLook == MokaSwatchNoneLook.Chip;

	private bool OpensPanel => CustomLook is MokaSwatchCustomLook.Rainbow or MokaSwatchCustomLook.Pill;

	// Anything wider than a swatch can't sit in the default look's fixed tracks, so the swatches flow instead, which
	// lays the same swatches out in the same rows.
	private bool HasWideItems => (ShowNone && NoneIsChip) ||
	                             (CustomAllowed && CustomLook is MokaSwatchCustomLook.Pill or MokaSwatchCustomLook.Hex) ||
	                             EndContent is not null;

	private bool Flows => Look != MokaSwatchLook.Standard || HasWideItems;

	// Columns only limits the swatches in a row. A group with no more swatches than that is one row however it's capped,
	// so it loses the cap, and a custom entry or the end slot can sit beside its swatches instead of wrapping under them.
	private string GridCssClass(SwatchSection section, bool first) => new CssBuilder("moka-color-swatch-grid")
		.AddClass("moka-color-swatch-grid--flow", Look == MokaSwatchLook.Standard && HasWideItems)
		.AddClass("moka-color-swatch-grid--one-row",
			Flows && section.Slots.Count + (first && ShowNone ? 1 : 0) <= Math.Max(1, Columns))
		.Build();

	// The default look's check keeps its size; the others size it to the swatch, as their swatches differ.
	private string CheckSize => Look == MokaSwatchLook.Standard ? "12px" : "55%";

	private string PanelId => $"{_groupName}-custom";

	private string PanelKey => $"{_groupName}-panel";

	private string HexErrorId => $"{_groupName}-hex-error";

	private string ExpandedText => _customOpen ? "true" : "false";

	private string RainbowCssClass => new CssBuilder("moka-color-swatch-rainbow")
		.AddClass(ShapeClass)
		.Build();

	// Each look has a size of its own at Md, and steps from it like the default look does.
	private static string LookSize(MokaSwatchLook look, MokaSize size)
	{
		int medium = look switch
		{
			MokaSwatchLook.Ring => 26,
			MokaSwatchLook.Tiles or MokaSwatchLook.Strip => 30,
			MokaSwatchLook.Chips => 18,
			MokaSwatchLook.Menu => 14,
			MokaSwatchLook.Terminal => 22,
			_ => 24
		};
		int step = look is MokaSwatchLook.Chips or MokaSwatchLook.Menu ? 2 : 4;
		int pixels = size switch
		{
			MokaSize.Xs => medium - 2 * step,
			MokaSize.Sm => medium - step,
			MokaSize.Lg => medium + 2 * step,
			_ => medium
		};
		return pixels.ToString(CultureInfo.InvariantCulture) + "px";
	}

	// The check reads the color from this property to pick black or white. The color goes into a declaration, where a
	// semicolon or a parenthesis would end it, so only a real color is written.
	private static string? SwatchStyle(string? color) => new StyleBuilder()
		.AddStyle("--_swatch-color", CssValues.IsColor(color) ? color.Trim() : null)
		.Build();

	private string ShapeClass => Shape == MokaSwatchShape.Circle
		? "moka-color-swatch-item--circle"
		: "moka-color-swatch-item--square";

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		// The selection is only seeded when the parent passes a new value, so a parent render that passes the old one
		// again cannot undo the user's pick.
		if (LabelMode)
		{
			if (!_seededFromLabels || _lastSelectedLabel != SelectedLabel)
			{
				_lastSelectedLabel = SelectedLabel;
				_selectedColor = SelectedLabel is { } label ? MokaLabelColors.Solid(label) : null;
			}
		}
		else if (_seededFromLabels || !string.Equals(_lastSelectedColor, SelectedColor, StringComparison.Ordinal))
		{
			_lastSelectedColor = SelectedColor;
			_selectedColor = SelectedColor;
			if (CustomAllowed && CssValues.IsColor(_selectedColor))
			{
				RememberCustom(_selectedColor);
			}
		}

		_seededFromLabels = LabelMode;
	}

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		await base.OnAfterRenderAsync(firstRender);

		if (_panelShown != _customOpen)
		{
			_panelShown = _customOpen;
			if (_customOpen)
			{
				await SafeModuleInvokeVoidAsync(PopupModule, "open", PanelKey, _panel, _anchor, _backdrop,
					new Dictionary<string, object?> { ["placement"] = "bottom-start" });
			}
			else
			{
				await SafeModuleInvokeVoidAsync(PopupModule, "close", PanelKey);
			}
		}

		if (_focusPanelHex)
		{
			_focusPanelHex = false;
			await SafeModuleInvokeVoidAsync(SwatchModule, "focusHexField", _panelHexInput);
		}
		else if (_refocusPanelHex)
		{
			_refocusPanelHex = false;
			await SafeFocusAsync(_panelHexInput);
		}
		else if (_focusCustomInput)
		{
			_focusCustomInput = false;
			await SafeFocusAsync(_customInput);
		}
		else if (_focusAddButton)
		{
			_focusAddButton = false;
			await SafeFocusAsync(_addButton);
		}

		await BindInlineHexFieldAsync();
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		if (_panelShown)
		{
			_panelShown = false;
			await SafeModuleInvokeVoidAsync(PopupModule, "close", PanelKey);
		}

		await base.DisposeAsyncCore();
	}

	// Enter in the hex field takes its color, and must not also submit a form around the swatch.
	private async Task BindInlineHexFieldAsync()
	{
		bool shown = CustomAllowed && CustomLook == MokaSwatchCustomLook.Hex;
		string? id = shown ? _hexInput.Id : null;
		if (string.IsNullOrEmpty(id) || string.Equals(id, _boundHexId, StringComparison.Ordinal))
		{
			return;
		}

		_boundHexId = id;
		await SafeModuleInvokeVoidAsync(SwatchModule, "bindHexField", _hexInput);
	}

	private static bool SameColor(string? a, string? b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

	private IEnumerable<(string? Title, IEnumerable<string> Colors)> Sources()
	{
		if (Labels is { } labels)
		{
			yield return (null, labels.Select(MokaLabelColors.Solid));
		}
		else if (Groups is { } groups)
		{
			foreach (MokaSwatchGroup group in groups)
			{
				yield return (group.Title, group.Colors);
			}
		}
		else
		{
			yield return (null, Colors);
		}
	}

	private IEnumerable<string> PaletteColors => Sources().SelectMany(source => source.Colors);

	// Every swatch is numbered across all the grids, so a color drawn twice still has one place that is checked.
	private List<SwatchSection> BuildSections()
	{
		List<SwatchSection> sections = [];
		int index = 0;
		foreach ((string? title, IEnumerable<string> colors) in Sources())
		{
			List<SwatchSlot> slots = [];
			foreach (string color in colors)
			{
				slots.Add(new SwatchSlot(index++, color, false));
			}

			sections.Add(new SwatchSection(title, slots));
		}

		if (sections.Count == 0)
		{
			sections.Add(new SwatchSection(null, []));
		}

		if (CustomAllowed)
		{
			List<string> palette = [.. PaletteColors];
			foreach (string color in _customColors.Where(color => !palette.Contains(color, StringComparer.OrdinalIgnoreCase)))
			{
				sections[^1].Slots.Add(new SwatchSlot(index++, color, true));
			}
		}

		return sections;
	}

	private int? CheckedIndex(IEnumerable<SwatchSection> sections)
	{
		if (_selectedColor is null)
		{
			return null;
		}

		int? first = null;
		foreach (SwatchSlot slot in sections.SelectMany(section => section.Slots).Where(slot => SameColor(slot.Color, _selectedColor)))
		{
			if (slot.Index == _pickedSlot)
			{
				return slot.Index;
			}

			first ??= slot.Index;
		}

		return first;
	}

	private void RememberCustom(string color)
	{
		if (!PaletteColors.Contains(color, StringComparer.OrdinalIgnoreCase) &&
		    !_customColors.Contains(color, StringComparer.OrdinalIgnoreCase))
		{
			_customColors.Add(color);
		}
	}

	private MokaLabelColor? LabelOf(string? color) =>
		LabelMode && MokaLabelColors.TryParse(color, out MokaLabelColor label) ? label : null;

	private string NameOf(string color)
	{
		if (LabelOf(color) is { } label)
		{
			return MokaLabelColors.Name(label, LabelName);
		}

		string? name = ColorLabel?.Invoke(color);
		return string.IsNullOrWhiteSpace(name) ? color : name;
	}

	private string ItemCssClass(SwatchSlot slot, bool isChecked, StripEnds ends) => new CssBuilder("moka-color-swatch-item")
		.AddClass(ShapeClass)
		.AddClass("moka-color-swatch-item--selected", isChecked)
		.AddClass("moka-color-swatch-item--custom", slot.Custom)
		.AddClass("moka-color-swatch-item--disabled", IsDisabled(slot.Color))
		.AddClass("moka-color-swatch-item--start", ends.Start)
		.AddClass("moka-color-swatch-item--end", ends.End)
		.Build();

	private string NoneCssClass(bool isChecked, StripEnds ends) => new CssBuilder("moka-color-swatch-item")
		.AddClass(ShapeClass)
		.AddClass("moka-color-swatch-item--none")
		.AddClass($"moka-color-swatch-item--none-{MokaEnumHelpers.ToCssClass(NoneLook)}", NoneLook != MokaSwatchNoneLook.Standard)
		.AddClass("moka-color-swatch-item--selected", isChecked)
		.AddClass("moka-color-swatch-item--disabled", Disabled)
		.AddClass("moka-color-swatch-item--start", ends.Start)
		.AddClass("moka-color-swatch-item--end", ends.End)
		.Build();

	// A strip rounds only its two ends. The no color swatch is a segment of the bar unless it is a chip.
	private StripEnds NoneEnds(SwatchSection section) =>
		Strip && !NoneIsChip ? new StripEnds(true, section.Slots.Count == 0) : default;

	private StripEnds SlotEnds(SwatchSection section, int position, bool noneFirst) => Strip
		? new StripEnds(position == 0 && !(noneFirst && !NoneIsChip), position == section.Slots.Count - 1)
		: default;

	// The look's own mark, or the one Mark asks for. The default look's no color swatch is marked by its border alone.
	private ShownMark MarkFor(bool none)
	{
		if (Look == MokaSwatchLook.Menu)
		{
			return ShownMark.Nothing;
		}

		if (Look == MokaSwatchLook.Chips)
		{
			return ShownMark.Check;
		}

		if (Look == MokaSwatchLook.Standard && Mark == MokaSwatchMark.Standard)
		{
			return none ? ShownMark.Nothing : ShownMark.Check;
		}

		return Mark switch
		{
			MokaSwatchMark.Ring or MokaSwatchMark.ColorRing => ShownMark.Nothing,
			MokaSwatchMark.Dot => ShownMark.Dot,
			MokaSwatchMark.Badge => ShownMark.Badge,
			_ => ShownMark.Check
		};
	}

	private static string? HexDotStyle(string text) => new StyleBuilder()
		.AddStyle("--_swatch-try", ParseHex(text))
		.Build();

	private static string HexCssClass(bool invalid) => new CssBuilder("moka-color-swatch-hex")
		.AddClass("moka-color-swatch-hex--invalid", invalid)
		.Build();

	// Six hex digits, with or without the #, as the lower case #rrggbb a color input writes.
	private static string? ParseHex(string? text)
	{
		ReadOnlySpan<char> value = text.AsSpan().Trim();
		if (value.StartsWith("#"))
		{
			value = value[1..];
		}

		if (value.Length != 6)
		{
			return null;
		}

		Span<char> hex = stackalloc char[7];
		hex[0] = '#';
		for (int i = 0; i < 6; i++)
		{
			if (!char.IsAsciiHexDigit(value[i]))
			{
				return null;
			}

			hex[i + 1] = char.ToLowerInvariant(value[i]);
		}

		return hex.ToString();
	}

	private bool IsDisabled(string color) => Disabled || (LabelOf(color) is { } label
		? LabelDisabled?.Invoke(label) == true
		: ColorDisabled?.Invoke(color) == true);

	// Only the checked radio listens for clicks, and only to clear: a click on any other radio checks it, which the
	// change event already reports. A listener on every radio would be a round trip for every pick.
	private EventCallback ClearOnClick(SwatchSlot slot, bool isChecked) =>
		ClearsHere(slot, isChecked) ? EventCallback.Factory.Create(this, () => SelectAsync(null)) : default;

	// Space on a checked radio fires no click in Chromium, so the keyboard clears with Delete or Backspace, which a
	// radio has no use for. Space is left alone: where a browser does click, a clear racing the key's release
	// would check the radio again.
	private EventCallback<KeyboardEventArgs> ClearOnKey(SwatchSlot slot, bool isChecked) =>
		ClearsHere(slot, isChecked)
			? EventCallback.Factory.Create<KeyboardEventArgs>(this, e => e.Key is "Delete" or "Backspace" ? SelectAsync(null) : Task.CompletedTask)
			: default;

	private bool ClearsHere(SwatchSlot slot, bool isChecked) => AllowClear && isChecked && !IsDisabled(slot.Color);

	private Task PickAsync(SwatchSlot slot)
	{
		if (IsDisabled(slot.Color))
		{
			return Task.CompletedTask;
		}

		_pickedSlot = slot.Index;
		return SelectAsync(slot.Color);
	}

	private Task PickNoneAsync() => Disabled ? Task.CompletedTask : SelectAsync(null);

	private async Task SelectAsync(string? color)
	{
		_selectedColor = color;
		if (color is null)
		{
			_pickedSlot = null;
		}

		if (LabelMode)
		{
			if (SelectedLabelChanged.HasDelegate)
			{
				await SelectedLabelChanged.InvokeAsync(LabelOf(color));
			}

			return;
		}

		if (SelectedColorChanged.HasDelegate)
		{
			await SelectedColorChanged.InvokeAsync(color);
		}
	}

	private void OpenCustom()
	{
		if (Disabled)
		{
			return;
		}

		_customValue = ColorInputValue(_selectedColor);
		_showCustomInput = true;
		_focusCustomInput = true;
	}

	private void CloseCustom()
	{
		_showCustomInput = false;
		_focusAddButton = true;
	}

	private void HandleCustomKey(KeyboardEventArgs e)
	{
		if (e.Key == "Escape")
		{
			CloseCustom();
		}
	}

	private async Task ConfirmCustomAsync()
	{
		if (string.IsNullOrWhiteSpace(_customValue))
		{
			return;
		}

		CloseCustom();
		await TakeCustomAsync(_customValue);
	}

	// A color the palette has picks that swatch; any other gets a swatch of its own after the palette.
	private Task TakeCustomAsync(string value)
	{
		string color = PaletteColors.FirstOrDefault(palette => SameColor(palette, value)) ?? value;
		RememberCustom(color);
		_pickedSlot = null;
		return SelectAsync(color);
	}

	private void ToggleCustomPanel()
	{
		if (_customOpen)
		{
			_customOpen = false;
			return;
		}

		if (Disabled)
		{
			return;
		}

		_customValue = ColorInputValue(_selectedColor);
		_panelHex = _customValue.ToUpperInvariant();
		_panelInvalid = false;
		_customOpen = true;
		_focusPanelHex = true;
	}

	private void CloseCustomPanel()
	{
		_customOpen = false;
		_focusAddButton = true;
	}

	private void CloseCustomPanelFromBackdrop() => _customOpen = false;

	private void HandlePanelKey(KeyboardEventArgs e)
	{
		if (e.Key == "Escape")
		{
			CloseCustomPanel();
		}
	}

	private void SetPanelColor(string? value)
	{
		_customValue = string.IsNullOrEmpty(value) ? ColorInputDefault : value;
		_panelHex = _customValue.ToUpperInvariant();
		_panelInvalid = false;
	}

	private void SetPanelHex(string? value)
	{
		_panelHex = value ?? string.Empty;
		_panelInvalid = false;
		if (ParseHex(_panelHex) is { } hex)
		{
			_customValue = hex;
		}
	}

	private Task HandlePanelHexKeyAsync(KeyboardEventArgs e) =>
		e.Key == "Enter" && !e.Repeat ? AddFromPanelAsync() : Task.CompletedTask;

	private async Task AddFromPanelAsync()
	{
		if (ParseHex(_panelHex) is not { } hex)
		{
			_panelInvalid = true;
			_refocusPanelHex = true;
			return;
		}

		CloseCustomPanel();
		await TakeCustomAsync(hex);
	}

	private void SetHexText(string? value)
	{
		_hexText = value ?? string.Empty;
		_hexInvalid = false;
	}

	private async Task HandleHexKeyAsync(KeyboardEventArgs e)
	{
		if (e.Key != "Enter" || e.Repeat || Disabled)
		{
			return;
		}

		if (ParseHex(_hexText) is not { } hex)
		{
			_hexInvalid = _hexText.Trim().Length > 0;
			return;
		}

		_hexText = string.Empty;
		_hexInvalid = false;
		await TakeCustomAsync(hex);
	}

	// A color input holds only #rrggbb in lower case, and shows black for anything else.
	private static string ColorInputValue(string? color)
	{
		ReadOnlySpan<char> value = color.AsSpan().Trim();
		if (value.Length is not (4 or 5 or 7 or 9) || value[0] != '#')
		{
			return ColorInputDefault;
		}

		ReadOnlySpan<char> digits = value[1..];
		foreach (char digit in digits)
		{
			if (!char.IsAsciiHexDigit(digit))
			{
				return ColorInputDefault;
			}
		}

		Span<char> hex = stackalloc char[7];
		hex[0] = '#';
		for (int i = 0; i < 6; i++)
		{
			char digit = digits.Length < 6 ? digits[i / 2] : digits[i];
			hex[i + 1] = char.ToLowerInvariant(digit);
		}

		return hex.ToString();
	}

	private readonly record struct SwatchSlot(int Index, string Color, bool Custom);

	private readonly record struct StripEnds(bool Start, bool End);

	private sealed record SwatchSection(string? Title, List<SwatchSlot> Slots);
}
