using Microsoft.JSInterop;
using Moka.Red.Core.Base;

namespace Moka.Red.Feedback.SystemNotification;

/// <summary>The default <see cref="IMokaSystemNotifier" />, over the browser's Notification API.</summary>
public sealed class MokaSystemNotifier : IMokaSystemNotifier, IAsyncDisposable
{
	private const string ModulePath = "./_content/Moka.Red.Feedback/moka-system-notifier.js";

	private readonly IJSRuntime _js;
	private Task<IJSObjectReference>? _module;

	/// <summary>A notifier for the browser the <paramref name="js" /> runtime talks to.</summary>
	/// <param name="js">The JS runtime.</param>
	public MokaSystemNotifier(IJSRuntime js)
	{
		ArgumentNullException.ThrowIfNull(js);
		_js = js;
	}

	/// <inheritdoc />
	public async ValueTask<MokaSystemNotificationConsent> GetPermissionAsync(
		CancellationToken cancellationToken = default)
	{
		IJSObjectReference module = await ModuleAsync();
		return ReadPermission(await module.InvokeAsync<string>("permission", cancellationToken));
	}

	/// <inheritdoc />
	public async ValueTask<MokaSystemNotificationConsent> RequestPermissionAsync(
		CancellationToken cancellationToken = default)
	{
		IJSObjectReference module = await ModuleAsync();
		return ReadPermission(await module.InvokeAsync<string>("requestPermission", cancellationToken));
	}

	/// <inheritdoc />
	public async ValueTask<MokaSystemNotificationResult> NotifyAsync(MokaSystemNotification notification,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(notification);

		IJSObjectReference module = await ModuleAsync();
		Dictionary<string, object?> options = new()
		{
			["title"] = notification.Title,
			["body"] = notification.Body,
			["icon"] = notification.Icon,
			["tag"] = notification.Tag,
			["onlyWhenHidden"] = notification.OnlyWhenHidden
		};

		MokaSystemNotificationClick? click = notification.OnClick is null ? null : new(notification.OnClick);
		DotNetObjectReference<MokaSystemNotificationClick>? reference = click is null ? null : DotNetObjectReference.Create(click);
		click?.Own(reference!);

		string result;
		try
		{
			result = await module.InvokeAsync<string>("notify", cancellationToken, options, reference);
		}
		catch
		{
			reference?.Dispose();
			throw;
		}

		MokaSystemNotificationResult read = result switch
		{
			"shown" => MokaSystemNotificationResult.Shown,
			"visible" => MokaSystemNotificationResult.PageVisible,
			"not-allowed" => MokaSystemNotificationResult.NotAllowed,
			_ => MokaSystemNotificationResult.Unsupported
		};

		// Only a shown notification can call back; the script lets go of the reference once it closes.
		if (read != MokaSystemNotificationResult.Shown)
		{
			reference?.Dispose();
		}

		return read;
	}

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
		if (_module is { IsCompletedSuccessfully: true } module)
		{
			await (await module).SafeDisposeAsync();
		}
	}

	private static MokaSystemNotificationConsent ReadPermission(string permission) => permission switch
	{
		"granted" => MokaSystemNotificationConsent.Granted,
		"denied" => MokaSystemNotificationConsent.Denied,
		"default" => MokaSystemNotificationConsent.NotAsked,
		_ => MokaSystemNotificationConsent.Unsupported
	};

	// A failed import is forgotten, so the next call tries again.
	private Task<IJSObjectReference> ModuleAsync()
	{
		Task<IJSObjectReference>? module = _module;
		if (module is null || module.IsFaulted || module.IsCanceled)
		{
			module = _js.InvokeAsync<IJSObjectReference>("import", ModulePath).AsTask();
			_module = module;
		}

		return module;
	}
}
