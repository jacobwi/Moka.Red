using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Forms.SearchInput;

/// <summary>
///     Enhanced search input with clear button, loading indicator, debounce, and keyboard shortcut support.
/// </summary>
public partial class MokaSearchInput : MokaVisualComponentBase
{
	private readonly string _generatedId = $"moka-search-{Guid.NewGuid():N}";
	private ITimer? _debounceTimer;
	private string? _lastValue;
	private int _search;
	private string _value = "";

	// Optional, so looked up rather than injected: [Inject] throws when a service is missing.
	[Inject]
	private IServiceProvider Services { get; set; } = default!;

	// An app can register its own clock. A test does, so no test waits on a real debounce.
	private TimeProvider Clock => Services.GetService<TimeProvider>() ?? TimeProvider.System;

	// Id goes on the input, not a wrapper, so a label's for and getElementById reach the control.
	private string InputId => string.IsNullOrEmpty(Id) ? _generatedId : Id;

	/// <summary>The search value.</summary>
	[Parameter]
	public string Value { get; set; } = "";

	/// <summary>Callback when the value changes.</summary>
	[Parameter]
	public EventCallback<string> ValueChanged { get; set; }

	/// <summary>Label text displayed above the input. With <see cref="HideLabel" /> it names the input without showing.</summary>
	[Parameter]
	public string? Label { get; set; }

	/// <summary>
	///     Keeps <see cref="Label" /> as the input's name for screen readers and hides it from sight, for a search box in
	///     a toolbar. Default false.
	/// </summary>
	[Parameter]
	public bool HideLabel { get; set; }

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

	/// <summary>
	///     Fires after the debounce delay with the text in the box, and at once with an empty string when the clear
	///     button empties it. The clear button drops a search still waiting, and a waiting search reports the box as it
	///     is when the delay ends, so this never reports text the box no longer holds.
	/// </summary>
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

	private string LabelClass => new CssBuilder("moka-search-label")
		.AddClass("moka-visually-hidden", HideLabel)
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

	private async Task HandleInput(string? typed)
	{
		string value = typed ?? "";
		_value = value;

		// Taken before any await, so an older keystroke that resumes late can't schedule over a newer one.
		int search = DropWaitingSearch();

		if (ValueChanged.HasDelegate)
		{
			await ValueChanged.InvokeAsync(value);
		}

		if (search != _search || !OnSearch.HasDelegate)
		{
			return;
		}

		if (Debounce <= 0)
		{
			await OnSearch.InvokeAsync(value);
			return;
		}

		_debounceTimer = Clock.CreateTimer(
			_ => RunDetached(() => SearchAsync(search)),
			null,
			TimeSpan.FromMilliseconds(Debounce),
			Timeout.InfiniteTimeSpan);
	}

	// Disposing a timer doesn't stop a callback that already fired and is waiting for the renderer, so each
	// search carries the number it was scheduled under and only the latest one reports. It reports the box as it
	// is then, which is the parent's value if the parent passed one meanwhile.
	private Task SearchAsync(int search) =>
		search == _search && OnSearch.HasDelegate ? OnSearch.InvokeAsync(_value) : Task.CompletedTask;

	private int DropWaitingSearch()
	{
		_debounceTimer?.Dispose();
		_debounceTimer = null;
		return ++_search;
	}

	private async Task HandleClear()
	{
		_value = "";
		int search = DropWaitingSearch();

		if (ValueChanged.HasDelegate)
		{
			await ValueChanged.InvokeAsync("");
		}

		if (search == _search && OnSearch.HasDelegate)
		{
			await OnSearch.InvokeAsync("");
		}
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		DropWaitingSearch();
		await base.DisposeAsyncCore();
	}
}
