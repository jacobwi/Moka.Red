using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Utilities;
using Moka.Red.Feedback.JobProgress;
using Moka.Red.Primitives.Bytes;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Primitives.Tests.Components;

public class MokaBytesTests : BunitContext
{
	private static string Number(double value) => value.ToString("0.#", CultureInfo.CurrentCulture);

	private static void InSwedish(Action test)
	{
		CultureInfo previous = CultureInfo.CurrentCulture;
		CultureInfo.CurrentCulture = new CultureInfo("sv-SE");
		try
		{
			test();
		}
		finally
		{
			CultureInfo.CurrentCulture = previous;
		}
	}

	private IRenderedComponent<MokaBytes> Bytes(long? value, Action<ComponentParameterCollectionBuilder<MokaBytes>>? more = null) =>
		Render<MokaBytes>(p =>
		{
			p.Add(x => x.Value, value);
			more?.Invoke(p);
		});

	private string Shown(long? value, Action<ComponentParameterCollectionBuilder<MokaBytes>>? more = null) =>
		Bytes(value, more).Find(".moka-bytes").TextContent;

	[Theory]
	[InlineData(0L, 0, "B")]
	[InlineData(1L, 1, "B")]
	[InlineData(999L, 999, "B")]
	[InlineData(1000L, 1, "KB")]
	[InlineData(1500L, 1.5, "KB")]
	[InlineData(999_000L, 999, "KB")]
	[InlineData(1_000_000L, 1, "MB")]
	[InlineData(148_000_000L, 148, "MB")]
	[InlineData(1_500_000_000L, 1.5, "GB")]
	[InlineData(1_000_000_000_000L, 1, "TB")]
	[InlineData(5_000_000_000_000_000L, 5, "PB")]
	[InlineData(999_500L, 1, "MB")]
	public void DecimalUnits_ReadInTheLargestUnitThatFits(long bytes, double number, string unit) =>
		Assert.Equal($"{Number(number)} {unit}", Shown(bytes));

	[Theory]
	[InlineData(1023L, 1023, "B")]
	[InlineData(1024L, 1, "KiB")]
	[InlineData(1536L, 1.5, "KiB")]
	[InlineData(1048575L, 1, "MiB")]
	[InlineData(1_048_576L, 1, "MiB")]
	[InlineData(1_073_741_824L, 1, "GiB")]
	[InlineData(1_099_511_627_776L, 1, "TiB")]
	[InlineData(2251799813685248L, 2, "PiB")]
	public void BinaryUnits_CountInPowersOf1024(long bytes, double number, string unit) =>
		Assert.Equal($"{Number(number)} {unit}", Shown(bytes, p => p.Add(x => x.Units, MokaByteUnits.Binary)));

	[Fact]
	public void TheNumber_FollowsTheCurrentCulture() => InSwedish(() =>
		Assert.Equal("1,5 GB", Shown(1_500_000_000)));

	[Fact]
	public void PerSecond_ReadsAsARate() =>
		Assert.Equal("12 MB/s", Shown(12_000_000, p => p.Add(x => x.PerSecond, true)));

	[Fact]
	public void Null_ShowsAHyphen() => Assert.Equal("-", Shown(null));

	[Fact]
	public void EmptyText_ReplacesTheHyphen() => Assert.Equal("Unknown", Shown(null, p => p.Add(x => x.EmptyText, "Unknown")));

	[Fact]
	public void Null_SkipsTheFormatter() =>
		Assert.Equal("-", Shown(null, p => p.Add(x => x.Formatter, _ => "written")));

	[Fact]
	public void Null_HasNoTooltip() => Assert.Empty(Bytes(null, p => p.Add(x => x.ShowTooltip, true)).FindAll("[role=tooltip]"));

