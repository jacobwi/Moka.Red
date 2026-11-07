using Moka.Red.Core.Icons;

namespace Moka.Red.Navigation.Breadcrumb;

// Wide and Narrow differ only while MokaBreadcrumb.Fold folds the trail in a narrow box.
internal enum MokaBreadcrumbShow
{
	None,
	Always,
	Wide,
	Narrow
}

internal readonly record struct MokaBreadcrumbPlace(int Index, bool Current, MokaBreadcrumbShow Show, MokaBreadcrumbShow Separator)
{
	public static MokaBreadcrumbPlace Alone { get; } = new(0, false, MokaBreadcrumbShow.Always, MokaBreadcrumbShow.None);

	// A level created and dropped again while the trail rebuilds.
	public static MokaBreadcrumbPlace Unheld { get; } = new(-1, false, MokaBreadcrumbShow.None, MokaBreadcrumbShow.None);
}

// Read when the fold is drawn: the menu renders later, when the set may have moved on.
internal sealed record MokaBreadcrumbFoldedLevel(string? Text, string? Href, MokaIconDefinition Icon, int Depth);
