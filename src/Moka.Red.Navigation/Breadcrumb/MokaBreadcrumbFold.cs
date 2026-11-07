namespace Moka.Red.Navigation.Breadcrumb;

/// <summary>
///     When a <see cref="MokaBreadcrumb" /> folds its middle levels into a menu on its own, from the width of its box.
/// </summary>
public enum MokaBreadcrumbFold
{
	/// <summary>Never on its own. <see cref="MokaBreadcrumb.MaxItems" /> still folds.</summary>
	Never,

	/// <summary>In a box under 400 px (25rem): the first level and the last two stay.</summary>
	Narrow,

	/// <summary>In a box under 560 px (35rem): the first level and the last two stay.</summary>
	Medium
}
