using System.Globalization;
using Moka.Red.Primitives.Diff;

namespace Moka.Red.Primitives.Tests.Components;

#pragma warning disable CA5394 // Seeded Random keeps the property tests repeatable; nothing here is security related

public class LineDiffEngineTests
{
	private static readonly string[] Tokens = ["a", "b", "c", "", "}", "{"];

	[Fact]
	public void ChangedLines_ListRemovedBeforeAdded()
	{
		IReadOnlyList<MokaDiffLine> diff = LineDiffEngine.Compute("a\nb\nc", "a\nx\ny\nc");

		Assert.Equal(
		[
			new MokaDiffLine(MokaDiffLineKind.Unchanged, "a"),
			new MokaDiffLine(MokaDiffLineKind.Removed, "b"),
			new MokaDiffLine(MokaDiffLineKind.Added, "x"),
			new MokaDiffLine(MokaDiffLineKind.Added, "y"),
			new MokaDiffLine(MokaDiffLineKind.Unchanged, "c")
		], diff);
	}

	[Fact]
	public void DifferentLineEndings_CompareEqual()
	{
		IReadOnlyList<MokaDiffLine> diff = LineDiffEngine.Compute("a\r\nb\r\n", "a\nb\n");

		Assert.All(diff, line => Assert.Equal(MokaDiffLineKind.Unchanged, line.Kind));
	}

	// The old table walk matched the closing brace and blank line of the existing function to the
	// new function's, so the added block started inside the old function.
	[Fact]
	public void AppendedFunction_IsOneBlockAfterTheExistingOne()
	{
		const string before = "function a() {\n  return 1;\n}\n";
		const string after = before + "\nfunction b() {\n  return 2;\n}\n";

		IReadOnlyList<MokaDiffLine> diff = LineDiffEngine.Compute(before, after);

		Assert.Equal(
		[
			new MokaDiffLine(MokaDiffLineKind.Unchanged, "function a() {"),
			new MokaDiffLine(MokaDiffLineKind.Unchanged, "  return 1;"),
			new MokaDiffLine(MokaDiffLineKind.Unchanged, "}"),
			new MokaDiffLine(MokaDiffLineKind.Added, ""),
			new MokaDiffLine(MokaDiffLineKind.Added, "function b() {"),
			new MokaDiffLine(MokaDiffLineKind.Added, "  return 2;"),
			new MokaDiffLine(MokaDiffLineKind.Added, "}"),
			new MokaDiffLine(MokaDiffLineKind.Unchanged, "")
		], diff);
	}

	[Fact]
	public void AppendedFunction_WithoutTrailingNewline_IsOneBlockAtTheEnd()
	{
		IReadOnlyList<MokaDiffLine> diff = LineDiffEngine.Compute(
			"function a() {\n}",
			"function a() {\n}\n\nfunction b() {\n}");

		Assert.Equal(
		[
			new MokaDiffLine(MokaDiffLineKind.Unchanged, "function a() {"),
			new MokaDiffLine(MokaDiffLineKind.Unchanged, "}"),
			new MokaDiffLine(MokaDiffLineKind.Added, ""),
			new MokaDiffLine(MokaDiffLineKind.Added, "function b() {"),
			new MokaDiffLine(MokaDiffLineKind.Added, "}")
		], diff);
	}

	// The common end is taken first here, which leaves the added block starting with "}" and "".
	// Sliding it down makes it read as the inserted function.
	[Fact]
	public void InsertedBlock_SlidesDownPastTheLinesItRepeats()
	{
		IReadOnlyList<MokaDiffLine> diff = LineDiffEngine.Compute(
			"x\na\n}\n\nc\n}",
			"y\na\n}\n\nb\n}\n\nc\n}");

		Assert.Equal(
		[
			new MokaDiffLine(MokaDiffLineKind.Removed, "x"),
			new MokaDiffLine(MokaDiffLineKind.Added, "y"),
			new MokaDiffLine(MokaDiffLineKind.Unchanged, "a"),
			new MokaDiffLine(MokaDiffLineKind.Unchanged, "}"),
			new MokaDiffLine(MokaDiffLineKind.Unchanged, ""),
			new MokaDiffLine(MokaDiffLineKind.Added, "b"),
			new MokaDiffLine(MokaDiffLineKind.Added, "}"),
			new MokaDiffLine(MokaDiffLineKind.Added, ""),
			new MokaDiffLine(MokaDiffLineKind.Unchanged, "c"),
			new MokaDiffLine(MokaDiffLineKind.Unchanged, "}")
		], diff);
	}

