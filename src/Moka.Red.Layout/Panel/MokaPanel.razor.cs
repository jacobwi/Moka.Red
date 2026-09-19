using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Layout.Panel;

/// <summary>
///     A titled content panel with optional toolbar actions and collapsible body.
/// </summary>
public partial class MokaPanel : MokaVisualComponentBase
{
	private readonly string _regionId = $"moka-panel-{Guid.NewGuid():N}";
	private bool _collapsed;
	private bool? _lastCollapsed;

	/// <summary>The child content rendered in the panel body.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Simple text title for the panel header.</summary>
	[Parameter]
	public string? Title { get; set; }

	/// <summary>Custom title content. Overrides <see cref="Title" />.</summary>
	[Parameter]
	public RenderFragment? TitleContent { get; set; }

	/// <summary>Toolbar actions rendered in the header, aligned to the right.</summary>
	[Parameter]
	public RenderFragment? Actions { get; set; }

	/// <summary>When true, the panel body can be collapsed. Default false.</summary>
	[Parameter]
	public bool Collapsible { get; set; }

	/// <summary>Whether the panel body is collapsed. Two-way bindable.</summary>
	[Parameter]
	public bool Collapsed { get; set; }

	/// <summary>Callback when <see cref="Collapsed" /> changes.</summary>
	[Parameter]
	public EventCallback<bool> CollapsedChanged { get; set; }

	/// <summary>When true (default), renders a border around the panel.</summary>
	[Parameter]
	public bool Bordered { get; set; } = true;

	/// <summary>When true, applies elevation shadow instead of border.</summary>
	[Parameter]
	public bool Elevated { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-panel";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-panel--bordered", Bordered && !Elevated)
		.AddClass("moka-panel--elevated", Elevated)
		.AddClass("moka-panel--collapsed", _collapsed)
		.AddClass(Class)
		.Build();

	private string BodyId => $"{_regionId}-body";

	private string TitleId => $"{_regionId}-title";

	private string ToggleExpanded => _collapsed ? "false" : "true";

	// The toggle is an icon, so it takes its name from the title, or names itself without one.
	private string? ToggleLabelledBy => TitleContent is null && Title is not null ? TitleId : null;

	private string? ToggleLabel => ToggleLabelledBy is null ? "Toggle panel" : null;

	private string ChevronCssClass => new CssBuilder("moka-panel-chevron")
		.AddClass("moka-panel-chevron--expanded", !_collapsed)
		.Build();

	/// <summary>The panel collapses itself on click, outside the parameter flow.</summary>
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		// Collapsed only seeds the state when the parent passes a new value. Copying it on every
		// parent render snapped an unbound panel back to where it started.
		if (_lastCollapsed != Collapsed)
		{
			_lastCollapsed = Collapsed;
			_collapsed = Collapsed;
		}
	}

	private async Task ToggleCollapse()
	{
		_collapsed = !_collapsed;
		await CollapsedChanged.InvokeAsync(_collapsed);
	}
}
