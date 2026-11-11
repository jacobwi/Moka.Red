namespace Moka.Red.Primitives.DataList;

/// <summary>
///     How a <see cref="MokaDataList" />'s terms weigh against their values. <see cref="MokaDataListLook.FactTiles" /> and
///     <see cref="MokaDataListLook.Terminal" /> draw their terms their own way and ignore it.
/// </summary>
public enum MokaDataListTermStyle
{
	/// <summary>The look's own term.</summary>
	Standard,

	/// <summary>The term in regular weight and a muted color, so the value is what the eye lands on.</summary>
	Quiet,

	/// <summary>The term as the small uppercase label used over groups. Distinct at a glance, louder in a long list.</summary>
	Micro,

	/// <summary>The term in semibold full color and the value muted, the way a glossary reads.</summary>
	Strong
}
