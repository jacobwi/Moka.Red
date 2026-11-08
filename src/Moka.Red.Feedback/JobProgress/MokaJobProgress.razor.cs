using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Feedback.JobProgress;

/// <summary>
///     A long job in words: what it's doing now in a sentence, how far it has got, a round bar, and facts such as how
///     much has come down, how fast and for how long. With no number yet, a short bar slides along the track. A screen
///     reader hears the sentence each time it changes, and the percent only when it asks the bar. For the bar alone,
///     use <c>MokaProgress</c>.
/// </summary>
public partial class MokaJobProgress : MokaVisualComponentBase
{
	private readonly string _textId = $"moka-job-progress-{Guid.NewGuid():N}-text";

	/// <summary>
	///     What the job is doing now, in a sentence. It names the bar, and it's announced politely each time it
	///     changes, so a new step is heard and a new percent isn't.
	/// </summary>
	[Parameter]
	public string? Text { get; set; }

	/// <summary>How much is done, 0 to 100. Null means there's no number yet, and a short bar slides along the track.</summary>
	[Parameter]
	public double? Percent { get; set; }

	/// <summary>Bytes done so far. Shown as "148 of 240 MB" with <see cref="TotalBytes" />, else on its own.</summary>
	[Parameter]
	public long? Bytes { get; set; }

	/// <summary>Bytes the whole job takes, when known.</summary>
	[Parameter]
	public long? TotalBytes { get; set; }

	/// <summary>How fast it's going, in bytes a second, such as "12.4 MB/s". Leave it null when it says nothing.</summary>
	[Parameter]
	public double? BytesPerSecond { get; set; }

	/// <summary>
	///     How sizes are counted: <see cref="MokaByteUnits.Decimal" /> (the default) in powers of 1000 with KB, MB, GB
	///     and TB, or <see cref="MokaByteUnits.Binary" /> in powers of 1024 with KiB, MiB, GiB and TiB. Applies to the
	///     amount, the speed and what a screen reader hears from the bar.
	/// </summary>
	[Parameter]
	public MokaByteUnits ByteUnits { get; set; }

	/// <summary>
	///     Writes a size in bytes, unit included, such as "148 MB". When set it wins over <see cref="ByteUnits" />,
	///     for the amount, the speed (which adds "/s") and what a screen reader hears from the bar.
	/// </summary>
	[Parameter]
	public Func<double, string>? ByteFormatter { get; set; }

	/// <summary>How long the job has been going, such as "12 s so far". The app moves it on.</summary>
	[Parameter]
	public TimeSpan? Elapsed { get; set; }

	/// <summary>More facts in the app's own words, after the ones above.</summary>
	[Parameter]
	public IReadOnlyList<string>? Facts { get; set; }

	/// <summary>How bytes done and the whole read. <c>{0}</c> is what's done and <c>{1}</c> the whole. Defaults to "{0} of {1}".</summary>
	[Parameter]
	public string BytesFormat { get; set; } = "{0} of {1}";

	/// <summary>How the time so far reads. <c>{0}</c> is the time. Defaults to "{0} so far".</summary>
	[Parameter]
	public string ElapsedFormat { get; set; } = "{0} so far";

	/// <inheritdoc />
	protected override string RootClass => "moka-job-progress";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-job-progress--waiting", ShownPercent is null)
		.AddClass(Class)
		.Build();

	// The root only stacks the parts, so the radius shapes the track and the fill follows its corners.

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin)
		.AddStyle("padding", ResolvedPadding)
		.AddStyle(Style)
		.Build();

	private string? TrackStyle => new StyleBuilder()
		.AddStyle("border-radius", ResolvedRounding)
		.Build();

	private double? ClampedPercent => Percent is { } percent && !double.IsNaN(percent) ? Math.Clamp(percent, 0, 100) : null;

	// Rounded down, so the words never say 100% before the job is done.
	private int? ShownPercent => ClampedPercent is { } percent ? (int)Math.Floor(percent) : null;

	private string? ValueNow => ShownPercent?.ToString(CultureInfo.InvariantCulture);

	private string? FillStyle => ClampedPercent is { } percent
		? new StyleBuilder().AddStyle("width", percent.ToString("0.###", CultureInfo.InvariantCulture) + "%").Build()
		: null;

	private static string FormatPercent(int percent) => percent.ToString(CultureInfo.CurrentCulture) + "%";

	// The amount done, not the speed or the time, which change on every tick and say nothing about how far it is.
	private string? ValueText(string? amount)
	{
		string? percent = ShownPercent is { } shown ? FormatPercent(shown) : null;
		if (percent is null)
		{
			return amount;
		}

		return amount is null ? percent : $"{percent}, {amount}";
	}

	private string? AmountFact()
	{
		if (Bytes is not { } done || done < 0)
		{
			return null;
		}

		if (TotalBytes is { } total && total > 0)
		{
			if (ByteFormatter is { } format)
			{
				return string.Format(CultureInfo.CurrentCulture, BytesFormat, format(done), format(total));
			}

			(string doneText, string totalText) = MokaByteFormat.Pair(done, total, ByteUnits);
			return string.Format(CultureInfo.CurrentCulture, BytesFormat, doneText, totalText);
		}

		if (done == 0)
		{
			return null;
		}

		return SizeText(done);
	}

	private List<string> BuildFacts(string? amount)
	{
		List<string> facts = [];
		if (amount is not null)
		{
			facts.Add(amount);
		}

		if (BytesPerSecond is { } rate && double.IsFinite(rate) && rate > 0)
		{
			facts.Add($"{SizeText(rate)}/s");
		}

		if (Elapsed is { } elapsed && elapsed >= TimeSpan.Zero)
		{
			facts.Add(string.Format(CultureInfo.CurrentCulture, ElapsedFormat, Duration(elapsed)));
		}

		if (Facts is not null)
		{
			facts.AddRange(Facts.Where(fact => !string.IsNullOrWhiteSpace(fact)));
		}

		return facts;
	}

	private string SizeText(double bytes) => ByteFormatter is { } format
		? format(bytes)
		: MokaByteFormat.Format(bytes, ByteUnits);

	private static string Duration(TimeSpan span) => span.TotalSeconds switch
	{
		< 1 => "0 s",
		< 60 => string.Create(CultureInfo.CurrentCulture, $"{(int)span.TotalSeconds} s"),
		< 3600 => string.Create(CultureInfo.CurrentCulture, $"{(int)span.TotalMinutes} min {span.Seconds} s"),
		_ => string.Create(CultureInfo.CurrentCulture, $"{(int)span.TotalHours} h {span.Minutes} min")
	};
}
