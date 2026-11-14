using System.Diagnostics;
using System.Text;
using Moka.Red.Core.Code;

namespace Moka.Red.Core.Tests.Code;

public sealed class MokaCodeHighlightersTests
{
	public static TheoryData<string, string> Names => new()
	{
		{ "razor", nameof(MokaCodeHighlighters.Razor) },
		{ "CSHTML", nameof(MokaCodeHighlighters.Razor) },
		{ "csharp", nameof(MokaCodeHighlighters.CSharp) },
		{ "cs", nameof(MokaCodeHighlighters.CSharp) },
		{ "C#", nameof(MokaCodeHighlighters.CSharp) },
		{ "html", nameof(MokaCodeHighlighters.Html) },
		{ "xml", nameof(MokaCodeHighlighters.Xml) },
		{ "XAML", nameof(MokaCodeHighlighters.Xml) },
		{ "css", nameof(MokaCodeHighlighters.Css) },
		{ "Json", nameof(MokaCodeHighlighters.Json) },
		{ "bash", nameof(MokaCodeHighlighters.Shell) },
		{ "sh", nameof(MokaCodeHighlighters.Shell) },
		{ "shell", nameof(MokaCodeHighlighters.Shell) },
		{ "PowerShell", nameof(MokaCodeHighlighters.Shell) },
		{ "ps1", nameof(MokaCodeHighlighters.Shell) },
		{ "pwsh", nameof(MokaCodeHighlighters.Shell) }
	};

	public static TheoryData<string, string> Pathological => new()
	{
		{ "csharp", Repeat("$\"{", 60_000) + "x" + Repeat("}\"", 60_000) },
		{ "csharp", "var s = \"" + new string('a', 400_000) },
		{ "csharp", "var s = @\"" + new string('a', 400_000) },
		{ "csharp", "var s = \"\"\"" + new string('a', 400_000) },
		{ "csharp", "/*" + new string('a', 400_000) },
		{ "csharp", Repeat("a<", 200_000) },
		{ "csharp", Repeat("Foo ", 100_000) },
		{ "csharp", Repeat("(", 400_000) },
		{ "csharp", Repeat("[a][", 100_000) },
		{ "razor", Repeat("@if (a) { <div>", 30_000) },
		{ "razor", Repeat("@{<div>", 50_000) },
		{ "razor", Repeat("@(", 200_000) },
		{ "razor", Repeat("<a b=\"@x ", 50_000) },
		{ "razor", "@*" + new string('a', 400_000) },
		{ "razor", "<!--" + new string('a', 400_000) },
		{ "razor", "@code {" + new string('{', 200_000) },
		{ "razor", Repeat("<", 400_000) },
		{ "html", "<a title=\"" + new string('a', 400_000) },
		{ "css", Repeat("a{", 200_000) },
		{ "css", "/*" + new string('a', 400_000) },
		{ "json", Repeat("[\"", 200_000) },
		{ "shell", "cat <<EOF\n" + Repeat("line\n", 100_000) },
		{ "shell", Repeat("\"$a", 100_000) }
	};

	private static Func<string, IEnumerable<MokaCodeSpan>> Field(string name) => name switch
	{
		nameof(MokaCodeHighlighters.Razor) => MokaCodeHighlighters.Razor,
		nameof(MokaCodeHighlighters.CSharp) => MokaCodeHighlighters.CSharp,
		nameof(MokaCodeHighlighters.Html) => MokaCodeHighlighters.Html,
		nameof(MokaCodeHighlighters.Xml) => MokaCodeHighlighters.Xml,
		nameof(MokaCodeHighlighters.Css) => MokaCodeHighlighters.Css,
		nameof(MokaCodeHighlighters.Json) => MokaCodeHighlighters.Json,
		_ => MokaCodeHighlighters.Shell
	};

	private static string Repeat(string text, int count) => new StringBuilder(text.Length * count).Insert(0, text, count).ToString();

