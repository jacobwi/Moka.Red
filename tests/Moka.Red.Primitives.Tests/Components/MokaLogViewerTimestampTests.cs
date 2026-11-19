using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using Moka.Red.Primitives.LogViewer;

namespace Moka.Red.Primitives.Tests.Components;

/// <summary>How a log viewer writes its timestamps: as before unless told, else in a format, on a clock and in a zone.</summary>
public sealed class MokaLogViewerTimestampTests : BunitContext
{
	private static readonly DateTime Utc = new(2026, 11, 19, 9, 30, 15, 250, DateTimeKind.Utc);

	private static readonly TimeZoneInfo PlusTwo =
		TimeZoneInfo.CreateCustomTimeZone("Plus Two", TimeSpan.FromHours(2), "Plus Two", "Plus Two");

	public MokaLogViewerTimestampTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	private IElement Stamp(DateTime at, Action<ComponentParameterCollectionBuilder<MokaLogViewer>>? options = null) =>
		Render<MokaLogViewer>(p =>
		{
			p.Add(x => x.Entries, [new MokaLogEntry(at, MokaLogLevel.Info, "Started", "Host")]);
			options?.Invoke(p);
		}).Find(".moka-log-viewer__timestamp");

	[Fact]
	public void ByDefault_TheTimeIsWrittenAsBefore_InASpan_InAnyCulture()
	{
		using CultureScope culture = new("fi-FI");

		IElement stamp = Stamp(Utc);

		Assert.Equal("SPAN", stamp.TagName);
		Assert.Equal("09:30:15.250", stamp.TextContent);
		Assert.False(stamp.HasAttribute("datetime"));
	}

	[Fact]
	public void TimestampFormat_WritesADate_InATimeElementWithTheWholeMoment()
	{
		using CultureScope culture = new("en-US");
		IElement stamp = Stamp(Utc, p => p.Add(x => x.TimestampFormat, "yyyy-MM-dd HH:mm:ss"));

		Assert.Equal("TIME", stamp.TagName);
		Assert.Equal("2026-11-19 09:30:15", stamp.TextContent);
		Assert.Equal("2026-11-19T09:30:15.250+00:00", stamp.GetAttribute("datetime"));
		Assert.Contains("moka-log-viewer__timestamp", stamp.ClassList);
	}

	[Fact]
	public void TimestampFormat_IsWrittenInTheCurrentCulture()
	{
		using CultureScope culture = new("de-DE");

		IElement stamp = Stamp(Utc, p => p.Add(x => x.TimestampFormat, "dddd, HH:mm"));

		Assert.Equal("Donnerstag, 09:30", stamp.TextContent);
	}

	[Fact]
	public void Hour12_WithoutAFormat_PutsTheDefaultOnATwelveHourClock()
	{
		using CultureScope culture = new("en-US");
		IElement stamp = Stamp(Utc.AddHours(5), p => p.Add(x => x.Hour12, true));

		Assert.Equal("2:30:15.250 PM", stamp.TextContent);
	}

	[Fact]
	public void Hour12False_WithoutAFormat_WritesWhatTheDefaultDoes()
	{
		using CultureScope culture = new("fi-FI");

		Assert.Equal("09:30:15.250", Stamp(Utc, p => p.Add(x => x.Hour12, false)).TextContent);
	}

	[Theory]
	[InlineData("HH:mm", true, "2:30 PM")]
	[InlineData("h:mm tt", false, "14:30")]
	[InlineData("h:mm tt", null, "2:30 PM")]
	[InlineData("HH:mm", null, "14:30")]
	public void Hour12_MovesAFormatOntoTheClockAskedFor(string format, bool? hour12, string written)
	{
		using CultureScope culture = new("en-US");
		IElement stamp = Stamp(Utc.AddHours(5), p => p
			.Add(x => x.TimestampFormat, format)
			.Add(x => x.Hour12, hour12));

		Assert.Equal(written, stamp.TextContent);
	}

	[Fact]
	public void TimeZone_MovesAUtcTimestampIntoIt()
	{
		using CultureScope culture = new("en-US");
		IElement stamp = Stamp(Utc, p => p.Add(x => x.TimeZone, PlusTwo));

		Assert.Equal("11:30:15.250", stamp.TextContent);
		Assert.Equal("2026-11-19T11:30:15.250+02:00", stamp.GetAttribute("datetime"));
	}

