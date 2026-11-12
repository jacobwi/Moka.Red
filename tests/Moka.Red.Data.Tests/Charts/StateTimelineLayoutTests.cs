using Moka.Red.Data.Chart;

namespace Moka.Red.Data.Tests.Charts;

// How a state timeline resolves spans once per data change and cuts them into marks whenever the geometry moves.
public sealed class StateTimelineLayoutTests
{
	private const int Up = 0;
	private const int Warn = 1;
	private const int Down = 2;
	private const int Unknown = 3;

	private static readonly DateTimeOffset Midnight = new(2026, 11, 11, 0, 0, 0, TimeSpan.Zero);
	private static readonly int[] Severity = [10, 40, 50, 0];

	[Fact]
	public void SpansWithoutAnEnd_LastUntilTheNextStart_AndTheLastUntilNow()
	{
		StateSegment[] segments = Resolve(Minutes(30),
			new MokaStateSpan(At(10), "down"), new MokaStateSpan(At(0), "up"), new MokaStateSpan(At(20), "up"));

		Assert.Equal([(0d, 10d, Up), (10, 20, Down), (20, 30, Up)], Shape(segments));
	}

	[Fact]
	public void ASpanWithAnEnd_SitsOverTheOpenSpanAroundIt_InsteadOfCuttingItOff()
	{
		StateSegment[] segments = Resolve(Minutes(60),
			new MokaStateSpan(At(0), "up"), new MokaStateSpan(At(10), "down") { End = At(15) },
			new MokaStateSpan(At(30), "warn") { End = At(40) }, new MokaStateSpan(At(50), "up") { Detail = "Restarted" });

		Assert.Equal([(0d, 10d, Up), (10, 15, Down), (15, 30, Up), (30, 40, Warn), (40, 50, Up), (50, 60, Up)], Shape(segments));
	}

	[Fact]
	public void OpenSpansThatStartTogether_BothRunUntilTheNextStart()
	{
		StateSegment[] segments = Resolve(Minutes(30),
			new MokaStateSpan(At(0), "up"), new MokaStateSpan(At(0), "warn"), new MokaStateSpan(At(20), "up"));

		Assert.Equal([(0d, 20d, Warn), (20, 30, Up)], Shape(segments));
	}

	[Fact]
	public void WhereSpansOverlap_TheMoreSevereStateWins()
	{
		StateSegment[] segments = Resolve(Minutes(60),
			new MokaStateSpan(At(0), "up") { End = At(60) }, new MokaStateSpan(At(10), "down") { End = At(15) },
			new MokaStateSpan(At(30), "warn") { End = At(40), Detail = "Slow" });

		Assert.Equal([(0d, 10d, Up), (10, 15, Down), (15, 30, Up), (30, 40, Warn), (40, 60, Up)], Shape(segments));
		Assert.Equal("Slow", segments[3].Detail);
	}

	[Fact]
	public void BetweenEqualStates_TheLaterStartWins()
	{
		StateSegment[] segments = Resolve(Minutes(60),
			new MokaStateSpan(At(0), "up") { End = At(20), Detail = "first" },
			new MokaStateSpan(At(10), "up") { End = At(15), Detail = "second" });

		Assert.Equal(["first", "second", "first"], segments.Select(segment => segment.Detail));
	}

	[Fact]
	public void TimeBetweenSpans_IsUnknown_AndNeverPastNow()
	{
		StateSegment[] segments = Resolve(Minutes(50),
			new MokaStateSpan(At(0), "up") { End = At(10) }, new MokaStateSpan(At(20), "up") { End = At(30) },
			new MokaStateSpan(At(70), "warn") { End = At(80) });

		Assert.Equal([(0d, 10d, Up), (10, 20, Unknown), (20, 30, Up), (30, 50, Unknown), (70, 80, Warn)], Shape(segments));
		Assert.True(segments[1].Gap);
		Assert.False(segments[0].Gap);
	}

	[Fact]
	public void NeighboursInOneState_Join_OnlyWhenTheySayTheSame()
	{
		StateSegment[] joined = Resolve(Minutes(30),
			new MokaStateSpan(At(0), "up"), new MokaStateSpan(At(10), "up"), new MokaStateSpan(At(20), "up"));
		StateSegment[] apart = Resolve(Minutes(30),
			new MokaStateSpan(At(0), "down") { Detail = "HTTP 503" }, new MokaStateSpan(At(10), "down") { Detail = "Timeout" });

		Assert.Equal([(0d, 30d, Up)], Shape(joined));
		Assert.Equal(2, apart.Length);
	}

	[Fact]
	public void Pieces_PutUnknownBeforeTheFirstSpan_AndAfterTheLast_UpToNow()
	{
		StateSegment[] segments = Resolve(Minutes(40), new MokaStateSpan(At(10), "up") { End = At(20) });

		List<StatePiece> pieces = StateTimelineLayout.Pieces(segments, Ms(0), Ms(60), Ms(40), Unknown, _ => false);

		Assert.Equal([(0d, 10d, Unknown), (10, 20, Up), (20, 40, Unknown)],
			pieces.Select(piece => ((piece.From - Ms(0)) / 60000, (piece.To - Ms(0)) / 60000, piece.State)));
		Assert.Equal(-1, pieces[0].Segment);
		Assert.Equal(0, pieces[1].Segment);
	}

