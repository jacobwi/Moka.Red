using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;

namespace Moka.Red.Feedback.Dialog;

/// <summary>
///     Service for showing dialogs imperatively (confirm, prompt, custom content).
///     Register as scoped and inject into components or other services.
/// </summary>
[SuppressMessage("Design", "CA1003:Use generic event handler instances",
	Justification = "Action delegates are simpler for lightweight service events.")]
public interface IMokaDialogService
{
	/// <summary>Raised when a dialog is requested via the service.</summary>
	event Action<MokaDialogRequest>? OnDialogRequested;

	/// <summary>Raised when the current dialog is closed.</summary>
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
	///     Closes the currently open service dialog.
	/// </summary>
	/// <param name="result">Whether the dialog was confirmed (true) or cancelled (false).</param>
	void Close(bool result = false);

	/// <summary>
	///     Closes the currently open service dialog with a typed result.
	///     Use this from inside component dialogs to return data.
	/// </summary>
	/// <param name="result">The result object to return to the caller.</param>
	void CloseWithResult(object? result);
}
