using System.Collections.Frozen;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Core.Clipboard;
using Moka.Red.Core.Code;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.CodeBlock;

/// <summary>
///     A styled code display block with optional line numbers, language label, copy button and coloring.
///     Unlike <c>MokaCode</c> which is an inline code element, this renders a full
///     <c>&lt;pre&gt;&lt;code&gt;</c> block suitable for multi-line code snippets.
/// </summary>
/// <remarks>
///     The browser copies inside the click, reading the lines already on the page, because Safari, iOS and Firefox
///     refuse a copy made once the click is over. .NET only hears how it went, and the lines don't redraw for it.
///     The copy reads the lines' text, so colored code copies as plain text.
/// </remarks>
public partial class MokaCodeBlock
{
	private static readonly TimeSpan ResetDelay = TimeSpan.FromSeconds(2);

	private static readonly FrozenDictionary<MokaCodeTokenKind, string> TokenClasses = Enum.GetValues<MokaCodeTokenKind>()
		.Where(static kind => kind != MokaCodeTokenKind.Plain)
		.ToFrozenDictionary(static kind => kind, static kind => new CssBuilder("moka-code-block__token")
			.AddClass($"moka-code-block__token--{MokaEnumHelpers.ToCssClass(kind)}")
			.Build());

	private bool _copied;
	private int _copyCount;
	private bool _disposed;
	private string? _drawnCode;
	private Func<string, IEnumerable<MokaCodeSpan>>? _drawnHighlighter;
	private string? _drawnMaxHeight;
	private bool _drawnNumbers;
	private string[] _lines = [string.Empty];
	private MokaCodeSpan[][]? _lineTokens;
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

	/// <summary>
	///     Colors the code: given the whole code, it returns the stretches to color, with indexes into it. One of
	///     <see cref="MokaCodeHighlighters" />, such as <see cref="MokaCodeHighlighters.CSharp" />, or the app's own.
	///     Stretches can come in any order; overlaps and anything past the end are ignored. It runs when the code or the
	///     highlighter changes, so pass the same delegate each render rather than a new lambda. Null, the default, draws
	///     plain text, and <see cref="Language" /> never picks one.
	/// </summary>
	[Parameter]
	public Func<string, IEnumerable<MokaCodeSpan>>? Highlighter { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-code-block";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-code-block--line-numbers", ShowLineNumbers)
		.AddClass("moka-code-block--wrap", Wrap)
		.AddClass("moka-code-block--highlighted", _lineTokens is not null)
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
		if (_drawnCode == code && _drawnNumbers == ShowLineNumbers && _drawnMaxHeight == MaxHeight
		    && ReferenceEquals(_drawnHighlighter, Highlighter))
		{
			return;
		}

		_drawnCode = code;
		_drawnNumbers = ShowLineNumbers;
		_drawnMaxHeight = MaxHeight;
		_drawnHighlighter = Highlighter;
		// Text with \r\n split on \n alone kept a \r on every line, drawn as a trailing space and copied with the line.
		string text = code.ReplaceLineEndings("\n");
		_lines = text.Split('\n');
		_lineTokens = Highlighter is null ? null : SplitIntoLines(_lines, MokaCodeHighlighters.Normalize(text, Highlighter(text)));
		_listingVersion++;
	}

	// Each line's spans, from its own start, so a span that crosses a line break is cut at every break. Plain spans
	// draw as text.
	private static MokaCodeSpan[][] SplitIntoLines(string[] lines, MokaCodeSpan[] spans)
	{
		MokaCodeSpan[][] perLine = new MokaCodeSpan[lines.Length][];
		List<MokaCodeSpan> line = [];
		int next = 0;
		int lineStart = 0;
		for (int i = 0; i < lines.Length; i++)
		{
			int lineEnd = lineStart + lines[i].Length;
			while (next < spans.Length && spans[next].Start < lineEnd)
			{
				MokaCodeSpan span = spans[next];
				int spanEnd = span.Start + span.Length;
				int start = Math.Max(span.Start, lineStart);
				int end = Math.Min(spanEnd, lineEnd);
				if (end > start && TokenClasses.ContainsKey(span.Kind))
				{
					line.Add(new MokaCodeSpan(start - lineStart, end - start, span.Kind));
				}

				if (spanEnd > lineEnd)
				{
					break;
				}

				next++;
			}

			perLine[i] = [.. line];
			line.Clear();
			lineStart = lineEnd + 1;
		}

		return perLine;
	}

	// Built in C#, so the spans carry no scope attribute and the stylesheet reaches them through ::deep.
	private RenderFragment LineTokens(int index) => builder =>
	{
		string line = _lines[index];
		int position = 0;
		foreach (MokaCodeSpan token in _lineTokens![index])
		{
			if (token.Start > position)
			{
				builder.AddContent(0, line[position..token.Start]);
			}

			builder.OpenElement(1, "span");
			builder.AddAttribute(2, "class", TokenClasses[token.Kind]);
			builder.AddContent(3, line.Substring(token.Start, token.Length));
			builder.CloseElement();
			position = token.Start + token.Length;
		}

		if (position < line.Length)
		{
			builder.AddContent(4, line[position..]);
		}
	};

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
