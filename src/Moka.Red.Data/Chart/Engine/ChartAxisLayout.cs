using System.Globalization;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Data.Chart;

/// <summary>A label along an axis before it is laid out: its text, where it is centred, and its rank.</summary>
/// <param name="Index">Its position among the labels the caller gave.</param>
/// <param name="Text">What it reads.</param>
/// <param name="Share">Where it is centred, as a share of the axis.</param>
/// <param name="HalfEm">Half its width, or half its height on a vertical axis, in the axis's em.</param>
/// <param name="Rank">How much it matters: a higher rank shows on a narrower axis.</param>
/// <param name="Reach">How far it reaches back across the axis from its tick at 45 degrees, in em.</param>
internal readonly record struct AxisSpot(int Index, string Text, double Share, double HalfEm, int Rank, double Reach = 0);

/// <summary>A category axis's labels, laid out for every width the stylesheet knows.</summary>
/// <param name="Labels">The labels that show on some width, in axis order, with their classes and variables.</param>
/// <param name="AxisClass">The axis's stage classes, which the stylesheet answers with container queries.</param>
/// <param name="AxisStyle">The axis's own variables: how tall it grows when rotated, and how many lines a label wraps to.</param>
internal sealed record CategoryAxisLayout(IReadOnlyList<AxisLabelSpot> Labels, string AxisClass, string? AxisStyle);

/// <summary>Where one label of a laid-out axis goes, before it is turned into a <see cref="PlotLabel" />.</summary>
/// <param name="Index">Its position among the labels the caller gave.</param>
/// <param name="Class">Its classes.</param>
/// <param name="Style">Its variables: its share, half width, rotated reach, slot and longest length.</param>
/// <param name="MayBeCut">Whether the stylesheet may cut it, so it keeps its whole text in a tooltip.</param>
internal readonly record struct AxisLabelSpot(int Index, string Class, string Style, bool MayBeCut);

/// <summary>
///     Decides how an axis's labels fit without script. Every width is in the axis's own em, rounded up to the width
///     table the stylesheet answers with container queries, so a label hides, or the whole axis staggers, rotates,
///     wraps or truncates, by the axis's own width. Text widths are estimated at 0.66 em a character, on the generous
///     side: a label that hides a little early costs less than two that overlap.
/// </summary>
internal static class ChartAxisLayout
{
	/// <summary>The axis widths, and heights, in em that the stylesheet has a container query for.</summary>
	internal static readonly int[] Widths = [5, 6, 8, 10, 12, 15, 19, 24, 30, 38, 48, 60, 75, 95, 120];

	/// <summary>A character's estimated width, in em.</summary>
	internal const double CharEm = 0.66;

	/// <summary>The space kept between two labels, in em.</summary>
	internal const double GapEm = 1;

	/// <summary>A line of label text, in em.</summary>
	internal const double LineEm = 1.2;

	/// <summary>How far apart two parallel labels at 45 degrees have to sit, across the axis: a line over the sine of 45.</summary>
	internal const double RotatedEm = 1.7;

	/// <summary>
	///     How wide the widest label has to be, in em, before Auto turns labels: shorter ones, such as hours or month
	///     names, gain too little from turning to be worth reading at an angle, so they thin flat instead.
	/// </summary>
	internal const double RotateFromEm = 2 * RotatedEm;

	/// <summary>The sine and cosine of 45 degrees.</summary>
	internal const double Diagonal = 0.7071;

	/// <summary>The space kept between two value ticks on a vertical axis, in em.</summary>
	internal const double TickGapEm = 0.8;

	/// <summary>A label shows on every width.</summary>
	internal const int Always = 0;

	/// <summary>A label shows on no width the stylesheet knows.</summary>
	internal const int Never = -1;

	// A cut label never keeps less room than this, in em, so it can still show an ellipsis and a letter or two.
	private const double LeastLabelEm = 1.5;

	// A label at 45 degrees is cut past this, in em, so a long name can't make the axis taller than the plot.
	private const double RotatedLongestEm = 12;

	// A label at 45 degrees turns about its end, so its top corner reaches this far past its tick, in em.
	private const double RotatedEndEm = 0.45;

	/// <summary>The width class a need rounds up to, or null past the widest the stylesheet knows.</summary>
	/// <param name="needEm">The width needed, in em.</param>
	internal static int? WidthFor(double needEm)
	{
		if (double.IsNaN(needEm) || needEm < 0)
		{
			return null;
		}

		foreach (int width in Widths)
		{
			if (width >= needEm)
			{
				return width;
			}
		}

		return null;
	}

