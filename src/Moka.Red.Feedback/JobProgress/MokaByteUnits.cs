using System.Diagnostics.CodeAnalysis;

namespace Moka.Red.Feedback.JobProgress;

/// <summary>How <see cref="MokaJobProgress" /> counts bytes into larger units.</summary>
public enum MokaByteUnits
{
	/// <summary>Powers of 1000: B, KB, MB, GB, TB. The default.</summary>
	[SuppressMessage("Naming", "CA1720:Identifier contains type name",
		Justification = "Decimal names the counting system, as in KB against KiB, not the .NET type.")]
	Decimal,

	/// <summary>Powers of 1024: B, KiB, MiB, GiB, TiB.</summary>
	Binary
}
