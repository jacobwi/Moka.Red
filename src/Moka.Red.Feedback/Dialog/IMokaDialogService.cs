using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;

namespace Moka.Red.Feedback.Dialog;

/// <summary>
///     Service for showing dialogs imperatively (confirm, prompt, custom content).
///     Register as scoped and inject into components or other services.
/// </summary>
/// <remarks>
///     <para>
///         <b>Concurrency.</b> Only one dialog is on screen at a time. A call made while another
///         dialog is already open is queued and shown when the one ahead of it closes, in the order
///         the calls were made. Every call therefore completes exactly once: no request is dropped
///         and no returned <see cref="Task" /> is left pending forever.
///     </para>
///     <para>
///         <see cref="Close" /> and <see cref="CloseWithResult" /> always act on the dialog that is
///         currently on screen, never on a queued one. If the service is disposed (the circuit ends)
///         while dialogs are still open or queued, each of them resolves as cancelled:
///         <see cref="ConfirmAsync" /> returns false and <see cref="PromptAsync(string, string, string)" />
///         returns null.
///     </para>
/// </remarks>
[SuppressMessage("Design", "CA1003:Use generic event handler instances",
	Justification = "Action delegates are simpler for lightweight service events.")]
public interface IMokaDialogService
{
	/// <summary>Raised when a dialog becomes the active (on screen) dialog.</summary>
	event Action<MokaDialogRequest>? OnDialogRequested;

	/// <summary>Raised after the active dialog closes and its result has been delivered.</summary>
	event Action? OnDialogClosed;

	/// <summary>
	///     Shows a confirmation dialog and returns true if the user confirms.
	/// </summary>
	/// <param name="message">The confirmation message.</param>
	/// <param name="title">Optional dialog title.</param>
	/// <param name="configure">Optional action to configure dialog options.</param>
	Task<bool> ConfirmAsync(string message, string? title = null, Action<MokaDialogOptions>? configure = null);

	/// <summary>
	///     Shows a prompt dialog and returns the entered text, or null if cancelled.
	/// </summary>
	/// <param name="message">The prompt message.</param>
	/// <param name="title">Optional dialog title.</param>
	/// <param name="defaultValue">Default value for the input field.</param>
	Task<string?> PromptAsync(string message, string? title = null, string? defaultValue = null);

	/// <summary>
	///     Shows a prompt dialog with custom options and returns the entered text, or null if cancelled.
	/// </summary>
	/// <param name="message">The prompt message.</param>
	/// <param name="title">Dialog title. Pass null for the default ("Input").</param>
	/// <param name="defaultValue">Default value for the input field. Pass null for an empty field.</param>
	/// <param name="configure">Action to configure dialog options such as ConfirmText or Size.</param>
	Task<string?> PromptAsync(string message, string? title, string? defaultValue,
		Action<MokaDialogOptions>? configure);

	/// <summary>
	///     Shows a dialog with custom content.
	/// </summary>
	/// <param name="title">Dialog title.</param>
	/// <param name="content">The dialog body content.</param>
	/// <param name="configure">Optional action to configure dialog options.</param>
	Task ShowAsync(string title, RenderFragment content, Action<MokaDialogOptions>? configure = null);

	/// <summary>
	///     Shows any Blazor component inside a dialog. The component receives a
	///     <see cref="MokaDialogContext" /> cascading parameter to close the dialog and return a result.
	/// </summary>
	/// <typeparam name="TComponent">The component type to render inside the dialog.</typeparam>
	/// <param name="title">Dialog title.</param>
	/// <param name="parameters">Optional action to configure component parameters.</param>
	/// <param name="configure">Optional action to configure dialog options.</param>
	/// <returns>The result object passed to <see cref="MokaDialogContext.Close" />, or null if cancelled.</returns>
	Task<object?> ShowComponentAsync<TComponent>(
		string title,
		Action<Dictionary<string, object>>? parameters = null,
		Action<MokaDialogOptions>? configure = null) where TComponent : IComponent;

	/// <summary>
	///     Closes the currently open service dialog. Does nothing when no dialog is open.
	/// </summary>
	/// <param name="result">Whether the dialog was confirmed (true) or cancelled (false).</param>
	/// <remarks>
	///     The value the caller receives is coerced to the dialog's own result type:
	///     <see cref="ConfirmAsync" /> gets the boolean,
	///     <see cref="PromptAsync(string, string, string)" /> gets the entered text when confirmed
	///     and null when cancelled, and <see cref="ShowComponentAsync{TComponent}" /> gets null when
	///     cancelled.
	/// </remarks>
	void Close(bool result = false);

	/// <summary>
	///     Closes the currently open service dialog with a typed result.
	///     Use this from inside component dialogs to return data.
	///     Does nothing when no dialog is open.
	/// </summary>
	/// <param name="result">The result object to return to the caller.</param>
	void CloseWithResult(object? result);
}