	/// <summary>A text's estimated width in em, cut to <paramref name="maxEm" /> when that is set.</summary>
	/// <param name="text">The text.</param>
	/// <param name="maxEm">The longest a label may be, in em.</param>
	internal static double TextEm(string? text, double? maxEm)
	{
		double em = (text?.Length ?? 0) * CharEm;
		return LongestEm(maxEm) is { } most ? Math.Min(em, most) : em;
	}

	/// <summary>
	///     The rank labels have always had: the last always shows, the first outranks the rest, then the largest power of
	///     two dividing the position, so evenly filled axes halve as they narrow.
	/// </summary>
	/// <param name="position">The label's position among the labels with text.</param>
	/// <param name="count">How many labels have text.</param>
	internal static int RankOf(int position, int count) =>
		position == count - 1 ? int.MaxValue : position == 0 ? int.MaxValue - 1 : position & -position;

	/// <summary>
	///     The narrowest axis on which two labels sit with a gap between them, each centred on its share. When labels are
	///     kept inside the axis, one held back from an end moves toward the other, so those conditions count too.
	/// </summary>
	/// <param name="a">One label.</param>
	/// <param name="b">The other.</param>
	/// <param name="clamped">Whether labels are kept inside the axis.</param>
	/// <param name="gapEm">The gap between them, in em.</param>
	internal static double PairNeed(AxisSpot a, AxisSpot b, bool clamped, double gapEm = GapEm)
	{
		if (a.Share > b.Share)
		{
			(a, b) = (b, a);
		}

		double room = a.HalfEm + b.HalfEm + gapEm;
		double between = b.Share - a.Share;
		double need = between > 0 ? room / between : double.PositiveInfinity;
		if (!clamped)
		{
			return need;
		}

		need = Math.Max(need, b.Share > 0 ? (room + a.HalfEm) / b.Share : double.PositiveInfinity);
		need = Math.Max(need, a.Share < 1 ? (room + b.HalfEm) / (1 - a.Share) : double.PositiveInfinity);
		return Math.Max(need, room + a.HalfEm + b.HalfEm);
	}

	/// <summary>
	///     Thins labels by rank: the highest ranked always shows, and each other one shows once the axis is wide enough
	///     for it to sit beside every label that outranks it. Returns, per label in the order given, the width it needs,
	///     <see cref="Always" /> or <see cref="Never" />.
	/// </summary>
	/// <param name="spots">The labels, in axis order.</param>
	/// <param name="clamped">Whether labels are kept inside the axis.</param>
	/// <param name="gapEm">The gap between two labels, in em.</param>
	/// <param name="rotated">Whether the labels lie at 45 degrees.</param>
	/// <param name="knownWidthEm">A width the axis is known to have at least, below which a label always shows.</param>
	internal static int[] Thin(IReadOnlyList<AxisSpot> spots, bool clamped, double gapEm = GapEm, bool rotated = false,
		double knownWidthEm = 0)
	{
		ArgumentNullException.ThrowIfNull(spots);

		int[] widths = new int[spots.Count];
		if (spots.Count == 0)
		{
			return widths;
		}

		int[] order = [.. Enumerable.Range(0, spots.Count).OrderByDescending(i => spots[i].Rank).ThenBy(i => i)];
		widths[order[0]] = Always;
		List<AxisSpot> placed = [spots[order[0]]];

		// Everything placed fits at once on the widest axis, so this stays short however many labels there are.
		foreach (int i in order.Skip(1))
		{
			double needed = 0;
			foreach (AxisSpot other in placed)
			{
				needed = Math.Max(needed, rotated ? RotatedPairNeed(spots[i], other) : PairNeed(spots[i], other, clamped, gapEm));
			}

			if (needed <= knownWidthEm)
			{
				widths[i] = Always;
				placed.Add(spots[i]);
			}
			else if (WidthFor(needed) is { } width)
			{
				widths[i] = width;
				placed.Add(spots[i]);
			}
			else
			{
				widths[i] = Never;
			}
		}

		return widths;
	}

