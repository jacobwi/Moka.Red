using AngleSharp.Dom;
using Bunit;
using Moka.Red.Primitives.Diff;

namespace Moka.Red.Primitives.Tests.Components;

public class MokaDiffViewerTests : BunitContext
{
	[Fact]
	public void Renders_Root()
	{
		IRenderedComponent<MokaDiffViewer> cut = Render<MokaDiffViewer>(p => p
			.Add(x => x.OldText, "a")
			.Add(x => x.NewText, "a"));

		Assert.NotNull(cut.Find(".moka-diff-viewer"));
	}

	[Fact]
	public void OldNewText_ProducesAddedAndRemovedRows()
	{
		IRenderedComponent<MokaDiffViewer> cut = Render<MokaDiffViewer>(p => p
			.Add(x => x.OldText, "a\nb\nc")
			.Add(x => x.NewText, "a\nx\nc"));

		Assert.Single(cut.FindAll(".moka-diff-viewer-line--removed"));
		Assert.Single(cut.FindAll(".moka-diff-viewer-line--added"));
	}

	[Fact]
	public void IdenticalText_NoAddedOrRemoved()
	{
		IRenderedComponent<MokaDiffViewer> cut = Render<MokaDiffViewer>(p => p
			.Add(x => x.OldText, "one\ntwo")
			.Add(x => x.NewText, "one\ntwo"));

		Assert.Empty(cut.FindAll(".moka-diff-viewer-line--added"));
		Assert.Empty(cut.FindAll(".moka-diff-viewer-line--removed"));
	}

	[Fact]
	public void ShowLineNumbers_RendersGutters()
	{
		IRenderedComponent<MokaDiffViewer> cut = Render<MokaDiffViewer>(p => p
			.Add(x => x.OldText, "a\nb")
			.Add(x => x.NewText, "a\nb")
			.Add(x => x.ShowLineNumbers, true));

		// two gutters (old + new) per row
		Assert.Equal(4, cut.FindAll(".moka-diff-viewer-num").Count);
	}

	[Fact]
	public void ShowLineNumbers_Default_NoGutters()
	{
		IRenderedComponent<MokaDiffViewer> cut = Render<MokaDiffViewer>(p => p
			.Add(x => x.OldText, "a")
			.Add(x => x.NewText, "a"));

		Assert.Empty(cut.FindAll(".moka-diff-viewer-num"));
	}

	[Fact]
	public void ShowMarkers_Default_RendersMarkers()
	{
		IRenderedComponent<MokaDiffViewer> cut = Render<MokaDiffViewer>(p => p
			.Add(x => x.OldText, "a")
			.Add(x => x.NewText, "b"));

		Assert.NotEmpty(cut.FindAll(".moka-diff-viewer-marker"));
	}

	[Fact]
	public void ShowMarkers_False_NoMarkers()
	{
		IRenderedComponent<MokaDiffViewer> cut = Render<MokaDiffViewer>(p => p
			.Add(x => x.OldText, "a")
			.Add(x => x.NewText, "b")
			.Add(x => x.ShowMarkers, false));

		Assert.Empty(cut.FindAll(".moka-diff-viewer-marker"));
	}

	[Fact]
	public void Lines_TakePrecedenceOverText()
	{
		var lines = new List<MokaDiffLine>
		{
			new(MokaDiffLineKind.Unchanged, "kept"),
			new(MokaDiffLineKind.Added, "new line"),
			new(MokaDiffLineKind.Removed, "gone")
		};

		IRenderedComponent<MokaDiffViewer> cut = Render<MokaDiffViewer>(p => p
			.Add(x => x.OldText, "totally\ndifferent")
			.Add(x => x.NewText, "text\nhere")
			.Add(x => x.Lines, lines));

		Assert.Single(cut.FindAll(".moka-diff-viewer-line--added"));
		Assert.Single(cut.FindAll(".moka-diff-viewer-line--removed"));
		Assert.Contains("new line", cut.Find(".moka-diff-viewer").TextContent, StringComparison.Ordinal);
	}

	// The markers are aria-hidden and the tint is colour only, so screen readers used to hear every
	// line without being told which ones changed.
	[Fact]
	public void ChangedLines_CarryAHiddenWordForScreenReaders()
	{
		IRenderedComponent<MokaDiffViewer> cut = Render<MokaDiffViewer>(p => p
			.Add(x => x.OldText, "a\nb\nc")
			.Add(x => x.NewText, "a\nx\nc"));

		IElement removed = cut.Find(".moka-diff-viewer-line--removed");
		IElement added = cut.Find(".moka-diff-viewer-line--added");
		Assert.Equal("Removed: ", removed.QuerySelector(".moka-visually-hidden")?.TextContent);
		Assert.Equal("Added: ", added.QuerySelector(".moka-visually-hidden")?.TextContent);

		// The word comes before the text, so it is read first.
		List<IElement> parts = removed.Children.ToList();
		Assert.True(parts.FindIndex(e => e.ClassList.Contains("moka-visually-hidden"))
		            < parts.FindIndex(e => e.ClassList.Contains("moka-diff-viewer-text")));
	}

	[Fact]
	public void UnchangedLines_HaveNoHiddenWord()
	{
		IRenderedComponent<MokaDiffViewer> cut = Render<MokaDiffViewer>(p => p
			.Add(x => x.OldText, "one\ntwo")
			.Add(x => x.NewText, "one\ntwo"));

		Assert.Empty(cut.FindAll(".moka-visually-hidden"));
	}

	[Fact]
	public void HiddenWords_StayWithoutTheMarkerGutter()
	{
		IRenderedComponent<MokaDiffViewer> cut = Render<MokaDiffViewer>(p => p
			.Add(x => x.OldText, "a")
			.Add(x => x.NewText, "b")
			.Add(x => x.ShowMarkers, false));

		Assert.Equal(2, cut.FindAll(".moka-visually-hidden").Count);
	}

	[Fact]
	public void AddedAndRemovedLabels_CanBeTranslated()
	{
		IRenderedComponent<MokaDiffViewer> cut = Render<MokaDiffViewer>(p => p
			.Add(x => x.OldText, "a")
			.Add(x => x.NewText, "b")
			.Add(x => x.AddedLabel, "Hinzugefügt")
			.Add(x => x.RemovedLabel, "Entfernt"));

		Assert.Equal(["Entfernt: ", "Hinzugefügt: "], cut.FindAll(".moka-visually-hidden").Select(e => e.TextContent));
	}

	[Fact]
	public void Wrap_AddsModifier()
	{
		IRenderedComponent<MokaDiffViewer> cut = Render<MokaDiffViewer>(p => p
			.Add(x => x.OldText, "a")
			.Add(x => x.NewText, "a")
			.Add(x => x.Wrap, true));

		Assert.Contains("moka-diff-viewer--wrap", cut.Find(".moka-diff-viewer").ClassName, StringComparison.Ordinal);
	}
}
