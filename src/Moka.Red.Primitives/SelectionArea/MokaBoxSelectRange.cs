namespace Moka.Red.Primitives.SelectionArea;

/// <summary>
///     The items a box covers according to a <see cref="MokaBoxSelectGeometry" />, drawn or not: every item on the lines
///     from <see cref="FirstLine" /> to <see cref="LastLine" /> and in the columns from <see cref="FirstColumn" /> to
///     <see cref="LastColumn" />. With one column that is every index from <see cref="FirstIndex" /> to
///     <see cref="LastIndex" />.
/// </summary>
/// <param name="FirstLine">The first line covered, from 0.</param>
/// <param name="LastLine">The last line covered.</param>
/// <param name="FirstColumn">The first column covered, from 0.</param>
/// <param name="LastColumn">The last column covered.</param>
/// <param name="Columns">Items on each line, as the geometry said when the box measured.</param>
/// <param name="Count">How many items there are, as the geometry said when the box measured.</param>
public readonly record struct MokaBoxSelectRange(int FirstLine, int LastLine, int FirstColumn, int LastColumn, int Columns, int Count)
{
	/// <summary>The lowest index covered.</summary>
	public int FirstIndex => FirstLine * Columns + FirstColumn;

	/// <summary>The highest index covered, or -1 when the range covers no item.</summary>
	public int LastIndex
	{
		get
		{
			for (int line = LastLine; line >= FirstLine; line--)
			{
				int last = Math.Min(line * Columns + LastColumn, Count - 1);
				if (last >= line * Columns + FirstColumn)
				{
					return last;
				}
			}

			return -1;
		}
	}

	/// <summary>Every index covered, lowest first.</summary>
	public IEnumerable<int> Indexes
	{
		get
		{
			for (int line = FirstLine; line <= LastLine; line++)
			{
				for (int column = FirstColumn; column <= LastColumn; column++)
				{
					int index = line * Columns + column;
					if (index >= Count)
					{
						yield break;
					}

					yield return index;
				}
			}
		}
	}

	/// <summary>Whether the range covers an index.</summary>
	/// <param name="index">The item's index, from 0.</param>
	/// <returns>True when the item's line and column are both inside the range.</returns>
	public bool Contains(int index)
	{
		if (Columns <= 0 || index < 0 || index >= Count)
		{
			return false;
		}

		int line = index / Columns;
		int column = index % Columns;
		return line >= FirstLine && line <= LastLine && column >= FirstColumn && column <= LastColumn;
	}
}
