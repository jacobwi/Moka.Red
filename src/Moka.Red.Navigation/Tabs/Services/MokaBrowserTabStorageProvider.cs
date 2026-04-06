using System.Diagnostics.CodeAnalysis;
using Microsoft.JSInterop;

namespace Moka.Red.Navigation.Tabs.Services;

/// <summary>
///     An <see cref="ITabStorageProvider" /> that persists tab state to browser sessionStorage or localStorage via JS
///     interop.
/// </summary>
public sealed class MokaBrowserTabStorageProvider : ITabStorageProvider, IAsyncDisposable
{
	#region Constructor

	/// <summary>
	///     Initializes a new <see cref="MokaBrowserTabStorageProvider" />.
	/// </summary>
	/// <param name="jsRuntime">The JS runtime.</param>
	/// <param name="storageType">"session" for sessionStorage or "local" for localStorage.</param>
	public MokaBrowserTabStorageProvider(IJSRuntime jsRuntime, string storageType = "session")
	{
		_jsRuntime = jsRuntime;
		_storageType = storageType;
	}

	#endregion

	#region IAsyncDisposable

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;
		if (_module is not null)
		{
			await _module.DisposeAsync();
			_module = null;
		}

		_semaphore.Dispose();
	}

	#endregion

	#region Private

	[SuppressMessage("Code Quality", "CA1508:Avoid dead conditional code",
		Justification = "False positive: double-checked locking — _module may be set between first check and semaphore acquisition.")]
	private async Task<IJSObjectReference> GetModuleAsync()
	{
		ObjectDisposedException.ThrowIf(_disposed, this);

		if (_module is not null)
		{
			return _module;
		}

		await _semaphore.WaitAsync();
		try
		{
			if (_module is null)
			{
				_module = await _jsRuntime.InvokeAsync<IJSObjectReference>(
					"import", "./_content/Moka.Red.Navigation/moka-tabs.js");
			}
		}
		finally
		{
			_semaphore.Release();
		}

		return _module;
	}

	#endregion

	#region Fields

	private readonly IJSRuntime _jsRuntime;
	private readonly string _storageType;
	private readonly SemaphoreSlim _semaphore = new(1, 1);
	private IJSObjectReference? _module;
	private bool _disposed;

	#endregion

	#region ITabStorageProvider

	/// <inheritdoc />
	public async Task SaveAsync(string key, string value)
	{
		IJSObjectReference module = await GetModuleAsync();
		await module.InvokeVoidAsync("MokaTabs.saveState", _storageType, key, value);
	}

	/// <inheritdoc />
	public async Task<string?> LoadAsync(string key)
	{
		IJSObjectReference module = await GetModuleAsync();
		return await module.InvokeAsync<string?>("MokaTabs.loadState", _storageType, key);
	}

	/// <inheritdoc />
	public async Task RemoveAsync(string key)
	{
		IJSObjectReference module = await GetModuleAsync();
		await module.InvokeVoidAsync("MokaTabs.removeState", _storageType, key);
	}

	#endregion
}
