using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Forms.SearchInput;

/// <summary>
///     Enhanced search input with clear button, loading indicator, debounce, and keyboard shortcut support.
/// </summary>
public partial class MokaSearchInput : MokaVisualComponentBase
{
	private readonly string _inputId = $"moka-search-{Guid.NewGuid():N}";
	private Timer? _debounceTimer;
	private string? _pendingValue;

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

	private string ComputedCssClass => new CssBuilder(RootClass)
		.AddClass("moka-search--loading", Loading)
		.AddClass("moka-search--has-value", !string.IsNullOrEmpty(Value))
		.AddClass(Class)
		.Build();

	private string InputCssClass => new CssBuilder("moka-search-input")
		.AddClass($"moka-search-input--{SizeToKebab(Size)}")
		.Build();

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	private async Task HandleInput(ChangeEventArgs e)
	{
		string value = e.Value?.ToString() ?? "";
		Value = value;

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
		Value = "";

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
