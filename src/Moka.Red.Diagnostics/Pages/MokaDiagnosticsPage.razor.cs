using System.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Core.Theming;
using Moka.Red.Diagnostics.Services;

namespace Moka.Red.Diagnostics.Pages;

/// <summary>
///     Full-page diagnostics view designed to be opened in a separate browser window.
///     Renders all diagnostic panels in a proper layout without app chrome. Opened from the overlay on
///     Blazor Server, it shows the data of the window that opened it.
/// </summary>
public sealed partial class MokaDiagnosticsPage : ComponentBase
{
	private string _activeTab = "Console";
	private IMokaDiagnosticsService? _diagnosticsService;
	private IMokaDiagnosticsService? _openerService;
	private MokaTheme? _theme;

	/// <summary>
	///     The current theme, provided via cascading parameter from the app root.
	/// </summary>
	[CascadingParameter]
	public MokaTheme? Theme { get; set; }

	// Looked up rather than injected: [Inject] throws for a service that is not registered, even on
	// a nullable property.
	[Inject] private IServiceProvider Services { get; set; } = default!;

	[Inject] private NavigationManager Navigation { get; set; } = default!;

	/// <inheritdoc />
	protected override void OnInitialized()
	{
		// The overlay's new-window button adds ?session=<key>. On Blazor Server that key finds the
		// opener's service; anywhere else nothing is registered under it and the page shows its own.
		string? sessionKey = HttpUtility.ParseQueryString(new Uri(Navigation.Uri).Query)["session"];
		_openerService = Services.GetService<MokaDiagnosticsSessions>()?.Find(sessionKey);
		_diagnosticsService = _openerService ?? Services.GetService<IMokaDiagnosticsService>();
	}

	/// <inheritdoc />
	protected override void OnParametersSet() => _theme = Theme;

	private string TabClass(string tab) =>
		_activeTab == tab ? "moka-diag-page-tab moka-diag-page-tab--active" : "moka-diag-page-tab";
}
