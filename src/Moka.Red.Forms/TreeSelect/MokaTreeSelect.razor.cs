using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Theming;
using Moka.Red.Core.Utilities;
using Moka.Red.Forms.Common;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Moka.Red.Forms.TreeSelect;

/// <summary>
///     A dropdown that displays a tree hierarchy for selection, suitable for
///     folder pickers, category selectors, and other hierarchical data.
///     Supports single and multiple selection modes with optional search filtering.
/// </summary>
/// <typeparam name="TValue">The type of the selectable value.</typeparam>
public partial class MokaTreeSelect<TValue> : MokaVisualComponentBase
{
	// What the select shows, apart from Value and SelectedValues: a pick shows before the parent answers, and a
	// re-render that passes the old values cannot undo it (gotcha #9). Two lists with the same values are the same.
	private readonly MokaInputValue<IReadOnlyList<TValue>?> _selected;
	private readonly MokaInputValue<TValue?> _value;

	private const string PopupModule = "./_content/Moka.Red.Core/moka-popup.js";

	private readonly HashSet<int> _expandedNodes = [];
	private readonly string _generatedId = $"moka-tree-select-{Guid.NewGuid():N}";
	private ElementReference _backdrop;
	private bool _isOpen;
	private ElementReference _panel;
	private bool _panelShown;
	private bool _watchingSide;
	private ElementReference _root;
	private string _searchText = string.Empty;

	/// <summary>Creates the select. It shows its own choice until the parent passes one.</summary>
	public MokaTreeSelect()
	{
		_value = new MokaInputValue<TValue?>(StateHasChanged, DispatchExceptionAsync);
		_selected = new MokaInputValue<IReadOnlyList<TValue>?>(StateHasChanged, DispatchExceptionAsync,
			comparer: MokaSequenceComparer<TValue>.Default);
	}

	/// <summary>The currently selected value in single-selection mode.</summary>
	[Parameter]
	public TValue? Value { get; set; }

	/// <summary>Callback invoked when the selected value changes.</summary>
	[Parameter]
	public EventCallback<TValue?> ValueChanged { get; set; }

	/// <summary>The hierarchical items to display in the tree.</summary>
	[Parameter]
	public IReadOnlyList<MokaTreeSelectItem<TValue>>? Items { get; set; }

	/// <summary>Placeholder text shown when no value is selected.</summary>
	[Parameter]
	public string? Placeholder { get; set; }

	/// <summary>Label text displayed above the dropdown.</summary>
	[Parameter]
	public string? Label { get; set; }

	/// <summary>Whether the dropdown includes a search/filter input.</summary>
	[Parameter]
	public bool Searchable { get; set; }

	/// <summary>Whether multiple items can be selected simultaneously.</summary>
	[Parameter]
	public bool Multiple { get; set; }

	/// <summary>The selected values when <see cref="Multiple" /> is true.</summary>
	[Parameter]
	public IReadOnlyList<TValue>? SelectedValues { get; set; }

	/// <summary>Callback invoked when the selected values change in multiple mode.</summary>
	[Parameter]
	public EventCallback<IReadOnlyList<TValue>?> SelectedValuesChanged { get; set; }

	/// <summary>
	///     How the rows of the open tree are drawn. Null takes the theme's choice (<see cref="MokaTheme.WithLists" />),
	///     which is <see cref="MokaListLook.Standard" /> unless the app set one. The box around the rows follows the theme's
	///     <see cref="MokaTheme.WithPopups" />. Looks belong to the new design: on <see cref="MokaTheme.Dark" /> and
	///     <see cref="MokaTheme.Light" /> the tree stays as it is, whatever this says.
	/// </summary>
	[Parameter]
	public MokaListLook? Look { get; set; }

	/// <summary>The word after Tab in the key hints <see cref="MokaListLook.Compact" /> draws. Default "move".</summary>
	[Parameter]
	public string MoveHintText { get; set; } = "move";

	/// <summary>The word after Enter in the key hints <see cref="MokaListLook.Compact" /> draws. Default "pick".</summary>
	[Parameter]
	public string PickHintText { get; set; } = "pick";

	/// <summary>The word after Esc in the key hints <see cref="MokaListLook.Compact" /> draws. Default "close".</summary>
	[Parameter]
	public string CloseHintText { get; set; } = "close";

