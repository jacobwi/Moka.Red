namespace Moka.Red.Navigation.Breadcrumb;

/// <summary>How a <see cref="MokaBreadcrumb" /> draws its levels.</summary>
public enum MokaBreadcrumbLook
{
	/// <summary>Links with separators between them and the current page at the end.</summary>
	Trail,

	/// <summary>
	///     A monospace path in a well, such as <c>network/devices/office-desktop</c>, for technical apps. Every level
	///     stays: a path too long for its box keeps its end and fades its start.
	/// </summary>
	Path
}
