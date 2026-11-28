using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Moka.Red.Core.Base;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Utilities;
using Moka.Red.Forms.Common;

namespace Moka.Red.Forms.RadioGroup;

/// <summary>
///     A radio button group component. Contains <see cref="MokaRadioItem{TValue}" /> children
///     that register via CascadingParameter. Supports vertical and horizontal layouts.
/// </summary>
/// <typeparam name="TValue">The type of the selected value.</typeparam>
public partial class MokaRadioGroup<TValue> : MokaVisualInputBase<TValue>
{
	/// <summary>The radio item children.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Label text displayed above the radio group.</summary>
	[Parameter]
	public string? Label { get; set; }

	/// <summary>
	///     Accessible name for the group, for a group that already has a heading of its own and should not
	///     draw a second one. Without it the group is named by <see cref="Label" />, which is also the
	///     visible heading.
	/// </summary>
	[Parameter]
	public string? AriaLabel { get; set; }

	/// <summary>Helper text displayed below the radio group.</summary>
	[Parameter]
	public string? HelperText { get; set; }

	/// <summary>Layout direction. Default Column (vertical).</summary>
	[Parameter]
	public MokaDirection Orientation { get; set; } = MokaDirection.Column;

	/// <summary>
	///     Draws the options as cards that wrap to fit, each with a picture above its name
	///     (<see cref="MokaRadioItem{TValue}.Preview" />, <see cref="MokaRadioItem{TValue}.Hint" />).
	///     <see cref="Orientation" /> doesn't apply.
	/// </summary>
	[Parameter]
	public bool Cards { get; set; }

	/// <summary>
	///     The narrowest a card gets before the cards wrap, with <see cref="Cards" />. A CSS length or <c>var()</c>, 10rem when unset.
	/// </summary>
	[Parameter]
	public string? MinColumn { get; set; }

	/// <summary>
	///     With <see cref="Cards" />, lets people look through the cards before choosing: the cards are a list whose
	///     highlight moves with the arrow keys, Home, End and the pointer without changing the value, and Enter, Space or a
	///     click chooses the highlighted card. <see cref="HighlightChanged" /> reports where the highlight is, for a preview.
	///     On a static page and until the group renders interactively the cards stay native radios, so a form still posts a
	///     choice.
	/// </summary>
	[Parameter]
	public bool Browse { get; set; }

	/// <summary>
	///     Raised in a <see cref="Browse" /> group when the highlight lands on another card, from a key, the pointer or focus,
	///     with that card's value, and with the chosen value again once the pointer and focus have left the cards. Raised
	///     only when the card changes, never for each pointer move.
	/// </summary>
	[Parameter]
	public EventCallback<TValue> HighlightChanged { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-radiogroup";

	// AriaLabel wins so a group under someone else's heading can be named without drawing a second one.
	private string? GroupAriaLabel => AriaLabel ?? Label;

	private string ComputedCssClass => new CssBuilder(RootClass)
		.AddClass("moka-radiogroup--row", Orientation == MokaDirection.Row && !Cards)
		.AddClass("moka-radiogroup--cards", Cards)
		.AddClass("moka-radiogroup--browse", Browses)
		.AddClass("moka-radiogroup--disabled", Disabled)
		.AddClass(CssClass) // InputBase's field classes: modified, valid, invalid
		.AddClass(Class)
		.Build();

	// The field wrapper is the outermost element, so the margin goes there. The group itself is
	// the field's box, so it keeps the padding and the radius.

	/// <inheritdoc />
	protected override string? ComponentStyle => new StyleBuilder()
		.AddStyle("padding", ResolvedPadding)
		.AddStyle("border-radius", ResolvedRounding)
		.AddStyle("grid-template-columns", CardColumns, Cards)
		.AddStyle(Style)
		.Build();

	// auto-fill rather than auto-fit, so two cards in a wide group stay the size a full row of them would be.
	private string CardColumns =>
		$"repeat(auto-fill, minmax(min(100%, {(CssValues.IsLengthOrVariable(MinColumn) ? CssValues.WithPixels(MinColumn.Trim()) : DefaultCardWidth)}), 1fr))";

	private string? WrapperStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin)
		.Build();

	// Before the group renders interactively the script that moves the highlight can't run, so the cards stay radios a
	// static form can post.
	internal bool Browses => Cards && Browse && RendererInfo.IsInteractive;

	private string? ListDisabled => Disabled ? "true" : null;

	private string? ListTabIndex => Disabled ? null : "0";

	/// <inheritdoc />
	protected override string? PostedValue => Convert.ToString(CurrentValue, CultureInfo.InvariantCulture);

