using System.Diagnostics.CodeAnalysis;

namespace Moka.Red.Core.Code;

/// <summary>
///     What a stretch of code is, which decides its color in a <c>MokaCodeEditor</c> or a <c>MokaCodeBlock</c>. New
///     kinds are added at the end, so a value stored as a number keeps its meaning.
/// </summary>
public enum MokaCodeTokenKind
{
	/// <summary>Ordinary text, in the code's text color.</summary>
	Plain,

	/// <summary>A section or heading, such as <c>[Interface]</c>.</summary>
	Heading,

	/// <summary>A language keyword.</summary>
	Keyword,

	/// <summary>The name in a name and value pair, or a property.</summary>
	Key,

	/// <summary>The value in a name and value pair.</summary>
	Value,

	/// <summary>A literal that isn't a string or a number, such as an address or a date.</summary>
	Literal,

	/// <summary>A number.</summary>
	Number,

	/// <summary>A constant, such as a key or a hash.</summary>
	Constant,

	/// <summary>A comment.</summary>
	Comment,

	/// <summary>An operator or punctuation.</summary>
	Punctuation,

	/// <summary>The name of a markup element or a component, such as <c>div</c> in <c>&lt;div&gt;</c>.</summary>
	Tag,

	/// <summary>The name of a markup attribute, such as <c>class</c> in <c>class="x"</c>.</summary>
	Attribute,

	/// <summary>A string or a character literal, quotes included.</summary>
	[SuppressMessage("Naming", "CA1720:Identifier contains type name",
		Justification = "String names the kind of token, a string literal in the code, not the .NET type.")]
	String,

	/// <summary>The name of a type, such as a class in a declaration or after <c>new</c>.</summary>
	Type,

	/// <summary>A directive: a Razor directive or transition such as <c>@page</c>, or a preprocessor line.</summary>
	Directive
}