	// Above 2,000 lines a side the old engine gave up and listed everything as removed, then added.
	[Fact]
	public void LargeTexts_WithScatteredEdits_KeepEveryOtherLine()
	{
		string[] before = Enumerable.Range(0, 20_000).Select(i => "line " + i.ToString(CultureInfo.InvariantCulture)).ToArray();
		string[] after = (string[])before.Clone();
		for (int i = 1_000; i < after.Length; i += 2_000)
		{
			after[i] = "changed " + i.ToString(CultureInfo.InvariantCulture);
		}

		IReadOnlyList<MokaDiffLine> diff = LineDiffEngine.Compute(string.Join('\n', before), string.Join('\n', after));

		Assert.Equal(10, diff.Count(l => l.Kind == MokaDiffLineKind.Removed));
		Assert.Equal(10, diff.Count(l => l.Kind == MokaDiffLineKind.Added));
		Assert.Equal(19_990, diff.Count(l => l.Kind == MokaDiffLineKind.Unchanged));
	}

	[Fact]
	public void RandomTexts_ReproduceBothSides_WithAShortestDiff()
	{
		var random = new Random(20260919);
		for (int run = 0; run < 1_500; run++)
		{
			string[] before = RandomLines(random, random.Next(1, 30));
			string[] after = RandomLines(random, random.Next(1, 30));

			AssertShortestDiff(before, after, $"run {run}");
		}
	}

	[Fact]
	public void RandomEdits_ReproduceBothSides_WithAShortestDiff()
	{
		var random = new Random(918);
		for (int run = 0; run < 300; run++)
		{
			string[] before = RandomLines(random, random.Next(1, 200));
			string[] after = Edit(random, before, random.Next(0, 25));

			AssertShortestDiff(before, after, $"run {run}");
		}
	}

	[Fact]
	public void OutOfBudget_StillReproducesBothSides()
	{
		var random = new Random(7);
		foreach (long budget in new long[] { 0, 50, 500, 5_000 })
		{
			for (int run = 0; run < 100; run++)
			{
				string[] before = RandomLines(random, random.Next(1, 120));
				string[] after = Edit(random, before, random.Next(0, 40));

				IReadOnlyList<MokaDiffLine> diff = LineDiffEngine.Compute(string.Join('\n', before), string.Join('\n', after), budget);

				Assert.Equal(before, OldSide(diff));
				Assert.Equal(after, NewSide(diff));
				AssertRemovedBeforeAdded(diff, $"budget {budget}, run {run}");
			}
		}
	}

	[Fact]
	public void NoBudget_ShowsTheMiddleAsRemovedThenAdded()
	{
		IReadOnlyList<MokaDiffLine> diff = LineDiffEngine.Compute("start\na\nb\nend", "start\nb\na\nend", 0);

		Assert.Equal(
		[
			new MokaDiffLine(MokaDiffLineKind.Unchanged, "start"),
			new MokaDiffLine(MokaDiffLineKind.Removed, "a"),
			new MokaDiffLine(MokaDiffLineKind.Removed, "b"),
			new MokaDiffLine(MokaDiffLineKind.Added, "b"),
			new MokaDiffLine(MokaDiffLineKind.Added, "a"),
			new MokaDiffLine(MokaDiffLineKind.Unchanged, "end")
		], diff);
	}

