namespace Moka.Red.Core.Base;

/// <summary>
///     Hears about the JS interop calls <see cref="MokaComponentBase" /> makes through its interop helpers
///     (<c>GetJsModuleAsync</c>, <c>SafeJsInvokeAsync</c>, <c>SafeModuleInvokeAsync</c> and their void forms),
///     and the ones form inputs make through <see cref="MokaInputBase{TValue}" /> (<c>GetJsModuleAsync</c>
///     and <c>SafeModuleInvokeVoidAsync</c>).
///     Nothing is registered by default. Register an implementation in DI to receive the calls;
///     Moka.Red.Diagnostics registers one that feeds its Network tab.
/// </summary>
/// <remarks>
///     Calls arrive on the component's render thread, once each call has completed. An implementation
///     must not throw, because the component that made the call would see the exception.
/// </remarks>
public interface IMokaJsInteropObserver
{
	/// <summary>Called after a JS interop call completes.</summary>
	/// <param name="identifier">The JS function that was called, or <c>import</c> for a module import.</param>
	/// <param name="duration">How long the call took, including the round trip to the browser.</param>
	void OnJsInteropCompleted(string identifier, TimeSpan duration);
}
