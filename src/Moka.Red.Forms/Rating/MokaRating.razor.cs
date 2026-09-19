using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Base;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Utilities;
using Moka.Red.Forms.Common;
using Moka.Red.Icons;

namespace Moka.Red.Forms.Rating;

/// <summary>
///     A star rating input component. Renders stars via MokaIcon.
///     Supports hover preview, custom icons, and clearing. It is a WAI-ARIA slider: one tab stop,
///     with the arrow keys, Home and End changing the value.
/// </summary>
public partial class MokaRating : MokaVisualComponentBase
{
	private const string KeysModule = "./_content/Moka.Red.Core/moka-keys.js";

	// The keys the slider consumes. Without this the arrows, Home and End also scroll the page.
	// HandleKeyDown ignores them with Ctrl, Alt or Meta, so those stay with the browser (Alt+Left
	// is back). Dictionaries rather than anonymous types, which trimming can strip in WebAssembly apps.
	private static readonly Dictionary<string, object?>[] SliderKeyRules =
	[
		new()
		{
			["selector"] = null,
			["keys"] = new[] { "ArrowLeft", "ArrowRight", "ArrowUp", "ArrowDown", "Home", "End" },
			["unlessModified"] = true
		}
	];

	private static readonly Dictionary<string, object?>[] NoKeyRules = [];

	private readonly string _generatedId = $"moka-rating-{Guid.NewGuid():N}";
	private int _hoverValue;
	private ElementReference _slider;
	private bool _keysCancelled;

	// The rating shows _value, not Value: a click or a key changes it before the parent answers,
	// and a parent that does not bind Value must not undo it by re-rendering.
	private int _value;
	private int? _lastValueParameter;

	// Id goes on the slider, not the wrapper, and the label's id is built from it.
	private string InputId => string.IsNullOrEmpty(Id) ? _generatedId : Id;

	/// <summary>Current rating value (0 to <see cref="MaxValue" />). Two-way bindable.</summary>
	[Parameter]
	public int Value { get; set; }

	/// <summary>Callback invoked when <see cref="Value" /> changes.</summary>
	[Parameter]
	public EventCallback<int> ValueChanged { get; set; }

	/// <summary>Number of stars. Default 5.</summary>
	[Parameter]
	public int MaxValue { get; set; } = 5;

	/// <summary>
	///     Label text displayed above the rating. It also names the slider for screen readers.
	///     Without a label, pass <c>aria-label</c>.
	/// </summary>
	[Parameter]
	public string? Label { get; set; }

	/// <summary>
	///     Whether the rating is read-only. Default false. A read-only rating keeps its tab stop so
	///     the value can still be read, but neither clicks nor keys change it.
	/// </summary>
	[Parameter]
	public bool ReadOnly { get; set; }

	/// <summary>
	///     Whether the rating can go back to 0. Default true. A click on the current star clears it,
	///     and Left, Down and Home reach 0 from the keyboard.
	/// </summary>
	[Parameter]
	public bool AllowClear { get; set; } = true;

	/// <summary>Custom outline icon definition.</summary>
	[Parameter]
	public MokaIconDefinition? Icon { get; set; }

	/// <summary>Custom filled icon definition.</summary>
	[Parameter]
	public MokaIconDefinition? FilledIcon { get; set; }

	/// <summary>Whether to highlight stars on hover. Default true.</summary>
	[Parameter]
	public bool HoverPreview { get; set; } = true;

	/// <summary>Whether to show the numeric value next to the stars. Default false.</summary>
	[Parameter]
	public bool ShowValue { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-rating";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-rating--readonly", ReadOnly)
		.AddClass("moka-rating--disabled", Disabled)
		.AddClass($"moka-rating--{SizeToKebab(Size)}")
		.AddClass(Class)
		.Build();

	// The field wrapper is the outermost element, so the margin goes there. The star row is the
	// control itself, so it keeps the padding and the radius (its focus ring follows the radius).

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("padding", ResolvedPadding)
		.AddStyle("border-radius", ResolvedRounding)
		.AddStyle(Style)
		.Build();

