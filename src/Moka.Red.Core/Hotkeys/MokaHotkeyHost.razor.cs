using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Moka.Red.Core.Hotkeys;

/// <summary>
///     Listens for every hotkey registered with <see cref="IMokaHotkeyService" />. Place one in your root layout,
///     beside the other hosts; without it the shortcuts are listed but nothing fires them. Renders no markup.
/// </summary>
public sealed partial class MokaHotkeyHost : ComponentBase, IAsyncDisposable
{
	private const string ModulePath = "./_content/Moka.Red.Core/moka-hotkeys.js";

	private bool _disposed;
	private DotNetObjectReference<MokaHotkeyHost>? _dotNetRef;
	private int _handle;
	private IJSObjectReference? _module;
	private bool _pendingPush;

	[Inject]
	private IMokaHotkeyService? Service { get; set; }

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;

		if (Service is not null)
		{
			Service.OnChanged -= HandleChanged;
		}

		try
		{
			if (_module is not null && _handle != 0)
			{
				await _module.InvokeVoidAsync("unbindHotkeys", _handle);
			}

			if (_module is not null)
			{
				await _module.DisposeAsync();
			}
		}
		catch (JSDisconnectedException)
		{
		}
		catch (OperationCanceledException)
		{
		}

		_dotNetRef?.Dispose();
	}

	/// <summary>Called from JS when a registered hotkey's keys are pressed.</summary>
	/// <param name="id">The <see cref="MokaHotkey.Id" /> of the hotkey that matched.</param>
	/// <returns>A task that completes when the hotkey's action has run.</returns>
	[JSInvokable]
	public async Task InvokeFromJs(string id)
	{
		if (_disposed || Service is null)
		{
			return;
		}

		await Service.InvokeAsync(id);

		// The action is the consumer's and may have changed anything; the host renders nothing itself, so this
		// is only to let whatever it touched be drawn.
		StateHasChanged();
	}

	/// <inheritdoc />
	protected override void OnInitialized()
	{
		if (Service is not null)
		{
			Service.OnChanged += HandleChanged;
		}
	}

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender && Service is not null)
		{
			await BindAsync();
		}
		else if (_pendingPush)
		{
			_pendingPush = false;
			await PushAsync();
		}
	}

	private void HandleChanged()
	{
		_pendingPush = true;
		_ = InvokeAsync(StateHasChanged);
	}

	private async Task BindAsync()
	{
		try
		{
			_module = await JsRuntime.InvokeAsync<IJSObjectReference>("import", ModulePath);
			_dotNetRef = DotNetObjectReference.Create(this);
			_handle = await _module.InvokeAsync<int>("bindHotkeys", _dotNetRef);
			await PushAsync();
		}
		catch (JSDisconnectedException)
		{
		}
		catch (OperationCanceledException)
		{
		}
	}

	// The browser side keeps the whole set, so it can match without asking .NET anything.
	private async Task PushAsync()
	{
		if (_module is null || _handle == 0 || Service is null || _disposed)
		{
			return;
		}

		object[] bindable = Service.Hotkeys
			.Where(h => h.IsBindable)
			.Select(object (h) => new
			{
				id = h.Id,
				keys = h.Keys,
				allowInInput = h.AllowInInput,
				preventDefault = h.PreventDefault
			})
			.ToArray();

		try
		{
			await _module.InvokeVoidAsync("setHotkeys", _handle, bindable);
		}
		catch (JSDisconnectedException)
		{
		}
		catch (OperationCanceledException)
		{
		}
	}
}
