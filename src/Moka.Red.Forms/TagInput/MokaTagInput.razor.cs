using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;
using Moka.Red.Forms.Common;

namespace Moka.Red.Forms.TagInput;

/// <summary>
///     A multi-value tag input component. Type text and press Enter to add tags.
///     Tags are displayed as dismissible chips alongside the input.
/// </summary>
public partial class MokaTagInput : MokaVisualComponentBase
{
	private const string PopupModule = "./_content/Moka.Red.Core/moka-popup.js";

	private readonly string _generatedId = $"moka-taginput-{Guid.NewGuid():N}";
	private string _inputText = string.Empty;
	private ElementReference _panel;
	private bool _panelShown;
	private ElementReference _root;
	private bool _showSuggestions;

	// The tags on screen. Changes build a new list for ValuesChanged instead of editing the caller's:
	// editing it threw on arrays and read-only lists, and a parent handed back its own instance
	// could not tell that anything had changed.
	private readonly MokaInputValue<IList<string>> _input;

	// What Values held when it was last read. Comparing contents rather than the reference also
	// picks up a parent that edits its own list in place and re-renders.
	private List<string> _seen = [];

	/// <summary>Creates the tag input. It shows its own tags until the parent passes a list.</summary>
	public MokaTagInput() => _input = new MokaInputValue<IList<string>>(StateHasChanged, DispatchExceptionAsync, []);

	private IList<string> Tags => _input.Shown ?? [];

	// The consumer's Id names the text input, and the label's for follows it. Once MaxTags is reached
	// the input is gone, and so is the label's for, which would point at nothing.
	private string InputId => string.IsNullOrEmpty(Id) ? _generatedId : Id;

	private string? LabelFor => CanAddMore ? InputId : null;

	/// <summary>The current list of tags. Two-way bindable.</summary>
	[Parameter]
	[SuppressMessage("Usage", "CA2227:Collection properties should be read only",
		Justification = "Blazor two-way binding requires a setter.")]
	public IList<string> Values { get; set; } = [];

	/// <summary>Callback invoked when <see cref="Values" /> changes.</summary>
	[Parameter]
	public EventCallback<IList<string>> ValuesChanged { get; set; }

	/// <summary>Label text displayed above the input.</summary>
	[Parameter]
	public string? Label { get; set; }

	/// <summary>Helper text displayed below the input.</summary>
	[Parameter]
	public string? HelperText { get; set; }

	/// <summary>
	///     Content beside the field's box, in its row, such as a button that acts on the value. The label stays above
	///     and the helper text below, and on the new design a button here is as tall as the box.
	/// </summary>
	[Parameter]
	public RenderFragment? Actions { get; set; }

	/// <summary>Error text displayed below the input when in error state.</summary>
	[Parameter]
	public string? ErrorText { get; set; }

	/// <summary>Placeholder text for the input. Default "Add tag...".</summary>
	[Parameter]
	public string Placeholder { get; set; } = "Add tag...";

	/// <summary>Maximum number of tags. Null = unlimited.</summary>
	[Parameter]
	public int? MaxTags { get; set; }

	/// <summary>Whether to allow duplicate tags. Default false.</summary>
	[Parameter]
	public bool AllowDuplicates { get; set; }

	/// <summary>Delimiter character that triggers tag creation. Default ",".</summary>
	[Parameter]
	public string Delimiter { get; set; } = ",";

	/// <summary>Whether to show a clear-all button. Default true.</summary>
	[Parameter]
	public bool Clearable { get; set; } = true;

	/// <summary>Autocomplete suggestions shown in a dropdown on focus.</summary>
	[Parameter]
	public IReadOnlyList<string>? Suggestions { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-taginput";

	private bool HasError => !string.IsNullOrEmpty(ErrorText);

	private string ComputedCssClass => new CssBuilder(RootClass)
		.AddClass("moka-taginput--error", HasError)
		.AddClass("moka-taginput--disabled", Disabled)
		.AddClass(Class)
		.Build();

	// The field wrapper is the outermost element, so the margin goes there. The tag container draws
	// the field's border, so it takes the padding and the radius.

	/// <inheritdoc />
	protected override string? CssStyle => Style;

	private string? WrapperStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin)
		.Build();

	private string? ContainerStyle => new StyleBuilder()
		.AddStyle("padding", ResolvedPadding)
		.AddStyle("border-radius", ResolvedRounding)
		.Build();

	private string InputCssClass => new CssBuilder("moka-taginput-input")
		.AddClass($"moka-taginput-input--{SizeToKebab(Size)}")
		.Build();

	private bool CanAddMore => !MaxTags.HasValue || Tags.Count < MaxTags.Value;

	private IEnumerable<string> FilteredSuggestions
	{
		get
		{
			if (Suggestions is null || string.IsNullOrWhiteSpace(_inputText))
			{
				return [];
			}

			return Suggestions
				.Where(s => s.Contains(_inputText, StringComparison.OrdinalIgnoreCase))
				.Where(s => AllowDuplicates || !Tags.Contains(s, StringComparer.OrdinalIgnoreCase));
		}
	}

