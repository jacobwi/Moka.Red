using Microsoft.AspNetCore.Components;
using Moka.Red.Diagnostics.Services;

namespace Moka.Red.Diagnostics.Components.Panels;

/// <summary>
///     Panel showing whether the diagnostics, toast and dialog services are registered. Component counts are in
///     <see cref="PerformancePanel" /> and GC figures in <see cref="MemoryPanel" />. Registrations don't change while the
///     app runs, so it checks them once and renders once.
/// </summary>
public sealed partial class ServicesPanel : ComponentBase
{
	private List<ServiceCheck> _serviceChecks = [];
	private bool _shown;

	[Inject] private IServiceProvider _serviceProvider { get; set; } = default!;

	[CascadingParameter(Name = DiagnosticsServiceResolver.CascadeName)]
	private IMokaDiagnosticsService? SharedService { get; set; }

	/// <inheritdoc />
	protected override void OnInitialized()
	{
		_shown = DiagnosticsServiceResolver.IsOn(_serviceProvider);
		if (!_shown)
		{
			return;
		}

		_serviceChecks =
		[
			new ServiceCheck("DiagnosticsService", DiagnosticsServiceResolver.Resolve(SharedService, _serviceProvider) is not null),
			new ServiceCheck("ToastService", CheckServiceByName("Moka.Red.Feedback.Toast.IMokaToastService")),
			new ServiceCheck("DialogService", CheckServiceByName("Moka.Red.Feedback.Dialog.IMokaDialogService"))
		];
	}

	private bool CheckServiceByName(string fullTypeName)
	{
		Type? type = AppDomain.CurrentDomain.GetAssemblies()
			.Select(a => a.GetType(fullTypeName))
			.FirstOrDefault(t => t is not null);

		return type is not null && _serviceProvider.GetService(type) is not null;
	}

	private sealed record ServiceCheck(string Name, bool IsRegistered);
}