	// A component compares its highlighter by reference, so a name has to give back the very same delegate.
	[Theory]
	[MemberData(nameof(Names))]
	public void For_GivesTheSameDelegateAsTheField(string language, string field) =>
		Assert.Same(Field(field), MokaCodeHighlighters.For(language));

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("text")]
	[InlineData("javascript")]
	[InlineData(" csharp")]
	public void For_AnyOtherName_IsNull(string? language) => Assert.Null(MokaCodeHighlighters.For(language));

	[Fact]
	public void Normalize_SortsClipsAndCuts()
	{
		MokaCodeSpan[] tidy = MokaCodeHighlighters.Normalize("abcdef",
		[
			new MokaCodeSpan(4, 10, MokaCodeTokenKind.Number),
			new MokaCodeSpan(0, 3, MokaCodeTokenKind.Keyword),
			new MokaCodeSpan(2, 3, MokaCodeTokenKind.Comment),
			new MokaCodeSpan(1, 0, MokaCodeTokenKind.Key),
			new MokaCodeSpan(5, int.MaxValue, MokaCodeTokenKind.Tag)
		]);

		Assert.Equal(
		[
			new MokaCodeSpan(0, 3, MokaCodeTokenKind.Keyword),
			new MokaCodeSpan(3, 2, MokaCodeTokenKind.Comment),
			new MokaCodeSpan(5, 1, MokaCodeTokenKind.Number)
		], tidy);
	}

	// Every file the library and its samples are written in, as the strongest check that nothing throws or overlaps.
	[Fact]
	public void EveryFileInTheRepository_HighlightsIntoValidSpans()
	{
		string root = RepositoryRoot();
		int checkedFiles = 0;
		foreach (string file in Directory.EnumerateFiles(Path.Combine(root, "src"), "*", SearchOption.AllDirectories)
			         .Concat(Directory.EnumerateFiles(Path.Combine(root, "samples"), "*", SearchOption.AllDirectories)))
		{
			string relative = Path.GetRelativePath(root, file);
			if (relative.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
			    || relative.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
			{
				continue;
			}

			Func<string, IEnumerable<MokaCodeSpan>>? highlighter = Path.GetExtension(file) switch
			{
				".razor" => MokaCodeHighlighters.Razor,
				".cs" => MokaCodeHighlighters.CSharp,
				".css" => MokaCodeHighlighters.Css,
				".json" => MokaCodeHighlighters.Json,
				".xml" or ".csproj" or ".props" => MokaCodeHighlighters.Xml,
				".html" => MokaCodeHighlighters.Html,
				".ps1" or ".sh" => MokaCodeHighlighters.Shell,
				_ => null
			};
			if (highlighter is null)
			{
				continue;
			}

			string text = File.ReadAllText(file);
			CodeSpanAssert.Valid(text, [.. highlighter(text)], relative);
			checkedFiles++;
		}

		Assert.True(checkedFiles > 1000, $"Only {checkedFiles} files were checked.");
	}

	[Fact]
	public void TwoThousandLines_HighlightInWellUnderFiftyMilliseconds()
	{
		string razor = File.ReadAllText(Path.Combine(RepositoryRoot(), "src", "Moka.Red.Forms", "CodeEditor", "MokaCodeEditor.razor"));
		string csharp = File.ReadAllText(Path.Combine(RepositoryRoot(), "src", "Moka.Red.Forms", "CodeEditor", "MokaCodeEditor.razor.cs"));

		Assert.InRange(BestTime(MokaCodeHighlighters.Razor, ToLines(razor, 2000)).TotalMilliseconds, 0, 25);
		Assert.InRange(BestTime(MokaCodeHighlighters.CSharp, ToLines(csharp, 2000)).TotalMilliseconds, 0, 25);
	}

	// Each is far past anything real. A scanner that read any part again for every character would take minutes.
	[Theory]
	[MemberData(nameof(Pathological), DisableDiscoveryEnumeration = true)]
	public void PathologicalInput_StaysLinear(string language, string code)
	{
		Func<string, IEnumerable<MokaCodeSpan>> highlighter = MokaCodeHighlighters.For(language)!;
		Stopwatch watch = Stopwatch.StartNew();
		List<MokaCodeSpan> spans = [.. highlighter(code)];
		watch.Stop();

		CodeSpanAssert.Valid(code, spans);
		Assert.InRange(watch.Elapsed.TotalSeconds, 0, 2);
	}

	private static string ToLines(string source, int lines)
	{
		string[] sourceLines = source.ReplaceLineEndings("\n").Split('\n');
		return string.Join('\n', Enumerable.Range(0, lines).Select(i => sourceLines[i % sourceLines.Length]));
	}

	private static TimeSpan BestTime(Func<string, IEnumerable<MokaCodeSpan>> highlighter, string code)
	{
		TimeSpan best = TimeSpan.MaxValue;
		for (int run = 0; run < 5; run++)
		{
			Stopwatch watch = Stopwatch.StartNew();
			_ = highlighter(code).Count();
			watch.Stop();
			best = watch.Elapsed < best ? watch.Elapsed : best;
		}

		return best;
	}

	private static string RepositoryRoot()
	{
		for (DirectoryInfo? folder = new(AppContext.BaseDirectory); folder is not null; folder = folder.Parent)
		{
			if (File.Exists(Path.Combine(folder.FullName, "Moka.Red.slnx")))
			{
				return folder.FullName;
			}
		}

		Assert.Fail("The tests are not running inside the repository.");
		return string.Empty;
	}
}
