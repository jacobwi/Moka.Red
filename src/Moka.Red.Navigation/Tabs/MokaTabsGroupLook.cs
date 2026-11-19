namespace Moka.Red.Navigation.Tabs;

/// <summary>
///     How <see cref="MokaTabs" /> shows where its groups start (<see cref="MokaTabItem.Group" />). Either way screen
///     readers hear each tab's group, and a line stands between two groups.
/// </summary>
public enum MokaTabsGroupLook
{
	/// <summary>The group's name, as a small label before its first tab.</summary>
	Label,

	/// <summary>Only the line between groups, for a row with little room.</summary>
	Divider
}
