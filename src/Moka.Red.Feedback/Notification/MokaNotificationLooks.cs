using System.Globalization;
using Moka.Red.Core.Theming;
using Moka.Red.Core.Utilities;
using Moka.Red.Feedback.Toast;

namespace Moka.Red.Feedback.Notification;

/// <summary>A run of notifications under one heading, or all of them under none.</summary>
/// <param name="Key">Tells the runs apart: "all", "today" or "earlier".</param>
/// <param name="Heading">The words over the run, or null for none.</param>
/// <param name="Start">The position of the run's first item among everything shown, for moving focus.</param>
/// <param name="Items">The run's items, in the order they came.</param>
/// <param name="Keys">Each item's render key, null where it has none.</param>
internal sealed record MokaNotificationGroup<TItem>(
	string Key,
	string? Heading,
	int Start,
	IReadOnlyList<TItem> Items,
	IReadOnlyList<object?> Keys);

/// <summary>
///     What <see cref="MokaNotificationCenter" /> and <c>MokaNotificationBell</c> share about their looks: which one
///     shows, its class, the parts only some looks draw, and the day headings of
///     <see cref="MokaNotificationLook.Grouped" />.
/// </summary>
internal static class MokaNotificationLooks
{
	/// <summary>
	///     The look a list draws: its own, else the theme's. The original design keeps its one look, so a theme on
	///     <see cref="MokaLook.Matrix" /> shows <see cref="MokaNotificationLook.Standard" /> whatever was asked for.
	/// </summary>
	internal static MokaNotificationLook Resolve(MokaNotificationLook? own, MokaTheme? theme) =>
		theme?.Look == MokaLook.Matrix
			? MokaNotificationLook.Standard
			: own ?? theme?.Notifications ?? MokaNotificationLook.Standard;

	internal static string? ModifierClass(string rootClass, MokaNotificationLook look) =>
		look == MokaNotificationLook.Standard ? null : $"{rootClass}--{MokaEnumHelpers.ToCssClass(look)}";

	internal static bool ShowsNewCount(MokaNotificationLook look) =>
		look is MokaNotificationLook.Inbox or MokaNotificationLook.Grouped or MokaNotificationLook.Terminal;

	internal static string DefaultNewCount(int count) => $"{count.ToString(CultureInfo.InvariantCulture)} new";

	/// <summary>The terminal look's tag for a severity, all six characters wide so the titles line up.</summary>
	internal static string SeverityTag(MokaToastSeverity severity) => severity switch
	{
		MokaToastSeverity.Success => "[ OK ]",
		MokaToastSeverity.Warning => "[WARN]",
		MokaToastSeverity.Error => "[FAIL]",
		_ => "[INFO]"
	};

	/// <summary>The icon size a look draws: smaller on the timeline, where the icon sits in a ring on the line.</summary>
	internal static string IconSize(MokaNotificationLook look) =>
		look == MokaNotificationLook.Timeline ? "12px" : "16px";

	/// <summary>
	///     The items as the look shows them: one run without a heading, or under
	///     <see cref="MokaNotificationLook.Grouped" /> the ones from today and then the rest, each keeping its order.
	/// </summary>
	internal static IReadOnlyList<MokaNotificationGroup<TItem>> Group<TItem>(
		IReadOnlyList<TItem> items,
		IReadOnlyList<object?> keys,
		MokaNotificationLook look,
		Func<TItem, DateTime> timestamp,
		TimeProvider clock,
		string todayText,
		string earlierText)
	{
		if (look != MokaNotificationLook.Grouped)
		{
			return [new MokaNotificationGroup<TItem>("all", null, 0, items, keys)];
		}

		List<TItem> today = [];
		List<object?> todayKeys = [];
		List<TItem> earlier = [];
		List<object?> earlierKeys = [];
		DateTime todayDate = clock.GetLocalNow().Date;
		for (int i = 0; i < items.Count; i++)
		{
			if (LocalDate(timestamp(items[i]), clock) >= todayDate)
			{
				today.Add(items[i]);
				todayKeys.Add(keys[i]);
			}
			else
			{
				earlier.Add(items[i]);
				earlierKeys.Add(keys[i]);
			}
		}

		List<MokaNotificationGroup<TItem>> groups = [];
		if (today.Count > 0)
		{
			groups.Add(new MokaNotificationGroup<TItem>("today", todayText, 0, today, todayKeys));
		}

		if (earlier.Count > 0)
		{
			groups.Add(new MokaNotificationGroup<TItem>("earlier", earlierText, today.Count, earlier, earlierKeys));
		}

		return groups;
	}

	// A timestamp of no stated kind counts as UTC, as the relative time beside it does.
	private static DateTime LocalDate(DateTime timestamp, TimeProvider clock)
	{
		DateTime utc = timestamp.Kind switch
		{
			DateTimeKind.Local => timestamp.ToUniversalTime(),
			DateTimeKind.Unspecified => DateTime.SpecifyKind(timestamp, DateTimeKind.Utc),
			_ => timestamp
		};

		return TimeZoneInfo.ConvertTimeFromUtc(utc, clock.LocalTimeZone).Date;
	}
}