	private string? WrapperStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin)
		.Build();

	private bool IsInteractive => !ReadOnly && !Disabled;

	// A disabled rating leaves the tab order. A read-only one stays in it, like a read-only field.
	private int? TabIndex => Disabled ? null : 0;

	private int HighestValue => Math.Max(MaxValue, 0);

	private int CurrentValue => Math.Clamp(_value, 0, HighestValue);

	// Without AllowClear, 0 only means "not rated yet": the keys, like the mouse, cannot go back to it.
	private int LowestValue => AllowClear ? 0 : Math.Min(1, HighestValue);

	private string ValueText => CurrentValue == 0
		? "No rating"
		: string.Create(CultureInfo.CurrentCulture, $"{CurrentValue} of {HighestValue}");

	// <label for> names only native controls, so the slider points at the wrapper's label.
	private string? LabelledBy => Label is null ? null : MokaFieldWrapper.LabelIdFor(InputId);

	private string? AccessibleName
	{
		get
		{
			if (Label is not null)
			{
				return null;
			}

			if (AdditionalAttributes?.TryGetValue("aria-label", out object? ariaLabel) == true
			    && ariaLabel is string name && !string.IsNullOrWhiteSpace(name))
			{
				return name;
			}

			return "Rating";
		}
	}

	// The slider is the control, so unmatched attributes go on it. aria-label is left out: the
	// slider already renders it through AccessibleName.
	private IReadOnlyDictionary<string, object>? SliderAttributes =>
		MokaAttributes.Without(AdditionalAttributes, "aria-label");

	/// <summary>The effective display value (hover value or actual value).</summary>
	private int DisplayValue => _hoverValue > 0 ? _hoverValue : CurrentValue;

	/// <summary><see cref="FilledIcon" /> when set, otherwise the built-in solid star.</summary>
	private MokaIconDefinition ResolvedFilledIcon => FilledIcon ?? MokaIcons.Toggle.Star;

	/// <summary><see cref="Icon" /> when set, otherwise the built-in outline star.</summary>
	private MokaIconDefinition ResolvedOutlineIcon => Icon ?? MokaIcons.Toggle.StarOutline;

	/// <summary>Rating has hover state that changes independently of parameters.</summary>
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		if (_lastValueParameter != Value)
		{
			_lastValueParameter = Value;
			_value = Value;
		}
	}

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		// The value keys are cancelled in the browser only while they do something. A read-only or
		// disabled rating leaves them to the page, and never loads the script if it starts that way.
		if (IsInteractive == _keysCancelled)
		{
			return;
		}

		_keysCancelled = IsInteractive;
		await SafeModuleInvokeVoidAsync(KeysModule, "preventKeys", _slider,
			IsInteractive ? SliderKeyRules : NoKeyRules);
	}

	private static string StarCssClass(bool filled) => new CssBuilder("moka-rating-star")
		.AddClass("moka-rating-star--filled", filled)
		.Build();

	private async Task HandleClick(int star)
	{
		if (!IsInteractive)
		{
			return;
		}

		await SetValueAsync(AllowClear && star == CurrentValue ? 0 : star);
	}

	private async Task HandleKeyDown(KeyboardEventArgs e)
	{
		if (!IsInteractive || e.AltKey || e.CtrlKey || e.MetaKey)
		{
			return;
		}

		int current = CurrentValue;
		int? next = e.Key switch
		{
			"ArrowRight" or "ArrowUp" => Math.Min(current + 1, HighestValue),
			"ArrowLeft" or "ArrowDown" => current > LowestValue ? current - 1 : current,
			"Home" => LowestValue,
			"End" => HighestValue,
			_ => null
		};

		if (next is null)
		{
			return;
		}

		// The stars follow the keys even while the pointer rests on one.
		_hoverValue = 0;
		await SetValueAsync(next.Value);
	}

	private async Task SetValueAsync(int value)
	{
		if (value == _value)
		{
			return;
		}

		_value = value;
		await ValueChanged.InvokeAsync(value);
	}

	private void HandleMouseEnter(int star)
	{
		if (IsInteractive && HoverPreview)
		{
			_hoverValue = star;
		}
	}

	private void HandleMouseLeave() => _hoverValue = 0;
}
