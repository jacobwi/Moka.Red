using Microsoft.JSInterop;

namespace Moka.Red.Core.Base;

/// <summary>
///     JS interop for code that may run after the browser has gone: a component disposed as its
///     Blazor Server circuit ends, or a call still out when the tab closes or reloads. Blazor
///     disposes every component of a circuit that has ended, and a JS call made then fails, either
///     with <see cref="JSDisconnectedException" /> or, for a call already on its way, with a
///     <see cref="TaskCanceledException" />. A BlazorWebView (MAUI, WPF, Windows Forms) that
///     reloads its page disposes the old page's components after the new page has loaded, and a
///     call on one of the old page's JS objects reaches a page that never had it and fails with a
///     <see cref="JSException" />. None of these is a fault of the component, but unhandled in a
///     disposal each is logged as an error, and on a BlazorWebView it reaches the app.
/// </summary>
public static class MokaJsInterop
{
	private const string MissingObjectStart = "JS object instance with ID ";
	private const string MissingObjectEnd = " does not exist (has it been disposed?)";

	/// <summary>
	///     Whether an exception from a JS interop call means only that the browser side is gone: the
	///     circuit disconnected (<see cref="JSDisconnectedException" />), the call was cancelled as the
	///     circuit ended (<see cref="OperationCanceledException" />, which covers
	///     <see cref="TaskCanceledException" />), the JS runtime or the object reference was already
	///     disposed (<see cref="ObjectDisposedException" />), or the page no longer holds the JS object the
	///     call was made on, as after a BlazorWebView reload (a <see cref="JSException" /> saying the
	///     object instance does not exist). Any other error thrown by the script itself is not one of them.
	/// </summary>
	/// <param name="exception">The exception a JS interop call threw.</param>
	/// <returns><see langword="true" /> when the call failed only because the browser side is gone.</returns>
	public static bool IsCircuitGone(Exception exception) =>
		exception is JSDisconnectedException or OperationCanceledException or ObjectDisposedException
		|| (exception is JSException script && IsMissingObject(script.Message));

	// Blazor's JS side answers a call on an object id it doesn't hold with this text, the same in .NET 9 and 10, then
	// the stack. The exception carries nothing else to tell it by. A reloaded BlazorWebView page is where it happens:
	// the old page's disposals reach the new page over the same channel.
	private static bool IsMissingObject(string? message)
	{
		if (message is null || !message.StartsWith(MissingObjectStart, StringComparison.Ordinal))
		{
			return false;
		}

		int end = message.IndexOf(MissingObjectEnd, MissingObjectStart.Length, StringComparison.Ordinal);
		if (end <= MissingObjectStart.Length)
		{
			return false;
		}

		for (int i = MissingObjectStart.Length; i < end; i++)
		{
			if (!char.IsAsciiDigit(message[i]))
			{
				return false;
			}
		}

		return true;
	}

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
