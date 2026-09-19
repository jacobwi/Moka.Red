using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.CodeBlock;

/// <summary>
///     A styled code display block with optional line numbers, language label, and copy button.
///     Unlike <c>MokaCode</c> which is an inline code element, this renders a full
///     <c>&lt;pre&gt;&lt;code&gt;</c> block suitable for multi-line code snippets.
/// </summary>
public partial class MokaCodeBlock
{
	private const string DragModule = "./_content/Moka.Red.Core/moka-drag.js";

	private static readonly TimeSpan ResetDelay = TimeSpan.FromSeconds(2);

	private bool _copied;
	private int _copyCount;
	private bool _disposed;
	private CancellationTokenSource? _resetCts;

	/// <summary>The code text to display. Required.</summary>
	[Parameter]
	[EditorRequired]
	public string Code { get; set; } = string.Empty;

	/// <summary>Optional language hint label displayed in the header (e.g., "C#", "JSON", "HTML").</summary>
	[Parameter]
	public string? Language { get; set; }

	/// <summary>Whether to show line numbers in the gutter. Defaults to true.</summary>
	[Parameter]
	public bool ShowLineNumbers { get; set; } = true;

	/// <summary>Whether to show a copy-to-clipboard button. Defaults to true.</summary>
	[Parameter]
	public bool ShowCopyButton { get; set; } = true;

	/// <summary>Maximum height of the code area. When set, the block becomes scrollable (e.g., "300px").</summary>
	[Parameter]
	public string? MaxHeight { get; set; }

	/// <summary>Whether to wrap long lines instead of scrolling horizontally. Defaults to false.</summary>
	[Parameter]
	public bool Wrap { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-code-block";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-code-block--line-numbers", ShowLineNumbers)
		.AddClass("moka-code-block--wrap", Wrap)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle(Style)
		.Build();

	private string? PreStyle => new StyleBuilder()
		.AddStyle("max-height", MaxHeight, MaxHeight is not null)
		.Build();

	private string CopyButtonClass => new CssBuilder("moka-code-block__copy")
		.AddClass("moka-code-block__copy--copied", _copied)
		.Build();

	private string[] Lines => Code.Split('\n');

	private bool ShowHeader => Language is not null || ShowCopyButton;

	private async Task HandleCopy()
	{
		bool copied;
		try
		{
			// The safe call returns false when the circuit is gone or the call was cancelled.
			copied = await SafeModuleInvokeAsync<bool>(DragModule, "copyToClipboard", Code);
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
