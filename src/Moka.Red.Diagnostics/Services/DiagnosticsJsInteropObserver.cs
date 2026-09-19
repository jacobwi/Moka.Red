using Moka.Red.Core.Base;

namespace Moka.Red.Diagnostics.Services;

/// <summary>
///     Passes the JS interop calls <see cref="MokaComponentBase" /> and <see cref="MokaInputBase{TValue}" />
///     report on to <see cref="IMokaDiagnosticsService.RecordJsInteropCall" />, which fills the Network tab.
/// </summary>
internal sealed class DiagnosticsJsInteropObserver(IMokaDiagnosticsService diagnostics) : IMokaJsInteropObserver
{
	public void OnJsInteropCompleted(string identifier, TimeSpan duration) =>
		diagnostics.RecordJsInteropCall(identifier, duration);
}
