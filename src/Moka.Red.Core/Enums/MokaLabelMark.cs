namespace Moka.Red.Core.Enums;

/// <summary>How a list or tree item shows its <see cref="MokaLabelColor" />.</summary>
public enum MokaLabelMark
{
	/// <summary>The icon, or whatever takes its place, in the label's color. An item with no icon shows a dot.</summary>
	Icon,

	/// <summary>
	///     Like <see cref="Icon" />, with a soft fill of the label's color inside an outline icon: the look of a colored
	///     folder. Suits closed shapes such as folders, books and tags.
	/// </summary>
	TintedIcon,

	/// <summary>A small dot in the label's color before the name. The icon keeps its own color.</summary>
	Dot
}
