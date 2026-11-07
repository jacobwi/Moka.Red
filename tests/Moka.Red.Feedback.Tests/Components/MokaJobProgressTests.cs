using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using Moka.Red.Feedback.JobProgress;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Feedback.Tests.Components;

public class MokaJobProgressTests : BunitContext
{
	private const string Stylesheet = "MokaJobProgress.razor.css";

	private IRenderedComponent<MokaJobProgress> Job(double? percent = 62, long? bytes = null, long? total = null,
		double? rate = null, TimeSpan? elapsed = null) =>
		Render<MokaJobProgress>(p => p
			.Add(x => x.Text, "Downloading the update")
			.Add(x => x.Percent, percent)
			.Add(x => x.Bytes, bytes)
			.Add(x => x.TotalBytes, total)
			.Add(x => x.BytesPerSecond, rate)
			.Add(x => x.Elapsed, elapsed));

	private static string[] FactsOf(IRenderedComponent<MokaJobProgress> cut) =>
		[.. cut.FindAll(".moka-job-progress__facts > span").Select(fact => fact.TextContent)];

	private static string Number(double value) => value.ToString("0.#", CultureInfo.CurrentCulture);

	[Fact]
	public void Root_HasItsClass_AndClassAndAttributesLandThere()
	{
		IRenderedComponent<MokaJobProgress> cut = Render<MokaJobProgress>(p => p
			.Add(x => x.Percent, 10d)
			.Add(x => x.Class, "mine")
			.Add(x => x.Id, "job")
			.AddUnmatched("data-test", "yes"));

		IElement root = cut.Find(".moka-job-progress");
		Assert.Equal("moka-job-progress mine", root.ClassName);
		Assert.Equal("job", root.Id);
		Assert.Equal("yes", root.GetAttribute("data-test"));
		Assert.Null(root.GetAttribute("style"));
	}

	[Fact]
	public void TheSentence_Shows_AndNamesTheBar()
	{
		IRenderedComponent<MokaJobProgress> cut = Job();

		IElement text = cut.Find(".moka-job-progress__text");
		IElement bar = cut.Find("[role=progressbar]");
		Assert.Equal("Downloading the update", text.TextContent);
		Assert.Equal(text.Id, bar.GetAttribute("aria-labelledby"));
		Assert.Contains("moka-job-progress__track", bar.ClassList);
	}

	[Fact]
	public void TheSentence_IsAPoliteStatus_AndNothingElseIsInIt()
	{
		IRenderedComponent<MokaJobProgress> cut = Job(bytes: 148_000_000, total: 240_000_000, elapsed: TimeSpan.FromSeconds(12));

		IReadOnlyList<IElement> regions = cut.FindAll("[role=status], [aria-live]");
		IElement region = Assert.Single(regions);
		Assert.Contains("moka-job-progress__text", region.ClassList);
		Assert.Empty(region.Children);
	}

	[Fact]
	public void ANewSentence_ReplacesTheOldInTheSameRegion()
	{
		IRenderedComponent<MokaJobProgress> cut = Job();

		cut.Render(p => p.Add(x => x.Text, "Checking the download"));

		Assert.Equal("Checking the download", cut.Find("[role=status]").TextContent);
	}

	[Fact]
	public void ThePercent_IsRoundedDown_AndTheFillTakesTheExactShare()
	{
		IRenderedComponent<MokaJobProgress> cut = Job(62.7);

		Assert.Equal("62%", cut.Find(".moka-job-progress__percent").TextContent);
		Assert.Equal("true", cut.Find(".moka-job-progress__percent").GetAttribute("aria-hidden"));
		Assert.Equal("width: 62.7%", cut.Find(".moka-job-progress__fill").GetAttribute("style"));
		Assert.Equal("62", cut.Find("[role=progressbar]").GetAttribute("aria-valuenow"));
	}

	[Fact]
	public void ThePercent_NeverSays100_BeforeItIs()
	{
		IRenderedComponent<MokaJobProgress> cut = Job(99.9);

		Assert.Equal("99%", cut.Find(".moka-job-progress__percent").TextContent);
	}

