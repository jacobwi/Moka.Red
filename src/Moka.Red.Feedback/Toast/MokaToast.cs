using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Icons;

namespace Moka.Red.Feedback.Toast;

/// <summary>
///     A toast written in markup and tied to page state: <c>&lt;MokaToast @bind-Open="_saved" Message="Saved" /&gt;</c>.
///     It renders nothing where it stands; it hands the toast to <see cref="IMokaToastService" />, so the layout's
///     <see cref="MokaToastHost" /> draws it with every other toast, in the same stack, countdown and announcements.
///     Changing its parameters updates the toast in place, and when the toast goes on its own (its countdown, its close
///     button, a swipe) <see cref="OpenChanged" /> says so.
/// </summary>
public sealed class MokaToast : ComponentBase, IDisposable
{
	private readonly HashSet<string> _passed = new(StringComparer.Ordinal);
	private bool _disposed;
	private MokaToastHandle? _handle;
	private bool _lastOpenParameter;

	// Names the toast shown last, so word from a toast this component took away itself is told apart.
	private object? _shown;

	[Inject]
	private IMokaToastService Toasts { get; set; } = default!;

	/// <summary>
	///     Whether the toast shows. Only a change of the value passed acts, so a one-way <c>true</c> doesn't bring the
	///     toast back after it went on its own; bind it two-way, or pass false and then true.
	/// </summary>
	[Parameter]
	public bool Open { get; set; }

	/// <summary>Raised with false when the toast goes on its own.</summary>
	[Parameter]
	public EventCallback<bool> OpenChanged { get; set; }

	/// <summary>The body text.</summary>
	[Parameter]
	[EditorRequired]
	public string Message { get; set; } = "";

	/// <summary>Color and icon. <see cref="MokaToastSeverity.Info" /> unless set.</summary>
	[Parameter]
	public MokaToastSeverity Severity { get; set; } = MokaToastSeverity.Info;

	/// <inheritdoc cref="MokaToastOptions.Title" />
	[Parameter]
	public string? Title { get; set; }

	/// <inheritdoc cref="MokaToastOptions.Duration" />
	[Parameter]
	public TimeSpan? Duration { get; set; } = TimeSpan.FromSeconds(5);

	/// <inheritdoc cref="MokaToastOptions.ShowCloseButton" />
	[Parameter]
	public bool ShowCloseButton { get; set; } = true;

	/// <inheritdoc cref="MokaToastOptions.ShowIcon" />
	[Parameter]
	public bool ShowIcon { get; set; } = true;

	/// <inheritdoc cref="MokaToastOptions.Icon" />
	[Parameter]
	public MokaIconDefinition? Icon { get; set; }

	/// <inheritdoc cref="MokaToastOptions.ActionText" />
	[Parameter]
	public string? ActionText { get; set; }

	/// <summary>Runs when the action button is pressed, and redraws the component that set it.</summary>
	[Parameter]
	public EventCallback OnAction { get; set; }

	/// <inheritdoc cref="MokaToastOptions.CloseOnAction" />
	[Parameter]
	public bool CloseOnAction { get; set; } = true;

	/// <inheritdoc cref="MokaToastOptions.Progress" />
	[Parameter]
	public MokaToastProgress? Progress { get; set; }

	/// <inheritdoc cref="MokaToastOptions.ProgressLook" />
	[Parameter]
	public MokaToastProgressLook? ProgressLook { get; set; }

	/// <inheritdoc cref="MokaToastOptions.AlsoInCenter" />
	[Parameter]
	public bool AlsoInCenter { get; set; }

	/// <summary>
	///     Anything more to draw under the message. Its event handlers run on the component that wrote it; it renders in
	///     the toast host, so it gets the host's cascading values.
	/// </summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Raised with why when the toast goes on its own, after <see cref="OpenChanged" />.</summary>
	[Parameter]
	public EventCallback<MokaToastDismissReason> OnDismissed { get; set; }

	/// <inheritdoc />
	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;
		TakeAway();
	}

	/// <inheritdoc />
	/// <remarks>Notes which parameters the page passes, so only those are written over the app's toast defaults.</remarks>
	public override Task SetParametersAsync(ParameterView parameters)
	{
		_passed.Clear();
		foreach (ParameterValue parameter in parameters)
		{
			_passed.Add(parameter.Name);
		}

		return base.SetParametersAsync(parameters);
	}

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		bool openChanged = Open != _lastOpenParameter;
		_lastOpenParameter = Open;

		if (openChanged && Open)
		{
			TakeAway();
			_shown = new object();
			_handle = Toasts.Show(Message, Severity, Configure);
		}
		else if (openChanged)
		{
			TakeAway();
		}
		else if (_handle is not null)
		{
			_handle.Update(Configure);
		}
	}

	// The page closed it, so nothing is raised back to the page.
	private void TakeAway()
	{
		MokaToastHandle? handle = _handle;
		_handle = null;
		_shown = null;
		handle?.Dismiss();
	}

	// Only what the page passes is written, so the app's MokaFeedbackOptions.ToastDefaults keep the rest; a parameter
	// left at its declared default would overwrite them.
	private void Configure(MokaToastOptions options)
	{
		options.Message = Message;
		options.Severity = Severity;
		if (Passed(nameof(Title)))
		{
			options.Title = Title;
		}

		if (Passed(nameof(Duration)))
		{
			options.Duration = Duration;
		}

		if (Passed(nameof(ShowCloseButton)))
		{
			options.ShowCloseButton = ShowCloseButton;
		}

		if (Passed(nameof(ShowIcon)))
		{
			options.ShowIcon = ShowIcon;
		}

		if (Passed(nameof(Icon)))
		{
			options.Icon = Icon;
		}

		if (Passed(nameof(ActionText)))
		{
			options.ActionText = ActionText;
		}

		if (Passed(nameof(OnAction)))
		{
			options.OnActionAsync = OnAction.HasDelegate ? () => OnAction.InvokeAsync() : null;
		}

		if (Passed(nameof(CloseOnAction)))
		{
			options.CloseOnAction = CloseOnAction;
		}

		if (Passed(nameof(Progress)))
		{
			options.Progress = Progress;
		}

		if (Passed(nameof(ProgressLook)))
		{
			options.ProgressLook = ProgressLook;
		}

		if (Passed(nameof(AlsoInCenter)))
		{
			options.AlsoInCenter = AlsoInCenter;
		}

		if (Passed(nameof(ChildContent)))
		{
			options.Content = ChildContent;
		}

		object? shown = _shown;
		options.OnDismissed = reason => HandleDismissed(shown, reason);
	}

	private bool Passed(string parameter) => _passed.Contains(parameter);

	private void HandleDismissed(object? shown, MokaToastDismissReason reason)
	{
		if (!_disposed && shown is not null)
		{
			_ = ReportAsync(shown, reason);
		}
	}

	// The service says on the thread that dismissed the toast; the page hears it on the renderer's thread, and a
	// failure in its handler reaches the app's error handling.
	private async Task ReportAsync(object shown, MokaToastDismissReason reason)
	{
		try
		{
			await InvokeAsync(async () =>
			{
				if (_disposed || !ReferenceEquals(shown, _shown))
				{
					return;
				}

				_handle = null;
				_shown = null;
				await OpenChanged.InvokeAsync(false);
				await OnDismissed.InvokeAsync(reason);
			});
		}
		catch (ObjectDisposedException)
		{
			// The renderer went away between the dismissal and the dispatch.
		}
		catch (Exception ex) when (!_disposed)
		{
			await DispatchExceptionAsync(ex);
		}
	}
}