	[Fact]
	public void TimeZone_MovesALocalTimestampIntoIt()
	{
		using CultureScope culture = new("en-US");
		IElement stamp = Stamp(Utc.ToLocalTime(), p => p.Add(x => x.TimeZone, TimeZoneInfo.Utc));

		Assert.Equal("09:30:15.250", stamp.TextContent);
		Assert.Equal("2026-11-19T09:30:15.250+00:00", stamp.GetAttribute("datetime"));
	}

	// A timestamp with no zone of its own is read as a time in the viewer's zone, so it isn't moved.
	[Fact]
	public void TimeZone_ReadsAnUnspecifiedTimestampAsATimeThere()
	{
		using CultureScope culture = new("en-US");
		DateTime unspecified = DateTime.SpecifyKind(Utc, DateTimeKind.Unspecified);

		IElement stamp = Stamp(unspecified, p => p.Add(x => x.TimeZone, PlusTwo));

		Assert.Equal("09:30:15.250", stamp.TextContent);
		Assert.Equal("2026-11-19T09:30:15.250+02:00", stamp.GetAttribute("datetime"));
	}

	[Fact]
	public void AnUnspecifiedTimestamp_WithNoZone_HasNoOffsetInItsDatetime()
	{
		using CultureScope culture = new("en-US");
		DateTime unspecified = DateTime.SpecifyKind(Utc, DateTimeKind.Unspecified);

		IElement stamp = Stamp(unspecified, p => p.Add(x => x.TimestampFormat, "HH:mm"));

		Assert.Equal("2026-11-19T09:30:15.250", stamp.GetAttribute("datetime"));
	}

	[Fact]
	public void TimestampFormatter_Wins_AndIsGivenTheTimeInTheZone()
	{
		using CultureScope culture = new("en-US");
		List<DateTime> given = [];
		IElement stamp = Stamp(Utc, p => p
			.Add(x => x.TimeZone, PlusTwo)
			.Add(x => x.TimestampFormat, "yyyy")
			.Add(x => x.Hour12, true)
			.Add(x => x.TimestampFormatter, at =>
			{
				given.Add(at);
				return at.ToString("HH'h'mm", CultureInfo.InvariantCulture);
			}));

		Assert.Equal("11h30", stamp.TextContent);
		Assert.Equal(new DateTime(2026, 11, 19, 11, 30, 15, 250), Assert.Single(given.Distinct()));
	}

	[Fact]
	public void TimestampFormatter_WithoutAZone_IsGivenTheTimestampAsTheEntryHoldsIt()
	{
		using CultureScope culture = new("en-US");
		DateTime? given = null;

		Stamp(Utc, p => p.Add(x => x.TimestampFormatter, at =>
		{
			given = at;
			return "now";
		}));

		Assert.Equal(Utc, given);
		Assert.Equal(DateTimeKind.Utc, given!.Value.Kind);
	}

	[Fact]
	public void ThePaneUnderVirtualize_WritesTheTimeTheSameWay()
	{
		using CultureScope culture = new("en-US");
		IRenderedComponent<MokaLogViewer> cut = Render<MokaLogViewer>(p => p
			.Add(x => x.Entries, [new MokaLogEntry(Utc, MokaLogLevel.Error, "Failed", "Jobs") { Exception = "boom" }])
			.Add(x => x.Virtualize, true)
			.Add(x => x.TimestampFormat, "MMM d HH:mm"));

		cut.Find(".moka-log-viewer__toggle").Click();

		IElement pane = cut.Find(".moka-log-viewer__pane");
		Assert.Equal("Nov 19 09:30 ERR Jobs", pane.GetAttribute("aria-label"));
		IElement time = pane.QuerySelector(".moka-log-viewer__pane-title > time")!;
		Assert.Equal("Nov 19 09:30", time.TextContent);
		Assert.Equal("2026-11-19T09:30:15.250+00:00", time.GetAttribute("datetime"));
	}

	/// <summary>Sets the culture for a test and puts the old one back.</summary>
	private sealed class CultureScope : IDisposable
	{
		private readonly CultureInfo _previous = CultureInfo.CurrentCulture;

		public CultureScope(string culture) => CultureInfo.CurrentCulture = new CultureInfo(culture);

		public void Dispose() => CultureInfo.CurrentCulture = _previous;
	}
}
