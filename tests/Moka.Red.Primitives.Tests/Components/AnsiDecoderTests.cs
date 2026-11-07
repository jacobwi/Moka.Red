using Moka.Red.Primitives.Terminal;

namespace Moka.Red.Primitives.Tests.Components;

public class AnsiDecoderTests
{
	private const string Esc = "\u001b";

	[Fact]
	public void PlainText_IsOnePlainSpan()
	{
		IReadOnlyList<AnsiSpan> line = Assert.Single(AnsiDecoder.Decode("hello"));

		AnsiSpan span = Assert.Single(line);
		Assert.Equal("hello", span.Text);
		Assert.Equal(AnsiStyle.Plain, span.Style);
	}

	[Fact]
	public void AColor_StartsANewSpan_AndAResetEndsIt()
	{
		IReadOnlyList<AnsiSpan> line = Assert.Single(AnsiDecoder.Decode($"a{Esc}[31mb{Esc}[0mc"));

		Assert.Equal(["a", "b", "c"], line.Select(s => s.Text));
		Assert.Equal(new AnsiColor(1), line[1].Style.Foreground);
		Assert.Equal(AnsiStyle.Plain, line[2].Style);
	}

	[Theory]
	[InlineData("1", nameof(AnsiStyle.Bold))]
	[InlineData("2", nameof(AnsiStyle.Dim))]
	[InlineData("3", nameof(AnsiStyle.Italic))]
	[InlineData("4", nameof(AnsiStyle.Underline))]
	[InlineData("7", nameof(AnsiStyle.Inverse))]
	public void Weights_AreKept(string code, string property)
	{
		AnsiSpan span = Assert.Single(Assert.Single(AnsiDecoder.Decode($"{Esc}[{code}mx")));

		Assert.Equal(true, typeof(AnsiStyle).GetProperty(property)!.GetValue(span.Style));
	}

	[Fact]
	public void BrightColors_AreEightToFifteen()
	{
		AnsiStyle style = Assert.Single(Assert.Single(AnsiDecoder.Decode($"{Esc}[91;102mx"))).Style;

		Assert.Equal(new AnsiColor(9), style.Foreground);
		Assert.Equal(new AnsiColor(10), style.Background);
	}

	[Fact]
	public void ExtendedColors_AreRead()
	{
		AnsiStyle style = Assert.Single(Assert.Single(AnsiDecoder.Decode($"{Esc}[38;5;196;48;2;1;2;3mx"))).Style;

		Assert.Equal(new AnsiColor(196), style.Foreground);
		Assert.Equal(new AnsiColor(0x010203, true), style.Background);
	}

	[Theory]
	[InlineData("\u001b[2Jhello")]
	[InlineData("\u001b[10;10Hhello")]
	[InlineData("\u001b]0;a title\ahello")]
	[InlineData("\u001b]0;a title\u001b\\hello")]
	[InlineData("hel\rlo")]
	[InlineData("hel\blo")]
	public void WhatIsNotAColor_IsDropped(string text)
	{
		Assert.Equal("hello", AnsiDecoder.Strip(text));
	}

	[Fact]
	public void ASequenceSplitAcrossTwoPieces_IsReadAsOne()
	{
		AnsiDecoder decoder = new();

		decoder.Feed($"a{Esc}[3");
		decoder.Feed("1mb");

		IReadOnlyList<AnsiSpan> line = decoder.Partial;
		Assert.Equal(["a", "b"], line.Select(s => s.Text));
		Assert.Equal(new AnsiColor(1), line[1].Style.Foreground);
	}

	[Fact]
	public void Feed_ReturnsTheLinesItFinished()
	{
		AnsiDecoder decoder = new();

		List<IReadOnlyList<AnsiSpan>> lines = decoder.Feed("one\ntwo\nthr");

		Assert.Equal(["one", "two"], lines.Select(AnsiDecoder.Text));
		Assert.Equal("thr", AnsiDecoder.Text(decoder.Partial));
		Assert.Equal("thr", AnsiDecoder.Text(decoder.Flush()!));
		Assert.Null(decoder.Flush());
	}

	[Fact]
	public void ReadLine_CarriesTheStyleToTheNextLine()
	{
		AnsiDecoder decoder = new();

		decoder.ReadLine($"{Esc}[32mone");
		IReadOnlyList<AnsiSpan> two = decoder.ReadLine("two");

		Assert.Equal(new AnsiColor(2), Assert.Single(two).Style.Foreground);
	}

	[Fact]
	public void ReadLine_KeepsALineBreakInTheText()
	{
		IReadOnlyList<AnsiSpan> line = new AnsiDecoder().ReadLine("one\ntwo");

		Assert.Equal("one\ntwo", AnsiDecoder.Text(line));
	}

	[Fact]
	public void ReadLine_ReadsAnEmptyLineAsNothing()
	{
		Assert.Empty(new AnsiDecoder().ReadLine(string.Empty));
	}

	[Theory]
	[InlineData(0, false, 0)]
	[InlineData(15, false, 15)]
	[InlineData(196, false, 9)]
	[InlineData(231, false, 15)]
	[InlineData(232, false, 0)]
	[InlineData(255, false, 7)]
	[InlineData(0xFFFFFF, true, 15)]
	[InlineData(0x000000, true, 0)]
	[InlineData(0xFF0000, true, 9)]
	[InlineData(0x0000EE, true, 4)]
	public void Classic_IsTheColorItselfOrTheNearest(int value, bool isRgb, int expected)
	{
		Assert.Equal(expected, new AnsiColor(value, isRgb).Classic);
	}

	[Fact]
	public void Classes_NameEachPartOfTheStyle()
	{
		AnsiStyle style = new(new AnsiColor(1), new AnsiColor(12), true, true, true, true);

		Assert.Equal("moka-ansi-f1 moka-ansi-b12 moka-ansi-bold moka-ansi-dim moka-ansi-italic moka-ansi-underline", style.Classes);
	}

	[Fact]
	public void Classes_SwapTheColors_WhenInverse()
	{
		Assert.Equal("moka-ansi-fi moka-ansi-b1", new AnsiStyle(new AnsiColor(1), Inverse: true).Classes);
		Assert.Equal("moka-ansi-bi moka-ansi-f2", new AnsiStyle(Background: new AnsiColor(2), Inverse: true).Classes);
		Assert.Equal("moka-ansi-fi moka-ansi-bi", new AnsiStyle(Inverse: true).Classes);
	}

	[Fact]
	public void FillsCell_MeansABackgroundOrInverse()
	{
		Assert.False(new AnsiStyle(new AnsiColor(1)).FillsCell);
		Assert.True(new AnsiStyle(Background: new AnsiColor(1)).FillsCell);
		Assert.True(new AnsiStyle(Inverse: true).FillsCell);
	}
}
