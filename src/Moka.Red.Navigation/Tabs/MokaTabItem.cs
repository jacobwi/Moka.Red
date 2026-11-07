using Moka.Red.Core.Icons;
using Moka.Red.Primitives.Badge;

namespace Moka.Red.Navigation.Tabs;

/// <summary>One tab in <see cref="MokaTabs" />.</summary>
/// <param name="Id">Stable and unique among the tabs. It is what <c>Active</c> holds.</param>
/// <param name="Text">The tab's name.</param>
/// <param name="Count">A number beside the name, drawn in <paramref name="Tone" />. Null shows none.</param>
/// <param name="Tone">What <paramref name="Count" /> means.</param>
/// <param name="Href">An address of its own, so the tab is a link. Without one the tab only switches state.</param>
/// <param name="Icon">An icon before the name.</param>
/// <param name="Disabled">The tab can't be picked, and the arrow keys pass over it.</param>
public sealed record MokaTabItem(
	string Id,
	string Text,
	int? Count = null,
	MokaBadgeTone Tone = MokaBadgeTone.Count,
	string? Href = null,
	MokaIconDefinition? Icon = null,
	bool Disabled = false);