	[Fact]
	public void Pieces_LeaveAHiddenStatesTimeEmpty_AndCutToTheAxis()
	{
		StateSegment[] segments = Resolve(Minutes(60),
			new MokaStateSpan(At(0), "up") { End = At(30) }, new MokaStateSpan(At(30), "down") { End = At(60) });

		List<StatePiece> pieces = StateTimelineLayout.Pieces(segments, Ms(15), Ms(45), Ms(60), Unknown, state => state == Up);

		StatePiece down = Assert.Single(pieces);
		Assert.Equal(Down, down.State);
		Assert.Equal((Ms(30), Ms(45)), (down.From, down.To));
	}

	[Fact]
	public void Runs_GatherThinNeighbours_AndShowTheMostSevere()
	{
		List<StatePiece> pieces = [Piece(0, 500, Up), Piece(500, 501, Up), Piece(501, 502, Down), Piece(502, 503, Up), Piece(503, 1000, Up)];

		List<StateRun> runs = StateTimelineLayout.Runs(pieces, 10, 0, Severity);

		Assert.Equal(3, runs.Count);
		Assert.Equal((1, 3, Down), (runs[1].First, runs[1].Count, runs[1].State));
		Assert.True(runs[1].Merged);
		Assert.False(runs[0].Merged);
	}

	[Fact]
	public void Runs_KeepAThinPieceAlone_WhenItHasNoThinNeighbour()
	{
		List<StatePiece> pieces = [Piece(0, 500, Up), Piece(500, 501, Down), Piece(501, 1000, Up)];

		List<StateRun> runs = StateTimelineLayout.Runs(pieces, 10, 0, Severity);

		Assert.Equal(3, runs.Count);
		Assert.False(runs[1].Merged);
		Assert.Equal(Down, runs[1].State);
	}

	[Fact]
	public void Runs_JoinNeighbouringGatherings_ThatShowOneState()
	{
		List<StatePiece> pieces = [.. Enumerable.Range(0, 1000).Select(i => Piece(i, i + 1, i % 2 == 0 ? Up : Down))];

		List<StateRun> runs = StateTimelineLayout.Runs(pieces, 2.5, 0, Severity);

		StateRun run = Assert.Single(runs);
		Assert.Equal(Down, run.State);
		Assert.Equal(1000, run.Count);
		List<(int State, double Milliseconds)> durations = StateTimelineLayout.Durations(pieces, run);
		Assert.Equal([Up, Down], durations.Select(total => total.State));
	}

	[Fact]
	public void Runs_GatherThinPiecesByCellsOfTime_SoASlidingWindowGathersThemTheSameWay()
	{
		List<StatePiece> pieces = [.. Enumerable.Range(0, 300).Select(i => Piece(i * 3, (i * 3) + 3, (i % 5) switch
		{
			0 => Down,
			1 or 2 => Warn,
			_ => Up
		}))];
		List<StatePiece> slid = [Piece(7, 9, pieces[2].State), .. pieces.Skip(3)];

		List<StateRun> runs = StateTimelineLayout.Runs(pieces, 10, -1000, Severity);
		List<StateRun> later = StateTimelineLayout.Runs(slid, 10, -1000, Severity);

		// Past the first mark, which the window cut, every mark is the same.
		double settled = runs[0].To;
		Assert.Equal(runs.Where(run => run.From >= settled).Select(run => (run.From, run.To, run.State)),
			later.Where(run => run.From >= settled).Select(run => (run.From, run.To, run.State)));
		Assert.True(runs.Count(run => run.From >= settled) > 50);
	}

	[Theory]
	[InlineData(21_600_000, 400, 60_000)]
	[InlineData(86_400_000, 400, 250_000)]
	[InlineData(1000, 400, 2.5)]
	[InlineData(10, 400, 1)]
	public void TheQuantum_IsANiceLength_NoShorterThanTheTrackAllows(double span, double marks, double quantum) =>
		Assert.Equal(quantum, StateTimelineLayout.Quantum(span, marks), 9);

	[Fact]
	public void Uptime_LeavesUnknownTimeOut()
	{
		StateSegment[] segments = Resolve(Minutes(40),
			new MokaStateSpan(At(0), "up") { End = At(10) }, new MokaStateSpan(At(20), "down") { End = At(25) },
			new MokaStateSpan(At(25), "up") { End = At(40) });

		(double known, double good) = StateTimelineLayout.Uptime(segments, Ms(0), Ms(40), state => state != Unknown,
			state => state == Up);

		Assert.Equal(30, known / 60000, 9);
		Assert.Equal(25, good / 60000, 9);
	}

	private static StateSegment[] Resolve(double now, params MokaStateSpan[] spans) =>
		StateTimelineLayout.Resolve(spans, key => key switch
		{
			"up" => Up,
			"warn" => Warn,
			"down" => Down,
			_ => Unknown
		}, Severity, Unknown, now);

	private static (double From, double To, int State)[] Shape(StateSegment[] segments) =>
		[.. segments.Select(segment => ((segment.From - Ms(0)) / 60000, (segment.To - Ms(0)) / 60000, segment.State))];

	private static StatePiece Piece(double from, double to, int state) => new(from, to, 0, state);

	private static DateTimeOffset At(int minutes) => Midnight.AddMinutes(minutes);

	private static double Ms(int minutes) => MokaChartX.FromDateTimeOffset(At(minutes)).Value;

	private static double Minutes(int minutes) => Ms(minutes);
}
