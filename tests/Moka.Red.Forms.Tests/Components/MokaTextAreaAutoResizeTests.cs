using AngleSharp.Dom;
using Bunit;
using Moka.Red.Forms.TextArea;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Forms.Tests.Components;

// AutoResize only added a class whose rule hid the overflow and the resize handle, so nothing grew and long text was cut
// off. The box now follows its text through field-sizing, between Rows and MaxRows lines.
public sealed class MokaTextAreaAutoResizeTests : BunitContext
{
	private const string Stylesheet = "MokaTextArea.razor.css";
	private const string AutoResizeInput = ".moka-textarea--auto-resize .moka-textarea-input";

	public MokaTextAreaAutoResizeTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public void AutoResize_WritesTheFewestLinesFromRows()
	{
		IElement input = Input(p => p.Add(x => x.AutoResize, true).Add(x => x.Rows, 2));

		Assert.Contains("--_textarea-rows: 2", input.GetAttribute("style"), StringComparison.Ordinal);
		Assert.DoesNotContain("--_textarea-max-rows", input.GetAttribute("style"), StringComparison.Ordinal);
		Assert.Equal("2", input.GetAttribute("rows"));
	}

	[Fact]
	public void MaxRows_WritesTheMostLines() =>
		Assert.Contains("--_textarea-max-rows: 8",
			Input(p => p.Add(x => x.AutoResize, true).Add(x => x.Rows, 1).Add(x => x.MaxRows, 8)).GetAttribute("style"),
			StringComparison.Ordinal);

	[Fact]
	public void MaxRows_IsNeverBelowRows() =>
		Assert.Contains("--_textarea-max-rows: 4",
			Input(p => p.Add(x => x.AutoResize, true).Add(x => x.Rows, 4).Add(x => x.MaxRows, 2)).GetAttribute("style"),
			StringComparison.Ordinal);

	[Fact]
	public void ARowCountBelowOne_CountsAsOne() =>
		Assert.Contains("--_textarea-rows: 1",
			Input(p => p.Add(x => x.AutoResize, true).Add(x => x.Rows, 0)).GetAttribute("style"), StringComparison.Ordinal);

	[Fact]
	public void WithoutAutoResize_TheMarkupIsAsItWas()
	{
		IRenderedComponent<MokaTextArea> cut = Render<MokaTextArea>(p => p.Add(x => x.Rows, 2).Add(x => x.MaxRows, 8));

		Assert.Empty(cut.FindAll(".moka-textarea--auto-resize"));
		Assert.False(cut.Find("textarea").HasAttribute("style"));
	}

	[Fact]
	public void TheStylesheet_GrowsTheBoxBetweenItsLimits_AndNeverHidesText()
	{
		IReadOnlyDictionary<string, string> input = ScopedCss.Declarations(Stylesheet, AutoResizeInput);

		Assert.Equal("content", input["field-sizing"]);
		Assert.Equal("inline-size", input["contain"]);
		Assert.Equal("20ch", input["contain-intrinsic-inline-size"]);
		Assert.Equal("content-box", input["box-sizing"]);
		Assert.Equal("calc(var(--_textarea-rows) * 1lh)", input["min-height"]);
		Assert.Equal("calc(var(--_textarea-max-rows) * 1lh)", input["max-height"]);
		Assert.False(input.ContainsKey("overflow"));
		Assert.Equal("column", ScopedCss.Declarations(Stylesheet, ".moka-textarea--auto-resize")["flex-direction"]);
	}

	// A browser without field-sizing keeps today's textarea that scrolls and resizes, so field-sizing, the width that
	// holds it and the missing handle all go only where the box can grow.
	[Fact]
	public void TheResizeHandle_GoesOnlyWhereFieldSizingWorks()
	{
		string css = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Css", Stylesheet));
		int supports = css.IndexOf("@supports (field-sizing: content)", StringComparison.Ordinal);

		Assert.True(supports > 0);
		Assert.True(css.IndexOf("field-sizing: content;", StringComparison.Ordinal) > supports);
		Assert.True(css.IndexOf("resize: none", StringComparison.Ordinal) > supports);
		Assert.True(css.IndexOf("contain: inline-size", StringComparison.Ordinal) > supports);
	}

	private IElement Input(Action<ComponentParameterCollectionBuilder<MokaTextArea>> parameters) =>
		Render(parameters).Find("textarea");
}
