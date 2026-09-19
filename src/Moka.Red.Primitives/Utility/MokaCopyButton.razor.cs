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
	private const string DragModule = "./_content/Moka.Red.Core/moka-drag.js";

	private static readonly TimeSpan ResetDelay = TimeSpan.FromSeconds(2);

	private bool _copied;
	private int _copyCount;
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
		bool copied;
		try
		{
			// The safe call returns false when the circuit is gone or the call was cancelled.
			copied = await SafeModuleInvokeAsync<bool>(DragModule, "copyToClipboard", Text);
		}
		catch (JSException)
		{
			// The module failed to load or the script threw. Nothing was copied, so nothing changes.
			return;
		}

		if (!copied || _disposed)
		{
			return;
		}

		_copied = true;
		ForceRender();

		// The new timer is in place before this handler waits on the old one, so a copy that finishes
		// meanwhile replaces and cancels this timer rather than the old one again. Only the latest
		// copy's timer resets the button.
		int copy = ++_copyCount;
		CancellationTokenSource? previous = _resetCts;
		_resetCts = new CancellationTokenSource();
		_ = ResetAfterDelayAsync(copy, _resetCts.Token);

		if (previous is not null)
		{
			await previous.CancelAsync();
			previous.Dispose();
		}
	}

	private async Task ResetAfterDelayAsync(int copy, CancellationToken token)
	{
		try
		{
			await Task.Delay(ResetDelay, token);

			// The delay ends on a thread-pool thread, and the next render reads this state.
			await InvokeAsync(() =>
			{
				if (copy != _copyCount || _disposed)
				{
					return;
				}

				_copied = false;
				ForceRender();
			});
		}
		catch (OperationCanceledException)
		{
			// A newer copy or disposal took over.
		}
		catch (ObjectDisposedException)
		{
			// The renderer went away during the delay.
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
			_resetCts = null;
		}

		await base.DisposeAsyncCore();
	}
}
