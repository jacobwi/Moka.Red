namespace Moka.Red.Data.Chart;

/// <summary>
///     A stretch of a <see cref="MokaStateTimeline" /> row in one state once its spans are resolved: no two overlap, the more
///     severe state won where they did, and time between spans is in the unknown state.
/// </summary>
/// <param name="From">Where it starts, as Unix milliseconds.</param>
/// <param name="To">Where it ends, as Unix milliseconds.</param>
/// <param name="State">The state's position in the chart's list of states.</param>
/// <param name="Detail">The winning span's detail.</param>
/// <param name="Gap">Whether no span covered it.</param>
internal readonly record struct StateSegment(double From, double To, int State, string? Detail, bool Gap);

/// <summary>A resolved stretch as it falls inside the axis.</summary>
/// <param name="From">When it starts, clipped to the axis, as Unix milliseconds.</param>
/// <param name="To">When it ends, clipped to the axis.</param>
/// <param name="Segment">Its resolved stretch, or -1 for time before the first span or after the last.</param>
/// <param name="State">The state's position in the chart's list of states.</param>
internal readonly record struct StatePiece(double From, double To, int Segment, int State);

/// <summary>A mark to draw: one piece, or neighbours too thin to draw apart merged into one showing the most severe state.</summary>
/// <param name="First">Its first piece.</param>
/// <param name="Count">How many pieces it holds.</param>
/// <param name="From">When it starts, as Unix milliseconds.</param>
/// <param name="To">When it ends.</param>
/// <param name="State">The state it shows.</param>
internal readonly record struct StateRun(int First, int Count, double From, double To, int State)
{
	/// <summary>Whether it holds more than one piece.</summary>
	public bool Merged => Count > 1;
}

/// <summary>
///     How a <see cref="MokaStateTimeline" /> turns spans into marks: resolving a row once per data change, then cutting it
///     to the axis and merging what is thinner than a quantum of time whenever the geometry moves.
/// </summary>
internal static class StateTimelineLayout
{
	private const double Touching = 0.5;

	private static readonly double[] NiceSteps = [1, 1.25, 1.5, 2, 2.5, 3, 4, 5, 6, 8, 10];

	/// <summary>
	///     Resolves a row's spans: sorted by start, each span without an end lasting until the next span without an end
	///     starts (the last until <paramref name="now" />), overlaps won by the more severe state (the later start between
	///     equals), neighbours in one state with one detail joined, and time between spans in <paramref name="unknown" />,
	///     never past now.
	/// </summary>
	/// <param name="spans">The row's spans, in any order.</param>
	/// <param name="stateOf">A span's state position, from its key.</param>
	/// <param name="severity">Each state's severity, by position.</param>
	/// <param name="unknown">The position of the state time no span covers is in.</param>
	/// <param name="now">The time now, as Unix milliseconds.</param>
	public static StateSegment[] Resolve(IReadOnlyList<MokaStateSpan> spans, Func<string, int> stateOf, IReadOnlyList<int> severity,
		int unknown, double now)
	{
		int n = spans?.Count ?? 0;
		if (n == 0)
		{
			return [];
		}

		int[] order = SortedOrder(spans!);
		double[] start = new double[n];
		double[] end = new double[n];
		int[] state = new int[n];
		for (int i = 0; i < n; i++)
		{
			start[i] = MokaChartX.FromDateTimeOffset(spans![order[i]].Start).Value;
		}

		// An open span runs until the next open span starts, so a span with an end, such as an incident, sits over the
		// state that was running rather than cutting it off; the more severe one shows while both run.
		double following = double.NaN;
		double pending = double.NaN;
		for (int i = n - 1; i >= 0; i--)
		{
			if (!double.IsNaN(pending) && pending > start[i])
			{
				following = pending;
				pending = double.NaN;
			}

			MokaStateSpan span = spans![order[i]];
			if (span.End is { } stated)
			{
				end[i] = MokaChartX.FromDateTimeOffset(stated).Value;
			}
			else
			{
				end[i] = double.IsNaN(following) ? now : following;
				pending = start[i];
			}

			state[i] = stateOf(span.State ?? string.Empty);
		}

		List<double> bounds = new(n * 2);
		for (int i = 0; i < n; i++)
		{
			if (end[i] > start[i])
			{
				bounds.Add(start[i]);
				bounds.Add(end[i]);
			}
		}

		if (bounds.Count == 0)
		{
			return [];
		}

		bounds.Sort();
		List<StateSegment> segments = [];
		PriorityQueue<int, (int Severity, int Order)> active = new(Stronger.Instance);
		int next = 0;
		for (int k = 0; k + 1 < bounds.Count; k++)
		{
			double from = bounds[k];
			double to = bounds[k + 1];
			if (to <= from)
			{
				continue;
			}

			while (next < n && start[next] <= from)
			{
				if (end[next] > start[next])
				{
					active.Enqueue(next, (severity[state[next]], next));
				}

				next++;
			}

			while (active.TryPeek(out int top, out _) && end[top] <= from)
			{
				active.Dequeue();
			}

			if (active.TryPeek(out int winner, out _))
			{
				Append(segments, new StateSegment(from, to, state[winner], spans![order[winner]].Detail, false));
			}
			else if (from < now)
			{
				Append(segments, new StateSegment(from, Math.Min(to, now), unknown, null, true));
			}
		}

		return [.. segments];
	}

