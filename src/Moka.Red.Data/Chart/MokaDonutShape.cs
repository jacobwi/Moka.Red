namespace Moka.Red.Data.Chart;

/// <summary>How a <see cref="MokaDonutChart" /> draws its shares of a whole.</summary>
public enum MokaDonutShape
{
	/// <summary>Arcs of one ring, with the shares listed beside it. Today's donut.</summary>
	Ring,

	/// <summary>
	///     One tall bar split into its shares, each printed inside where it fits, with a legend of names. Parts on one
	///     line compare more exactly than angles, so it suits six parts or fewer.
	/// </summary>
	Bar,

	/// <summary>A hundred squares, each one percent, with the shares listed beside them. Good when one share is the story.</summary>
	Waffle
}
