using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Moka.Red.Core.Theming;
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

	[Inject] private IMokaDiagnosticsService? _diagnosticsService { get; set; }

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

	protected override void OnParametersSet()
	{
		if (Theme is not null && _diagnosticsService is not null)
		{
			_tokenGroups = _diagnosticsService.GetThemeTokens(Theme);
		}
	}

	private void HandleFilter(ChangeEventArgs e) => _filterTerm = e.Value?.ToString();

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
