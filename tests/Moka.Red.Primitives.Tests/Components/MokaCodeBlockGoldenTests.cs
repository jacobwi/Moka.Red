using Bunit;
using Moka.Red.Primitives.CodeBlock;

namespace Moka.Red.Primitives.Tests.Components;

/// <summary>
///     The whole markup of a code block as it was before it could color code. With no <c>Highlighter</c> it has to stay
///     byte for byte what these files hold, apart from the scope attributes. <c>MOKA_RECORD_GOLDENS=1</c> writes them again.
/// </summary>
public sealed class MokaCodeBlockGoldenTests : BunitContext
{
	private const string Listing = "using System;\n\npublic class Greeter\n{\n\tpublic string Hello(string name) => $\"<b>{name}</b>\";\n}";

	[Fact]
	public void Default() => SmallChartGoldenTests.Golden("code-block-default", () => Render<MokaCodeBlock>(p => p
		.Add(x => x.Code, Listing)));

	[Fact]
	public void LanguageWrapAndHeight() => SmallChartGoldenTests.Golden("code-block-options", () => Render<MokaCodeBlock>(p => p
		.Add(x => x.Code, "first line\n\tsecond line  \n")
		.Add(x => x.Language, "csharp")
		.Add(x => x.ShowLineNumbers, false)
		.Add(x => x.Wrap, true)
		.Add(x => x.MaxHeight, "200px")
		.Add(x => x.Class, "mine")
		.Add(x => x.Style, "opacity: 0.9")
		.Add(x => x.Id, "sample")
		.AddUnmatched("data-sample", "yes")));

	[Fact]
	public void NoHeader() => SmallChartGoldenTests.Golden("code-block-bare", () => Render<MokaCodeBlock>(p => p
		.Add(x => x.Code, "dotnet add package Moka.Red")
		.Add(x => x.ShowCopyButton, false)));
}
