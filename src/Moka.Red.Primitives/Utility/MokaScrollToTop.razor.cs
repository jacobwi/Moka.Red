using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.Utility;

/// <summary>
///     Fixed-position button that appears when the user scrolls down,
///     and scrolls back to the top on click.
/// </summary>
public partial class MokaScrollToTop
{
	private const string ModulePath = "./_content/Moka.Red.Primitives/Utility/MokaScrollToTop.razor.js";

	private DotNetObjectReference<MokaScrollToTop>? _dotNetRef;
	private IJSObjectReference? _module;
	private int _scrollHandle;
	private bool _visible;

	/// <summary>Pixels scrolled before showing the button. Default 200.</summary>
	[Parameter]
	public int ShowAfter { get; set; } = 200;

	/// <summary>Use smooth scroll animation. Default true.</summary>
	[Parameter]
	public bool Smooth { get; set; } = true;

	/// <inheritdoc />
	protected override string RootClass => "moka-scroll-top";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		base.OnAfterRender(firstRender);

		if (firstRender)
		{
			if (_dotNetRef is not null)
			{
				return;
			}

			_dotNetRef = DotNetObjectReference.Create(this);
			try
			{
				_module = await GetJsModuleAsync(ModulePath);
				_scrollHandle = await _module.InvokeAsync<int>("init", _dotNetRef, ShowAfter);
			}
			catch (JSDisconnectedException)
			{
				// Circuit disconnected
			}
		}
	}

	/// <summary>Called from JS when scroll position changes.</summary>
	[JSInvokable]
	public void OnScrollChanged(bool visible)
	{
		if (_visible != visible)
		{
			_visible = visible;
			InvokeAsync(ForceRender);
		}
	}

	private async Task ScrollToTop()
	{
		try
		{
			IJSObjectReference module = await GetJsModuleAsync(ModulePath);
			await module.InvokeVoidAsync("scrollToTop", Smooth);
		}
		catch (JSDisconnectedException)
		{
			// Circuit disconnected
		}
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		// Drop the window scroll listener before the base disposes the module, otherwise it
		// stays attached for the lifetime of the page and keeps calling into a dead component.
		if (_module is not null && _scrollHandle != 0)
		{
			try
			{
				await _module.InvokeVoidAsync("dispose", _scrollHandle);
			}
			catch (JSDisconnectedException)
			{
				// Circuit disconnected, so the listener went with it
			}
			catch (ObjectDisposedException)
			{
				// JS runtime torn down mid-call
			}
			catch (OperationCanceledException)
			{
				// Covers TaskCanceledException too
			}

			_scrollHandle = 0;
		}

		_module = null;
		_dotNetRef?.Dispose();
		await base.DisposeAsyncCore();
	}
}
