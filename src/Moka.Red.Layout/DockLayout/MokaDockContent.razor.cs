using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Layout.DockLayout;

/// <summary>
///     The main area of a <see cref="MokaDockLayout" />: it takes the space the groups leave. Panels opened from code
///     land here as tabs; while none is open it shows its own child content.
/// </summary>
public partial class MokaDockContent : MokaDockPart
{
	private DockPlacement _placement = DockPlacement.Nowhere;
	private int _contentVersion;

	/// <summary>The content to show in the main area while no panel is open in it.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>
	///     Whether the area scrolls when its content overflows. Default true. Set false for content that scrolls itself,
	///     such as a terminal or an editor: the area then clips instead, and a single child fills it.
	/// </summary>
	[Parameter]
	public bool Scrollable { get; set; } = true;

	/// <summary>In a <see cref="MokaDockRow" /> or <see cref="MokaDockColumn" />, its size along the split. Unset is one share.</summary>
	[Parameter]
	public string? Size { get; set; }

	/// <summary>The area's name for screen readers and the narrow layout's tab. Unset is the layout's text for it.</summary>
	[Parameter]
	public string? Title { get; set; }

	internal override object Declaration => (Size, Title);

	/// <inheritdoc />
	protected override string RootClass => "moka-dock-content";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-dock-host", Layout is not null)
		.AddClass("moka-dock-content--no-scroll", !Scrollable)
		.AddClass(Class)
		.Build();

	private string HostId => Layout!.HostIdOf(MokaDockLayout.ContentPanelId);

	private string? HostStyle => new StyleBuilder()
		.AddStyle("position-anchor", _placement.Anchor > 0 ? MokaDockLayout.AnchorName(_placement.Anchor) : null)
		.AddStyle("z-index", _placement.Z > 0 ? _placement.Z.ToString(CultureInfo.InvariantCulture) : null)
		.AddStyle(Style)
		.Build();

	private string? AnchorAttribute => _placement.Anchor > 0 ? _placement.Anchor.ToString(CultureInfo.InvariantCulture) : null;

	/// <summary>The area redraws when the layout moves or covers it, outside the parameters its parent passes.</summary>
	/// <returns>Always true.</returns>
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();
		_contentVersion++;
		_placement = Layout?.ContentPlacement ?? DockPlacement.Nowhere;
	}

	internal void PlacementChanged()
	{
		DockPlacement placement = Layout?.ContentPlacement ?? DockPlacement.Nowhere;
		if (placement == _placement)
		{
			return;
		}

		_placement = placement;
		StateHasChanged();
	}
}
