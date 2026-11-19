using System.Collections.Frozen;

namespace Moka.Red.Core.Code;

/// <summary>
///     Highlighters for common languages, for the <c>Highlighter</c> of <c>MokaCodeBlock</c> and <c>MokaCodeEditor</c>.
///     Each one is a field, so the same delegate comes back every time and a component given one doesn't color its code
///     again on every render. They read the text once, in linear time, and mark what they recognize; anything else stays
///     uncolored.
/// </summary>
public static class MokaCodeHighlighters
{
	/// <summary>
	///     Razor: HTML with its transitions to C#, so <c>@code</c>, <c>@if</c> and the other blocks, <c>@expressions</c>,
	///     <c>@(...)</c>, directives such as <c>@page</c> and <c>@inject</c>, <c>@* comments *@</c>, component tags,
	///     directive attributes such as <c>@onclick</c>, and the markup inside code blocks.
	/// </summary>
	public static readonly Func<string, IEnumerable<MokaCodeSpan>> Razor = MarkupLexer.Razor;

	/// <summary>
	///     C#: keywords, strings of every form with interpolation holes colored as C#, characters, numbers, comments,
	///     preprocessor lines and attributes. Types are a guess from where a name stands: after <c>new</c>, in type
	///     arguments, in a declaration such as <c>Foo bar</c>, and in a base list.
	/// </summary>
	public static readonly Func<string, IEnumerable<MokaCodeSpan>> CSharp = CSharpLexer.Highlight;

	/// <summary>HTML: tags, attributes and their values, comments, the doctype, entities, and CSS inside <c>&lt;style&gt;</c>.</summary>
	public static readonly Func<string, IEnumerable<MokaCodeSpan>> Html = MarkupLexer.Html;

	/// <summary>XML and XAML: tags, attributes and their values, comments, CDATA, processing instructions and entities.</summary>
	public static readonly Func<string, IEnumerable<MokaCodeSpan>> Xml = MarkupLexer.Xml;

	/// <summary>CSS: selectors, at-rules, properties, custom properties, numbers with their units, colors, strings and comments.</summary>
	public static readonly Func<string, IEnumerable<MokaCodeSpan>> Css = CssLexer.Highlight;

	/// <summary>JSON: keys, strings, numbers, <c>true</c>, <c>false</c> and <c>null</c>, and comments where they're allowed.</summary>
	public static readonly Func<string, IEnumerable<MokaCodeSpan>> Json = JsonLexer.Highlight;

	/// <summary>
	///     Shell commands, bash and PowerShell alike: the command starting each step, options, variables, strings,
	///     here-documents and here-strings, comments and keywords.
	/// </summary>
	public static readonly Func<string, IEnumerable<MokaCodeSpan>> Shell = ShellLexer.Highlight;

	/// <summary>
	///     YAML: keys, plain scalars with the core schema's <c>null</c>, booleans and numbers, quoted strings, comments,
	///     anchors, aliases, tags, directives, document markers, sequence dashes, flow collections, and block scalars
	///     (<c>|</c> and <c>&gt;</c>), whose more-indented lines are one string.
	/// </summary>
	public static readonly Func<string, IEnumerable<MokaCodeSpan>> Yaml = YamlLexer.Highlight;

	private static readonly FrozenDictionary<string, Func<string, IEnumerable<MokaCodeSpan>>> ByName =
		new Dictionary<string, Func<string, IEnumerable<MokaCodeSpan>>>(StringComparer.OrdinalIgnoreCase)
		{
			["razor"] = Razor,
			["cshtml"] = Razor,
			["csharp"] = CSharp,
			["cs"] = CSharp,
			["c#"] = CSharp,
			["html"] = Html,
			["xml"] = Xml,
			["xaml"] = Xml,
			["css"] = Css,
			["json"] = Json,
			["bash"] = Shell,
			["sh"] = Shell,
			["shell"] = Shell,
			["powershell"] = Shell,
			["ps1"] = Shell,
			["pwsh"] = Shell,
			["yaml"] = Yaml,
			["yml"] = Yaml
		}.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

	/// <summary>
	///     The highlighter for a language name, in any case: razor, cshtml, csharp, cs, c#, html, xml, xaml, css, json,
	///     bash, sh, shell, powershell, ps1, pwsh, yaml or yml. Null for any other name, and for null.
	/// </summary>
	/// <param name="language">The language's name, such as a code block's <c>Language</c>.</param>
	public static Func<string, IEnumerable<MokaCodeSpan>>? For(string? language) =>
		language is not null && ByName.TryGetValue(language, out Func<string, IEnumerable<MokaCodeSpan>>? highlighter)
			? highlighter
			: null;

	/// <summary>
	///     Puts a highlighter's spans in order and makes them fit: empty spans are dropped, each is clipped to the text,
	///     and where two overlap the one that starts first keeps the shared part. The code block and the code editor draw
	///     any highlighter's spans through this, so a highlighter can return them in any order.
	/// </summary>
	/// <param name="text">The text the spans index into.</param>
	/// <param name="spans">The spans, in any order.</param>
	public static MokaCodeSpan[] Normalize(string text, IEnumerable<MokaCodeSpan> spans)
	{
		ArgumentNullException.ThrowIfNull(text);
		ArgumentNullException.ThrowIfNull(spans);

		List<MokaCodeSpan> tidy = [];
		int position = 0;
		foreach (MokaCodeSpan span in spans.Where(static s => s.Length > 0).OrderBy(static s => s.Start))
		{
			int start = Math.Max(span.Start, position);
			int end = (int)Math.Min((long)span.Start + span.Length, text.Length);
			if (end <= start)
			{
				continue;
			}

			tidy.Add(span with { Start = start, Length = end - start });
			position = end;
		}

		return [.. tidy];
	}
}
