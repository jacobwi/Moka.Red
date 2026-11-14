using System.Diagnostics.CodeAnalysis;

namespace Moka.Red.Feedback.Toast;

/// <summary>
///     Shows short messages that come and go. Registered scoped by <c>AddMokaFeedback()</c>; one
///     <see cref="MokaToastHost" /> in the layout draws them. Safe to call from any thread: the service keeps an
///     immutable list of immutable toasts and says when it changed, and the host reads the list on the renderer's thread.
/// </summary>
[SuppressMessage("Design", "CA1003:Use generic event handler instances",
	Justification = "A parameterless Action is simpler for this lightweight state-changed event.")]
public interface IMokaToastService
{
	/// <summary>
	///     The toasts showing or waiting their turn, oldest first. An immutable snapshot: a change replaces the list, so
	///     the one read here stays as it was. A host reads it when it starts, so toasts shown before it mounted still show.
	/// </summary>
	IReadOnlyList<MokaToastMessage> Toasts { get; }

	/// <summary>Raised after every change to <see cref="Toasts" />, on the thread that made it. Read the new list then.</summary>
	event Action? Changed;

	/// <summary>Shows a toast, or repeats one already showing (see <see cref="MokaToastOptions.Repeats" />).</summary>
	/// <param name="message">The body text.</param>
	/// <param name="severity">Color and icon. <see cref="MokaToastSeverity.Info" /> unless set.</param>
	/// <param name="configure">Sets the rest of the toast.</param>
	/// <returns>A handle on the toast, to update or dismiss it. A repeat returns a handle on the toast it repeated.</returns>
	MokaToastHandle Show(string message, MokaToastSeverity severity = MokaToastSeverity.Info,
		Action<MokaToastOptions>? configure = null);

	/// <summary>Shows a success toast.</summary>
	/// <param name="message">The body text.</param>
	/// <param name="configure">Sets the rest of the toast.</param>
	/// <returns>A handle on the toast.</returns>
	MokaToastHandle ShowSuccess(string message, Action<MokaToastOptions>? configure = null);

	/// <summary>Shows an error toast. Screen readers hear it at once, interrupting what they were saying.</summary>
	/// <param name="message">The body text.</param>
	/// <param name="configure">Sets the rest of the toast.</param>
	/// <returns>A handle on the toast.</returns>
	MokaToastHandle ShowError(string message, Action<MokaToastOptions>? configure = null);

	/// <summary>Shows a warning toast.</summary>
	/// <param name="message">The body text.</param>
	/// <param name="configure">Sets the rest of the toast.</param>
	/// <returns>A handle on the toast.</returns>
	MokaToastHandle ShowWarning(string message, Action<MokaToastOptions>? configure = null);

	/// <summary>Shows an info toast.</summary>
	/// <param name="message">The body text.</param>
	/// <param name="configure">Sets the rest of the toast.</param>
	/// <returns>A handle on the toast.</returns>
	MokaToastHandle ShowInfo(string message, Action<MokaToastOptions>? configure = null);

	/// <summary>
	///     Changes a toast in place. New words or a new severity start its countdown again and are read to screen
	///     readers, unless it still shows progress; taking the progress away starts the countdown and reads the result.
	///     Progress alone changes nothing else.
	/// </summary>
	/// <param name="id">The toast's id.</param>
	/// <param name="change">Changes the options, which hold the toast as it is.</param>
	/// <returns>Whether the toast was still there to change.</returns>
	bool Update(Guid id, Action<MokaToastOptions> change);

	/// <summary>Takes a toast away, if it's still there.</summary>
	/// <param name="id">The toast's id.</param>
	/// <param name="reason">Why, for <see cref="MokaToastOptions.OnDismissed" />. Hosts pass what the person did.</param>
	void Dismiss(Guid id, MokaToastDismissReason reason = MokaToastDismissReason.Dismissed);

	/// <summary>
	///     For hosts: ends a toast whose countdown ran out, unless it started a new countdown meanwhile.
	/// </summary>
	/// <param name="id">The toast's id.</param>
	/// <param name="run">The <see cref="MokaToastMessage.Run" /> of the countdown that ended.</param>
	/// <returns>Whether the toast went.</returns>
	bool Expire(Guid id, int run);

	/// <summary>Takes every toast away.</summary>
	void Clear();

	/// <summary>
	///     Shows a toast for the time <paramref name="task" /> runs, then turns it into its result: success, error, or
	///     on cancellation <paramref name="cancelled" /> (none of the toast when that's null, since whoever cancelled
	///     knows). The returned task finishes as <paramref name="task" /> does, failure and cancellation included: the
	///     toast reports a failure, it never swallows it.
	/// </summary>
	/// <param name="task">The work.</param>
	/// <param name="loading">The words while it runs.</param>
	/// <param name="success">The words once it has finished.</param>
	/// <param name="failure">The words for a failure, from the exception. It must not throw.</param>
	/// <param name="cancelled">The words for a cancellation, or null to take the toast away.</param>
	/// <param name="configure">Sets the rest of the toast, such as its title, its look or <see cref="MokaToastOptions.AlsoInCenter" />.</param>
	/// <returns><paramref name="task" />, awaited.</returns>
	Task Track(Task task, string loading, string success, Func<Exception, string> failure, string? cancelled = null,
		Action<MokaToastOptions>? configure = null);

	/// <summary>Like <see cref="Track(Task, string, string, Func{Exception, string}, string?, Action{MokaToastOptions}?)" />, for work with a result.</summary>
	/// <typeparam name="T">The result's type.</typeparam>
	/// <param name="task">The work.</param>
	/// <param name="loading">The words while it runs.</param>
	/// <param name="success">The words once it has finished, from its result.</param>
	/// <param name="failure">The words for a failure, from the exception. It must not throw.</param>
	/// <param name="cancelled">The words for a cancellation, or null to take the toast away.</param>
	/// <param name="configure">Sets the rest of the toast.</param>
	/// <returns>The result of <paramref name="task" />.</returns>
	Task<T> Track<T>(Task<T> task, string loading, Func<T, string> success, Func<Exception, string> failure,
		string? cancelled = null, Action<MokaToastOptions>? configure = null);
}
