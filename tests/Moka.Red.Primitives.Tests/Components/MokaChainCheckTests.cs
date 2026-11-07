using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Moka.Red.Icons;
using Moka.Red.Primitives.ChainCheck;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Primitives.Tests.Components;

public class MokaChainCheckTests : BunitContext
{
	private const string Stylesheet = "MokaChainCheck.razor.css";

	private static readonly RenderFragment OpenTheRule =
		builder => builder.AddMarkupContent(0, "<button type=\"button\">Open the rule</button>");

	private static MokaChainStep[] PhoneToInternet() =>
	[
		new(MokaIcons.Status.CheckCircle, "Your phone", MokaChainTone.Ok, "Connected", "Your phone is connected over Wi-Fi."),
		new(MokaIcons.Status.Info, "Wi-Fi", MokaChainTone.Warn, "Weak", "The signal is weak here.", "Move closer to the router."),
		new(MokaIcons.Toggle.Lock, "Firewall", MokaChainTone.Fail, "Blocked", "A rule stops this phone.", Fix: OpenTheRule),
		new(MokaIcons.Navigation.Home, "Router", MokaChainTone.Wait, "Can't tell yet", "We can't check past the firewall."),
		new(MokaIcons.Content.Link, "Internet", MokaChainTone.Off, "Off", "")
	];

	private IRenderedComponent<MokaChainCheck> Chain(IReadOnlyList<MokaChainStep>? steps = null, string? label = null) =>
		Render<MokaChainCheck>(p => p
			.Add(x => x.Steps, steps ?? PhoneToInternet())
			.Add(x => x.Label, label));

