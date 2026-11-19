using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Moka.Red.Build;

namespace Moka.Red.Core.Tests.Build;

public sealed partial class CssMinifierTests : IDisposable
{
	private readonly string _folder = Path.Combine(Path.GetTempPath(), "moka-minify-" + Guid.NewGuid().ToString("N"));

	public CssMinifierTests() => Directory.CreateDirectory(_folder);

	public void Dispose() => Directory.Delete(_folder, true);

	[Theory]
	[InlineData("/* note */\n.a {\n\tcolor: red;\n}\n", ".a{color:red;}")]
	[InlineData(".a , .b > .c ~ .d { margin : 0 }", ".a,.b>.c~.d{margin:0}")]
	[InlineData("@media (min-width: 600px) {\n  .a { color: red }\n}", "@media (min-width: 600px){.a{color:red}}")]
	[InlineData(".a { color: red !important; }", ".a{color:red!important;}")]
	[InlineData(".a { transition: opacity .2s ease ,\n\t\ttransform .2s ease; }", ".a{transition:opacity .2s ease,transform .2s ease;}")]
	[InlineData(".a { width: min( 100% , 20rem ) }", ".a{width:min(100%,20rem)}")]
	public void Minify_DropsCommentsAndWhitespaceTheGrammarIgnores(string css, string expected) =>
		Assert.Equal(expected, MokaCssMinifier.Minify(css));

	[Theory]
	[InlineData(".a .b{}", ".a .b{}")]
	[InlineData(".a :hover{}", ".a :hover{}")]
	[InlineData(".a + .b{}", ".a + .b{}")]
	[InlineData("@media screen and (min-width: 1px){}", "@media screen and (min-width: 1px){}")]
	[InlineData(".a{width:calc(100% - 2rem + 1px)}", ".a{width:calc(100% - 2rem + 1px)}")]
	[InlineData(".a{padding:0 var(--x)}", ".a{padding:0 var(--x)}")]
	[InlineData(".a{grid-area:1 / 2}", ".a{grid-area:1 / 2}")]
	[InlineData("@container (width > 30rem) and (width >= 40rem){}", "@container (width>30rem) and (width>= 40rem){}")]
	public void Minify_KeepsWhitespaceThatSeparatesTokens(string css, string expected) =>
		Assert.Equal(expected, MokaCssMinifier.Minify(css));

	[Theory]
	[InlineData(".a::after{content:\"a /* b */  ,  c\"}", ".a::after{content:\"a /* b */  ,  c\"}")]
	[InlineData(".a::after{content: '\\00b7' }", ".a::after{content:'\\00b7'}")]
	[InlineData(".a{background:url( a/*b*/c.png )}", ".a{background:url( a/*b*/c.png )}")]
	[InlineData(".a{background:url(\"x.png\") no-repeat}", ".a{background:url(\"x.png\") no-repeat}")]
	[InlineData(".\\31 0  .b{}", ".\\31 0 .b{}")]
	[InlineData(".\\31\n\n.b{}", ".\\31\n .b{}")]
	public void Minify_CopiesStringsEscapesAndUrlsAsWritten(string css, string expected) =>
		Assert.Equal(expected, MokaCssMinifier.Minify(css));

	[Theory]
	[InlineData(".a{ --x :  a ,  b ( c )  ; }", ".a{--x:a , b ( c );}")]
	[InlineData(".a{--x:\n\t\t0 1px 2px rgb(0 0 0 / 0.2),\n\t\t0 0 0 1px red}", ".a{--x:0 1px 2px rgb(0 0 0 / 0.2), 0 0 0 1px red}")]
	[InlineData(".a{--x: 1px !important}", ".a{--x:1px!important}")]
	public void Minify_KeepsACustomPropertyValueAsWrittenButForItsEnds(string css, string expected) =>
		Assert.Equal(expected, MokaCssMinifier.Minify(css));

	[Theory]
	[InlineData(".a{width:1px/**/2px}", ".a{width:1px/**/2px}")]
	[InlineData(".a/**/{color:red}", ".a{color:red}")]
	[InlineData("/*! License */\n.a{}", "/*! License */ .a{}")]
	public void Minify_KeepsACommentOnlyWhereItSeparatesOrIsMarkedToKeep(string css, string expected) =>
		Assert.Equal(expected, MokaCssMinifier.Minify(css));