	/// <summary>
	///     The pieces of a resolved row inside the axis, in time order: each stretch cut to the axis, with time before the
	///     first span and after the last, up to now, in <paramref name="unknown" />. Hidden states leave their time empty.
	/// </summary>
	/// <param name="segments">The resolved row.</param>
	/// <param name="from">Where the axis starts, as Unix milliseconds.</param>
	/// <param name="to">Where it ends.</param>
	/// <param name="now">The time now.</param>
	/// <param name="unknown">The unknown state's position.</param>
	/// <param name="hidden">Whether a state, by position, is hidden.</param>
	public static List<StatePiece> Pieces(StateSegment[] segments, double from, double to, double now, int unknown,
		Func<int, bool> hidden)
	{
		List<StatePiece> pieces = [];
		if (!(to > from))
		{
			return pieces;
		}

		double known = Math.Min(to, now);
		double first = segments.Length > 0 ? segments[0].From : double.PositiveInfinity;
		double last = segments.Length > 0 ? segments[^1].To : double.NegativeInfinity;
		bool unknownShown = !hidden(unknown);

		if (unknownShown)
		{
			Add(pieces, from, Math.Min(first, known), -1, unknown, from, to);
		}

		for (int i = 0; i < segments.Length; i++)
		{
			StateSegment segment = segments[i];
			if (segment.To <= from || segment.From >= to || hidden(segment.State))
			{
				continue;
			}

			Add(pieces, segment.From, segment.To, i, segment.State, from, to);
		}

		if (unknownShown && segments.Length > 0)
		{
			Add(pieces, Math.Max(last, from), known, -1, unknown, from, to);
		}

		return pieces;
	}

	/// <summary>
	///     The length of time one mark stands for at the narrowest, from the time the axis covers and how many marks fit
	///     across it, rounded up to a step of 1, 1.25, 1.5, 2, 2.5, 3, 4, 5, 6 or 8 times a power of ten milliseconds, so it
	///     holds still while a window slides or grows a little.
	/// </summary>
	/// <param name="span">The time the axis covers, in milliseconds.</param>
	/// <param name="marks">How many marks fit across the track.</param>
	public static double Quantum(double span, double marks)
	{
		double raw = span / Math.Max(1, marks);
		if (!(raw > 1))
		{
			return 1;
		}

		double power = Math.Pow(10, Math.Floor(Math.Log10(raw)));
		foreach (double step in NiceSteps)
		{
			if (step * power >= raw * (1 - 1e-12))
			{
				return step * power;
			}
		}

		return 10 * power;
	}

	/// <summary>
	///     The marks for a row's pieces: a piece at least a <paramref name="quantum" /> long is a mark of its own; thinner
	///     neighbours that touch gather by the quantum's cell of time, counted from <paramref name="origin" />, and show the
	///     most severe state among them; neighbouring gatherings showing one state join. Cells sit on a fixed grid, so a
	///     window that slides gathers the same pieces the same way.
	/// </summary>
	/// <param name="pieces">The row's pieces, in time order.</param>
	/// <param name="quantum">The shortest mark, in milliseconds.</param>
	/// <param name="origin">Where the grid of cells starts, as Unix milliseconds.</param>
	/// <param name="severity">Each state's severity, by position.</param>
	public static List<StateRun> Runs(IReadOnlyList<StatePiece> pieces, double quantum, double origin, IReadOnlyList<int> severity)
	{
		List<StateRun> runs = [];
		int open = -1;
		int count = 0;
		double from = 0;
		double to = 0;
		int best = -1;
		double cell = double.NaN;
		for (int i = 0; i < pieces.Count; i++)
		{
			StatePiece piece = pieces[i];
			bool wide = piece.To - piece.From >= quantum;
			double at = Math.Floor((piece.From - origin) / quantum);
			if (open >= 0 && (wide || piece.From - to > Touching || at != cell))
			{
				Close();
			}

			if (wide)
			{
				runs.Add(new StateRun(i, 1, piece.From, piece.To, piece.State));
				continue;
			}

			if (open < 0)
			{
				(open, count, from, to, best, cell) = (i, 1, piece.From, piece.To, piece.State, at);
			}
			else
			{
				count++;
				to = piece.To;
				if (severity[piece.State] >= severity[best])
				{
					best = piece.State;
				}
			}
		}

		if (open >= 0)
		{
			Close();
		}

		return runs;

		// A gathering joins the one before it when both were gathered from thin pieces and show one state; a piece long
		// enough to stand alone keeps its own mark, and its own tooltip.
		void Close()
		{
			StateRun run = new(open, count, from, to, best);
			if (runs.Count > 0 && runs[^1] is var before && Gathered(before) && before.State == run.State
			    && run.From - before.To <= Touching)
			{
				runs[^1] = before with { Count = before.Count + run.Count, To = run.To };
			}
			else
			{
				runs.Add(run);
			}

			open = -1;
		}

		bool Gathered(StateRun run) => run.Merged || run.To - run.From < quantum;
	}

