using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Icons;

namespace Moka.Red.Feedback.Toast;

/// <summary>
///     Everything an app says about one toast. <see cref="IMokaToastService.Show" /> hands a fresh one to its
///     <c>configure</c> action, and <see cref="MokaToastHandle.Update" /> one holding the toast as it is. The service
///     copies it into an immutable <see cref="MokaToastMessage" /> afterwards, so changing it later changes nothing.
/// </summary>
public sealed class MokaToastOptions
{
	/// <summary>The body text. <see cref="IMokaToastService.Show" /> sets it from its message.</summary>
	public string Message { get; set; } = "";

	/// <summary>Color and icon. <see cref="IMokaToastService.Show" /> sets it from its severity.</summary>
	public MokaToastSeverity Severity { get; set; } = MokaToastSeverity.Info;

	/// <summary>Optional title, in bold above the message.</summary>
	public string? Title { get; set; }

	/// <summary>
	///     How long the toast shows before it goes, counted only while it can be seen: the countdown waits while the
	///     pointer is over the toasts, focus is in them, or the browser tab is hidden. Null keeps the toast until someone
	///     closes it. Five seconds unless set.
	/// </summary>
	public TimeSpan? Duration { get; set; } = TimeSpan.FromSeconds(5);

	/// <summary>Whether the close button shows. On by default.</summary>
	public bool ShowCloseButton { get; set; } = true;

	/// <summary>Whether the severity icon shows. On by default.</summary>
	public bool ShowIcon { get; set; } = true;

	/// <summary>An icon in place of the severity's own.</summary>
	public MokaIconDefinition? Icon { get; set; }

	/// <summary>
	///     Runs when the toast's body is clicked. Left unset, the toast listens to no clicks. The click has no keyboard
	///     path, so anything someone must be able to do belongs on the action button.
	/// </summary>
	public Action? OnClick { get; set; }

	/// <summary>Like <see cref="OnClick" />, awaited. Wins when both are set.</summary>
	public Func<Task>? OnClickAsync { get; set; }

	/// <summary>The words on the action button, such as "Undo". Without them there's no button.</summary>
	public string? ActionText { get; set; }

	/// <summary>Runs when the action button is pressed.</summary>
	public Action? OnAction { get; set; }

	/// <summary>Like <see cref="OnAction" />, awaited. Wins when both are set.</summary>
	public Func<Task>? OnActionAsync { get; set; }

	/// <summary>Whether the toast goes once its action has run. On by default; a failing action leaves it showing.</summary>
	public bool CloseOnAction { get; set; } = true;

	/// <summary>
	///     Names the toast, so a later toast with the same key repeats it rather than standing beside it. How it repeats
	///     follows <see cref="Repeats" />; with a key, <see cref="MokaToastRepeats.Stack" /> replaces.
	/// </summary>
	public string? Key { get; set; }

	/// <summary>What a repeat of this toast does. <see cref="MokaToastRepeats.Stack" /> unless set, as before.</summary>
	public MokaToastRepeats Repeats { get; set; } = MokaToastRepeats.Stack;

	/// <summary>
	///     How far along the work is, for a toast that reports on it: a share from 0 to 1, or
	///     <see cref="MokaToastProgress.Indeterminate" />. While it's set the toast doesn't count down. Updates as fast
	///     as the app likes are drawn at most ten times a second.
	/// </summary>
	public MokaToastProgress? Progress { get; set; }

	/// <summary>How the progress shows. Null takes the host's <c>ProgressLook</c>.</summary>
	public MokaToastProgressLook? ProgressLook { get; set; }

	/// <summary>
	///     Also puts the toast in the notification center, where it stays after the toast has gone. Updates that change
	///     the words, such as a tracked task's result, update the entry there too.
	/// </summary>
	public bool AlsoInCenter { get; set; }

	/// <summary>
	///     Anything more to draw under the message. It renders in the toast host, so it gets the host's cascading values,
	///     not the caller's.
	/// </summary>
	public RenderFragment? Content { get; set; }

	/// <summary>Runs once when the toast goes, with why, on the thread that dismissed it.</summary>
	public Action<MokaToastDismissReason>? OnDismissed { get; set; }

	internal static MokaToastOptions From(MokaToastMessage toast) => new()
	{
		Message = toast.Message,
		Severity = toast.Severity,
		Title = toast.Title,
		Duration = toast.Duration,
		ShowCloseButton = toast.ShowCloseButton,
		ShowIcon = toast.ShowIcon,
		Icon = toast.Icon,
		OnClick = toast.OnClick,
		OnClickAsync = toast.OnClickAsync,
		ActionText = toast.ActionText,
		OnAction = toast.OnAction,
		OnActionAsync = toast.OnActionAsync,
		CloseOnAction = toast.CloseOnAction,
		Key = toast.Key,
		Repeats = toast.Repeats,
		Progress = toast.Progress,
		ProgressLook = toast.ProgressLook,
		AlsoInCenter = toast.AlsoInCenter,
		Content = toast.Content,
		OnDismissed = toast.OnDismissed
	};
}