	[Theory]
	[InlineData(140, "100", "width: 100%")]
	[InlineData(-5, "0", "width: 0%")]
	public void ThePercent_StaysBetween0And100(double percent, string now, string width)
	{
		IRenderedComponent<MokaJobProgress> cut = Job(percent);

		Assert.Equal(now, cut.Find("[role=progressbar]").GetAttribute("aria-valuenow"));
		Assert.Equal(width, cut.Find(".moka-job-progress__fill").GetAttribute("style"));
	}

	[Fact]
	public void TheBar_RunsFrom0To100()
	{
		IElement bar = Job().Find("[role=progressbar]");

		Assert.Equal("0", bar.GetAttribute("aria-valuemin"));
		Assert.Equal("100", bar.GetAttribute("aria-valuemax"));
	}

	[Theory]
	[InlineData(null)]
	[InlineData(double.NaN)]
	public void WithNoNumber_ABarSlides_AndTheBarHasNoValue(double? percent)
	{
		IRenderedComponent<MokaJobProgress> cut = Job(percent);

		Assert.Contains("moka-job-progress--waiting", cut.Find(".moka-job-progress").ClassList);
		Assert.False(cut.Find("[role=progressbar]").HasAttribute("aria-valuenow"));
		Assert.False(cut.Find("[role=progressbar]").HasAttribute("aria-valuetext"));
		Assert.Null(cut.Find(".moka-job-progress__fill").GetAttribute("style"));
		Assert.Empty(cut.FindAll(".moka-job-progress__percent"));
		Assert.Equal("true", cut.Find(".moka-job-progress__dot").GetAttribute("aria-hidden"));
	}

	[Fact]
	public void WithANumber_NothingSlides()
	{
		IRenderedComponent<MokaJobProgress> cut = Job();

		Assert.DoesNotContain("moka-job-progress--waiting", cut.Find(".moka-job-progress").ClassList);
		Assert.Empty(cut.FindAll(".moka-job-progress__dot"));
	}

	[Fact]
	public void TheValueText_SaysThePercentAndTheAmount_NotTheSpeedOrTheTime()
	{
		IRenderedComponent<MokaJobProgress> cut = Job(bytes: 148_000_000, total: 240_000_000, rate: 12_400_000,
			elapsed: TimeSpan.FromSeconds(12));

		Assert.Equal("62%, 148 of 240 MB", cut.Find("[role=progressbar]").GetAttribute("aria-valuetext"));
	}

	[Fact]
	public void TheValueText_IsThePercentAlone_WithNoAmount()
	{
		Assert.Equal("62%", Job().Find("[role=progressbar]").GetAttribute("aria-valuetext"));
	}

	[Fact]
	public void TheValueText_IsTheAmount_WithNoNumberYet()
	{
		IRenderedComponent<MokaJobProgress> cut = Job(null, bytes: 148_000_000);

		Assert.Equal("148 MB", cut.Find("[role=progressbar]").GetAttribute("aria-valuetext"));
	}

	[Fact]
	public void TheFacts_ReadAmountSpeedAndTime_InThatOrder()
	{
		IRenderedComponent<MokaJobProgress> cut = Job(bytes: 148_000_000, total: 240_000_000, rate: 12_400_000,
			elapsed: TimeSpan.FromSeconds(12));

		Assert.Equal(["148 of 240 MB", $"{Number(12.4)} MB/s", "12 s so far"], FactsOf(cut));
	}

	[Fact]
	public void AmountsInDifferentUnits_EachSayTheirOwn()
	{
		IRenderedComponent<MokaJobProgress> cut = Job(bytes: 900_000, total: 1_200_000_000);

		Assert.Equal([$"900 KB of {Number(1.2)} GB"], FactsOf(cut));
	}

	[Fact]
	public void NothingDoneYet_StillSaysTheWhole()
	{
		IRenderedComponent<MokaJobProgress> cut = Job(0, bytes: 0, total: 240_000_000);

		Assert.Equal(["0 of 240 MB"], FactsOf(cut));
	}

