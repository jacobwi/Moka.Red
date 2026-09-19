using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Forms.SearchInput;

/// <summary>
///     Enhanced search input with clear button, loading indicator, debounce, and keyboard shortcut support.
/// </summary>
public partial class MokaSearchInput : MokaVisualComponentBase
{
	private readonly string _generatedId = $"moka-search-{Guid.NewGuid():N}";
	private Timer? _debounceTimer;
	private string? _lastValue;
	private string? _pendingValue;
	private string _value = "";

	// Id goes on the input, not a wrapper, so a label's for and getElementById reach the control.
	private string InputId => string.IsNullOrEmpty(Id) ? _generatedId : Id;

	/// <summary>The search value.</summary>
	[Parameter]
	public string Value { get; set; } = "";

	/// <summary>Callback when the value changes.</summary>
	[Parameter]
	public EventCallback<string> ValueChanged { get; set; }

	/// <summary>Label text displayed above the input.</summary>
	[Parameter]
	public string? Label { get; set; }

	/// <summary>Placeholder text. Default "Search...".</summary>
	[Parameter]
	public string Placeholder { get; set; } = "Search...";

	/// <summary>Debounce delay in milliseconds for OnSearch. Default 300ms.</summary>
	[Parameter]
	public int Debounce { get; set; } = 300;

	/// <summary>Shows a spinner when true. Default false.</summary>
	[Parameter]
	public bool Loading { get; set; }

	/// <summary>Whether to show a clear button. Default true.</summary>
	[Parameter]
	public bool Clearable { get; set; } = true;

	/// <summary>Fires after debounce delay with the search value.</summary>
	[Parameter]
	public EventCallback<string> OnSearch { get; set; }

	/// <summary>Shows "/" shortcut hint badge. Default false.</summary>
	[Parameter]
	public bool ShowShortcut { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-search";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-search--loading", Loading)
		.AddClass("moka-search--has-value", !string.IsNullOrEmpty(_value))
		.AddClass(Class)
		.Build();

	// The root is the outermost element, so the margin goes there. The input draws the field's
	// border, so it takes the padding and the radius.

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin)
		.AddStyle(Style)
		.Build();

	private string? InputStyle => new StyleBuilder()
		.AddStyle("padding", ResolvedPadding)
		.AddStyle("border-radius", ResolvedRounding)
		.Build();

	private string InputCssClass => new CssBuilder("moka-search-input")
		.AddClass($"moka-search-input--{SizeToKebab(Size)}")
		.Build();

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		// Value only seeds the box when the parent passes a new value. Copying it on every parent
		// render wiped the typed text of an unbound box, for example when OnSearch set Loading.
		if (!string.Equals(_lastValue, Value, StringComparison.Ordinal))
		{
			_lastValue = Value;
			_value = Value ?? "";
		}
	}

	private async Task HandleInput(ChangeEventArgs e)
	{
		string value = e.Value?.ToString() ?? "";
		_value = value;

		if (ValueChanged.HasDelegate)
		{
			await ValueChanged.InvokeAsync(value);
		}

		// Debounced search
		if (OnSearch.HasDelegate)
		{
			_pendingValue = value;
			if (_debounceTimer is not null)
			{
				await _debounceTimer.DisposeAsync();
			}

			if (Debounce <= 0)
			{
				await OnSearch.InvokeAsync(value);
			}
			else
			{
				_debounceTimer = new Timer(
					_ => InvokeAsync(async () =>
					{
						if (OnSearch.HasDelegate)
						{
							await OnSearch.InvokeAsync(_pendingValue ?? "");
						}
					}),
					null,
					Debounce,
					Timeout.Infinite);
			}
		}
	}

	private async Task HandleClear()
	{
		_value = "";

		if (ValueChanged.HasDelegate)
		{
			await ValueChanged.InvokeAsync("");
		}

		if (OnSearch.HasDelegate)
		{
			await OnSearch.InvokeAsync("");
		}
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		if (_debounceTimer is not null)
		{
			await _debounceTimer.DisposeAsync();
			_debounceTimer = null;
		}

		await base.DisposeAsyncCore();
	}
}