	[Fact]
	public void Minify_LeavesModernSyntaxIntact()
	{
		const string css = """
			@container anchored(fallback: flip-block) {
				.tip::before { translate: 0 -100%; }
			}
			@starting-style { .pop:popover-open { opacity: 0; } }
			@supports (color: lab(from red calc(l * 1) 0 0)) {
				.a:has(> .b) { color: lab(from var(--c) calc(l - 10) a b); }
			}
			.grid {
				--fit: max(1, round(down, (100cqi + var(--gap)) / (var(--min) + var(--gap))));
				background: light-dark(white, black);
				text-box: trim-both cap alphabetic;
				transition: display 0.2s allow-discrete;
				position-try-fallbacks: flip-block, flip-inline;
			}
			""";

		Assert.Equal(
			"@container anchored(fallback: flip-block){.tip::before{translate:0 -100%;}}" +
			"@starting-style{.pop:popover-open{opacity:0;}}" +
			"@supports (color: lab(from red calc(l * 1) 0 0)){.a:has(>.b){color:lab(from var(--c) calc(l - 10) a b);}}" +
			".grid{--fit:max(1, round(down, (100cqi + var(--gap)) / (var(--min) + var(--gap))));" +
			"background:light-dark(white,black);text-box:trim-both cap alphabetic;" +
			"transition:display 0.2s allow-discrete;position-try-fallbacks:flip-block,flip-inline;}",
			MokaCssMinifier.Minify(css));
	}

	// Every shipped stylesheet loses only whitespace and comments, and minifying the result again changes nothing.
	[Fact]
	public void Minify_KeepsEveryOtherCharacterOfTheShippedStylesheets()
	{
		List<string> files = ShippedStylesheets().ToList();
		Assert.NotEmpty(files);

		foreach (string file in files)
		{
			string css = File.ReadAllText(file);
			string minified = MokaCssMinifier.Minify(css);

			Assert.True(Significant(css) == Significant(minified), $"{file} lost more than whitespace and comments.");
			Assert.Equal(minified, MokaCssMinifier.Minify(minified));
		}
	}

	[Fact]
	public void Execute_WritesTheMinifiedFileBesideTheSource()
	{
		string source = Path.Combine(_folder, "site.css");
		string destination = Path.Combine(_folder, "min", "site.css");
		File.WriteAllText(source, ".a {\n\tcolor: red;\n}\n");
		TaskItem item = new(source);
		item.SetMetadata("MinifiedFile", destination);

		Assert.True(Run(item).Succeeded);
		Assert.Equal(".a{color:red;}", File.ReadAllText(destination));
		Assert.Equal(".a {\n\tcolor: red;\n}\n", File.ReadAllText(source));
	}

	[Fact]
	public void Execute_MinifiesInPlaceAndKeepsAByteOrderMark()
	{
		string source = Path.Combine(_folder, "bom.css");
		File.WriteAllText(source, ".a { content: \"\u00b7\"; }", new UTF8Encoding(true));

		Assert.True(Run(new TaskItem(source)).Succeeded);
		byte[] bytes = File.ReadAllBytes(source);
		Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, bytes[..3]);
		Assert.Equal(".a{content:\"\u00b7\";}", Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3));
	}

	[Fact]
	public void Execute_FailsWithAnErrorForAFileItCannotRead()
	{
		(bool succeeded, IReadOnlyList<string> errors) = Run(new TaskItem(Path.Combine(_folder, "missing.css")));

		Assert.False(succeeded);
		Assert.Single(errors);
		Assert.Contains("missing.css", errors[0], StringComparison.Ordinal);
	}

	private static (bool Succeeded, IReadOnlyList<string> Errors) Run(ITaskItem file)
	{
		RecordingBuildEngine engine = new();
		MokaMinifyCss task = new() { BuildEngine = engine, Files = [file] };
		return (task.Execute(), engine.Errors);
	}

	private static string Significant(string css) => Whitespace().Replace(Comment().Replace(css, ""), "");

	private static IEnumerable<string> ShippedStylesheets()
	{
		string src = FindSrc();
		string obj = $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}";
		string bin = $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}";
		return Directory.EnumerateFiles(src, "*.css", SearchOption.AllDirectories)
			.Where(file => !file.Contains(obj, StringComparison.Ordinal) && !file.Contains(bin, StringComparison.Ordinal));
	}

	private static string FindSrc()
	{
		for (DirectoryInfo? dir = new(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
		{
			string candidate = Path.Combine(dir.FullName, "src");
			if (Directory.Exists(Path.Combine(candidate, "Moka.Red.Core")))
			{
				return candidate;
			}
		}

		throw new DirectoryNotFoundException("Could not find the src folder above the test output.");
	}

	[GeneratedRegex(@"/\*.*?\*/", RegexOptions.Singleline)]
	private static partial Regex Comment();

	[GeneratedRegex(@"\s+")]
	private static partial Regex Whitespace();

	private sealed class RecordingBuildEngine : IBuildEngine
	{
		public List<string> Errors { get; } = [];

		public bool ContinueOnError => false;

		public int LineNumberOfTaskNode => 0;

		public int ColumnNumberOfTaskNode => 0;

		public string ProjectFileOfTaskNode => string.Empty;

		public void LogErrorEvent(BuildErrorEventArgs e) => Errors.Add(e.Message ?? string.Empty);

		public void LogWarningEvent(BuildWarningEventArgs e)
		{
		}

		public void LogMessageEvent(BuildMessageEventArgs e)
		{
		}

		public void LogCustomEvent(CustomBuildEventArgs e)
		{
		}

		public bool BuildProjectFile(string projectFileName, string[] targetNames, IDictionary globalProperties, IDictionary targetOutputs) =>
			throw new NotSupportedException();
	}
}
