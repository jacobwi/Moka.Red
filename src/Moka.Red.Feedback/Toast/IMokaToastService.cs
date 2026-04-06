using System.Diagnostics.CodeAnalysis;

namespace Moka.Red.Feedback.Toast;

/// <summary>
///     Service for showing and managing toast notifications.
///     Register as scoped and inject into components or other services.
/// </summary>
[SuppressMessage("Design", "CA1003:Use generic event handler instances",
	Justification = "Action delegates are simpler for lightweight service events.")]
public interface IMokaToastService
{
	/// <summary>Raised when a new toast is added.</summary>
	event Action<MokaToastMessage> OnToastAdded;

	/// <summary>Raised when a toast is removed (dismissed or expired).</summary>
	event Action<Guid> OnToastRemoved;

	/// <summary>Shows a toast with the specified message and severity.</summary>
	/// <param name="message">The toast body text.</param>
	/// <param name="severity">The severity level. Defaults to <see cref="MokaToastSeverity.Info" />.</param>
	/// <param name="configure">Optional action to configure toast options.</param>
	void Show(string message, MokaToastSeverity severity = MokaToastSeverity.Info,
		Action<MokaToastOptions>? configure = null);

	/// <summary>Shows a success toast.</summary>
	void ShowSuccess(string message, Action<MokaToastOptions>? configure = null);

	/// <summary>Shows an error toast.</summary>
	void ShowError(string message, Action<MokaToastOptions>? configure = null);

	/// <summary>Shows a warning toast.</summary>
	void ShowWarning(string message, Action<MokaToastOptions>? configure = null);

	/// <summary>Shows an info toast.</summary>
	void ShowInfo(string message, Action<MokaToastOptions>? configure = null);

	/// <summary>Removes a specific toast by ID.</summary>
	void Remove(Guid id);

	/// <summary>Removes all active toasts.</summary>
	void Clear();
}
