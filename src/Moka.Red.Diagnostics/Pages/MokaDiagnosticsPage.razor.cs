using System.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Moka.Red.Core.Base;
using Moka.Red.Core.Theming;
using Moka.Red.Diagnostics.Components;
using Moka.Red.Diagnostics.Services;

namespace Moka.Red.Diagnostics.Pages;

/// <summary>
///     Full-page diagnostics view designed to be opened in a separate browser window.
///     Renders all diagnostic panels in a proper layout without app chrome. Opened from the overlay on
///     Blazor Server, it shows the data and the theme of the window that opened it. It renders nothing outside the
///     Development environment unless <see cref="DiagnosticsOptions.EnableOutsideDevelopment" /> is set.
/// </summary>
public sealed partial class MokaDiagnosticsPage : ComponentBase, IAsyncDisposable
{
	// The tabs and their panel point at each other by id, and an id must be unique in the page.
	private readonly string _idPrefix = $"moka-diag-page-{Guid.NewGuid():N}";
	private readonly string _panelId;
	private string _activeTab = DiagnosticsTabs.Console;
	private bool _disposed;
	private IMokaDiagnosticsService? _diagnosticsService;
	private IJSObjectReference? _module;
	private IMokaDiagnosticsService? _openerService;
	private MokaDiagnosticsSession? _session;
	private bool _shown;
	private IJSObjectReference? _tabList;
	private ElementReference _tabListRef;
	private MokaTheme? _theme;

	/// <summary>Creates the page.</summary>
	public MokaDiagnosticsPage() => _panelId = $"{_idPrefix}-panel";

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

	[Inject] private IJSRuntime JS { get; set; } = default!;

	/// <summary>Stops listening to the window that opened this page and releases the page's script.</summary>
	public async ValueTask DisposeAsync()
	{
		_disposed = true;
		if (_session is not null)
		{
			_session.Changed -= HandleSessionChanged;
		}

		await _tabList.SafeInvokeVoidAsync("dispose");
		await _tabList.SafeDisposeAsync();
		_tabList = null;
		await _module.SafeDisposeAsync();
		_module = null;
	}

	/// <inheritdoc />
	protected override void OnInitialized()
	{
		_shown = DiagnosticsServiceResolver.IsOn(Services);
		if (!_shown)
		{
			return;
		}

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

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender || !_shown)
		{
			return;
		}

		try
		{
			_module = await JS.InvokeAsync<IJSObjectReference>("import", "./_content/Moka.Red.Diagnostics/moka-diagnostics.js");
			_tabList = await _module.InvokeAsync<IJSObjectReference>("bindTabList", _tabListRef);
		}
		catch (Exception ex) when (MokaJsInterop.IsCircuitGone(ex))
		{
		}
	}

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

	private string TabId(string tab) => $"{_idPrefix}-{tab}";

	private string TabClass(string tab) =>
		_activeTab == tab ? "moka-diag-page-tab moka-diag-page-tab--active" : "moka-diag-page-tab";

	private string Selected(string tab) => _activeTab == tab ? "true" : "false";

	// One tab stop for the list, on the tab showing; the arrow keys move between the others.
	private string TabIndex(string tab) => _activeTab == tab ? "0" : "-1";
}
