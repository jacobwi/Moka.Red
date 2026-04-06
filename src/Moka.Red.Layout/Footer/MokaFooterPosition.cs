namespace Moka.Red.Layout.Footer;

/// <summary>
///     Defines the positioning behavior of <see cref="MokaFooter" />.
/// </summary>
public enum MokaFooterPosition
{
	/// <summary>Normal document flow.</summary>
	Static,

	/// <summary>Sticks to bottom of viewport on scroll.</summary>
	Sticky,

	/// <summary>Always at bottom of viewport.</summary>
	Fixed
}
