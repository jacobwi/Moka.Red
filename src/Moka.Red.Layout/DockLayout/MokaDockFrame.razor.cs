using System.ComponentModel;
using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Layout.DockLayout;

/// <summary>
///     Draws a <see cref="MokaDockLayout" />'s state: its splits and gutters, groups, floating windows, side bars and
///     the narrow layouts. Drawn by the layout; apps never write it. The layout asks it to redraw after a change, so a
///     change never redraws the layout itself, whose child content holds every panel.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed partial class MokaDockFrame : ComponentBase, IDisposable
{
	private readonly Queue<int> _drawing = new();
	private readonly List<(int Version, TaskCompletionSource Drawn)> _waiting = [];
	private int _order;
	private int _version;
	private int _rendered = -1;
	private int _drawn = -1;

	/// <summary>The layout to draw.</summary>
	[Parameter]
	[EditorRequired]
	public MokaDockLayout Layout { get; set; } = default!;

	private string FlyoutId => Layout.HostIdOf(MokaDockLayout.FlyoutKey) + "-flyout";

	internal void Refresh()
	{
		_version++;
		StateHasChanged();
	}

	// Completes once the page shows everything the frame has been asked to draw so far. OnAfterRender runs once the UI
	// has finished updating after each render (lifecycle.md); on Blazor Server that is after the browser applied the
	// batch, which can be after the frame has rendered again, so each render's version is queued and matched in turn.
	internal Task DrawnAsync()
	{
		if (_drawn >= _version)
		{
			return Task.CompletedTask;
		}

		TaskCompletionSource drawn = new(TaskCreationOptions.RunContinuationsAsynchronously);
		_waiting.Add((_version, drawn));
		return drawn.Task;
	}

	/// <inheritdoc />
	protected override void OnInitialized() => Layout.AttachFrame(this);

	/// <inheritdoc />
	protected override bool ShouldRender() => _version != _rendered;

	/// <inheritdoc />
	protected override void OnAfterRender(bool firstRender)
	{
		if (!_drawing.TryDequeue(out int drawn))
		{
			return;
		}

		_drawn = drawn;
		_waiting.RemoveAll(waiter =>
		{
			if (waiter.Version > drawn)
			{
				return false;
			}

			waiter.Drawn.SetResult();
			return true;
		});
	}

	/// <summary>Releases anyone waiting for a render that will no longer come.</summary>
	public void Dispose()
	{
		foreach ((_, TaskCompletionSource drawn) in _waiting)
		{
			drawn.SetResult();
		}

		_waiting.Clear();
	}

	private static IEnumerable<MokaDockPosition> SidesWithPins(MokaDockState state) =>
		state.AutoHidden.Select(a => a.Side).Distinct().Order();

	private static MokaDockAutoHidden? Flyout(MokaDockState state, string? group) =>
		group is null ? null : state.AutoHidden.FirstOrDefault(a => a.Group.Id == group);

	private MokaDockAutoHidden? Flyout(MokaDockState state) => Flyout(state, Layout.FlyoutGroup);

	private static string FlyoutStyle(MokaDockAutoHidden pinned) => new StyleBuilder()
		.AddStyle("--_flyout-size", pinned.Group.Size is { } size && !MokaDockState.TryShare(size, out _) ? size : "25%")
		.Build() ?? "";

	private static string FloatClass(MokaDockFloatingWindow window) => new CssBuilder("moka-dock-float")
		.AddClass("moka-dock-float--maximized", window.Maximized)
		.Build();

	private static string FloatStyle(MokaDockFloatingWindow window, int index)
	{
		StyleBuilder style = new StyleBuilder().AddStyle("z-index", (3 + index).ToString(CultureInfo.InvariantCulture));
		if (window.Maximized)
		{
			return style.Build() ?? "";
		}

		return style
			.AddStyle("left", Invariant($"clamp(0px, {window.X}px, calc(100% - 4rem))"))
			.AddStyle("top", Invariant($"clamp(0px, {window.Y}px, calc(100% - 2.5rem))"))
			.AddStyle("width", Invariant($"min({window.Width}px, 100%)"))
			.AddStyle("height", Invariant($"min({window.Height}px, 100%)"))
			.Build() ?? "";
	}

	private static string Invariant(FormattableString value) => FormattableString.Invariant(value);

	private static string SplitClass(MokaDockSplitNode split) => new CssBuilder("moka-dock-split")
		.AddClass(split.Direction == MokaDockSplitDirection.Row ? "moka-dock-split--row" : "moka-dock-split--column")
		.Build();

	private string SplitStyle(MokaDockSplitNode split) => new StyleBuilder()
		.AddStyle(split.Direction == MokaDockSplitDirection.Row ? "grid-template-columns" : "grid-template-rows",
			Layout.TrackList(split))
		.Build() ?? "";

	private string Limits(MokaDockSplitNode split, Func<DockGroupSettings, string?> limit) =>
		string.Join('|', split.Children.Select(c => c is MokaDockGroupNode ? limit(Layout.SettingsOf(c.Id)) ?? "" : ""));

	private static string Flex(MokaDockSplitNode split) =>
		string.Join('|', split.Children.Select(c => MokaDockLayout.IsShare(c) ? "1" : "0"));

	private static string GutterClass(bool vertical) => new CssBuilder("moka-dock-gutter")
		.AddClass(vertical ? "moka-dock-gutter--vertical" : "moka-dock-gutter--horizontal")
		.Build();

	private string NodeName(MokaDockNode node) => node switch
	{
		MokaDockGroupNode group => Layout.GroupName(group),
		MokaDockSplitNode split => string.Join(", ", MokaDockState.Groups(split).Select(Layout.GroupName)),
		_ => Layout.Words.Group
	};

	private string? ControlsOf(MokaDockNode node) =>
		MokaDockState.Groups(node).FirstOrDefault() is { Shown: { } shown } ? Layout.HostIdOf(shown) : null;

	// The first child's share of the pair in percent, when both are shares; the script writes the measured value once
	// it is bound, and after every move.
	private static string? ValueNow(MokaDockNode first, MokaDockNode second) =>
		MokaDockState.TryShare(first.Size, out double a) && MokaDockState.TryShare(second.Size, out double b)
			? Math.Round(a / (a + b) * 100).ToString(CultureInfo.InvariantCulture)
			: null;

	private static string ActivityClass(bool open) => new CssBuilder("moka-dock-activity__button")
		.AddClass("moka-dock-activity__button--open", open)
		.Build();

	private static string SideTabClass(bool open) => new CssBuilder("moka-dock-sidebar__tab")
		.AddClass("moka-dock-sidebar__tab--open", open)
		.Build();

	private string StackCellClass(MokaDockGroupNode group) => new CssBuilder("moka-dock-stack-cell")
		.AddClass("moka-dock-stack-cell--open", Layout.IsStackOpen(group.Id))
		.Build();

	private static string NarrowButtonClass(bool open) => new CssBuilder("moka-dock-narrow-bar__button")
		.AddClass("moka-dock-narrow-bar__button--open", open)
		.Build();

	private Task PinnedPressedAsync(string group, string panel)
	{
		bool open = Layout.FlyoutGroup == group && Layout.ViewState.FindGroup(group)?.Shown == panel;
		if (open)
		{
			Layout.CloseFlyout();
			return Task.CompletedTask;
		}

		Layout.OpenFlyout(group);
		return Layout.ActivatePanelAsync(panel);
	}

	private void SheetOpenChanged(bool open)
	{
		if (!open)
		{
			Layout.OpenSheet(null);
		}
	}
}
