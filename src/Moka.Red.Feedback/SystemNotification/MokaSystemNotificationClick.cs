using Microsoft.JSInterop;

namespace Moka.Red.Feedback.SystemNotification;

/// <summary>What a shown system notification calls back on. For the notifier's script only.</summary>
internal sealed class MokaSystemNotificationClick
{
	private readonly Func<Task> _onClick;
	private DotNetObjectReference<MokaSystemNotificationClick>? _reference;

	internal MokaSystemNotificationClick(Func<Task> onClick) => _onClick = onClick;

	/// <summary>For the notifier's script: the person clicked the notification.</summary>
	/// <returns>The app's click handler.</returns>
	[JSInvokable]
	public Task Clicked() => _onClick();

	/// <summary>For the notifier's script: the notification closed, so nothing will call back again.</summary>
	[JSInvokable]
	public void Closed() => _reference?.Dispose();

	internal void Own(DotNetObjectReference<MokaSystemNotificationClick> reference) => _reference = reference;
}
