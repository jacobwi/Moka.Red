using AngleSharp.Dom;
using Bunit;
using Moka.Red.Core.Enums;
using Moka.Red.Icons;
using Moka.Red.Primitives.DataList;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Primitives.Tests.Components;

// MokaDataListLook.Inline: short facts flow along a line as micro label and mono value pairs, wrapping whole.
public class MokaDataListInlineTests : BunitContext
{
	private const string Css = "MokaDataList.razor.css";
	private const string Term = ".moka-data-list--inline ::deep > div > dt";
	private const string Value = ".moka-data-list--inline ::deep > div > dd";

	private static readonly MokaDataListItem[] Facts =
	[
		new("Size", "12.4 MB") { Icon = MokaIcons.File.Database },
		new("Modified", "2026-11-12 09:41") { Help = "When it last changed.", Copyable = true },
		new("Kind", "PDF")
	];

	[Fact]
	public void Inline_IsAClassOnTheList_AndKeepsTheDefinitionList()
	{
		IRenderedComponent<MokaDataList> cut = Render<MokaDataList>(p => p.Add(x => x.Items, Facts).Add(x => x.Look, MokaDataListLook.Inline));

		Assert.Equal("moka-data-list moka-data-list--md moka-data-list--inline", cut.Find("dl").ClassName);
		Assert.Equal(3, cut.FindAll("dl > div > dt + dd").Count);
	}

	// Columns, lines and stripes are for a list that stacks its pairs; a line of pairs has none of them.
	[Fact]
	public void Inline_LeavesOutColumnsBordersAndStripes()
	{
		IRenderedComponent<MokaDataList> cut = Render<MokaDataList>(p => p
			.Add(x => x.Items, Facts)
			.Add(x => x.Look, MokaDataListLook.Inline)
			.Add(x => x.Columns, 3)
			.Add(x => x.Bordered, true)
			.Add(x => x.Striped, true)
			.Add(x => x.TermStyle, MokaDataListTermStyle.Strong)
			.Add(x => x.Orientation, MokaDirection.Row));

		IElement list = cut.Find("dl");
		Assert.Equal("moka-data-list moka-data-list--md moka-data-list--inline", list.ClassName);
		Assert.Null(list.GetAttribute("style"));
	}

	[Fact]
	public void Inline_LeavesOutHelp_ButKeepsTheIconAndTheCopyButton()
	{
		IRenderedComponent<MokaDataList> cut = Render<MokaDataList>(p => p.Add(x => x.Items, Facts).Add(x => x.Look, MokaDataListLook.Inline));

		Assert.Empty(cut.FindAll(".moka-data-list-help"));
		Assert.Single(cut.FindAll(".moka-data-list-icon"));
		Assert.Single(cut.FindAll(".moka-copyable"));
	}

	[Fact]
	public void TheOtherLooks_StillShowHelp()
	{
		IRenderedComponent<MokaDataList> cut = Render<MokaDataList>(p => p.Add(x => x.Items, Facts));

		Assert.Single(cut.FindAll(".moka-data-list-help"));
	}

	[Fact]
	public void ChildContentPairs_TakeTheLookToo()
	{
		IRenderedComponent<MokaDataList> cut = Render<MokaDataList>(p => p
			.Add(x => x.Look, MokaDataListLook.Inline)
			.AddChildContent("<div><dt>Size</dt><dd>12 MB</dd></div>"));

		Assert.Contains("moka-data-list--inline", cut.Find("dl").ClassList);
		Assert.Single(cut.FindAll("dl > div > dt + dd"));
	}

	// Read from moka.css, so the term stays the library's micro label if that ever changes.
	[Fact]
	public void TheTerm_IsTheMicroLabel()
	{
		IReadOnlyDictionary<string, string> micro = ScopedCss.Declarations("moka.css", ".moka-micro-label");
		IReadOnlyDictionary<string, string> term = ScopedCss.Declarations(Css, Term);

		Assert.All(micro, declaration => Assert.Equal(declaration.Value, term[declaration.Key]));
		Assert.Equal("nowrap", term["white-space"]);
		Assert.Equal("var(--moka-color-on-surface-tertiary)", ScopedCss.Declarations(Css, ".moka-gate .moka-data-list--inline ::deep > div > dt")["color"]);
	}

	[Fact]
	public void TheValue_IsMono()
	{
		IReadOnlyDictionary<string, string> value = ScopedCss.Declarations(Css, Value);

		Assert.Equal("var(--moka-font-family-mono)", value["font-family"]);
		Assert.Equal("var(--moka-font-size-mono)", value["font-size"]);
		Assert.Equal("tabular-nums", value["font-variant-numeric"]);
	}

	// MokaCopyable centres itself on its line, which lifted a copyable value's text above its label.
	[Fact]
	public void ACopyableValue_SitsOnTheLabelsBaseline()
	{
		Assert.Equal("baseline", ScopedCss.Declarations(Css, ".moka-data-list--inline ::deep > div > dd > .moka-copyable")["vertical-align"]);
		Assert.Equal("baseline",
			ScopedCss.Declarations(Css, ".moka-data-list--inline ::deep > div > dd > .moka-data-list-value > .moka-copyable")["vertical-align"]);
	}

	// A pair is as wide as its content and never wider than the line, so it moves down whole and only an overlong value
	// breaks inside it.
	[Fact]
	public void ThePairs_FlowAndWrapWhole()
	{
		Assert.Equal("row wrap", ScopedCss.Declarations(Css, ".moka-data-list.moka-data-list--inline")["flex-flow"]);

		IReadOnlyDictionary<string, string> pair = ScopedCss.Declarations(Css, ".moka-data-list--inline ::deep > div");
		Assert.Equal("0 1 auto", pair["flex"]);
		Assert.Equal("row", pair["flex-direction"]);
		Assert.Equal("100%", pair["max-width"]);
		Assert.Equal("0", pair["padding"]);
		Assert.Equal("none", ScopedCss.Declarations(Css, Term)["flex"]);
	}

	// The look's rules come after the size and dense rules they share a weight with, so a look keeps its own type.
	[Fact]
	public void TheLook_ComesAfterTheSizeAndDenseRules()
	{
		string css = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Css", Css)).ReplaceLineEndings("\n");

		int inline = css.IndexOf(Term + " {", StringComparison.Ordinal);
		Assert.True(inline > css.IndexOf(".moka-data-list--dense ::deep > div > dt {", StringComparison.Ordinal));
		Assert.True(inline > css.IndexOf(".moka-data-list--lg ::deep > div > dt {", StringComparison.Ordinal));
	}
}