	// What a screen reader reads: the text of everything not hidden with aria-hidden, with runs of white space as one.
	private static string Spoken(INode node) =>
		string.Join(' ', Text(node).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

	private static string Text(INode node) => node switch
	{
		IElement element when element.GetAttribute("aria-hidden") == "true" => string.Empty,
		IText text => text.Data,
		_ => string.Concat(node.ChildNodes.Select(Text))
	};

	[Fact]
	public void Root_HasItsClassAndNothingElse()
	{
		IRenderedComponent<MokaChainCheck> cut = Chain();

		Assert.Equal("moka-chain-check", cut.Find("div").ClassName);
		Assert.Null(cut.Find("div").GetAttribute("style"));
	}

	[Fact]
	public void ClassAndAttributes_LandOnTheRoot()
	{
		IRenderedComponent<MokaChainCheck> cut = Render<MokaChainCheck>(p => p
			.Add(x => x.Steps, PhoneToInternet())
			.Add(x => x.Class, "mine")
			.Add(x => x.Id, "chain")
			.AddUnmatched("data-test", "yes"));

		IElement root = cut.Find(".moka-chain-check");
		Assert.Equal("moka-chain-check mine", root.ClassName);
		Assert.Equal("chain", root.Id);
		Assert.Equal("yes", root.GetAttribute("data-test"));
	}

	[Fact]
	public void Steps_RenderInOrder_AsAnOrderedList()
	{
		IRenderedComponent<MokaChainCheck> cut = Chain();

		IReadOnlyList<IElement> labels = cut.FindAll("ol.moka-chain-check__chain > li .moka-chain-check__label");
		Assert.Equal(["Your phone", "Wi-Fi", "Firewall", "Router", "Internet"], labels.Select(l => l.FirstChild!.TextContent));
	}

	[Theory]
	[InlineData(0, "ok")]
	[InlineData(1, "warn")]
	[InlineData(2, "fail")]
	[InlineData(3, "wait")]
	[InlineData(4, "off")]
	public void EachTone_AddsItsClass(int index, string tone)
	{
		IRenderedComponent<MokaChainCheck> cut = Chain();

		IElement step = cut.FindAll(".moka-chain-check__step")[index];
		Assert.Contains($"moka-chain-check__step--{tone}", step.ClassList);
	}

	[Fact]
	public void TheBadge_ChecksOk_CrossesFail_AndMarksWarn()
	{
		IRenderedComponent<MokaChainCheck> cut = Chain();
		IReadOnlyList<IElement> badges = cut.FindAll(".moka-chain-check__badge");

		Assert.Contains(MokaIcons.Status.Check.SvgPath, badges[0].InnerHtml, StringComparison.Ordinal);
		Assert.Equal("!", badges[1].TextContent);
		Assert.Contains(MokaIcons.Navigation.Close.SvgPath, badges[2].InnerHtml, StringComparison.Ordinal);
		Assert.Empty(badges[3].Children);
		Assert.Empty(badges[4].Children);
	}

	[Fact]
	public void TheStepIcon_IsDrawnInTheMark()
	{
		IRenderedComponent<MokaChainCheck> cut = Chain();

		IElement mark = cut.FindAll(".moka-chain-check__mark")[2];
		Assert.Contains(MokaIcons.Toggle.Lock.SvgPath, mark.InnerHtml, StringComparison.Ordinal);
		Assert.Contains("width: 17px", mark.QuerySelector("svg")!.GetAttribute("style"), StringComparison.Ordinal);
	}

	[Fact]
	public void AStepWithNoIcon_DrawsOnlyItsBadge()
	{
		IRenderedComponent<MokaChainCheck> cut = Chain([new(default, "Cable", MokaChainTone.Wait, "Unknown", "")]);

		IElement mark = cut.Find(".moka-chain-check__mark");
		Assert.Single(mark.Children);
		Assert.Equal("moka-chain-check__badge", mark.Children[0].ClassName);
	}

	[Fact]
	public void StepsAfterTheFirstFailure_AreMarkedAfter_WhenTheyWereJudged()
	{
		MokaChainStep[] steps =
		[
			new(MokaIcons.Status.Check, "One", MokaChainTone.Ok, "Fine", ""),
			new(MokaIcons.Status.Check, "Two", MokaChainTone.Fail, "Broken", ""),
			new(MokaIcons.Status.Check, "Three", MokaChainTone.Ok, "Fine", ""),
			new(MokaIcons.Status.Check, "Four", MokaChainTone.Wait, "Waiting", ""),
			new(MokaIcons.Status.Check, "Five", MokaChainTone.Fail, "Broken", "")
		];

		IRenderedComponent<MokaChainCheck> cut = Chain(steps);

		Assert.Equal([false, false, true, false, true],
			cut.FindAll(".moka-chain-check__step").Select(s => s.ClassList.Contains("moka-chain-check__step--after")));
	}

	[Fact]
	public void WithoutAFailure_NothingIsMarkedAfter()
	{
		IRenderedComponent<MokaChainCheck> cut = Chain(PhoneToInternet().Where(s => s.Tone != MokaChainTone.Fail).ToArray());

		Assert.Empty(cut.FindAll(".moka-chain-check__step--after"));
		Assert.Empty(cut.FindAll(".moka-chain-check__row--after"));
	}

	[Fact]
	public void TheLineIntoAStep_IsGreenWhileItHolds_RedIntoTheBreak_AndDashedWhereNothingCanBeJudged()
	{
		Assert.Equal("var(--moka-color-success)", ScopedCss.Declarations(Stylesheet, ".moka-chain-check__step::before")["background"]);
		Assert.Equal("var(--moka-color-error)", ScopedCss.Declarations(Stylesheet, ".moka-chain-check__step--fail::before")["background"]);

		foreach (string dashed in new[] { "--wait", "--off", "--after" })
		{
			string background = ScopedCss.Declarations(Stylesheet, $".moka-chain-check__step{dashed}::before")["background"];
			Assert.StartsWith("repeating-linear-gradient(", background, StringComparison.Ordinal);
		}

		Assert.Equal("none", ScopedCss.Declarations(Stylesheet, ".moka-chain-check__step:first-child::before")["display"]);
	}

	[Theory]
	[InlineData("ok", "success")]
	[InlineData("warn", "warning")]
	[InlineData("fail", "error")]
	public void AJudgedMark_SitsOnItsTonesTint(string tone, string meaning)
	{
		IReadOnlyDictionary<string, string> mark =
			ScopedCss.Declarations(Stylesheet, $".moka-chain-check__step--{tone} > .moka-chain-check__mark");

		Assert.Equal($"var(--moka-color-{meaning}-bg)", mark["background"]);
		Assert.Equal($"var(--moka-color-{meaning})", mark["border-color"]);
	}

	[Fact]
	public void Rows_SayEachSentence_AndLeaveOutAStepWithout()
	{
		IRenderedComponent<MokaChainCheck> cut = Chain();

		IReadOnlyList<IElement> rows = cut.FindAll("ul.moka-chain-check__findings > li");
		Assert.Equal(4, rows.Count);
		Assert.Contains("moka-chain-check__row--fail", rows[2].ClassList);
		Assert.Equal("Move closer to the router.", rows[1].QuerySelector(".moka-chain-check__detail")!.TextContent);
		Assert.Null(rows[0].QuerySelector(".moka-chain-check__detail"));
	}

	[Fact]
	public void WithNoSentences_ThereIsNoList()
	{
		IRenderedComponent<MokaChainCheck> cut = Chain([new(MokaIcons.Status.Check, "One", MokaChainTone.Ok, "Fine", "")]);

		Assert.Empty(cut.FindAll("ul"));
	}

	[Fact]
	public void TheFix_RendersAtTheEndOfItsRow()
	{
		IRenderedComponent<MokaChainCheck> cut = Chain();

		IReadOnlyList<IElement> rows = cut.FindAll(".moka-chain-check__row");
		Assert.Equal("Open the rule", rows[2].QuerySelector(".moka-chain-check__fix button")!.TextContent);
		Assert.Equal("moka-chain-check__fix", rows[2].Children[^1].ClassName);
		Assert.Single(cut.FindAll(".moka-chain-check__fix"));
	}

	[Theory]
	[InlineData("warn", "warning")]
	[InlineData("fail", "error")]
	public void WarnAndFailRows_AreTinted_AndTheirFixButtonTakesTheTone(string tone, string meaning)
	{
		Assert.Equal($"var(--moka-color-{meaning}-bg)",
			ScopedCss.Declarations(Stylesheet, $".moka-chain-check__row--{tone}")["background"]);

		IReadOnlyDictionary<string, string> fix =
			ScopedCss.Declarations(Stylesheet, $".moka-chain-check__row--{tone} > .moka-chain-check__fix");
		Assert.Equal($"var(--moka-color-{meaning}-line)", fix["--_fix-line"]);
		Assert.Equal($"var(--moka-color-{meaning}-ink)", fix["--_fix-ink"]);

		IReadOnlyDictionary<string, string> button = ScopedCss.Declarations(Stylesheet,
			$".moka-chain-check .moka-chain-check__row--{tone} > .moka-chain-check__fix ::deep .moka-btn--outlined");
		Assert.Equal("var(--_fix-line)", button["border-color"]);
	}

	[Fact]
	public void AnOkRow_IsNotTinted()
	{
		Assert.False(ScopedCss.Declarations(Stylesheet, ".moka-chain-check__row").ContainsKey("background"));
	}

	[Fact]
	public void ScreenReaders_HearEachStepsNameAndState_InOrder()
	{
		IRenderedComponent<MokaChainCheck> cut = Chain(label: "From your phone to the internet");

		IElement chain = cut.Find("ol");
		Assert.Equal("From your phone to the internet", chain.GetAttribute("aria-label"));
		Assert.Equal(
			["Your phone, Connected", "Wi-Fi, Weak", "Firewall, Blocked", "Router, Can't tell yet", "Internet, Off"],
			cut.FindAll("ol > li").Select(Spoken));
	}

	[Fact]
	public void TheMarkAndTheVisibleWord_AreHidden_SinceTheHiddenCopySaysIt()
	{
		IRenderedComponent<MokaChainCheck> cut = Chain();

		Assert.All(cut.FindAll(".moka-chain-check__mark"), mark => Assert.Equal("true", mark.GetAttribute("aria-hidden")));
		Assert.All(cut.FindAll(".moka-chain-check__word"), word => Assert.Equal("true", word.GetAttribute("aria-hidden")));
	}

	[Fact]
	public void ScreenReaders_HearTheFindings_EachWithItsStepsName()
	{
		IRenderedComponent<MokaChainCheck> cut = Chain();

		IElement findings = cut.Find("ul");
		Assert.Equal("Findings", findings.GetAttribute("aria-label"));
		Assert.Equal("Firewall: A rule stops this phone.Open the rule",
			Spoken(cut.FindAll(".moka-chain-check__row")[2]));
		Assert.Equal("true", cut.Find(".moka-chain-check__dot").GetAttribute("aria-hidden"));
	}

	[Fact]
	public void FindingsLabel_RenamesTheList()
	{
		IRenderedComponent<MokaChainCheck> cut = Render<MokaChainCheck>(p => p
			.Add(x => x.Steps, PhoneToInternet())
			.Add(x => x.FindingsLabel, "What we found"));

		Assert.Equal("What we found", cut.Find("ul").GetAttribute("aria-label"));
	}

	[Fact]
	public void WithoutALabel_TheChainHasNoName()
	{
		IRenderedComponent<MokaChainCheck> cut = Chain();

		Assert.False(cut.Find("ol").HasAttribute("aria-label"));
	}

	[Fact]
	public void OnAPhone_TheMarksShrink_AndTheWordGoes()
	{
		Assert.Equal(["2.625rem", "2.25rem"], ScopedCss.Values(Stylesheet, ".moka-chain-check", "--_mark"));
		Assert.Equal(["none"], ScopedCss.Values(Stylesheet, ".moka-chain-check__word", "display"));
		Assert.Contains("@media (max-width: 720px)", File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Css", Stylesheet)),
			StringComparison.Ordinal);
	}

	[Fact]
	public void OneColumnPerStep_WithoutAnInlineCount()
	{
		IReadOnlyDictionary<string, string> chain = ScopedCss.Declarations(Stylesheet, ".moka-chain-check__chain");

		Assert.Equal("column", chain["grid-auto-flow"]);
		Assert.Equal("minmax(0, 1fr)", chain["grid-auto-columns"]);
		Assert.Null(Chain().Find("ol").GetAttribute("style"));
	}
}
