namespace Moka.Red.Data.FileBrowser;

/// <summary>The headings of <see cref="MokaFileGroupBy.Date" />, in the order they show, by the local date.</summary>
public enum MokaFileDateGroup
{
	/// <summary>Changed today, or a date still to come.</summary>
	Today,

	/// <summary>Changed yesterday.</summary>
	Yesterday,

	/// <summary>Changed in the 7 days before that.</summary>
	LastWeek,

	/// <summary>Changed in the 30 days before today, and not in the last week.</summary>
	LastMonth,

	/// <summary>Changed longer ago.</summary>
	Older,

	/// <summary>No date was given.</summary>
	Unknown
}