	private static void AssertShortestDiff(string[] before, string[] after, string context)
	{
		IReadOnlyList<MokaDiffLine> diff = LineDiffEngine.Compute(string.Join('\n', before), string.Join('\n', after));

		if (!before.SequenceEqual(OldSide(diff)) || !after.SequenceEqual(NewSide(diff)))
		{
			Assert.Fail($"{context}: the diff does not reproduce both texts. {Describe(before, after)}");
		}

		int kept = diff.Count(l => l.Kind == MokaDiffLineKind.Unchanged);
		int longest = LongestCommonSubsequence(before, after);
		if (kept != longest)
		{
			Assert.Fail($"{context}: kept {kept} lines, the longest common subsequence has {longest}. {Describe(before, after)}");
		}

		AssertRemovedBeforeAdded(diff, context);
		AssertBlocksSlidDown(diff, context);
	}

	private static void AssertRemovedBeforeAdded(IReadOnlyList<MokaDiffLine> diff, string context)
	{
		for (int i = 1; i < diff.Count; i++)
		{
			if (diff[i].Kind == MokaDiffLineKind.Removed && diff[i - 1].Kind == MokaDiffLineKind.Added)
			{
				Assert.Fail($"{context}: a removed line follows an added one at row {i}.");
			}
		}
	}

	// A block of one kind could still move down when the kept line after it repeats its first line
	// and nothing changed comes right after that. A kept empty last line (both texts' final newline)
	// does not count.
	private static void AssertBlocksSlidDown(IReadOnlyList<MokaDiffLine> diff, string context)
	{
		int i = 0;
		while (i < diff.Count)
		{
			if (diff[i].Kind == MokaDiffLineKind.Unchanged)
			{
				i++;
				continue;
			}

			int start = i;
			while (i < diff.Count && diff[i].Kind != MokaDiffLineKind.Unchanged)
			{
				i++;
			}

			bool singleKind = diff.Skip(start).Take(i - start).All(l => l.Kind == diff[start].Kind);
			bool canSlide = i < diff.Count
			                && diff[i].Text == diff[start].Text
			                && (i + 1 == diff.Count
				                ? diff[i].Text.Length > 0
				                : diff[i + 1].Kind == MokaDiffLineKind.Unchanged);
			if (singleKind && canSlide)
			{
				Assert.Fail($"{context}: the block at row {start} could still slide down.");
			}
		}
	}

	private static string[] OldSide(IReadOnlyList<MokaDiffLine> diff) =>
		diff.Where(l => l.Kind != MokaDiffLineKind.Added).Select(l => l.Text).ToArray();

	private static string[] NewSide(IReadOnlyList<MokaDiffLine> diff) =>
		diff.Where(l => l.Kind != MokaDiffLineKind.Removed).Select(l => l.Text).ToArray();

	private static int LongestCommonSubsequence(string[] a, string[] b)
	{
		int[] previous = new int[b.Length + 1];
		int[] current = new int[b.Length + 1];
		for (int i = 1; i <= a.Length; i++)
		{
			for (int j = 1; j <= b.Length; j++)
			{
				current[j] = a[i - 1] == b[j - 1]
					? previous[j - 1] + 1
					: Math.Max(previous[j], current[j - 1]);
			}

			(previous, current) = (current, previous);
		}

		return previous[b.Length];
	}

	// Mostly repeated tokens, so many different shortest diffs exist, plus a few unique lines.
	private static string[] RandomLines(Random random, int count) =>
		Enumerable.Range(0, count)
			.Select(_ => random.Next(10) == 0
				? "u" + random.Next(1_000_000).ToString(CultureInfo.InvariantCulture)
				: Tokens[random.Next(Tokens.Length)])
			.ToArray();

	private static string[] Edit(Random random, string[] lines, int edits)
	{
		var result = new List<string>(lines);
		for (int e = 0; e < edits; e++)
		{
			int at = random.Next(result.Count + 1);
			switch (random.Next(3))
			{
				case 0:
					result.Insert(at, RandomLines(random, 1)[0]);
					break;
				case 1 when result.Count > 1 && at < result.Count:
					result.RemoveAt(at);
					break;
				default:
					if (at < result.Count)
					{
						result[at] = RandomLines(random, 1)[0];
					}

					break;
			}
		}

		return result.ToArray();
	}

	private static string Describe(string[] before, string[] after) =>
		$"Old: [{string.Join(", ", before.Select(l => $"\"{l}\""))}] New: [{string.Join(", ", after.Select(l => $"\"{l}\""))}]";
}
