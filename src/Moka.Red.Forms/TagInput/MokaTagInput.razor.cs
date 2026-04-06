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
	private string _inputText = string.Empty;
	private bool _showSuggestions;

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

	private string InputCssClass => new CssBuilder("moka-taginput-input")
		.AddClass($"moka-taginput-input--{SizeToKebab(Size)}")
		.Build();

	private bool CanAddMore => !MaxTags.HasValue || Values.Count < MaxTags.Value;

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
				.Where(s => AllowDuplicates || !Values.Contains(s, StringComparer.OrdinalIgnoreCase));
		}
	}

	/// <summary>TagInput has internal state that changes independently of parameters.</summary>
	protected override bool ShouldRender() => true;

	private void HandleInput(ChangeEventArgs e)
	{
		string value = e.Value?.ToString() ?? string.Empty;

		// Check for delimiter in input
		if (!string.IsNullOrEmpty(Delimiter) && value.Contains(Delimiter, StringComparison.Ordinal))
		{
			string[] parts = value.Split(Delimiter,
				StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			foreach (string part in parts)
			{
				TryAddTag(part);
			}

			_inputText = string.Empty;
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
					TryAddTag(_inputText.Trim());
					_inputText = string.Empty;
					_showSuggestions = false;
					await ValuesChanged.InvokeAsync(Values);
				}

				break;

			case "Backspace":
				if (string.IsNullOrEmpty(_inputText) && Values.Count > 0)
				{
					Values.RemoveAt(Values.Count - 1);
					await ValuesChanged.InvokeAsync(Values);
				}

				break;

			case "Escape":
				_showSuggestions = false;
				break;
		}
	}

	private bool TryAddTag(string tag)
	{
		if (string.IsNullOrWhiteSpace(tag))
		{
			return false;
		}

		if (!CanAddMore)
		{
			return false;
		}

		if (!AllowDuplicates && Values.Contains(tag, StringComparer.OrdinalIgnoreCase))
		{
			return false;
		}

		Values.Add(tag);
		return true;
	}

	private async Task RemoveTag(int index)
	{
		if (Disabled)
		{
			return;
		}

		Values.RemoveAt(index);
		await ValuesChanged.InvokeAsync(Values);
	}

	private async Task HandleClearAll()
	{
		Values.Clear();
		_inputText = string.Empty;
		await ValuesChanged.InvokeAsync(Values);
	}

	private async Task SelectSuggestion(string suggestion)
	{
		TryAddTag(suggestion);
		_inputText = string.Empty;
		_showSuggestions = false;
		await ValuesChanged.InvokeAsync(Values);
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