	/// <summary>The narrowest axis on which every label shows: the widest need over pairs <paramref name="step" /> apart.</summary>
	/// <param name="spots">The labels, in axis order.</param>
	/// <param name="step">1 for neighbours, 2 for labels two apart, which is what stagger needs.</param>
	internal static double AllShowNeed(IReadOnlyList<AxisSpot> spots, int step)
	{
		ArgumentNullException.ThrowIfNull(spots);

		double need = 0;
		for (int i = 0; i + step < spots.Count; i++)
		{
			need = Math.Max(need, PairNeed(spots[i], spots[i + step], true));
		}

		foreach (AxisSpot spot in spots)
		{
			need = Math.Max(need, 2 * spot.HalfEm);
		}

		return need;
	}

	/// <summary>The narrowest axis on which every label shows at 45 degrees, whatever its length.</summary>
	/// <param name="spots">The labels, in axis order.</param>
	internal static double RotatedNeed(IReadOnlyList<AxisSpot> spots)
	{
		ArgumentNullException.ThrowIfNull(spots);

		double need = 0;
		for (int i = 0; i + 1 < spots.Count; i++)
		{
			need = Math.Max(need, RotatedPairNeed(spots[i], spots[i + 1]));
		}

		return need;
	}

	/// <summary>
	///     The narrowest axis on which two labels at 45 degrees sit far enough apart, each hung from its end at its tick.
	///     A label whose reach would cross the axis's start is held in, toward the next one, so that counts too.
	/// </summary>
	/// <param name="a">One label.</param>
	/// <param name="b">The other.</param>
	internal static double RotatedPairNeed(AxisSpot a, AxisSpot b)
	{
		if (a.Share > b.Share)
		{
			(a, b) = (b, a);
		}

		const double room = RotatedEm + GapEm;
		double between = b.Share - a.Share;
		double need = between > 0 ? room / between : double.PositiveInfinity;
		need = Math.Max(need, a.Share < 1 ? (room + RotatedEndEm) / (1 - a.Share) : double.PositiveInfinity);
		need = Math.Max(need, room + RotatedEndEm + a.Reach);
		return a.Reach > 0 ? Math.Max(need, b.Share > 0 ? (room + a.Reach) / b.Share : double.PositiveInfinity) : need;
	}

