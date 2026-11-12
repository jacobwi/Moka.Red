using System.Buffers.Binary;
using System.Globalization;
using System.Text.Json;
using Bunit;
using Moka.Red.Core.Theming;
using Moka.Red.Data.Chart;

namespace Moka.Red.Data.Tests.Charts;

// Parts in rows: where each part sits, the layout a listed chart sends the script, and where the script's arrow keys go,
// run in Node.js (skipped without it).
public sealed class ChartPartLayoutTests : BunitContext
{
	private static readonly DateTimeOffset Midnight = new(2026, 11, 11, 0, 0, 0, TimeSpan.Zero);

	// Row 0 holds three parts, row 1 two given right to left, row 2 none and row 3 one.
	private static readonly ChartPartLayout Listed = new(ChartPartOrder.Listed, 4, 3, 6)
	{
		RowStarts = [0, 3, 5, 5],
		From = [0f, 0.4f, 0.5f, 0.7f, 0f, 0.2f],
		To = [0.4f, 0.5f, 1f, 1f, 0.7f, 0.9f]
	};

	private static readonly Lazy<JsonElement?> Steps = new(() =>
		ChartScriptRunner.Run("part-steps.mjs", Convert.ToBase64String(ChartHoverEncoding.EncodeParts(Listed, 7))));

	[Fact]
	public void PlaceOf_ReadsRowsColumnsAndListedRows()
	{
		ChartPartLayout grid = new(ChartPartOrder.Rows, 3, 4, 12);
		ChartPartLayout days = new(ChartPartOrder.Columns, 7, 3, 17);

		Assert.Equal((1, 2), grid.PlaceOf(6));
		Assert.Equal((6, 1), days.PlaceOf(13));
		Assert.Equal((0, 2), days.PlaceOf(14));
		Assert.Equal((1, 1), Listed.PlaceOf(4));
		Assert.Equal((3, 0), Listed.PlaceOf(5));
		Assert.Null(grid.PlaceOf(12));
		Assert.Null(Listed.PlaceOf(-1));
	}

	[Fact]
	public void EncodeParts_WritesTheRowsStartsAndEachPartsSpan()
	{
		byte[] bytes = ChartHoverEncoding.EncodeParts(Listed, 7);

		Assert.Equal(ChartHoverEncoding.HeaderLength + 4 * 4 + 4 * 6, bytes.Length);
		Assert.Equal(2, BinaryPrimitives.ReadUInt16LittleEndian(bytes));
		Assert.Equal(6u, BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(4)));
		Assert.Equal(4d, BinaryPrimitives.ReadDoubleLittleEndian(bytes.AsSpan(8)));
		Assert.Equal(3d, BinaryPrimitives.ReadDoubleLittleEndian(bytes.AsSpan(16)));
		Assert.Equal(7d, BinaryPrimitives.ReadDoubleLittleEndian(bytes.AsSpan(24)));
		Assert.Equal(5u, BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(32 + 12)));
		Assert.Equal(ChartHoverEncoding.Share(0.7f, false), BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(48 + 6)));
	}

	[Fact]
	public void TheStateTimeline_ListsEachRowsSpans_AndAnswersWithThemWhenTheKeysAsk()
	{
		this.Static();
		CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
		IRenderedComponent<MokaStateTimeline> cut = Render<MokaStateTimeline>(p => p
			.Add(x => x.Rows,
			[
				new MokaStateRow("a", [
					new MokaStateSpan(Midnight, MokaStateTimeline.UpKey),
					new MokaStateSpan(Midnight.AddHours(1), MokaStateTimeline.DownKey),
					new MokaStateSpan(Midnight.AddHours(2), MokaStateTimeline.UpKey)
				]),
				new MokaStateRow("b", [new MokaStateSpan(Midnight, MokaStateTimeline.UpKey)])
			])
			.Add(x => x.Now, Midnight.AddHours(4))
			.Add(x => x.TimeZone, TimeZoneInfo.Utc)
			.Add(x => x.Look, MokaChartLook.Quiet)
			.AddCascadingValue(MokaTheme.Red));

		ChartPartLayout layout = cut.Instance.PartLayout(0)!;
		byte[] answer = cut.Instance.HoverGeometry(0, 0)!;

		Assert.Equal(ChartPartOrder.Listed, layout.Order);
		Assert.Equal((2, 3, 4), (layout.Rows, layout.Columns, layout.Count));
		Assert.Equal([0, 3], layout.RowStarts!);
		Assert.Equal([0f, 0.25f, 0.5f, 0f], layout.From!);
		Assert.Equal([0.25f, 0.5f, 1f, 1f], layout.To!);
		Assert.Equal(ChartHoverEncoding.PartsFormat, BinaryPrimitives.ReadUInt16LittleEndian(answer));
		Assert.Equal(cut.Instance.Words.SpanKeys, cut.Instance.OwnKeysText);
		Assert.Equal(1, cut.Instance.DescribeActive(0, -1, 3, null)!.Context.Points[0].Row);
	}

	[Fact]
	public void AGrid_StepsAlongItsRows_AndBetweenThem_StayingInside()
	{
		JsonElement steps = Results();

		Assert.Equal("[6,7,7,11,11,8,11,7,6,0,11,9]", steps.GetProperty("grid").GetRawText());
		Assert.Equal("[3]", steps.GetProperty("paged").GetRawText());
		Assert.Equal("[0,11,-1]", steps.GetProperty("fresh").GetRawText());
	}

	[Fact]
	public void AGridOnItsSide_StepsTheWayItIsDrawn()
	{
		Assert.Equal("[9,10,2,10,6]", Results().GetProperty("turned").GetRawText());
	}

	[Fact]
	public void ACalendar_MovesADayUpAndDown_AndAWeekAcross()
	{
		Assert.Equal("[7,6,13,13,14,14,7,0,14,16,15]", Results().GetProperty("calendar").GetRawText());
	}

	[Fact]
	public void ListedRows_StepToTheSpanAtTheSamePlace_SkippingEmptyRows_InTheOrderTheySit()
	{
		JsonElement steps = Results();

		Assert.Equal("[4,5,4,3,2,1,0,2,5,0]", steps.GetProperty("listed").GetRawText());
		JsonElement decoded = steps.GetProperty("decoded");
		Assert.Equal("[0,0,0,1,1,3]", decoded.GetProperty("rowOf").GetRawText());
		Assert.Equal("[[0,1,2],[4,3],[],[5]]", decoded.GetProperty("orders").GetRawText());
		Assert.Equal((4, 3, 6, 7d), (decoded.GetProperty("rows").GetInt32(), decoded.GetProperty("cols").GetInt32(),
			decoded.GetProperty("count").GetInt32(), decoded.GetProperty("version").GetDouble()));
	}

	private static JsonElement Results()
	{
		Assert.SkipUnless(Steps.Value is not null, "Node.js isn't installed, so the chart script can't be run.");
		return Steps.Value!.Value;
	}
}