	[Theory]
	[InlineData(-1L, -1, "B")]
	[InlineData(-1500L, -1.5, "KB")]
	[InlineData(-1_500_000_000L, -1.5, "GB")]
	public void ANegativeCount_KeepsItsSign_AndStillPicksAUnit(long bytes, double number, string unit) =>
		Assert.Equal($"{Number(number)} {unit}", Shown(bytes));

	[Fact]
	public void TheSmallestLong_DoesNotOverflow()
	{
		string shown = Shown(long.MinValue, p => p.Add(x => x.ShowTooltip, true));

		Assert.EndsWith(" EB", shown, StringComparison.Ordinal);
	}

	[Theory]
	[InlineData(double.NaN)]
	[InlineData(double.PositiveInfinity)]
	[InlineData(double.NegativeInfinity)]
	public void TheFormatCall_ReadsWhatIsNotANumberAsZero(double bytes) =>
		Assert.Equal("0 B", MokaByteFormat.Format(bytes));

	[Fact]
	public void Decimals_AreAlwaysThatMany()
	{
		Assert.Equal($"{1.5.ToString("F2", CultureInfo.CurrentCulture)} GB",
			Shown(1_500_000_000, p => p.Add(x => x.Decimals, 2)));
		Assert.Equal($"{2d.ToString("F1", CultureInfo.CurrentCulture)} GB",
			Shown(2_000_000_000, p => p.Add(x => x.Decimals, 1)));
		Assert.Equal("148 MB", Shown(148_400_000, p => p.Add(x => x.Decimals, 0)));
	}

	[Fact]
	public void Decimals_NeverGoOnPlainBytes() => Assert.Equal("512 B", Shown(512, p => p.Add(x => x.Decimals, 2)));

	[Theory]
	[InlineData(-3)]
	[InlineData(400)]
	public void Decimals_OutOfRange_DoNotThrow(int decimals) =>
		Assert.EndsWith(" GB", Shown(1_500_000_000, p => p.Add(x => x.Decimals, decimals)), StringComparison.Ordinal);

	[Fact]
	public void Formatter_WinsOverUnitsAndDecimals()
	{
		string shown = Shown(2048, p => p
			.Add(x => x.Units, MokaByteUnits.Binary)
			.Add(x => x.Decimals, 3)
			.Add(x => x.Formatter, bytes => $"<{bytes.ToString(CultureInfo.InvariantCulture)}>"));

		Assert.Equal("<2048>", shown);
	}

	[Fact]
	public void PerSecond_FollowsWhatTheFormatterWrites()
	{
		string shown = Shown(8, p => p
			.Add(x => x.PerSecond, true)
			.Add(x => x.Formatter, bytes => $"{bytes.ToString(CultureInfo.InvariantCulture)} octets"));

		Assert.Equal("8 octets/s", shown);
	}

	[Fact]
	public void ByDefault_ThereIsNoTooltip() => Assert.Empty(Bytes(1_536_000).FindAll("[role=tooltip]"));

	[Fact]
	public void ShowTooltip_GivesTheExactCount()
	{
		IRenderedComponent<MokaBytes> cut = Bytes(1_536_000, p => p.Add(x => x.ShowTooltip, true));
		string count = 1_536_000.ToString("N0", CultureInfo.CurrentCulture);

		Assert.Equal($"{count} bytes", cut.Find("[role=tooltip]").TextContent);
		Assert.Equal($"{count} bytes", cut.Find(".moka-bytes").GetAttribute("aria-description"));
		Assert.Null(cut.Find(".moka-bytes").GetAttribute("title"));
	}

	[Theory]
	[InlineData(1L, "1 byte")]
	[InlineData(-1L, "-1 byte")]
	[InlineData(0L, "0 bytes")]
	[InlineData(2L, "2 bytes")]
	public void TheExactCount_ReadsInTheSingularOnlyForOne(long bytes, string expected)
	{
		CultureInfo previous = CultureInfo.CurrentCulture;
		CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
		try
		{
			Assert.Equal(expected, Bytes(bytes, p => p.Add(x => x.ShowTooltip, true)).Find("[role=tooltip]").TextContent);
		}
		finally
		{
			CultureInfo.CurrentCulture = previous;
		}
	}

