using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.Utility;

/// <summary>
///     Click-to-copy button that copies text to the clipboard.
///     Shows a checkmark icon for 2 seconds after copying.
/// </summary>
public partial class MokaCopyButton
{
	private bool _copied;
	private bool _disposed;
	private CancellationTokenSource? _resetCts;

	/// <summary>Text to copy to the clipboard. Required.</summary>
	[Parameter]
	[EditorRequired]
	public string Text { get; set; } = default!;

	/// <summary>Custom button content. When null, shows copy/check icons.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Tooltip text shown before copying. Default "Copy".</summary>
	[Parameter]
	public string TooltipText { get; set; } = "Copy";

	/// <summary>Tooltip text shown after copying. Default "Copied!".</summary>
	[Parameter]
	public string CopiedText { get; set; } = "Copied!";

	/// <inheritdoc />
	protected override string RootClass => "moka-copy-btn";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-copy-btn--copied", _copied)
		.AddClass(Class)
		.Build();

	private async Task HandleCopy()
	{
		try
		{
			IJSObjectReference module =
				await GetJsModuleAsync("./_content/Moka.Red.Primitives/Utility/MokaCopyButton.razor.js");
			await module.InvokeVoidAsync("copyToClipboard", Text);

			_copied = true;
			ForceRender();

			if (_resetCts is not null)
			{
				await _resetCts.CancelAsync();
			}

			_resetCts = new CancellationTokenSource();
			CancellationToken token = _resetCts.Token;

			_ = Task.Delay(2000, token).ContinueWith(_ =>
			{
				if (!token.IsCancellationRequested && !_disposed)
				{
					_copied = false;
					InvokeAsync(ForceRender);
				}
			}, token, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Current);
		}
		catch (JSException)
		{
			// Clipboard API may not be available
		}
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		_disposed = true;
		if (_resetCts is not null)
		{
			await _resetCts.CancelAsync();
			_resetCts.Dispose();
		}

		await base.DisposeAsyncCore();
	}
}
