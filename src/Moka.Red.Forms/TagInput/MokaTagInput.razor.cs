using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Forms.TagInput;

/// <summary>
///     A multi-value tag input component. Type text and press Enter to add tags.
///     Tags are displayed as dismissible chips alongside the input.
/// </summary>
public partial class MokaTagInput : MokaVisualComponentBase
{
	private readonly string _generatedId = $"moka-taginput-{Guid.NewGuid():N}";
	private string _inputText = string.Empty;
	private bool _showSuggestions;

	// The tags on screen. Changes build a new list for ValuesChanged instead of editing the caller's:
	// editing it threw on arrays and read-only lists, and a parent handed back its own instance
	// could not tell that anything had changed.
	private List<string> _tags = [];

	// What Values held when it was last read. Comparing contents rather than the reference also
	// picks up a parent that edits its own list in place and re-renders.
	private List<string> _seen = [];

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

	private bool CanAddMore => !MaxTags.HasValue || _tags.Count < MaxTags.Value;

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
				.Where(s => AllowDuplicates || !_tags.Contains(s, StringComparer.OrdinalIgnoreCase));
		}
	}

	/// <summary>TagInput has internal state that changes independently of parameters.</summary>
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		IList<string> values = Values ?? [];
		if (!values.SequenceEqual(_seen))
		{
			_seen = [.. values];
			_tags = [.. values];
		}
	}

	private async Task HandleInput(ChangeEventArgs e)
	{
		string value = e.Value?.ToString() ?? string.Empty;

		// Check for delimiter in input
		if (!string.IsNullOrEmpty(Delimiter) && value.Contains(Delimiter, StringComparison.Ordinal))
		{
			string[] parts = value.Split(Delimiter,
				StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			List<string> next = [.. _tags];
			foreach (string part in parts)
			{
				TryAddTag(next, part);
			}

			_inputText = string.Empty;
			// Tags typed with the delimiter were added without ValuesChanged ever being raised.
			await CommitAsync(next);
		}
		else
		{
			_inputText = value;
		}

		_showSuggestions = Suggestions is not null && !string.IsNullOrWhiteSpace(_inputText);
	}

	private async Task HandleKeyDown(KeyboardEventArgs e)
	{
		switch (e.Key)
		{
			case "Enter":
				if (!string.IsNullOrWhiteSpace(_inputText))
				{
					List<string> next = [.. _tags];
					TryAddTag(next, _inputText.Trim());
					_inputText = string.Empty;
					_showSuggestions = false;
					await CommitAsync(next);
				}

				break;

			case "Backspace":
				if (string.IsNullOrEmpty(_inputText) && _tags.Count > 0)
				{
					await CommitAsync(_tags[..^1]);
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

	private async Task RemoveTag(int index)
	{
		if (Disabled)
		{
			return;
		}

		List<string> next = [.. _tags];
		next.RemoveAt(index);
		await CommitAsync(next);
	}

	private async Task HandleClearAll()
	{
		_inputText = string.Empty;
		await CommitAsync([]);
	}

	private async Task SelectSuggestion(string suggestion)
	{
		List<string> next = [.. _tags];
		TryAddTag(next, suggestion);
		_inputText = string.Empty;
		_showSuggestions = false;
		await CommitAsync(next);
	}

	// Only a real change is reported, and always as a new list.
	private async Task CommitAsync(List<string> next)
	{
		if (next.SequenceEqual(_tags))
		{
			return;
		}

		_tags = next;
		await ValuesChanged.InvokeAsync(next);
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
}
