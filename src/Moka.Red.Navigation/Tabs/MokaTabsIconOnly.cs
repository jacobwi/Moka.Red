namespace Moka.Red.Navigation.Tabs;

/// <summary>
///     When <see cref="MokaTabs" /> shows only the icons of tabs that have one. An icon-only tab keeps its name for screen
///     readers and shows it in a tooltip. A tab without an icon always shows its name.
/// </summary>
public enum MokaTabsIconOnly
{
	/// <summary>Every tab shows its name.</summary>
	Never,

	/// <summary>When the tabs' box is under 480 px (30rem), by a container query on that box with no script: a phone.</summary>
	Narrow,

	/// <summary>At every width.</summary>
	Always
}
