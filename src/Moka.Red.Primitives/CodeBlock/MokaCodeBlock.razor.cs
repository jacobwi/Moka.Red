using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Core.Clipboard;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.CodeBlock;

/// <summary>
///     A styled code display block with optional line numbers, language label, and copy button.
///     Unlike <c>MokaCode</c> which is an inline code element, this renders a full
///     <c>&lt;pre&gt;&lt;code&gt;</c> block suitable for multi-line code snippets.
/// </summary>
/// <remarks>
///     The browser copies inside the click, reading the lines already on the page, because Safari, iOS and Firefox
///     refuse a copy made once the click is over. .NET only hears how it went, and the lines don't redraw for it.
/// </remarks>
public partial class MokaCodeBlock
{
	private static readonly TimeSpan ResetDelay = TimeSpan.FromSeconds(2);

	private bool _copied;
	private int _copyCount;
	private bool _disposed;
	private string? _drawnCode;
	private string? _drawnMaxHeight;
	private bool _drawnNumbers;
	private string[] _lines = [string.Empty];
	private int _listingVersion;
	private ITimer? _resetTimer;

	// Optional, so looked up rather than injected: [Inject] throws when a service is missing.
	[Inject]
	private IServiceProvider Services { get; set; } = default!;

	// An app can register its own clock. A test does, so no test waits on a real reset.
	private TimeProvider Clock => Services.GetService<TimeProvider>() ?? TimeProvider.System;

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

	private bool ShowHeader => Language is not null || ShowCopyButton;

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		string code = Code ?? string.Empty;
		if (_drawnCode == code && _drawnNumbers == ShowLineNumbers && _drawnMaxHeight == MaxHeight)
		{
			return;
		}

		_drawnCode = code;
		_drawnNumbers = ShowLineNumbers;
		_drawnMaxHeight = MaxHeight;
		_lines = code.Split('\n');
		_listingVersion++;
	}

	private void HandleCopied(MokaCopyEventArgs args)
	{
		// A refused copy leaves the button as it was.
		if (!args.Copied || _disposed)
		{
			return;
		}

		_copied = true;
		ForceRender();

		// Only the latest copy's timer resets the button: disposing the older timer doesn't stop a callback that
		// already fired and is waiting for the renderer.
		int copy = ++_copyCount;
		_resetTimer?.Dispose();
		_resetTimer = Clock.CreateTimer(_ => RunDetached(() => Reset(copy)), null, ResetDelay, Timeout.InfiniteTimeSpan);
	}

	private void Reset(int copy)
	{
		if (copy != _copyCount)
		{
			return;
		}

		_copied = false;
		ForceRender();
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		_disposed = true;
		_resetTimer?.Dispose();
		_resetTimer = null;

		await base.DisposeAsyncCore();
	}
}