	/// <summary>
	///     How long a row spent in each state between two times, from its pieces, in the order the time was first met.
	///     Used for the tooltip of a merged mark.
	/// </summary>
	/// <param name="pieces">The row's pieces.</param>
	/// <param name="run">The mark.</param>
	public static List<(int State, double Milliseconds)> Durations(IReadOnlyList<StatePiece> pieces, StateRun run)
	{
		List<(int State, double Milliseconds)> totals = [];
		for (int i = run.First; i < run.First + run.Count && i < pieces.Count; i++)
		{
			StatePiece piece = pieces[i];
			int at = totals.FindIndex(total => total.State == piece.State);
			if (at < 0)
			{
				totals.Add((piece.State, piece.To - piece.From));
			}
			else
			{
				totals[at] = (piece.State, totals[at].Milliseconds + piece.To - piece.From);
			}
		}

		return totals;
	}

	/// <summary>
	///     How a row did between two times: how long it was known and how long in a good state. Unknown-toned time counts as
	///     neither.
	/// </summary>
	/// <param name="segments">The resolved row.</param>
	/// <param name="from">Where the window starts, as Unix milliseconds.</param>
	/// <param name="to">Where it ends.</param>
	/// <param name="isKnown">Whether a state, by position, counts as known.</param>
	/// <param name="isGood">Whether a state, by position, counts as up.</param>
	public static (double Known, double Good) Uptime(StateSegment[] segments, double from, double to, Func<int, bool> isKnown,
		Func<int, bool> isGood)
	{
		double known = 0;
		double good = 0;
		foreach (StateSegment segment in segments)
		{
			double overlap = Math.Min(segment.To, to) - Math.Max(segment.From, from);
			if (overlap <= 0 || !isKnown(segment.State))
			{
				continue;
			}

			known += overlap;
			if (isGood(segment.State))
			{
				good += overlap;
			}
		}

		return (known, good);
	}

	private static void Add(List<StatePiece> pieces, double from, double to, int segment, int state, double axisFrom,
		double axisTo)
	{
		double start = Math.Max(from, axisFrom);
		double end = Math.Min(to, axisTo);
		if (end > start)
		{
			pieces.Add(new StatePiece(start, end, segment, state));
		}
	}

	private static void Append(List<StateSegment> segments, StateSegment segment)
	{
		if (segments.Count > 0 && segments[^1] is var last && last.To == segment.From && last.State == segment.State
		    && last.Gap == segment.Gap && string.Equals(last.Detail, segment.Detail, StringComparison.Ordinal))
		{
			segments[^1] = last with { To = segment.To };
			return;
		}

		segments.Add(segment);
	}

	// Spans usually arrive in time order, and then they are taken as they are.
	private static int[] SortedOrder(IReadOnlyList<MokaStateSpan> spans)
	{
		int[] order = new int[spans.Count];
		bool sorted = true;
		for (int i = 0; i < order.Length; i++)
		{
			order[i] = i;
			if (i > 0 && spans[i].Start < spans[i - 1].Start)
			{
				sorted = false;
			}
		}

		if (!sorted)
		{
			Array.Sort(order, (x, y) => spans[x].Start.CompareTo(spans[y].Start) is var by and not 0 ? by : x.CompareTo(y));
		}

		return order;
	}

	// The queue hands out its least item first, so the more severe state, then the later start, has to compare least.
	private sealed class Stronger : IComparer<(int Severity, int Order)>
	{
		public static Stronger Instance { get; } = new();

		public int Compare((int Severity, int Order) x, (int Severity, int Order) y) =>
			y.Severity != x.Severity ? y.Severity.CompareTo(x.Severity) : y.Order.CompareTo(x.Order);
	}
}