	[CascadingParameter]
	private MokaTheme? CascadedTheme { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-tree-select";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-tree-select--{SizeToKebab(Size)}")
		.AddClass("moka-tree-select--open", _isOpen)
		.AddClass("moka-tree-select--disabled", Disabled)
		.AddClass("moka-tree-select--multiple", Multiple)
		.AddClass(MokaListLooks.RootClass(RootClass, ListLook))
		.AddClass(Class)
		.Build();

	private MokaListLook ListLook => MokaListLooks.Resolve(Look, CascadedTheme);

	private bool DrawsMark => MokaListLooks.DrawsMark(ListLook);

	// The original look marks a chosen row with a check only in a multiple select.
	private bool EndsWithCheck => ListLook == MokaListLook.Standard ? Multiple : MokaListLooks.DrawsEndCheck(ListLook);

	private bool DrawsHints => ListLook == MokaListLook.Compact && Items is not null && Items.Any(MatchesSearch);

	private bool Attached => ListLook == MokaListLook.Attached;

	private string MarkCssClass(bool on) => new CssBuilder("moka-tree-select-mark")
		.AddClass("moka-tree-select-mark--on", on)
		.AddClass("moka-tree-select-mark--box", Multiple)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin)
		.AddStyle(Style)
		.Build();

	// Id goes on the trigger button, the control, and the label's id is built from it.
	private string TriggerId => string.IsNullOrEmpty(Id) ? _generatedId : Id;

	private string? LabelId => Label is null ? null : MokaFieldWrapper.LabelIdFor(TriggerId);

	// A button's name replaces its content, so the label alone would hide the current choice from
	// screen readers. The button's own id after the label's adds it: "Department Engineering".
	private string? LabelledBy => Label is null ? null : $"{LabelId} {TriggerId}";

	// With a label, the label names the button and a consumer aria-label is left out, as on MokaSelect.
	private IReadOnlyDictionary<string, object>? TriggerAttributes =>
		Label is null ? AdditionalAttributes : MokaAttributes.Without(AdditionalAttributes, "aria-label");

	// The root also holds the label and the dropdown. The trigger draws the field's border, so it
	// takes the padding and the radius.
	private string? TriggerStyle => new StyleBuilder()
		.AddStyle("padding", ResolvedPadding)
		.AddStyle("border-radius", ResolvedRounding)
		.Build();

	private TValue? Chosen => _value.Shown;

	private IReadOnlyList<TValue> ChosenList => _selected.Shown ?? [];

	private string DisplayText
	{
		get
		{
			if (Multiple && ChosenList.Count > 0)
			{
				return $"{ChosenList.Count} selected";
			}

			if (Chosen is { } chosen)
			{
				MokaTreeSelectItem<TValue>? item = FindItem(Items, chosen);
				return item?.Text ?? chosen.ToString() ?? string.Empty;
			}

			return Placeholder ?? string.Empty;
		}
	}

	private bool HasValue => Multiple ? ChosenList.Count > 0 : Chosen is not null;

	/// <summary>Tree select has internal open/expand/search state that changes independently of parameters.</summary>
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		// The tree goes to the top layer, out of reach of an ancestor's overflow and of later stacking contexts.
		if (_panelShown != _isOpen)
		{
			_panelShown = _isOpen;
			if (_isOpen)
			{
				// The root holds the label too, so a list flipped above the root would sit above the label. An attached
				// list has to meet the field itself.
				ElementReference anchor = Attached ? _triggerRef : _root;
				await SafeModuleInvokeVoidAsync(PopupModule, "open", _generatedId, _panel, anchor, _backdrop,
					new Dictionary<string, object?> { ["placement"] = "bottom-start", ["width"] = "match" });
			}
			else
			{
				await SafeModuleInvokeVoidAsync(PopupModule, "close", _generatedId);
			}
		}

		bool watchSide = _isOpen && Attached;
		if (_watchingSide != watchSide)
		{
			_watchingSide = watchSide;
			if (watchSide)
			{
				await SafeModuleInvokeVoidAsync(MokaListLooks.SideModule, "watch", _generatedId, _root, _triggerRef, _panel);
			}
			else
			{
				await SafeModuleInvokeVoidAsync(MokaListLooks.SideModule, "unwatch", _generatedId);
			}
		}
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		if (_watchingSide)
		{
			_watchingSide = false;
			await SafeModuleInvokeVoidAsync(MokaListLooks.SideModule, "unwatch", _generatedId);
		}

		if (_panelShown)
		{
			_panelShown = false;
			await SafeModuleInvokeVoidAsync(PopupModule, "close", _generatedId);
		}

		_value.Stop();
		_selected.Stop();
		await base.DisposeAsyncCore();
	}

	/// <inheritdoc />
	public override Task SetParametersAsync(ParameterView parameters)
	{
		_value.Take(parameters, nameof(Value));
		_selected.Take(parameters, nameof(SelectedValues));
		return base.SetParametersAsync(parameters);
	}

	/// <inheritdoc />
	protected override void OnAfterRender(bool firstRender)
	{
		base.OnAfterRender(firstRender);
		_value.AfterRender();
		_selected.AfterRender();
	}

	private void ToggleDropdown()
	{
		if (Disabled)
		{
			return;
		}

		_isOpen = !_isOpen;
		if (!_isOpen)
		{
			_searchText = string.Empty;
		}
	}

	private void CloseDropdown()
	{
		_isOpen = false;
		_searchText = string.Empty;
	}

	private void SelectItem(MokaTreeSelectItem<TValue> item)
	{
		if (item.Disabled)
		{
			return;
		}

		if (Multiple)
		{
			List<TValue> current = [.. ChosenList];
			if (current.Any(v => EqualityComparer<TValue>.Default.Equals(v, item.Value)))
			{
				current.RemoveAll(v => EqualityComparer<TValue>.Default.Equals(v, item.Value));
			}
			else
			{
				current.Add(item.Value);
			}

			_selected.Pick(current, SelectedValuesChanged);
		}
		else
		{
			CloseDropdown();
			if (!EqualityComparer<TValue>.Default.Equals(Chosen, item.Value))
			{
				_value.Pick(item.Value, ValueChanged);
			}
		}
	}

	private void ToggleExpand(int hashCode)
	{
		if (!_expandedNodes.Remove(hashCode))
		{
			_expandedNodes.Add(hashCode);
		}
	}

	private bool IsExpanded(int hashCode) => _expandedNodes.Contains(hashCode);

	private bool IsSelected(TValue value)
	{
		if (Multiple)
		{
			return ChosenList.Any(v => EqualityComparer<TValue>.Default.Equals(v, value));
		}

		return EqualityComparer<TValue>.Default.Equals(Chosen, value);

	}

	private bool MatchesSearch(MokaTreeSelectItem<TValue> item)
	{
		if (string.IsNullOrWhiteSpace(_searchText))
		{
			return true;
		}

		if (item.Text.Contains(_searchText, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}

		return item.Children?.Any(MatchesSearch) ?? false;
	}

	private static MokaTreeSelectItem<TValue>? FindItem(IReadOnlyList<MokaTreeSelectItem<TValue>>? items, TValue value)
	{
		if (items is null)
		{
			return null;
		}

		foreach (MokaTreeSelectItem<TValue> item in items)
		{
			if (EqualityComparer<TValue>.Default.Equals(item.Value, value))
			{
				return item;
			}

			if (item.HasChildren)
			{
				MokaTreeSelectItem<TValue>? found = FindItem(item.Children, value);
				if (found is not null)
				{
					return found;
				}
			}
		}

		return null;
	}

	private void OnSearchInput(string? typed) => _searchText = typed ?? string.Empty;

	private ElementReference _triggerRef;

	// Escape closes the popup and puts focus back on the field, since the control that had it may
	// have gone with the popup. While open, keys stop at the picker (see the markup), so a MokaDialog
	// around it does not close on the same Escape.
	private async Task HandlePickerKeyDown(KeyboardEventArgs e)
	{
		if (e.Key != "Escape" || !_isOpen)
		{
			return;
		}

		CloseDropdown();
		await FocusTriggerAsync();
	}

	private async Task FocusTriggerAsync()
	{
		try
		{
			await _triggerRef.FocusAsync();
		}
		catch (Exception ex) when (MokaJsInterop.IsCircuitGone(ex))
		{
		}
		catch (InvalidOperationException)
		{
			// Not interactive, or the element is gone.
		}
	}
}
