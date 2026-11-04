using Microsoft.JSInterop;

namespace Moka.Red.Core.Base;

/// <summary>
///     JS interop for code that may run after the browser has gone: a component disposed as its
///     Blazor Server circuit ends, or a call still out when the tab closes or reloads. Blazor
///     disposes every component of a circuit that has ended, and a JS call made then fails, either
///     with <see cref="JSDisconnectedException" /> or, for a call already on its way, with a
///     <see cref="TaskCanceledException" />. Neither is a fault of the component, but unhandled in a
///     disposal both are logged as errors of the circuit.
/// </summary>
public static class MokaJsInterop
{
	/// <summary>
	///     Whether an exception from a JS interop call means only that the browser side is gone: the
	///     circuit disconnected (<see cref="JSDisconnectedException" />), the call was cancelled as the
	///     circuit ended (<see cref="OperationCanceledException" />, which covers
	///     <see cref="TaskCanceledException" />), or the JS runtime or the object reference was already
	///     disposed (<see cref="ObjectDisposedException" />). An error thrown by the script itself
	///     (<see cref="JSException" />) is not one of them.
	/// </summary>
	/// <param name="exception">The exception a JS interop call threw.</param>
	/// <returns><see langword="true" /> when the call failed only because the browser side is gone.</returns>
	public static bool IsCircuitGone(Exception exception) =>
		exception is JSDisconnectedException or OperationCanceledException or ObjectDisposedException;

	/// <summary>
	///     Disposes a JS object reference, such as an imported module, ignoring the failures
	///     <see cref="IsCircuitGone" /> names. Does nothing for <see langword="null" />.
	/// </summary>
	/// <param name="reference">The reference to dispose.</param>
	/// <returns>A task that completes when the reference is disposed or the browser side is known to be gone.</returns>
	public static async ValueTask SafeDisposeAsync(this IJSObjectReference? reference)
	{
		if (reference is null)
		{
			return;
		}

		try
		{
			await reference.DisposeAsync();
		}
		catch (Exception ex) when (IsCircuitGone(ex))
		{
		}
	}

	/// <summary>
	///     Invokes a void function on a JS object reference, ignoring the failures
	///     <see cref="IsCircuitGone" /> names. Does nothing for <see langword="null" />.
	/// </summary>
	/// <param name="reference">The reference to call, such as an imported module.</param>
	/// <param name="identifier">The name of the function to call.</param>
	/// <param name="args">The arguments to pass.</param>
	/// <returns>
	///     <see langword="true" /> when the call completed; <see langword="false" /> when there was no
	///     reference or the browser side was gone.
	/// </returns>
	public static async ValueTask<bool> SafeInvokeVoidAsync(this IJSObjectReference? reference, string identifier,
		params object?[]? args)
	{
		if (reference is null)
		{
			return false;
		}

		try
		{
			await reference.InvokeVoidAsync(identifier, args);
			return true;
		}
		catch (Exception ex) when (IsCircuitGone(ex))
		{
			return false;
		}
	}
}
