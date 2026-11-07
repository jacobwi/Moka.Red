using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.Terminal;

/// <summary>
///     Up to <see cref="LinesPerChunk" /> lines of terminal output. The terminal redraws on every new line, and a chunk
///     holding the same lines as last time would draw the same thing, so it skips the render. On a long log that leaves
///     only the last chunk drawing.
/// </summary>
/// <remarks>
///     It compares every line it holds, so a line rewritten long after it was written still shows. Comparing is cheap next
///     to a render: the lines are records, and the same record answers at once.
/// </remarks>
internal sealed class MokaTerminalChunk : ComponentBase
{
	/// <summary>How many lines one chunk holds.</summary>
	public const int LinesPerChunk = 200;

	private bool _changed = true;
	private MokaTerminalLine[] _drawnLines = [];
	private bool _drawnNumbers;
	private IReadOnlyList<AnsiSpan>[]? _drawnSpans;

	/// <summary>All the lines of the output, this chunk's among them.</summary>
	[Parameter]
	public IReadOnlyList<MokaTerminalLine> Lines { get; set; } = [];

	/// <summary>The decoded spans of <see cref="Lines" />, position for position, or null when ANSI is off.</summary>
	[Parameter]
	public IReadOnlyList<IReadOnlyList<AnsiSpan>>? Spans { get; set; }

	/// <summary>Where in <see cref="Lines" /> this chunk starts.</summary>
	[Parameter]
	public int Start { get; set; }

	/// <summary>Whether each line shows its number.</summary>
	[Parameter]
	public bool ShowLineNumbers { get; set; }

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		int count = Math.Clamp(Lines.Count - Start, 0, LinesPerChunk);
		_changed = !SameAsDrawn(count);
		if (!_changed)
		{
			return;
		}

		_drawnNumbers = ShowLineNumbers;
		_drawnLines = new MokaTerminalLine[count];
		_drawnSpans = Spans is null ? null : new IReadOnlyList<AnsiSpan>[count];
		for (int i = 0; i < count; i++)
		{
			_drawnLines[i] = Lines[Start + i];
			if (_drawnSpans is not null)
			{
				_drawnSpans[i] = Spans![Start + i];
			}
		}
	}

	/// <inheritdoc />
	protected override bool ShouldRender() => _changed;

	/// <inheritdoc />
	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		ArgumentNullException.ThrowIfNull(builder);

		for (int i = 0; i < _drawnLines.Length; i++)
		{
			MokaTerminalLine line = _drawnLines[i];
			IReadOnlyList<AnsiSpan>? spans = _drawnSpans?[i];

			builder.OpenElement(0, "div");
			builder.AddAttribute(1, "class", LineClass(spans));
			if (_drawnNumbers)
			{
				builder.OpenElement(2, "span");
				builder.AddAttribute(3, "class", "moka-terminal-line-number");
				builder.AddContent(4, (Start + i + 1).ToString(CultureInfo.CurrentCulture));
				builder.CloseElement();
			}

			if (!string.IsNullOrEmpty(line.Prefix))
			{
				builder.OpenElement(5, "span");
				builder.AddAttribute(6, "class", "moka-terminal-prefix");
				builder.AddContent(7, line.Prefix);
				builder.CloseElement();
			}

			builder.OpenElement(8, "span");
			builder.AddAttribute(9, "class", "moka-terminal-text");
			builder.AddAttribute(10, "style", LineStyle(line));
			if (spans is null)
			{
				builder.AddContent(11, line.Text);
			}
			else
			{
				AddSpans(builder, spans);
			}

			builder.CloseElement();
			builder.CloseElement();
		}
	}

	private static void AddSpans(RenderTreeBuilder builder, IReadOnlyList<AnsiSpan> spans)
	{
		builder.OpenRegion(12);
		foreach (AnsiSpan span in spans)
		{
			if (span.Style == AnsiStyle.Plain)
			{
				builder.AddContent(0, span.Text);
				continue;
			}

			builder.OpenElement(1, "span");
			builder.AddAttribute(2, "class", span.Style.Classes);
			builder.AddContent(3, span.Text);
			builder.CloseElement();
		}

		builder.CloseRegion();
	}

	private static string LineClass(IReadOnlyList<AnsiSpan>? spans) => new CssBuilder("moka-terminal-line")
		.AddClass("moka-terminal-line--blocks", spans is not null && spans.Any(span => span.Style.FillsCell))
		.Build();

	private static string? LineStyle(MokaTerminalLine line) => new StyleBuilder()
		.AddStyle("color", CssValues.IsColor(line.Color) ? line.Color.Trim() : null)
		.Build();

	private bool SameAsDrawn(int count)
	{
		if (count != _drawnLines.Length || ShowLineNumbers != _drawnNumbers || (Spans is null) != (_drawnSpans is null))
		{
			return false;
		}

		for (int i = 0; i < count; i++)
		{
			if (!_drawnLines[i].Equals(Lines[Start + i]))
			{
				return false;
			}

			if (_drawnSpans is not null && !ReferenceEquals(_drawnSpans[i], Spans![Start + i]))
			{
				return false;
			}
		}

		return true;
	}
}