	[Fact]
	public void TheExactCount_OfARate_SaysSo()
	{
		IRenderedComponent<MokaBytes> cut = Bytes(5, p => p.Add(x => x.ShowTooltip, true).Add(x => x.PerSecond, true));

		Assert.Equal("5 bytes/s", cut.Find("[role=tooltip]").TextContent);
	}

	[Fact]
	public void ExactText_ReplacesTheTooltipsWords()
	{
		IRenderedComponent<MokaBytes> cut = Bytes(5, p => p
			.Add(x => x.ShowTooltip, true)
			.Add(x => x.ExactText, bytes => $"{bytes.ToString(CultureInfo.InvariantCulture)} octets"));

		Assert.Equal("5 octets", cut.Find("[role=tooltip]").TextContent);
	}

	[Fact]
	public void ClassIdAndOtherAttributes_LandOnTheText()
	{
		IRenderedComponent<MokaBytes> cut = Bytes(1, p => p
			.Add(x => x.Class, "mine")
			.Add(x => x.Id, "size")
			.AddUnmatched("data-size", "yes"));

		IElement root = cut.Find(".moka-bytes");
		Assert.True(root.ClassList.Contains("mine"));
		Assert.Equal("size", root.Id);
		Assert.Equal("yes", root.GetAttribute("data-size"));
	}

	[Fact]
	public void WithNothingSet_ItTakesTheSizeAndColorOfTheTextAroundIt() =>
		Assert.Null(Bytes(1).Find(".moka-bytes").GetAttribute("style"));

	[Fact]
	public void ColorAndSize_ReachTheText()
	{
		IRenderedComponent<MokaBytes> cut = Bytes(1, p => p.Add(x => x.Color, MokaColor.Warning).Add(x => x.Size, MokaSize.Lg));

		Assert.Equal("font-size: var(--moka-font-size-lg); color: var(--moka-color-warning)",
			cut.Find(".moka-bytes").GetAttribute("style"));
	}

	[Fact]
	public void TheSurfaceColor_IsTheTextColorOnASurface()
	{
		IRenderedComponent<MokaBytes> cut = Bytes(1, p => p.Add(x => x.Color, MokaColor.Surface));

		Assert.Equal("color: var(--moka-color-on-surface)", cut.Find(".moka-bytes").GetAttribute("style"));
	}

	[Fact]
	public void SizeValue_WinsOverSize()
	{
		IRenderedComponent<MokaBytes> cut = Bytes(1, p => p.Add(x => x.Size, MokaSize.Lg).Add(x => x.SizeValue, "18"));

		Assert.Equal("font-size: 18px", cut.Find(".moka-bytes").GetAttribute("style"));
	}

	[Fact]
	public void Digits_AreTabular_SoAChangingValueDoesNotJitter() =>
		Assert.Equal("tabular-nums", ScopedCss.Declarations("MokaBytes.razor.css", ".moka-bytes")["font-variant-numeric"]);

	[Fact]
	public void TheComponent_AndTheFormatCall_Agree() =>
		Assert.Equal(MokaByteFormat.Format(1_234_567_890, MokaByteUnits.Binary, 2, perSecond: true),
			Shown(1_234_567_890, p => p.Add(x => x.Units, MokaByteUnits.Binary).Add(x => x.Decimals, 2).Add(x => x.PerSecond, true)));

	[Fact]
	public void Parts_SplitTheNumberFromTheUnit() =>
		Assert.Equal((Number(1.5), "MiB"), MokaByteFormat.Parts(1_572_864, MokaByteUnits.Binary));

	[Fact]
	public void TheFormatCall_DefaultsToDecimalUnits() => Assert.Equal("148 MB", MokaByteFormat.Format(148_000_000));
}
