using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Moka.Red.Primitives.Terminal;

/// <summary>
///     A run of terminal lines, split into <see cref="MokaTerminalChunk" />s. It also keeps the decoded ANSI spans, since
///     a color set on one line carries on to the next, so each line can only be read after the one before it.
/// </summary>
internal sealed class MokaTerminalOutput : ComponentBase
{
	private readonly List<MokaTerminalLine> _decodedLines = [];
	private readonly List<IReadOnlyList<AnsiSpan>> _spans = [];
	private AnsiDecoder _decoder = new();

	/// <summary>The lines to draw.</summary>
	[Parameter]
	public IReadOnlyList<MokaTerminalLine> Lines { get; set; } = [];

	/// <summary>Whether the lines' escape sequences are decoded.</summary>
	[Parameter]
	public bool Ansi { get; set; }

	/// <summary>Whether each line shows its number.</summary>
	[Parameter]
	public bool ShowLineNumbers { get; set; }

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		if (Ansi)
		{
			DecodeNewLines();
		}
	}

	/// <inheritdoc />
	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		ArgumentNullException.ThrowIfNull(builder);

		int chunk = 0;
		for (int start = 0; start < Lines.Count; start += MokaTerminalChunk.LinesPerChunk, chunk++)
		{
			builder.OpenComponent<MokaTerminalChunk>(0);
			builder.SetKey(chunk);
			builder.AddComponentParameter(1, nameof(MokaTerminalChunk.Lines), Lines);
			builder.AddComponentParameter(2, nameof(MokaTerminalChunk.Spans), Ansi ? _spans : null);
			builder.AddComponentParameter(3, nameof(MokaTerminalChunk.Start), start);
			builder.AddComponentParameter(4, nameof(MokaTerminalChunk.ShowLineNumbers), ShowLineNumbers);
			builder.CloseComponent();
		}
	}

	private void DecodeNewLines()
	{
		if (!StartsWithDecodedLines())
		{
			_decodedLines.Clear();
			_spans.Clear();
			_decoder = new AnsiDecoder();
		}

		for (int i = _decodedLines.Count; i < Lines.Count; i++)
		{
			_decodedLines.Add(Lines[i]);
			_spans.Add(_decoder.ReadLine(Lines[i].Text));
		}
	}

	private bool StartsWithDecodedLines()
	{
		if (Lines.Count < _decodedLines.Count)
		{
			return false;
		}

		for (int i = 0; i < _decodedLines.Count; i++)
		{
			if (!_decodedLines[i].Equals(Lines[i]))
			{
				return false;
			}
		}

		return true;
	}
}
