namespace Moka.Red.Feedback.Banner;

/// <summary>Every word a <see cref="MokaBannerHost" /> writes around the app's own, to change or translate.</summary>
public sealed record MokaBannerText
{
	/// <summary>Names the close button, in its tooltip and for screen readers.</summary>
	public string Dismiss { get; init; } = "Dismiss";

	/// <summary>Names the strip of banners for screen readers, so it can be found as a landmark.</summary>
	public string Region { get; init; } = "Notices";

	/// <summary>What a screen reader hears when a banner shows: its title and message by default.</summary>
	public Func<MokaBanner, string> Announcement { get; init; } = DefaultAnnouncement;

	private static string DefaultAnnouncement(MokaBanner banner)
	{
		ArgumentNullException.ThrowIfNull(banner);
		return string.IsNullOrWhiteSpace(banner.Title) ? banner.Message : $"{banner.Title}. {banner.Message}";
	}
}