	/// <summary>
	///     Lays out a category axis for a strategy. A strategy is a sequence of stages, flat first, each entered when the
	///     axis narrows past the width the stage before it needs to show every label. Most end in a thinning, flat or at
	///     45 degrees, in which labels hide by rank; each label's hiding width is clamped to where that thinning starts,
	///     so no label hides while an earlier stage still shows them all.
	/// </summary>
	/// <param name="labels">The labels with text, in axis order: their text and share.</param>
	/// <param name="fit">The strategy; <see cref="MokaAxisLabelFit.Scroll" /> lays out as <see cref="MokaAxisLabelFit.Auto" />.</param>
	/// <param name="wrapLines">How many lines a wrapped label may take.</param>
	/// <param name="maxLabelEm">The longest a label may be, in em.</param>
	/// <param name="knownWidthEm">A width the axis is known to have at least, as a scrolling axis does; 0 when unknown.</param>
	internal static CategoryAxisLayout Categories(IReadOnlyList<(string Text, double Share)> labels, MokaAxisLabelFit fit,
		int wrapLines, double? maxLabelEm, double knownWidthEm = 0)
	{
		ArgumentNullException.ThrowIfNull(labels);

		int lines = Math.Clamp(wrapLines, 1, 6);
		AxisSpot[] flat = new AxisSpot[labels.Count];
		AxisSpot[] rotated = new AxisSpot[labels.Count];
		double widestEm = 0;
		for (int i = 0; i < labels.Count; i++)
		{
			(string text, double share) = labels[i];
			double em = TextEm(text, maxLabelEm);
			double slanted = Math.Min(em, RotatedLongestEm);
			widestEm = Math.Max(widestEm, em);
			int rank = RankOf(i, labels.Count);
			flat[i] = new AxisSpot(i, text, share, em / 2, rank, Diagonal * slanted);
			rotated[i] = new AxisSpot(i, text, share, RotatedEm / 2, rank, Diagonal * slanted);
		}

		MokaAxisLabelFit strategy = fit == MokaAxisLabelFit.Scroll ? MokaAxisLabelFit.Auto : fit;
		double flatNeed = AllShowNeed(flat, 1);
		bool rotationHelps = widestEm > RotateFromEm;

		List<(string Name, double Need)> stages = [];
		Tail tail = Tail.Flat;
		switch (strategy)
		{
			case MokaAxisLabelFit.Auto:
				stages.Add(("stagger", AllShowNeed(flat, 2)));
				if (rotationHelps)
				{
					stages.Add(("rotate", RotatedNeed(flat)));
					tail = Tail.Rotated;
				}
				else
				{
					tail = Tail.FlatAfterStage;
				}

				break;
			case MokaAxisLabelFit.Stagger:
				stages.Add(("stagger", AllShowNeed(flat, 2)));
				tail = Tail.FlatAfterStage;
				break;
			case MokaAxisLabelFit.Rotate:
				stages.Add(("rotate", RotatedNeed(flat)));
				tail = Tail.Rotated;
				break;
			case MokaAxisLabelFit.Wrap:
				stages.Add(("wrap", WrapNeed(labels, lines, maxLabelEm)));
				tail = Tail.FlatAfterStage;
				break;
			case MokaAxisLabelFit.Truncate:
				stages.Add(("trunc", 0));
				tail = Tail.None;
				break;
		}

		// A stagger or wrap that needs no less room than flat labels never shows them all, so it only gets in the way.
		if (tail == Tail.FlatAfterStage && stages.Count == 1 && WidthFor(stages[0].Need) is var stageWidth
		    && (stageWidth is null || stageWidth >= WidthFor(flatNeed)))
		{
			stages.Clear();
			tail = Tail.Flat;
		}

		// A stage is entered only below the width the one before it needs, and an axis known to be at least that wide
		// (one that scrolls) never gets there.
		List<string> axisClasses = [$"moka-chart-plot__x--fit-{FitName(strategy)}"];
		List<(string Name, double Need)> used = [];
		double entryNeed = flatNeed;
		foreach ((string name, double need) in stages)
		{
			if (entryNeed <= knownWidthEm)
			{
				break;
			}

			axisClasses.Add(StageClass(name, WidthFor(entryNeed)));
			used.Add((name, need));
			entryNeed = need;
		}

		if (entryNeed <= knownWidthEm)
		{
			tail = Tail.None;
		}

		int? hideBelow = null;
		if (tail == Tail.FlatAfterStage)
		{
			hideBelow = WidthFor(entryNeed);
			axisClasses.Add(StageClass("flat", hideBelow));
		}
		else if (tail == Tail.Rotated)
		{
			double before = used.Count > 1 ? used[^2].Need : flatNeed;
			hideBelow = WidthFor(Math.Min(entryNeed, before));
		}

		bool clampToTail = used.Count > 0;
		int[] hide = tail switch
		{
			Tail.None => new int[flat.Length],
			Tail.Rotated => Thin(rotated, false, GapEm, true, knownWidthEm),
			_ => Thin(flat, true, GapEm, false, knownWidthEm)
		};
		bool rotates = used.Any(stage => stage.Name == "rotate");
		bool staggers = used.Any(stage => stage.Name == "stagger");
		bool sized = strategy is MokaAxisLabelFit.Wrap or MokaAxisLabelFit.Truncate;
		double? longest = LongestEm(maxLabelEm);

		List<AxisLabelSpot> result = [];
		for (int i = 0; i < flat.Length; i++)
		{
			int width = clampToTail ? ClampHide(hide[i], hideBelow) : hide[i];
			if (width == Never)
			{
				continue;
			}

			AxisSpot spot = flat[i];
			string classes = new CssBuilder("moka-chart-plot__xtick")
				.AddClass("moka-chart-plot__xtick--odd", staggers && i % 2 == 1)
				.AddClass(HideClass(width))
				.Build();
			double textEm = spot.Text.Length * CharEm;
			bool cut = sized || (longest is { } most && textEm > most) || (rotates && textEm > RotatedLongestEm);
			result.Add(new AxisLabelSpot(spot.Index, classes, LabelStyle(spot, sized ? SlotOf(flat, i) : null, rotates, longest), cut));
		}

		string? axisStyle = new StyleBuilder()
			.AddStyle("--_rot-h", rotates ? Em((Diagonal * (Math.Min(widestEm, RotatedLongestEm) + LineEm)) + (LineEm / 2)) : null)
			.AddStyle("--_rot-max", rotates ? Em(RotatedLongestEm) : null)
			.AddStyle("--_lines", strategy == MokaAxisLabelFit.Wrap ? lines.ToString(CultureInfo.InvariantCulture) : null)
			.Build();

		return new CategoryAxisLayout(result, string.Join(' ', axisClasses), string.IsNullOrEmpty(axisStyle) ? null : axisStyle);
	}