	/// <inheritdoc />
	protected override bool TryParseValueFromString(string? value, out TValue result, out string validationErrorMessage)
	{
		result = default!;
		validationErrorMessage = $"Cannot convert '{value}' to {typeof(TValue).Name}: the selected value comes from a child MokaRadioItem, not from a string.";
		return false;
	}

	private const string DefaultCardWidth = "10rem";

	private readonly string _generatedName = $"moka-radiogroup-{Guid.NewGuid():N}";

	/// <summary>
	///     The <c>name</c> every radio in the group shares. The browser needs it to treat them as one
	///     group: one checked at a time, one tab stop, and the arrow keys moving between them. In a form
	///     it's the field's name, so a post carries the choice.
	/// </summary>
	/// <remarks>
	///     Outside a form it stays generated, because two groups bound through a loop variable would
	///     both be called <c>row.Plan</c> and the browser would make them one group.
	/// </remarks>
	internal string GroupName =>
		(EditContext is not null || AdditionalAttributes?.ContainsKey("name") == true) && FieldName is { } postedName
			? postedName
			: _generatedName;

	/// <summary>Selects a value from a radio item click.</summary>
	internal void SelectValue(TValue value)
	{
		CurrentValue = value;
		StateHasChanged();
	}

	/// <summary>Checks if a value is currently selected.</summary>
	internal bool IsSelected(TValue value) => EqualityComparer<TValue>.Default.Equals(value, CurrentValue);

	internal void AddOption(string key, MokaRadioItem<TValue> item) => _options[key] = item;

	internal void RemoveOption(string key) => _options.Remove(key);

	/// <summary>Called by the browse script when a card is clicked, or chosen with Enter or Space.</summary>
	/// <param name="key">The card's key.</param>
	[JSInvokable]
	public void ChooseFromBrowse(string key)
	{
		if (Browses && !Disabled && _options.TryGetValue(key, out MokaRadioItem<TValue>? item) && !item.IsDisabled)
		{
			SelectValue(item.Value);
		}
	}

	/// <summary>Called by the browse script when the highlight lands on another card, or goes back to the chosen one.</summary>
	/// <param name="key">The highlighted card's key, or null for the chosen value when no card shows it.</param>
	[JSInvokable]
	public void HighlightFromBrowse(string? key)
	{
		TValue value = key is not null && _options.TryGetValue(key, out MokaRadioItem<TValue>? item) ? item.Value : CurrentValue!;
		RunDetached(() => HighlightChanged.InvokeAsync(value));
	}

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		await base.OnAfterRenderAsync(firstRender);

		bool browses = Browses;
		bool reports = HighlightChanged.HasDelegate;
		if (browses != _bound)
		{
			_bound = browses;
			if (browses)
			{
				_reporting = reports;
				_dotNetRef ??= DotNetObjectReference.Create(this);
				await SafeModuleInvokeVoidAsync(ModulePath, "bindBrowse", _browseKey, _group, _dotNetRef, reports);
			}
			else
			{
				await SafeModuleInvokeVoidAsync(ModulePath, "unbindBrowse", _browseKey);
			}
		}
		else if (browses && reports != _reporting)
		{
			_reporting = reports;
			await SafeModuleInvokeVoidAsync(ModulePath, "reportBrowse", _browseKey, reports);
		}
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		if (_bound)
		{
			_bound = false;
			await SafeModuleInvokeVoidAsync(ModulePath, "unbindBrowse", _browseKey);
		}

		_dotNetRef?.Dispose();
		await base.DisposeAsyncCore();
	}

	private const string ModulePath = "./_content/Moka.Red.Forms/RadioGroup/MokaRadioGroup.razor.js";

	private readonly Dictionary<string, MokaRadioItem<TValue>> _options = new(StringComparer.Ordinal);

	private readonly string _browseKey = Guid.NewGuid().ToString("N");

	private ElementReference _group;

	private DotNetObjectReference<MokaRadioGroup<TValue>>? _dotNetRef;

	private bool _bound;

	private bool _reporting;

	private readonly string _generatedId = $"moka-radio-group-{Guid.NewGuid():N}";

	private string FieldId => string.IsNullOrEmpty(Id) ? _generatedId : Id;

	private string? DescribedBy => MokaAttributes.DescribedBy(AdditionalAttributes,
		MokaFieldWrapper.HelperIdIfShown(FieldId, HelperText, null));

	private IReadOnlyDictionary<string, object>? ControlAttributes => MokaAttributes.WithoutDescribedBy(AdditionalAttributes);
}
