using Microsoft.JSInterop;
using Moka.Red.Core.Base;

namespace Moka.Red.Feedback.Banner;

/// <summary>
///     The default <see cref="IMokaBannerStorage" />: the browser's local storage, under keys starting
///     <c>moka-banner:</c>. Where the browser refuses storage (a private window that blocks it) dismissals are simply not
///     remembered, and the browser's console says why.
/// </summary>
public sealed class MokaBrowserBannerStorage : IMokaBannerStorage, IAsyncDisposable
{
	private const string ModulePath = "./_content/Moka.Red.Feedback/moka-banners.js";

	private readonly IJSRuntime _js;
	private Task<IJSObjectReference>? _module;

	/// <summary>Storage in the browser the <paramref name="js" /> runtime talks to.</summary>
	/// <param name="js">The JS runtime.</param>
	public MokaBrowserBannerStorage(IJSRuntime js)
	{
		ArgumentNullException.ThrowIfNull(js);
		_js = js;
	}

	/// <inheritdoc />
	public async ValueTask<bool> IsDismissedAsync(string id, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(id);
		IJSObjectReference module = await ModuleAsync();
		return await module.InvokeAsync<bool>("isDismissed", cancellationToken, id);
	}

	/// <inheritdoc />
	public async ValueTask RememberDismissedAsync(string id, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(id);
		IJSObjectReference module = await ModuleAsync();
		await module.InvokeVoidAsync("remember", cancellationToken, id);
	}

	/// <inheritdoc />
	public async ValueTask ForgetAsync(string id, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(id);
		IJSObjectReference module = await ModuleAsync();
		await module.InvokeVoidAsync("forget", cancellationToken, id);
	}

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
		if (_module is { IsCompletedSuccessfully: true } module)
		{
			await (await module).SafeDisposeAsync();
		}
	}

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