	/// <summary>
	///     Lays out labels that always lie flat and only thin, such as the ticks of a time or number axis. A label that
	///     never fits isn't returned.
	/// </summary>
	/// <param name="spots">The labels, in axis order, with their ranks.</param>
	/// <param name="maxLabelEm">The longest a label may be, in em.</param>
	/// <param name="layers">
	///     Optional layers, one per label, lowest first: a whole layer shows at once, at the width its widest pair needs
	///     and no narrower than the key itself, so a time axis gains every 2-hour tick together rather than a few here
	///     and there. Without layers labels thin one by one by rank.
	/// </param>
	internal static List<AxisLabelSpot> Flat(IReadOnlyList<AxisSpot> spots, double? maxLabelEm, IReadOnlyList<double>? layers = null)
	{
		ArgumentNullException.ThrowIfNull(spots);

		int[] hide = layers is null ? Thin(spots, true) : ThinByLayer(spots, layers);
		double? longest = LongestEm(maxLabelEm);
		List<AxisLabelSpot> labels = [];
		for (int i = 0; i < spots.Count; i++)
		{
			if (hide[i] == Never)
			{
				continue;
			}

			string classes = new CssBuilder("moka-chart-plot__xtick").AddClass(HideClass(hide[i])).Build();
			bool cut = longest is { } most && spots[i].Text.Length * CharEm > most;
			labels.Add(new AxisLabelSpot(spots[i].Index, classes, LabelStyle(spots[i], null, false, longest), cut));
		}

		return labels;
	}

	/// <summary>
	///     The class that hides a value tick on a vertical axis too short for it: the labels-across rule with a tick's
	///     height in place of its width, so on an axis under 8 em tall every other one of four divisions hides. Ticks marked
	///     major (a log axis's powers of ten) outrank the rest.
	/// </summary>
	/// <param name="shares">Each tick's share, bottom first.</param>
	/// <param name="major">Which ticks outrank the rest.</param>
	internal static string?[] TickHeightClasses(IReadOnlyList<double> shares, IReadOnlyList<bool> major)
	{
		ArgumentNullException.ThrowIfNull(shares);
		ArgumentNullException.ThrowIfNull(major);

		int count = shares.Count;
		int majors = major.Count(m => m);
		AxisSpot[] spots = new AxisSpot[count];
		int majorPosition = 0;
		int minorPosition = 0;
		for (int i = 0; i < count; i++)
		{
			int rank = major[i]
				? MajorRank(RankOf(majorPosition++, majors))
				: Math.Min(RankOf(minorPosition++, count - majors), 1 << 29);
			spots[i] = new AxisSpot(i, string.Empty, shares[i], LineEm / 2, rank);
		}

		int[] heights = Thin(spots, false, TickGapEm);
		string?[] classes = new string?[count];
		for (int i = 0; i < count; i++)
		{
			classes[i] = heights[i] switch
			{
				Always => null,
				Never => "moka-chart-plot__ytick--hide",
				var height => $"moka-chart-plot__ytick--h{height.ToString(CultureInfo.InvariantCulture)}"
			};
		}

		return classes;
	}

	/// <summary>A length in em, in the invariant culture.</summary>
	/// <param name="em">The length.</param>
	internal static string Em(double em) => FormattableString.Invariant($"{Math.Round(em, 3):0.###}em");

	/// <summary>A share as a percentage, in the invariant culture.</summary>
	/// <param name="share">The share.</param>
	internal static string Percent(double share) => FormattableString.Invariant($"{share * 100:0.###}%");

	/// <summary>The class that hides a label below a width, or that always hides it.</summary>
	/// <param name="width">The width, <see cref="Always" /> or <see cref="Never" />.</param>
	internal static string? HideClass(int width) => width switch
	{
		Always => null,
		Never => "moka-chart-plot__xtick--hide",
		_ => $"moka-chart-plot__xtick--w{width.ToString(CultureInfo.InvariantCulture)}"
	};

	/// <summary>The variables a label is placed and sized by.</summary>
	/// <param name="spot">The label.</param>
	/// <param name="slot">The room it has before its neighbours, as a share, when it wraps or is cut to it.</param>
	/// <param name="rotates">Whether the axis may rotate it.</param>
	/// <param name="longestEm">The longest it may be, in em.</param>
	internal static string LabelStyle(AxisSpot spot, double? slot, bool rotates, double? longestEm) =>
		new StyleBuilder()
			.AddStyle("--_s", Percent(spot.Share))
			.AddStyle("--_hw", Em(spot.HalfEm))
			.AddStyle("--_rw", rotates ? Em(spot.Reach) : null)
			.AddStyle("--_ds", slot is { } room ? room.ToString("0.#####", CultureInfo.InvariantCulture) : null)
			.AddStyle("--_max", longestEm is { } most ? Em(most) : null)
			.Build() ?? string.Empty;

