using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Theming;
using Moka.Red.Diagnostics.Services;

namespace Moka.Red.Diagnostics.Pages;

/// <summary>
///     Full-page diagnostics view designed to be opened in a separate browser window.
///     Renders all diagnostic panels in a proper layout without app chrome.
/// </summary>
public sealed partial class MokaDiagnosticsPage : ComponentBase
{
	private string _activeTab = "Console";
	private MokaTheme? _theme;

	/// <summary>
	///     The current theme, provided via cascading parameter from the app root.
	/// </summary>
	[CascadingParameter]
	public MokaTheme? Theme { get; set; }

	[Inject] private IMokaDiagnosticsService? _diagnosticsService { get; set; }

	protected override void OnParametersSet() => _theme = Theme;

	private string TabClass(string tab) =>
		_activeTab == tab ? "moka-diag-page-tab moka-diag-page-tab--active" : "moka-diag-page-tab";
}
