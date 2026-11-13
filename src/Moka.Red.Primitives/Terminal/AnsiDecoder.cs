using System.Globalization;
using System.Text;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.Terminal;

/// <summary>A terminal color: one of the 256 palette entries (0 to 15 are the classic ones), or a 24-bit RGB value.</summary>
internal readonly record struct AnsiColor(int Value, bool IsRgb = false)
{
	private static readonly (int R, int G, int B)[] ClassicColors =
	[
		(0, 0, 0), (205, 0, 0), (0, 205, 0), (205, 205, 0), (0, 0, 238), (205, 0, 205), (0, 205, 205), (229, 229, 229),
		(127, 127, 127), (255, 0, 0), (0, 255, 0), (255, 255, 0), (92, 92, 255), (255, 0, 255), (0, 255, 255), (255, 255, 255)
	];

	/// <summary>
	///     The classic color this one draws as: itself, or the nearest of the 16. A theme picks those, so what a program
	///     wrote for a dark screen stays readable on a light one.
	/// </summary>
	public int Classic
	{
		get
		{
			if (!IsRgb && Value < 16)
			{
				return Value;
			}

			(int r, int g, int b) = Rgb();
			int nearest = 0;
			int nearestDistance = int.MaxValue;
			for (int i = 0; i < ClassicColors.Length; i++)
			{
				(int cr, int cg, int cb) = ClassicColors[i];
				int distance = (r - cr) * (r - cr) + (g - cg) * (g - cg) + (b - cb) * (b - cb);
				if (distance < nearestDistance)
				{
					nearest = i;
					nearestDistance = distance;
				}
			}

			return nearest;
		}
	}

	private (int R, int G, int B) Rgb()
	{
		if (IsRgb)
		{
			return ((Value >> 16) & 0xFF, (Value >> 8) & 0xFF, Value & 0xFF);
		}

		if (Value >= 232)
		{
			int gray = 8 + (Value - 232) * 10;
			return (gray, gray, gray);
		}

		int[] steps = [0, 95, 135, 175, 215, 255];
		int cube = Value - 16;
		return (steps[cube / 36], steps[cube / 6 % 6], steps[cube % 6]);
	}
}

/// <summary>How a run of text looks.</summary>
internal sealed record AnsiStyle(
	AnsiColor? Foreground = null,
	AnsiColor? Background = null,
	bool Bold = false,
	bool Dim = false,
	bool Italic = false,
	bool Underline = false,
	bool Inverse = false)
{
	public static readonly AnsiStyle Plain = new();

	/// <summary>The classes that draw this style. They read theme tokens in the terminal's stylesheet.</summary>
	public string Classes
	{
		get
		{
			(AnsiColor? foreground, AnsiColor? background) = Inverse ? (Background, Foreground) : (Foreground, Background);
			return new CssBuilder()
				.AddClass("moka-ansi-fi", Inverse && foreground is null)
				.AddClass("moka-ansi-bi", Inverse && background is null)
				.AddClass(foreground is { } f ? $"moka-ansi-f{f.Classic}" : null)
				.AddClass(background is { } b ? $"moka-ansi-b{b.Classic}" : null)
				.AddClass("moka-ansi-bold", Bold)
				.AddClass("moka-ansi-dim", Dim)
				.AddClass("moka-ansi-italic", Italic)
				.AddClass("moka-ansi-underline", Underline)
				.Build();
		}
	}

	/// <summary>Whether this style fills the cell behind the text, as a QR code drawn in half blocks does.</summary>
	public bool FillsCell => Background is not null || Inverse;
}

/// <summary>A run of text in one style.</summary>
internal sealed record AnsiSpan(string Text, AnsiStyle Style);

/// <summary>
///     Turns a program's output, as it arrives in pieces, into lines of styled text: SGR color and weight codes are kept
///     (the 16 classic colors, 256 and 24-bit ones), every other escape sequence and control character is dropped, so
///     output can't move a cursor, clear the screen or retitle anything. A sequence split across two pieces is still read
///     as one.
/// </summary>
internal sealed class AnsiDecoder
{
	private readonly StringBuilder _csi = new();
	private readonly List<AnsiSpan> _line = [];
	private readonly StringBuilder _text = new();
	private State _state;
	private AnsiStyle _style = AnsiStyle.Plain;

	/// <summary>The line being written: what arrived since the last line break.</summary>
	public IReadOnlyList<AnsiSpan> Partial => _text.Length == 0 ? [.. _line] : [.. _line, new AnsiSpan(_text.ToString(), _style)];

