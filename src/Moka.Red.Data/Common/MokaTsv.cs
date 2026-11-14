using System.Text;

namespace Moka.Red.Data.Common;

/// <summary>
///     Tab-separated text, the way spreadsheets copy and paste a block of cells: a cell holding a tab, a quote or a line
///     break goes in quotes with its quotes doubled, and rows end in CR LF.
/// </summary>
internal static class MokaTsv
{
	/// <summary>Writes rows of cells as one block a spreadsheet pastes into as many cells.</summary>
	/// <param name="rows">The rows, each a list of the cells' text.</param>
	/// <returns>The text.</returns>
	public static string Write(IEnumerable<IReadOnlyList<string>> rows)
	{
		StringBuilder text = new();
		foreach (IReadOnlyList<string> row in rows)
		{
			for (int i = 0; i < row.Count; i++)
			{
				if (i > 0)
				{
					text.Append('\t');
				}

				text.Append(Cell(row[i]));
			}

			text.Append("\r\n");
		}

		return text.ToString();
	}

	/// <summary>Reads a pasted block into rows of cells. A last line break makes no empty row.</summary>
	/// <param name="text">The pasted text.</param>
	/// <returns>The rows.</returns>
	public static List<List<string>> Read(string text)
	{
		List<List<string>> rows = [];
		List<string> row = [];
		StringBuilder cell = new();
		bool quoted = false;
		bool cellStarted = false;
		for (int i = 0; i < text.Length; i++)
		{
			char c = text[i];
			if (quoted)
			{
				if (c == '"' && i + 1 < text.Length && text[i + 1] == '"')
				{
					cell.Append('"');
					i++;
				}
				else if (c == '"')
				{
					quoted = false;
				}
				else
				{
					cell.Append(c);
				}

				continue;
			}

			switch (c)
			{
				case '"' when !cellStarted:
					quoted = true;
					cellStarted = true;
					break;
				case '\t':
					row.Add(cell.ToString());
					cell.Clear();
					cellStarted = false;
					break;
				case '\r' or '\n':
					if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
					{
						i++;
					}

					row.Add(cell.ToString());
					rows.Add(row);
					row = [];
					cell.Clear();
					cellStarted = false;
					break;
				default:
					cell.Append(c);
					cellStarted = true;
					break;
			}
		}

		if (cellStarted || cell.Length > 0 || row.Count > 0)
		{
			row.Add(cell.ToString());
			rows.Add(row);
		}

		return rows;
	}

	// The same guard as a CSV export: a spreadsheet would run a pasted cell that starts like a formula.
	private static string Cell(string value)
	{
		string safe = MokaCsv.Defuse(value);
		return safe.AsSpan().IndexOfAny('\t', '"', '\n') >= 0 || safe.Contains('\r', StringComparison.Ordinal)
			? $"\"{safe.Replace("\"", "\"\"", StringComparison.Ordinal)}\""
			: safe;
	}
}
