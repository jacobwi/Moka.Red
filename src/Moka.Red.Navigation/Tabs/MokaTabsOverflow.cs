namespace Moka.Red.Navigation.Tabs;

/// <summary>What <see cref="MokaTabs" /> does with tabs that don't fit.</summary>
public enum MokaTabsOverflow
{
	/// <summary>The list scrolls sideways with no scrollbar.</summary>
	Scroll,

	/// <summary>Tabs that don't fit go into a menu behind a button with their count. The tab showing always stays.</summary>
	Menu
}