	/// <summary>Reads a piece of output; returns the lines it finished.</summary>
	public List<IReadOnlyList<AnsiSpan>> Feed(string chunk)
	{
		ArgumentNullException.ThrowIfNull(chunk);

		List<IReadOnlyList<AnsiSpan>> done = [];
		foreach (char c in chunk)
		{
			switch (_state)
			{
				case State.Escape:
					_state = c switch
					{
						'[' => State.Csi,
						']' => State.Osc,
						_ => State.Text
					};
					_csi.Clear();
					break;
				case State.Csi:
					if (c is >= '@' and <= '~')
					{
						if (c == 'm')
						{
							Select(_csi.ToString());
						}

						_state = State.Text;
					}
					else if (_csi.Length < 64)
					{
						_csi.Append(c);
					}

					break;
				case State.Osc:
					_state = c switch
					{
						'\a' => State.Text,
						'\u001b' => State.OscEscape,
						_ => State.Osc
					};
					break;
				case State.OscEscape:
					_state = c == '\\' ? State.Text : State.Osc;
					break;
				default:
					if (c == '\u001b')
					{
						_state = State.Escape;
					}
					else if (c == '\n')
					{
						done.Add(Finish());
					}
					else if (c == '\t' || !char.IsControl(c))
					{
						_text.Append(c);
					}

					break;
			}
		}

		return done;
	}

	/// <summary>Ends the output: the last line, if it had no line break.</summary>
	public IReadOnlyList<AnsiSpan>? Flush()
	{
		_state = State.Text;
		return _line.Count == 0 && _text.Length == 0 ? null : Finish();
	}

	/// <summary>
	///     Reads one whole line of a terminal's output. The style carries on to the next line, as it does on a terminal, and
	///     a line break inside the text stays in it.
	/// </summary>
	public IReadOnlyList<AnsiSpan> ReadLine(string text)
	{
		List<IReadOnlyList<AnsiSpan>> finished = Feed(text);
		IReadOnlyList<AnsiSpan> last = Flush() ?? [];
		if (finished.Count == 0)
		{
			return last;
		}

		List<AnsiSpan> joined = [];
		foreach (IReadOnlyList<AnsiSpan> line in finished)
		{
			joined.AddRange(line);
			joined.Add(new AnsiSpan("\n", AnsiStyle.Plain));
		}

		joined.AddRange(last);
		return joined;
	}

	/// <summary>The text of a line without its styles.</summary>
	public static string Text(IReadOnlyList<AnsiSpan> line) => string.Concat(line.Select(s => s.Text));

	/// <summary>A text with its escape sequences and control characters taken out, for copying.</summary>
	public static string Strip(string text) => Text(new AnsiDecoder().ReadLine(text));

	/// <summary>All of a text's lines at once.</summary>
	public static List<IReadOnlyList<AnsiSpan>> Decode(string text)
	{
		AnsiDecoder decoder = new();
		List<IReadOnlyList<AnsiSpan>> lines = decoder.Feed(text);
		if (decoder.Flush() is { } last)
		{
			lines.Add(last);
		}

		return lines;
	}

	private AnsiSpan[] Finish()
	{
		Close();
		AnsiSpan[] line = [.. _line];
		_line.Clear();
		return line;
	}

	private void Close()
	{
		if (_text.Length > 0)
		{
			_line.Add(new AnsiSpan(_text.ToString(), _style));
			_text.Clear();
		}
	}

	private void Select(string parameters)
	{
		int[] codes =
		[
			.. parameters.Split(';', ':')
				.Select(p => int.TryParse(p, NumberStyles.None, CultureInfo.InvariantCulture, out int n) ? n : 0)
		];
		AnsiStyle next = _style;
		for (int i = 0; i < codes.Length; i++)
		{
			int code = codes[i];
			next = code switch
			{
				0 => AnsiStyle.Plain,
				1 => next with { Bold = true },
				2 => next with { Dim = true },
				3 => next with { Italic = true },
				4 => next with { Underline = true },
				7 => next with { Inverse = true },
				22 => next with { Bold = false, Dim = false },
				23 => next with { Italic = false },
				24 => next with { Underline = false },
				27 => next with { Inverse = false },
				>= 30 and <= 37 => next with { Foreground = new AnsiColor(code - 30) },
				39 => next with { Foreground = null },
				>= 40 and <= 47 => next with { Background = new AnsiColor(code - 40) },
				49 => next with { Background = null },
				>= 90 and <= 97 => next with { Foreground = new AnsiColor(code - 90 + 8) },
				>= 100 and <= 107 => next with { Background = new AnsiColor(code - 100 + 8) },
				38 or 48 => Extended(next, code == 38, codes, ref i),
				_ => next
			};
		}

		if (next != _style)
		{
			Close();
			_style = next;
		}
	}

	// 38;5;n and 38;2;r;g;b (48 for the background).
	private static AnsiStyle Extended(AnsiStyle style, bool foreground, int[] codes, ref int i)
	{
		AnsiColor? color = null;
		if (i + 2 < codes.Length && codes[i + 1] == 5)
		{
			color = new AnsiColor(Math.Clamp(codes[i + 2], 0, 255));
			i += 2;
		}
		else if (i + 4 < codes.Length && codes[i + 1] == 2)
		{
			color = new AnsiColor(
				(Math.Clamp(codes[i + 2], 0, 255) << 16) | (Math.Clamp(codes[i + 3], 0, 255) << 8) |
				Math.Clamp(codes[i + 4], 0, 255), true);
			i += 4;
		}

		if (color is null)
		{
			return style;
		}

		return foreground ? style with { Foreground = color } : style with { Background = color };
	}

	private enum State
	{
		Text,
		Escape,
		Csi,
		Osc,
		OscEscape
	}
}
