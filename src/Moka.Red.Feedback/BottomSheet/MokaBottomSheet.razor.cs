using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Feedback.BottomSheet;

/// <summary>
///     A slide-up panel from the bottom of the screen.
///     Common on mobile, useful for actions and forms.
///     Supports backdrop close, escape close, and body scroll lock.
/// </summary>
public partial class MokaBottomSheet : MokaVisualComponentBase
{
	private IJSObjectReference? _jsModule;
	private bool _previousOpen;

	/// <summary>The sheet body content.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Optional header content displayed at the top of the sheet.</summary>
	[Parameter]
	public RenderFragment? Header { get; set; }

	/// <summary>Whether the bottom sheet is currently visible. Two-way bindable.</summary>
	[Parameter]
	public bool Open { get; set; }

	/// <summary>Callback invoked when the open state changes.</summary>
	[Parameter]
	public EventCallback<bool> OpenChanged { get; set; }

	/// <summary>Whether clicking the backdrop closes the sheet. Defaults to true.</summary>
	[Parameter]
	public bool CloseOnBackdrop { get; set; } = true;

	/// <summary>Whether pressing Escape closes the sheet. Defaults to true.</summary>
	[Parameter]
	public bool CloseOnEscape { get; set; } = true;

	/// <summary>Maximum height of the sheet. Defaults to "70vh".</summary>
	[Parameter]
	public string MaxHeight { get; set; } = "70vh";

	/// <summary>Whether to show a drag handle at the top. Defaults to true.</summary>
	[Parameter]
	public bool ShowHandle { get; set; } = true;

	/// <summary>Whether to expand to full screen height. Defaults to false.</summary>
	[Parameter]
	public bool FullScreen { get; set; }

	/// <summary>Whether to prevent body scrolling when open. Defaults to true.</summary>
	[Parameter]
	public bool PreventScroll { get; set; } = true;

	[Inject] private IJSRuntime JsRuntime { get; set; } = default!;

	/// <inheritdoc />
	protected override string RootClass => "moka-bottom-sheet";

	private string SheetCss => new CssBuilder(RootClass)
		.AddClass("moka-bottom-sheet--fullscreen", FullScreen)
		.AddClass(Class)
		.Build();

	private string? SheetStyle => new StyleBuilder()
		.AddStyle("max-height", FullScreen ? "100vh" : MaxHeight)
		.AddStyle("height", "100vh", FullScreen)
		.AddStyle(Style)
		.Build();

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override async Task OnParametersSetAsync()
	{
		await base.OnParametersSetAsync();

		if (Open != _previousOpen)
		{
			_previousOpen = Open;
			if (Open)
			{
				await OnOpenedAsync();
			}
			else
			{
				await OnClosedAsync();
			}
		}
	}

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		base.OnAfterRender(firstRender);

		if (firstRender && Open)
		{
			await OnOpenedAsync();
		}
	}

	private async Task OnOpenedAsync()
	{
		if (PreventScroll)
		{
			await EnsureJsModuleAsync();
			try
			{
				await _jsModule!.InvokeVoidAsync("lockBodyScroll");
			}
			catch (JSDisconnectedException)
			{
			}
		}
	}

	private async Task OnClosedAsync()
	{
		if (PreventScroll && _jsModule is not null)
		{
			try
			{
				await _jsModule.InvokeVoidAsync("unlockBodyScroll");
			}
			catch (JSDisconnectedException)
			{
			}
		}
	}

	private async Task HandleBackdropClick()
	{
		if (CloseOnBackdrop)
		{
			await CloseAsync();
		}
	}

	private async Task HandleKeyDown(KeyboardEventArgs e)
	{
		if (CloseOnEscape && e.Key == "Escape")
		{
			await CloseAsync();
		}
	}

	private async Task CloseAsync()
	{
		Open = false;
		_previousOpen = false;

		await OnClosedAsync();

		if (OpenChanged.HasDelegate)
		{
			await OpenChanged.InvokeAsync(false);
		}
	}

	private async ValueTask EnsureJsModuleAsync()
	{
		if (_jsModule is not null)
		{
			return;
		}

		try
		{
			_jsModule = await JsRuntime.InvokeAsync<IJSObjectReference>(
				"import", "./_content/Moka.Red.Feedback/moka-dialog.js");
		}
		catch (JSDisconnectedException)
		{
			// Circuit disconnected during init
		}
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		if (_jsModule is not null)
		{
			try
			{
				await _jsModule.InvokeVoidAsync("dispose");
				await _jsModule.DisposeAsync();
			}
			catch (JSDisconnectedException)
			{
			}

			_jsModule = null;
		}

		await base.DisposeAsyncCore();
	}
}