	/// <summary>TagInput has internal state that changes independently of parameters.</summary>
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		// The suggestions go to the top layer, out of reach of an ancestor's overflow and of later stacking contexts.
		bool shown = SuggestionsShown;
		if (_panelShown != shown)
		{
			_panelShown = shown;
			if (shown)
			{
				await SafeModuleInvokeVoidAsync(PopupModule, "open", _generatedId, _panel, _root, null,
					new Dictionary<string, object?> { ["placement"] = "bottom-start", ["width"] = "match" });
			}
			else
			{
				await SafeModuleInvokeVoidAsync(PopupModule, "close", _generatedId);
			}
		}
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		if (_panelShown)
		{
			_panelShown = false;
			await SafeModuleInvokeVoidAsync(PopupModule, "close", _generatedId);
		}

		_input.Stop();
		await base.DisposeAsyncCore();
	}

	/// <inheritdoc />
	public override Task SetParametersAsync(ParameterView parameters)
	{
		if (parameters.TryGetValue(nameof(Values), out IList<string>? values))
		{
			IList<string> given = values ?? [];
			if (!given.SequenceEqual(_seen))
			{
				_seen = [.. given];
			}

			_input.Take(_seen);
		}

		return base.SetParametersAsync(parameters);
	}

	/// <inheritdoc />
	protected override void OnAfterRender(bool firstRender)
	{
		base.OnAfterRender(firstRender);
		_input.AfterRender();
	}

	private void HandleInput(string? typed)
	{
		string value = typed ?? string.Empty;

		// Check for delimiter in input
		if (!string.IsNullOrEmpty(Delimiter) && value.Contains(Delimiter, StringComparison.Ordinal))
		{
			string[] parts = value.Split(Delimiter,
				StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			List<string> next = [.. Tags];
			foreach (string part in parts)
			{
				TryAddTag(next, part);
			}

			_inputText = string.Empty;
			// Tags typed with the delimiter were added without ValuesChanged ever being raised.
			Commit(next);
		}
		else
		{
			_inputText = value;
		}

		_showSuggestions = Suggestions is not null && !string.IsNullOrWhiteSpace(_inputText);
	}

	private void HandleKeyDown(KeyboardEventArgs e)
	{
		switch (e.Key)
		{
			case "Enter":
				if (!string.IsNullOrWhiteSpace(_inputText))
				{
					List<string> next = [.. Tags];
					TryAddTag(next, _inputText.Trim());
					_inputText = string.Empty;
					_showSuggestions = false;
					Commit(next);
				}

				break;

			case "Backspace":
				if (string.IsNullOrEmpty(_inputText) && Tags.Count > 0)
				{
					Commit([.. Tags.Take(Tags.Count - 1)]);
				}

				break;

			case "Escape":
				_showSuggestions = false;
				break;
		}
	}

	// While suggestions show, Escape closes them and stops there, so a MokaDialog around the input
	// does not close on the same key.
	private bool SuggestionsShown => _showSuggestions && FilteredSuggestions.Any();

	private bool TryAddTag(List<string> tags, string tag)
	{
		if (string.IsNullOrWhiteSpace(tag))
		{
			return false;
		}

		if (MaxTags.HasValue && tags.Count >= MaxTags.Value)
		{
			return false;
		}

		if (!AllowDuplicates && tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
		{
			return false;
		}

		tags.Add(tag);
		return true;
	}

	private void RemoveTag(int index)
	{
		if (Disabled)
		{
			return;
		}

		List<string> next = [.. Tags];
		next.RemoveAt(index);
		Commit(next);
	}

	private void HandleClearAll()
	{
		_inputText = string.Empty;
		Commit([]);
	}

	private void SelectSuggestion(string suggestion)
	{
		List<string> next = [.. Tags];
		TryAddTag(next, suggestion);
		_inputText = string.Empty;
		_showSuggestions = false;
		Commit(next);
	}

	// Only a real change is reported, and always as a new list.
	private void Commit(List<string> next)
	{
		if (!next.SequenceEqual(Tags))
		{
			_input.Pick(next, ValuesChanged);
		}
	}

	private void HandleFocus()
	{
		if (Suggestions is not null && !string.IsNullOrWhiteSpace(_inputText))
		{
			_showSuggestions = true;
		}
	}

	private void HandleBlur()
	{
		// Delay to allow suggestion click to fire
		_showSuggestions = false;
	}
	private string? DescribedBy => MokaAttributes.DescribedBy(AdditionalAttributes,
		MokaFieldWrapper.HelperIdIfShown(LabelFor, HelperText, ErrorText));

	private IReadOnlyDictionary<string, object>? ControlAttributes => MokaAttributes.WithoutDescribedBy(AdditionalAttributes);
}
