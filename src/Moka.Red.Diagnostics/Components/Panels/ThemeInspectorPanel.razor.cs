using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Moka.Red.Core.Theming;
using Moka.Red.Core.Utilities;
using Moka.Red.Diagnostics.Services;

namespace Moka.Red.Diagnostics.Components.Panels;

/// <summary>
///     Panel that displays all theme tokens with previews and click-to-copy.
/// </summary>
public sealed partial class ThemeInspectorPanel : ComponentBase, IDisposable
{
	private string? _copiedVariable;
	private string? _filterTerm;
	private CancellationTokenSource? _toastCts;
	private IReadOnlyList<ThemeTokenGroup> _tokenGroups = [];

	/// <summary>The current theme to inspect.</summary>
	[Parameter]
	public MokaTheme? Theme { get; set; }

	private IMokaDiagnosticsService? _diagnosticsService;

	[Inject] private IServiceProvider Services { get; set; } = default!;

	[CascadingParameter(Name = DiagnosticsServiceResolver.CascadeName)]
	private IMokaDiagnosticsService? SharedService { get; set; }

	[Inject] private IJSRuntime? _jsRuntime { get; set; }

	private IReadOnlyList<ThemeTokenGroup> FilteredGroups => string.IsNullOrWhiteSpace(_filterTerm)
		? _tokenGroups
		: _tokenGroups
			.Select(g => new ThemeTokenGroup(g.Name,
				g.Tokens.Where(t => t.CssVariable.Contains(_filterTerm, StringComparison.OrdinalIgnoreCase) ||
				                    t.Value.Contains(_filterTerm, StringComparison.OrdinalIgnoreCase)).ToList()))
			.Where(g => g.Tokens.Count > 0)
			.ToList();

	public void Dispose()
	{
		if (_toastCts is not null)
		{
			_toastCts.Cancel();
			_toastCts.Dispose();
		}
	}

	protected override void OnInitialized() => _diagnosticsService = DiagnosticsServiceResolver.Resolve(SharedService, Services);

	protected override void OnParametersSet()
	{
		if (Theme is not null && _diagnosticsService is not null)
		{
			_tokenGroups = _diagnosticsService.GetThemeTokens(Theme);
		}
	}

	private void HandleFilter(ChangeEventArgs e) => _filterTerm = e.Value?.ToString();

	// Token values come from the theme, which anyone can build, and used to go into the style
	// attribute as they were. A value that is not a colour could add declarations of its own or load
	// a url() as a background, so it gets no swatch.
	private static string? SwatchStyle(string value) => CssValues.IsColor(value)
		? new StyleBuilder().AddStyle("background-color", value.Trim()).Build()
		: null;

	// A size can be calc() or var(), so it is only checked for what could end the declaration.
	private static string? RulerStyle(string value) => CssValues.IsSafe(value)
		? new StyleBuilder().AddStyle("width", value).Build()
		: null;

	private async Task CopyToClipboard(string cssVariable)
	{
		string text = $"var({cssVariable})";

		if (_jsRuntime is not null)
		{
			try
			{
				await _jsRuntime.InvokeVoidAsync("navigator.clipboard.writeText", text);
			}
			catch (JSException)
			{
				// Clipboard API may not be available in all contexts.
				return;
			}
		}

		_copiedVariable = cssVariable;
		StateHasChanged();

		// Clear toast after 2 seconds
		if (_toastCts is not null)
		{
			await _toastCts.CancelAsync();
			_toastCts.Dispose();
		}

		_toastCts = new CancellationTokenSource();
		CancellationToken token = _toastCts.Token;

		try
		{
			await Task.Delay(2000, token);

			if (!token.IsCancellationRequested)
			{
				_copiedVariable = null;
				StateHasChanged();
			}
		}
		catch (OperationCanceledException)
		{
			// Expected when a new copy replaces the old toast.
		}
	}
}