	private enum Tail
	{
		None,
		Flat,
		FlatAfterStage,
		Rotated
	}

	// The two ends keep their order on top; every other major tick still outranks every minor one.
	internal static int MajorRank(int rank) => rank >= int.MaxValue - 1 ? rank : (1 << 30) + Math.Min(rank, 1 << 29);

	private static int[] ThinByLayer(IReadOnlyList<AxisSpot> spots, IReadOnlyList<double> layers)
	{
		int[] widths = new int[spots.Count];
		List<AxisSpot> placed = [];
		foreach (IGrouping<double, int> layer in Enumerable.Range(0, spots.Count).GroupBy(i => layers[i]).OrderBy(group => group.Key))
		{
			int[] members = [.. layer.OrderBy(i => spots[i].Share)];
			double need = Math.Max(0, layer.Key);
			for (int m = 0; m < members.Length; m++)
			{
				AxisSpot spot = spots[members[m]];
				need = Math.Max(need, 2 * spot.HalfEm);
				if (m + 1 < members.Length)
				{
					need = Math.Max(need, PairNeed(spot, spots[members[m + 1]], true));
				}
			}

			// The layer shows together; a member crowded by a label shown on a narrower axis waits longer on its own.
			List<AxisSpot> shown = [];
			foreach (int member in members)
			{
				double own = need;
				foreach (AxisSpot other in placed)
				{
					own = Math.Max(own, PairNeed(spots[member], other, true));
				}

				widths[member] = WidthFor(own) ?? Never;
				if (widths[member] != Never)
				{
					shown.Add(spots[member]);
				}
			}

			placed.AddRange(shown);
		}

		return widths;
	}

	private static double? LongestEm(double? maxEm) =>
		maxEm is { } most && double.IsFinite(most) && most > 0 ? Math.Max(most, LeastLabelEm) : null;

	private static double WrapNeed(IReadOnlyList<(string Text, double Share)> labels, int lines, double? maxLabelEm)
	{
		double need = 0;
		double? longest = LongestEm(maxLabelEm);
		for (int i = 0; i < labels.Count; i++)
		{
			string text = labels[i].Text;
			int longestWord = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(word => word.Length).DefaultIfEmpty(0).Max();
			double lineEm = Math.Max(Math.Ceiling(text.Length / (double)lines), longestWord) * CharEm;
			if (longest is { } most)
			{
				lineEm = Math.Min(lineEm, most);
			}

			double slot = SlotOf(i, labels.Count, j => labels[j].Share);
			need = Math.Max(need, slot > 0 ? (lineEm + (GapEm / 2)) / slot : double.PositiveInfinity);
		}

		return need;
	}

	private static double SlotOf(AxisSpot[] spots, int i) => SlotOf(i, spots.Length, j => spots[j].Share);

	// The room a label has before it meets its neighbours, as a share of the axis.
	private static double SlotOf(int i, int count, Func<int, double> share)
	{
		double slot = 1;
		if (i > 0)
		{
			slot = Math.Min(slot, Math.Abs(share(i) - share(i - 1)));
		}

		if (i < count - 1)
		{
			slot = Math.Min(slot, Math.Abs(share(i + 1) - share(i)));
		}

		return slot;
	}

	// A label in a stage before the thinning never hides, so its width is clamped to where the thinning starts.
	private static int ClampHide(int width, int? hideBelow)
	{
		if (width == Always || hideBelow is not { } start)
		{
			return width;
		}

		return width == Never || width > start ? start : width;
	}

	private static string StageClass(string stage, int? below) => below is { } width
		? $"moka-chart-plot__x--{stage}-at-w{width.ToString(CultureInfo.InvariantCulture)}"
		: $"moka-chart-plot__x--{stage}";

	private static string FitName(MokaAxisLabelFit fit) => fit switch
	{
		MokaAxisLabelFit.Thin => "thin",
		MokaAxisLabelFit.Rotate => "rotate",
		MokaAxisLabelFit.Stagger => "stagger",
		MokaAxisLabelFit.Wrap => "wrap",
		MokaAxisLabelFit.Truncate => "truncate",
		_ => "auto"
	};
}
