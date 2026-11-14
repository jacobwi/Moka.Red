using System.Globalization;

namespace Moka.Red.Feedback.Toast;

/// <summary>
///     Every word a <see cref="MokaToastHost" /> writes around the app's own. Set the ones to change, or all of them to
///     translate it. Anything holding a number is a function, so the words can go around it.
/// </summary>
public sealed record MokaToastText
{
	/// <summary>Names the close button, in its tooltip and for screen readers.</summary>
	public string Close { get; init; } = "Close";

	/// <summary>What a screen reader hears for progress nobody can measure.</summary>
	public string Working { get; init; } = "Working";

	/// <summary>A share of the work as a percent, such as "40%".</summary>
	public Func<double, string> Percent { get; init; } =
		share => share.ToString("P0", CultureInfo.CurrentCulture);

	/// <summary>The time the rest of the work should take, such as "About 2 min left".</summary>
	public Func<TimeSpan, string> TimeLeft { get; init; } = DefaultTimeLeft;

	/// <summary>The count beside a repeated toast's title, such as "×3". Screen readers hear <see cref="Repeated" /> instead.</summary>
	public Func<int, string> CountBadge { get; init; } =
		count => string.Create(CultureInfo.CurrentCulture, $"×{count}");

	/// <summary>What a screen reader hears for a repeated toast's count, such as "shown 3 times".</summary>
	public Func<int, string> Repeated { get; init; } =
		count => string.Create(CultureInfo.CurrentCulture, $"shown {count} times");

	/// <summary>The tag the terminal look writes in place of the icon, six characters wide so titles line up.</summary>
	public Func<MokaToastSeverity, string> TerminalTag { get; init; } = severity => severity switch
	{
		MokaToastSeverity.Success => "[ OK ]",
		MokaToastSeverity.Warning => "[WARN]",
		MokaToastSeverity.Error => "[FAIL]",
		_ => "[INFO]"
	};

	/// <summary>What a screen reader hears when a toast shows: its title and message by default.</summary>
	public Func<MokaToastMessage, string> Announcement { get; init; } = DefaultAnnouncement;

	private static string DefaultAnnouncement(MokaToastMessage toast)
	{
		ArgumentNullException.ThrowIfNull(toast);
		if (string.IsNullOrWhiteSpace(toast.Title))
		{
			return toast.Message;
		}

		return string.IsNullOrWhiteSpace(toast.Message) ? toast.Title : $"{toast.Title}. {toast.Message}";
	}

	private static string DefaultTimeLeft(TimeSpan left)
	{
		if (left < TimeSpan.FromSeconds(10))
		{
			return "A few seconds left";
		}

		if (left < TimeSpan.FromMinutes(1))
		{
			int seconds = (int)Math.Round(left.TotalSeconds / 5) * 5;
			return string.Create(CultureInfo.CurrentCulture, $"About {seconds} s left");
		}

		if (left < TimeSpan.FromHours(1))
		{
			int minutes = (int)Math.Round(left.TotalMinutes);
			return string.Create(CultureInfo.CurrentCulture, $"About {minutes} min left");
		}

		return string.Create(CultureInfo.CurrentCulture, $"About {left.TotalHours:0.#} h left");
	}
}
