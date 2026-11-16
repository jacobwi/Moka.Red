using System.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Core.Theming;
using Moka.Red.Diagnostics.Services;

namespace Moka.Red.Diagnostics.Pages;

/// <summary>
///     Full-page diagnostics view designed to be opened in a separate browser window.
///     Renders all diagnostic panels in a proper layout without app chrome. Opened from the overlay on
///     Blazor Server, it shows the data and the theme of the window that opened it.
/// </summary>
public sealed partial class MokaDiagnosticsPage : ComponentBase, IDisposable
{
	private string _activeTab = "Console";
	private bool _disposed;
	private IMokaDiagnosticsService? _diagnosticsService;
	private IMokaDiagnosticsService? _openerService;
	private MokaDiagnosticsSession? _session;
	private MokaTheme? _theme;

	/// <summary>
	///     The current theme, provided via cascading parameter from the app root. The Theme tab shows it unless the
	///     window that opened this page shares the theme its overlay inspects.
	/// </summary>
	[CascadingParameter]
	public MokaTheme? Theme { get; set; }

	// Looked up rather than injected: [Inject] throws for a service that is not registered, even on
	// a nullable property.
	[Inject] private IServiceProvider Services { get; set; } = default!;

	[Inject] private NavigationManager Navigation { get; set; } = default!;

	/// <summary>Stops listening to the window that opened this page.</summary>
	public void Dispose()
	{
		_disposed = true;
		if (_session is not null)
		{
			_session.Changed -= HandleSessionChanged;
		}
	}

	/// <inheritdoc />
	protected override void OnInitialized()
	{
		// The overlay's new-window button adds ?session=<key>. On Blazor Server that key finds the
		// opener's service; anywhere else nothing is registered under it and the page shows its own.
		string? sessionKey = HttpUtility.ParseQueryString(new Uri(Navigation.Uri).Query)["session"];
		_session = Services.GetService<MokaDiagnosticsSessions>()?.Find(sessionKey);
		_openerService = _session?.Service;
		_diagnosticsService = _openerService ?? Services.GetService<IMokaDiagnosticsService>();

		if (_session is not null)
		{
			_session.Changed += HandleSessionChanged;
		}
	}

	/// <inheritdoc />
	protected override void OnParametersSet() => _theme = _session?.Theme ?? Theme;

	private void HandleSessionChanged(object? sender, EventArgs e) => _ = ApplySessionAsync();

	// The opener's overlay raises the change on its own circuit's thread; the render goes to this page's.
	private async Task ApplySessionAsync()
	{
		if (_disposed)
		{
			return;
		}

		try
		{
			await InvokeAsync(() =>
			{
				if (_disposed)
				{
					return;
				}

				_theme = _session?.Theme ?? Theme;
				StateHasChanged();
			});
		}
		catch (ObjectDisposedException)
		{
			// The renderer went away between the change and the dispatch.
		}
		catch (Exception ex) when (!_disposed)
		{
			await DispatchExceptionAsync(ex);
		}
	}

	private string TabClass(string tab) =>
		_activeTab == tab ? "moka-diag-page-tab moka-diag-page-tab--active" : "moka-diag-page-tab";

	private string TabPressed(string tab) => _activeTab == tab ? "true" : "false";
}
