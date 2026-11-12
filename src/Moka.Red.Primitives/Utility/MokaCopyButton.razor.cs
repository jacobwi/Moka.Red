using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Moka.Red.Core.Base;
using Moka.Red.Core.Clipboard;
using Moka.Red.Core.Utilities;
using Moka.Red.Feedback.Tooltip;

namespace Moka.Red.Primitives.Utility;

/// <summary>
///     Click-to-copy button that copies text to the clipboard.
///     Shows a checkmark icon for 2 seconds after copying.
/// </summary>
/// <remarks>
///     The browser copies inside the click itself, with no round trip to .NET first, because Safari, iOS and Firefox
///     refuse a copy made once the click is over. .NET only hears how it went, and only the button redraws.
/// </remarks>
public partial class MokaCopyButton
{
	/// <summary>Default <see cref="TooltipText" />, for a host that builds a name of its own and falls back to this.</summary>
	public const string DefaultTooltipText = "Copy";

	/// <summary>Default <see cref="CopiedText" />, for a host that falls back to it.</summary>
	public const string DefaultCopiedText = "Copied!";

	/// <summary>Default <see cref="FailedText" />, for a host that falls back to it.</summary>
	public const string DefaultFailedText = "Couldn't copy";

	private const string CopyModule = "./_content/Moka.Red.Core/moka-copy.js";

	private static readonly TimeSpan ResetDelay = TimeSpan.FromSeconds(2);

	private ElementReference _button;
	private bool _copied;
	private int _copyCount;
	private bool _disposed;
	private bool _failed;
	private CancellationTokenSource? _resetCts;

	/// <summary>Text to copy to the clipboard. Required.</summary>
	[Parameter]
	[EditorRequired]
	public string Text { get; set; } = default!;

	/// <summary>
	///     Reads the text when the button is clicked, instead of <see cref="Text" />, for text too long to build on every
	///     render, such as a log that grows while it is on screen. The browser asks .NET for it inside the click and
	///     copies what arrives, so on a static render, where nothing answers, the button copies nothing.
	/// </summary>
	[Parameter]
	public Func<string>? TextSource { get; set; }

	/// <summary>Custom button content. When null, shows copy/check icons.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>
	///     Tooltip text shown before copying, in a <see cref="MokaTooltip" /> on hover and on keyboard focus. Default
	///     "Copy". Without <see cref="ChildContent" /> the button shows only an icon and this is also its name.
	/// </summary>
	[Parameter]
	public string TooltipText { get; set; } = DefaultTooltipText;

	/// <summary>Tooltip text shown after copying, for two seconds. Default "Copied!".</summary>
	[Parameter]
	public string CopiedText { get; set; } = DefaultCopiedText;

	/// <summary>Tooltip text shown for two seconds when the browser refused the copy. Default "Couldn't copy".</summary>
	[Parameter]
	public string FailedText { get; set; } = DefaultFailedText;

	/// <summary>
	///     A small borderless button for beside a value, which says "Copied" in a tag above itself. A
	///     <see cref="MokaCopyable" /> draws this. In a table row or an element with the <c>moka-quiet-copies</c>
	///     class it stays faint until the row is hovered, so a list of addresses stays calm.
	/// </summary>
	[Parameter]
	public bool Quiet { get; set; }

	/// <summary>
	///     Where the tooltip opens. Defaults to <see cref="MokaTooltipPosition.Bottom" />, for a button in a row; use
	///     Top in a row at the bottom of a view, and Left or Right in a vertical rail. Where the side has no room the
	///     tooltip moves to one that has.
	/// </summary>
	[Parameter]
	public MokaTooltipPosition TooltipPosition { get; set; } = MokaTooltipPosition.Bottom;

	private string CurrentLabel => _copied ? CopiedText : _failed ? FailedText : TooltipText;

	// A quiet button says it in a tag above itself, so a tooltip in the same place would say it twice.
	private string? TooltipLabel => Quiet && (_copied || _failed) ? null : CurrentLabel;

	private string? Announcement => _copied || _failed ? CurrentLabel : null;

	private string IconSize => Quiet ? "13px" : "16px";

	// With a TextSource the button never offers Text, which callers leave empty: copied on a static page, where nothing
	// answers the question, it would empty the person's clipboard.
	private bool Asks => TextSource is not null;

	private string? OwnText => Asks ? null : Text;

	private EventCallback<EventArgs> AskHandler =>
		Asks ? EventCallback.Factory.Create<EventArgs>(this, AnswerAsync) : default;

	/// <inheritdoc />
	protected override string RootClass => "moka-copy-btn";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-copy-btn--quiet", Quiet)
		.AddClass("moka-copy-btn--copied", _copied)
		.AddClass("moka-copy-btn--failed", _failed)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		// On WebAssembly .NET answers the browser's question before the click is over once the module is loaded, and the
		// browser can then copy the plain way, which an insecure origin or an older browser needs. On Server the answer
		// is a round trip away whatever is loaded, so nothing is loaded early there.
		if (!firstRender || !Asks || !OperatingSystem.IsBrowser())
		{
			return;
		}

		try
		{
			await GetJsModuleAsync(CopyModule);
		}
		catch (Exception ex) when (ex is JSException || MokaJsInterop.IsCircuitGone(ex))
		{
		}
	}

	private void HandleCopied(MokaCopyEventArgs args)
	{
		if (_disposed)
		{
			return;
		}

		_copied = args.Copied;
		_failed = !args.Copied;
		ForceRender();

		// Only the latest copy's timer resets the button.
		int copy = ++_copyCount;
		_resetCts?.Cancel();
		_resetCts?.Dispose();
		_resetCts = new CancellationTokenSource();
		_ = ResetAfterDelayAsync(copy, _resetCts.Token);
	}

	private async Task AnswerAsync()
	{
		string text = TextSource?.Invoke() ?? Text ?? string.Empty;
		try
		{
			await SafeModuleInvokeVoidAsync(CopyModule, "answerCopy", _button, text);
		}
		catch (JSException)
		{
			// The module didn't load. The browser stops waiting for the text and the button says it couldn't copy.
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
				_failed = false;
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
