using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Moka.Red.Core.Base;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Layout.DockLayout;

/// <summary>
///     A part of a <see cref="MokaDockLayout" /> declared in markup: a split, a group, a panel or the main area. The
///     markup only says where things start; the layout draws them from its <see cref="MokaDockState" />.
/// </summary>
public abstract class MokaDockPart : MokaComponentBase
{
	[CascadingParameter]
	internal MokaDockLayout? Layout { get; set; }

	[CascadingParameter]
	internal MokaDockContainer? Container { get; set; }

	private object? _declared;

	// What the layout builds from this part. Only a change to it rebuilds anything.
	internal abstract object Declaration { get; }

	/// <inheritdoc />
	protected override void OnInitialized()
	{
		base.OnInitialized();
		Layout?.Register(this);
	}

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();
		object declared = Declaration;
		if (declared.Equals(_declared))
		{
			return;
		}

		bool first = _declared is null;
		_declared = declared;
		if (!first)
		{
			Layout?.DeclarationChanged(this);
		}
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		Layout?.Unregister(this);
		await base.DisposeAsyncCore();
	}
}

/// <summary>A declared part that holds others: <see cref="MokaDockRow" />, <see cref="MokaDockColumn" /> or <see cref="MokaDockGroup" />.</summary>
public abstract class MokaDockContainer : MokaDockPart
{
	internal List<MokaDockPart> Parts { get; } = [];

	/// <summary>The parts it holds.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>
	///     Its starting size along the split it is in: a CSS length such as <c>260px</c> or <c>25%</c>, or a share
	///     such as <c>2fr</c>. Unset is one share.
	/// </summary>
	[Parameter]
	public string? Size { get; set; }

	internal override object Declaration => (Id, Size);

	/// <inheritdoc />
	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		ArgumentNullException.ThrowIfNull(builder);
		builder.OpenComponent<CascadingValue<MokaDockContainer>>(0);
		builder.AddComponentParameter(1, nameof(CascadingValue<MokaDockContainer>.Value), this);
		builder.AddComponentParameter(2, nameof(CascadingValue<MokaDockContainer>.IsFixed), true);
		builder.AddComponentParameter(3, nameof(CascadingValue<MokaDockContainer>.ChildContent), ChildContent);
		builder.CloseComponent();
	}
}

/// <summary>A declared split: its parts side by side.</summary>
public sealed class MokaDockRow : MokaDockContainer
{
	/// <inheritdoc />
	protected override string RootClass => "moka-dock-row";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass).AddClass(Class).Build();
}

/// <summary>A declared split: its parts one above the other.</summary>
public sealed class MokaDockColumn : MokaDockContainer
{
	/// <inheritdoc />
	protected override string RootClass => "moka-dock-column";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass).AddClass(Class).Build();
}

/// <summary>
///     A declared group: its <see cref="MokaDockPanel" />s start as tabs in one place. <c>Class</c> and <c>Style</c>
///     go on the group's frame wherever it is drawn.
/// </summary>
public sealed class MokaDockGroup : MokaDockContainer
{
	/// <summary>The smallest the group can be dragged to along its split, as a CSS length. Unset is 48px.</summary>
	[Parameter]
	public string? MinSize { get; set; }

	/// <summary>The largest the group can be dragged to along its split, as a CSS length.</summary>
	[Parameter]
	public string? MaxSize { get; set; }

	/// <summary>Whether the splitter beside the group moves. Default true.</summary>
	[Parameter]
	public bool Resizable { get; set; } = true;

	/// <summary>Whether the header shows a button that folds the group to a strip.</summary>
	[Parameter]
	public bool Collapsible { get; set; }

	/// <summary>Whether the group starts folded.</summary>
	[Parameter]
	public bool Collapsed { get; set; }

	/// <summary>The folded strip's size, as a CSS length. Default <c>0px</c>, which leaves nothing on screen.</summary>
	[Parameter]
	public string? CollapsedSize { get; set; } = "0px";

	/// <summary>How the group shows its tabs. Unset follows <see cref="MokaDockLayout.TabStyle" />.</summary>
	[Parameter]
	public MokaDockTabStyle? TabStyle { get; set; }

	/// <summary>
	///     For a group placed directly in the layout, not in a <see cref="MokaDockRow" /> or <see cref="MokaDockColumn" />:
	///     the edge it docks to, around the main area.
	/// </summary>
	[Parameter]
	public MokaDockPosition? Dock { get; set; }

	/// <summary>Starts the group pinned to this side's bar instead of docked.</summary>
	[Parameter]
	public MokaDockPosition? AutoHide { get; set; }

	/// <summary>The group's name for screen readers and menus. Unset is its showing panel's title.</summary>
	[Parameter]
	public string? Title { get; set; }

	internal override object Declaration =>
		(Id, Size, (MinSize, MaxSize, Resizable, Collapsible, Collapsed, CollapsedSize), (TabStyle, Dock, AutoHide, Title, Class, Style));

	/// <inheritdoc />
	protected override string RootClass => "moka-dock-group";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass).AddClass(Class).Build();
}
