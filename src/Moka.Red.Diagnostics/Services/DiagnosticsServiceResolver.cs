using Microsoft.Extensions.DependencyInjection;

namespace Moka.Red.Diagnostics.Services;

/// <summary>
///     Finds the diagnostics service a panel reads. <see cref="Pages.MokaDiagnosticsPage" /> cascades the
///     opener's service when it shows another window's data; otherwise the panel uses its own scope's
///     service, or none when <c>AddMokaDiagnostics()</c> was not called.
/// </summary>
internal static class DiagnosticsServiceResolver
{
	/// <summary>Name of the cascading value that carries a shared service.</summary>
	public const string CascadeName = "MokaDiagnosticsService";

	// [Inject] cannot express an optional service: it throws when the type is not registered, even on
	// a nullable property. GetService returns null instead.
	public static IMokaDiagnosticsService? Resolve(IMokaDiagnosticsService? cascaded, IServiceProvider services) =>
		cascaded ?? services.GetService<IMokaDiagnosticsService>();
}
