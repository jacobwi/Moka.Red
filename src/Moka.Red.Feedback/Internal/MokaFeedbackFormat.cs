using System.Globalization;
using Moka.Red.Core.Icons;
using Moka.Red.Feedback.Toast;
using Moka.Red.Icons;

namespace Moka.Red.Feedback.Internal;

/// <summary>
///     Presentation helpers shared by the feedback components (toast host, notification
///     center, notification bell). Lives here so the relative-time wording and the
///     severity icon mapping stay identical across all of them.
/// </summary>
internal static class MokaFeedbackFormat
{
	/// <summary>
	///     Renders a UTC timestamp as a short relative age ("just now", "5m ago", "3h ago",
	///     "2d ago"), falling back to an abbreviated date once it is a week old.
	/// </summary>
	internal static string RelativeTime(DateTime timestamp)
	{
		TimeSpan diff = DateTime.UtcNow - timestamp;

		if (diff.TotalMinutes < 1)
		{
			return "just now";
		}

		if (diff.TotalMinutes < 60)
		{
			return $"{(int)diff.TotalMinutes}m ago";
		}

		if (diff.TotalHours < 24)
		{
			return $"{(int)diff.TotalHours}h ago";
		}

		if (diff.TotalDays < 7)
		{
			return $"{(int)diff.TotalDays}d ago";
		}

		return timestamp.ToString("MMM d", CultureInfo.InvariantCulture);
	}

	/// <summary>Maps a severity to its default status icon.</summary>
	internal static MokaIconDefinition SeverityIcon(MokaToastSeverity severity) => severity switch
	{
		MokaToastSeverity.Success => MokaIcons.Status.CheckCircle,
		MokaToastSeverity.Warning => MokaIcons.Status.Warning,
		MokaToastSeverity.Error => MokaIcons.Status.Error,
		_ => MokaIcons.Status.Info
	};
}
