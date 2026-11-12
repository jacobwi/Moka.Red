using System.Diagnostics;
using AngleSharp.Dom;
using Bunit;
using Moka.Red.Primitives.CodeBlock;

namespace Moka.Red.Primitives.Tests.Components;

// The copy ran from a .NET click handler after a module import and a call, a round trip after the click on Blazor
// Server, so Safari, iOS and Firefox refused it. The browser now copies the lines on the page inside the click, and
// .NET hears the result.
public class MokaCodeBlockTests : BunitContext
{
	private const string Code = "dotnet add package Moka.Red";

	private static IElement CopyButton(IRenderedComponent<MokaCodeBlock> cut) => cut.Find(".moka-code-block__copy");

	private IRenderedComponent<MokaCodeBlock> RenderBlock(string code = Code) =>
		Render<MokaCodeBlock>(p => p.Add(x => x.Code, code));

	private static void AssertNotCopied(IRenderedComponent<MokaCodeBlock> cut)
	{
		IElement button = CopyButton(cut);
		Assert.Equal("Copy", button.TextContent.Trim());
		Assert.False(button.HasAttribute("title"));
		Assert.DoesNotContain("moka-code-block__copy--copied", button.ClassName, StringComparison.Ordinal);
	}

	// The browser joins what these lines hold with line breaks, which is the code without the line numbers.
	[Fact]
	public void TheButton_CopiesTheLinesOnThePage_AndLeavesTheClickToTheBrowser()
	{
		const string code = "first\n\n  indented <tag>\nlast";
		IRenderedComponent<MokaCodeBlock> cut = RenderBlock(code);

		Assert.Equal(".moka-code-block__line", CopyButton(cut).GetAttribute("data-moka-copy-from"));
		Assert.Equal(code, string.Join('\n', cut.FindAll(".moka-code-block__line").Select(line => line.TextContent)));
		Assert.Throws<MissingEventHandlerException>(() => CopyButton(cut).Click());
		Assert.Empty(JSInterop.Invocations);
	}

	[Fact]
	public async Task ACopy_ShowsTheCopiedState_ThenResets()
	{
		IRenderedComponent<MokaCodeBlock> cut = RenderBlock();

		await CopyButton(cut).CopiedAsync();

		Assert.Equal("Copied!", CopyButton(cut).TextContent.Trim());
		Assert.Contains("moka-code-block__copy--copied", CopyButton(cut).ClassName, StringComparison.Ordinal);
		cut.WaitForAssertion(() => AssertNotCopied(cut), TimeSpan.FromSeconds(5));
	}

	[Fact]
	public async Task ARefusedCopy_LeavesTheButtonAsItWas()
	{
		IRenderedComponent<MokaCodeBlock> cut = RenderBlock();

		await CopyButton(cut).CopiedAsync(false);

		AssertNotCopied(cut);
	}

	// Saying "Copied!" and back used to build every line again.
	[Fact]
	public async Task TheCopiedState_DoesNotRedrawTheLines()
	{
		IRenderedComponent<MokaCodeBlock> cut = RenderBlock("a\nb\nc");
		IRenderedComponent<MokaCodeBlockGate> listing = cut.FindComponent<MokaCodeBlockGate>();
		int drawn = listing.RenderCount;

		await CopyButton(cut).CopiedAsync();
		Assert.Equal("Copied!", CopyButton(cut).TextContent.Trim());

		Assert.Equal(drawn, listing.RenderCount);
	}

	[Fact]
	public void ANewCode_RedrawsTheLines()
	{
		IRenderedComponent<MokaCodeBlock> cut = RenderBlock("a\nb");

		cut.Render(p => p.Add(x => x.Code, "a\nb\nc"));
		Assert.Equal(3, cut.FindAll(".moka-code-block__line").Count);

		cut.Render(p => p.Add(x => x.ShowLineNumbers, false));
		Assert.Empty(cut.FindAll(".moka-code-block__line-number"));

		cut.Render(p => p.Add(x => x.MaxHeight, "120px"));
		Assert.Equal("max-height: 120px", cut.Find("pre").GetAttribute("style"));
	}

	// A timer left from an earlier copy reset the button in the middle of a newer copy's two seconds.
	[Fact]
	public async Task OnlyTheLatestCopy_ResetsTheButton()
	{
		IRenderedComponent<MokaCodeBlock> cut = RenderBlock();

		await CopyButton(cut).CopiedAsync();
		await CopyButton(cut).CopiedAsync();
		await Task.Delay(TimeSpan.FromSeconds(1), Xunit.TestContext.Current.CancellationToken);
		await CopyButton(cut).CopiedAsync();
		var sinceLatest = Stopwatch.StartNew();

		// A timer left from the first two copies would fire about a second from now.
		while (sinceLatest.Elapsed < TimeSpan.FromSeconds(1.7))
		{
			Assert.Equal("Copied!", await cut.InvokeAsync(() => CopyButton(cut).TextContent.Trim()));
			await Task.Delay(50, Xunit.TestContext.Current.CancellationToken);
		}

		cut.WaitForAssertion(() => Assert.Equal("Copy", CopyButton(cut).TextContent.Trim()), TimeSpan.FromSeconds(3));
	}

	// break-all split ordinary words at the line's end even where a space was there to break at.
	[Fact]
	public void Wrap_BreaksAtSpacesFirst_AndInsideWhatCannotFit()
	{
		IReadOnlyDictionary<string, string> line = Moka.Red.Tests.Shared.ScopedCss.Declarations(
			"MokaCodeBlock.razor.css", ".moka-code-block--wrap .moka-code-block__line");

		Assert.Equal("normal", line["word-break"]);
		Assert.Equal("anywhere", line["overflow-wrap"]);
	}

	// Each line is a grid item, and an empty one had no height, so blank lines between paragraphs of code vanished.
	[Fact]
	public void ABlankLine_KeepsTheHeightOfALine()
	{
		IReadOnlyDictionary<string, string> line = Moka.Red.Tests.Shared.ScopedCss.Declarations(
			"MokaCodeBlock.razor.css", ".moka-code-block__line");

		Assert.Equal("1lh", line["min-height"]);
	}
}
