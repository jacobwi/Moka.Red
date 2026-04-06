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
	private DotNetObjectReference<MokaScrollToTop>? _dotNetRef;
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
				IJSObjectReference module =
					await GetJsModuleAsync("./_content/Moka.Red.Primitives/Utility/MokaScrollToTop.razor.js");
				await module.InvokeVoidAsync("init", _dotNetRef, ShowAfter);
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
			IJSObjectReference module =
				await GetJsModuleAsync("./_content/Moka.Red.Primitives/Utility/MokaScrollToTop.razor.js");
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
		_dotNetRef?.Dispose();
		await base.DisposeAsyncCore();
	}
}