	[Fact]
	public void WithoutAWhole_TheAmountStandsAlone()
	{
		IRenderedComponent<MokaJobProgress> cut = Job(null, bytes: 512);

		Assert.Equal(["512 B"], FactsOf(cut));
	}

	[Theory]
	[InlineData(0.4, "0 s so far")]
	[InlineData(42, "42 s so far")]
	[InlineData(192, "3 min 12 s so far")]
	[InlineData(3840, "1 h 4 min so far")]
	public void TheTime_ReadsInTheLargestUnitsThatFit(double seconds, string expected)
	{
		IRenderedComponent<MokaJobProgress> cut = Job(elapsed: TimeSpan.FromSeconds(seconds));

		Assert.Equal([expected], FactsOf(cut));
	}

	[Fact]
	public void ASpeedOfNothing_IsLeftOut()
	{
		IRenderedComponent<MokaJobProgress> cut = Job(rate: 0);

		Assert.Empty(cut.FindAll(".moka-job-progress__facts"));
	}

	[Fact]
	public void TheAppsOwnFacts_ComeLast()
	{
		IRenderedComponent<MokaJobProgress> cut = Render<MokaJobProgress>(p => p
			.Add(x => x.Percent, 40d)
			.Add(x => x.Elapsed, TimeSpan.FromSeconds(4))
			.Add(x => x.Facts, ["Step 2 of 5", " "]));

		Assert.Equal(["4 s so far", "Step 2 of 5"], FactsOf(cut));
	}

	[Fact]
	public void TheWordsAroundTheFacts_CanBeChanged()
	{
		IRenderedComponent<MokaJobProgress> cut = Render<MokaJobProgress>(p => p
			.Add(x => x.Percent, 62d)
			.Add(x => x.Bytes, 148_000_000L)
			.Add(x => x.TotalBytes, 240_000_000L)
			.Add(x => x.Elapsed, TimeSpan.FromSeconds(12))
			.Add(x => x.BytesFormat, "{0} von {1}")
			.Add(x => x.ElapsedFormat, "seit {0}"));

		Assert.Equal(["148 von 240 MB", "seit 12 s"], FactsOf(cut));
	}

	[Fact]
	public void WithNoFacts_ThereIsNoFactsRow()
	{
		Assert.Empty(Job().FindAll(".moka-job-progress__facts"));
	}

	[Fact]
	public void TheTrack_IsAnEightPixelRound_Bar_WithTheAccentFill()
	{
		IReadOnlyDictionary<string, string> track = ScopedCss.Declarations(Stylesheet, ".moka-job-progress__track");
		IReadOnlyDictionary<string, string> fill = ScopedCss.Declarations(Stylesheet, ".moka-job-progress__fill");

		Assert.Equal("0.5rem", track["height"]);
		Assert.Equal("var(--moka-radius-full)", track["border-radius"]);
		Assert.Equal("var(--moka-color-primary)", fill["background"]);
		Assert.Equal("width 0.4s ease", fill["transition"]);
	}

	[Fact]
	public void ThePercent_UsesTheAccentText_AndTheFacts_TabularNumbers()
	{
		Assert.Equal("var(--moka-color-primary-text)",
			ScopedCss.Declarations(Stylesheet, ".moka-job-progress__percent")["color"]);
		Assert.Equal("tabular-nums",
			ScopedCss.Declarations(Stylesheet, ".moka-job-progress__facts")["font-variant-numeric"]);
	}

	[Fact]
	public void TheSlidingBar_IsA30PercentBar_ThatStandsStillForReducedMotion()
	{
		const string sliding = ".moka-job-progress--waiting .moka-job-progress__fill";

		Assert.Equal(["30%", "100%"], ScopedCss.Values(Stylesheet, sliding, "width"));
		IReadOnlyList<string> animations = ScopedCss.Values(Stylesheet, sliding, "animation");
		Assert.StartsWith("moka-job-progress-slide ", animations[0], StringComparison.Ordinal);
		Assert.Equal("none", animations[^1]);
		Assert.Contains("@media (prefers-reduced-motion: reduce)",
			File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Css", Stylesheet)), StringComparison.Ordinal);
	}
}
