using System.ComponentModel;
using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Moka.Red.Core.Enums;

namespace Moka.Red.Layout.DockLayout;

public partial class MokaDockLayout
{
	private const double NewWindowWidth = 360;
	private const double NewWindowHeight = 280;

	private readonly Dictionary<string, MokaDockWindowSession> _windows = new(StringComparer.Ordinal);
	private bool _popOutSupported;

	/// <summary>
	///     Opens a panel from code, such as a document, or shows it if it is already open. Either way the panel takes
	///     these options, so opening it again with a new title, icon, badge or content updates its tab and its content.
	/// </summary>
	/// <param name="options">The panel.</param>
	/// <param name="target">Where it opens. Null is the main area, else the group used last.</param>
	/// <returns>A task that completes once the panel is open.</returns>
	public async Task OpenPanelAsync(MokaDockPanelOptions options, MokaDockTarget? target = null)
	{
		ArgumentNullException.ThrowIfNull(options);
		EnsureCurrent();
		_opened[options.Id] = options;
		_structureDirty = true;
		_openedHost?.Refresh();
		if (_state.Locate(options.Id).Place != MokaDockPlace.None)
		{
			await ActivatePanelAsync(options.Id);
			return;
		}

		MokaDockState next = _state.Add(options.Id, target ?? _state.OpenTarget());
		await CommitAsync(next);
		await PanelOpened.InvokeAsync(new MokaDockPanelChange(options.Id, _state.Locate(options.Id)));
	}

	/// <summary>Shows a panel in its group, bringing it back first if it was closed.</summary>
	/// <param name="panel">The panel's id.</param>
	/// <returns>A task that completes once it shows.</returns>
	public async Task ActivatePanelAsync(string panel)
	{
		ArgumentNullException.ThrowIfNull(panel);
		EnsureCurrent();
		bool wasHidden = _state.Locate(panel).Place == MokaDockPlace.Hidden;
		await CommitAsync(_state.Activate(panel));
		if (_state.Locate(panel) is { Place: MokaDockPlace.AutoHidden, Group: { } group })
		{
			OpenFlyout(group);
		}

		if (wasHidden)
		{
			await PanelOpened.InvokeAsync(new MokaDockPanelChange(panel, _state.Locate(panel)));
		}

		await PanelActivated.InvokeAsync(new MokaDockPanelChange(panel, _state.Locate(panel)));
	}

	/// <summary>Shows a closed panel again where it was.</summary>
	/// <param name="panel">The panel's id.</param>
	/// <returns>A task that completes once it shows.</returns>
	public Task ShowPanelAsync(string panel) => ActivatePanelAsync(panel);

	/// <summary>
	///     Closes a panel. A declared panel can be shown again with <see cref="ShowPanelAsync" />; one opened from code is
	///     forgotten.
	/// </summary>
	/// <param name="panel">The panel's id.</param>
	/// <param name="force">True closes it without asking its <c>CanClose</c>.</param>
	/// <returns>True when the panel closed, false when <c>CanClose</c> refused.</returns>
	public async Task<bool> ClosePanelAsync(string panel, bool force = false)
	{
		ArgumentNullException.ThrowIfNull(panel);
		EnsureCurrent();
		if (_state.Locate(panel).Place is MokaDockPlace.None or MokaDockPlace.Hidden)
		{
			return false;
		}

		Func<Task<bool>>? canClose = PanelFor(panel)?.CanClose ?? (_opened.TryGetValue(panel, out MokaDockPanelOptions? o) ? o.CanClose : null);
		if (!force && canClose is not null && !await canClose())
		{
			return false;
		}

		string title = TitleOf(panel);
		bool declared = !_opened.ContainsKey(panel);
		if (!declared)
		{
			_opened.Remove(panel);
			_structureDirty = true;
			_openedHost?.Refresh();
		}

		if (_windows.Remove(panel, out MokaDockWindowSession? session))
		{
			session.ReturnRequested -= OnWindowReturnRequested;
			session.End();
		}

		await CommitAsync(_state.Close(panel, remember: declared));
		Announce(Words.Closed(title));
		await PanelClosed.InvokeAsync(new MokaDockPanelChange(panel, _state.Locate(panel)));
		return true;
	}

	/// <summary>Moves a panel into a group as a tab, or into a new group beside a node or at the layout's edge.</summary>
	/// <param name="panel">The panel's id.</param>
	/// <param name="target">Where it lands.</param>
	/// <returns>A task that completes once it moved.</returns>
	public Task MovePanelAsync(string panel, MokaDockTarget target)
	{
		EnsureCurrent();
		return ChangeAsync(panel, _state.Move(panel, target), Words.Moved);
	}

	/// <summary>Takes a panel out into a floating window, sized and placed to fit the layout.</summary>
	/// <param name="panel">The panel's id.</param>
	/// <returns>A task that completes once it floats.</returns>
	public async Task FloatPanelAsync(string panel)
	{
		EnsureCurrent();
		(double x, double y, double width, double height) = await NewWindowAsync();
		await ChangeAsync(panel, _state.FloatPanel(panel, x, y, width, height), Words.Floated);
	}

	/// <summary>Pins a panel's group to a side bar.</summary>
	/// <param name="panel">The panel's id.</param>
	/// <param name="side">The side bar.</param>
	/// <returns>A task that completes once it is pinned.</returns>
	public Task AutoHidePanelAsync(string panel, MokaDockPosition side)
	{
		EnsureCurrent();
		return _state.Locate(panel).Group is { } group
			? ChangeAsync(panel, _state.AutoHide(group, side), Words.Pinned)
			: Task.CompletedTask;
	}

	/// <summary>Makes a panel's group fill the layout, or brings every group back.</summary>
	/// <param name="panel">A panel in the group to maximize, or null to restore.</param>
	/// <returns>A task that completes once it is done.</returns>
	public Task MaximizeAsync(string? panel)
	{
		EnsureCurrent();
		string? group = panel is null ? null : _state.Locate(panel).Group;
		return CommitAsync(_state.Maximize(group));
	}

	/// <summary>
	///     Opens a panel in another browser window. The panel's content has to be a component (<c>Component</c>): the
	///     window renders it in a circuit of its own, since one circuit can't draw into two windows. On Blazor Server
	///     this needs <see cref="MokaDockServiceCollectionExtensions.AddMokaDockWindows" />. Where there is no window to
	///     open, as in a MAUI app, nothing happens.
	/// </summary>
	/// <param name="panel">The panel's id.</param>
	/// <returns>True when the window opened.</returns>
	public async Task<bool> PopOutPanelAsync(string panel)
	{
		ArgumentNullException.ThrowIfNull(panel);
		EnsureCurrent();
		if (!CanPopOut(panel) || Services.GetService<MokaDockWindows>() is not { } windows
		                       || Services.GetService<NavigationManager>() is not { } navigation)
		{
			return false;
		}

		MokaDockComponent component = (PanelFor(panel)?.Component ?? _opened.GetValueOrDefault(panel)?.Component)!;
		MokaDockWindowSession session = windows.Open(panel, TitleOf(panel), component);
		string url = navigation.ToAbsoluteUri("moka-dock-window?session=" + Uri.EscapeDataString(session.Key)).ToString();
		bool opened = await SafeModuleInvokeAsync<bool>(ModulePath, "openWindow", _root, url, panel);
		if (!opened)
		{
			session.End();
			return false;
		}

		session.ReturnRequested += OnWindowReturnRequested;
		_windows[panel] = session;
		await ChangeAsync(panel, _state.PopOut(panel), Words.Moved);
		return true;
	}

	/// <summary>Brings a panel shown in another window back where it was, and closes the window.</summary>
	/// <param name="panel">The panel's id.</param>
	/// <returns>A task that completes once it is back.</returns>
	public async Task BringBackAsync(string panel)
	{
		ArgumentNullException.ThrowIfNull(panel);
		if (_windows.Remove(panel, out MokaDockWindowSession? session))
		{
			session.ReturnRequested -= OnWindowReturnRequested;
			session.End();
			await SafeModuleInvokeVoidAsync(ModulePath, "closeWindow", _root, panel);
		}

		await ChangeAsync(panel, _state.ReturnPoppedOut(panel), Words.Docked);
	}

	/// <summary>
	///     Goes back to the layout the markup declares, and forgets the saved one. Panels opened from code stay open: in
	///     their group when the markup has it, else where <see cref="OpenPanelAsync" /> puts them. Panels shown in another
	///     window stay there.
	/// </summary>
	/// <returns>A task that completes once it is back.</returns>
	public async Task ResetAsync()
	{
		EnsureCurrent();
		_flyoutGroup = null;
		await ForgetSavedAsync();
		await CommitAsync(Fit(_defaults), save: false);
		_stateChanged = false;
	}

	/// <summary>
	///     Switches to one of the <see cref="Presets" />. The change animates through a view transition where the browser
	///     has one, the page is showing and the person hasn't asked for less motion. Panels opened from code stay open:
	///     in their group when the preset has it, else where <see cref="OpenPanelAsync" /> puts them. Panels shown in
	///     another window stay there.
	/// </summary>
	/// <param name="name">The preset's name.</param>
	/// <returns>True, once the preset is applied, when it exists.</returns>
	public async Task<bool> ApplyPresetAsync(string name)
	{
		if (Presets?.FirstOrDefault(p => p.Name == name) is null)
		{
			return false;
		}

		bool animated = _bound && await SafeModuleInvokeAsync<bool>(ModulePath, "transition", _root, _dotNetRef, name);
		if (!animated)
		{
			await ApplyPresetNowAsync(name);
		}

		return true;
	}

	/// <summary>
	///     Applies a preset. Called by the layout's script as the update of its view transition, which takes its picture
	///     of the new layout when this completes.
	/// </summary>
	/// <param name="name">The preset's name.</param>
	/// <returns>A task that completes once the page shows the new layout.</returns>
	[JSInvokable]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public async Task ApplyPresetNowAsync(string name)
	{
		EnsureCurrent();
		if (Presets?.FirstOrDefault(p => p.Name == name) is not { } preset)
		{
			return;
		}

		await CommitAsync(Fit(preset.State));
		if (_frame is { } frame)
		{
			await frame.DrawnAsync();
		}
	}

	internal async Task ActivateFromViewAsync(string panel)
	{
		if (panel == ContentPanelId)
		{
			_narrowShown = panel;
			Recompute();
			_frame?.Refresh();
			return;
		}

		if (IsNarrow)
		{
			_narrowShown = panel;
		}

		await ActivatePanelAsync(panel);
	}

	internal Task CloseFromViewAsync(string panel) => ClosePanelAsync(panel);

	internal async Task FloatGroupAsync(string group)
	{
		(double x, double y, double width, double height) = await NewWindowAsync();
		MokaDockGroupNode? node = _state.FindGroup(group);
		await ChangeAsync(node?.Shown, _state.FloatGroup(group, x, y, width, height), Words.Floated);
	}

	internal Task DockGroupAsync(string group)
	{
		MokaDockGroupNode? node = _state.FindGroup(group);
		if (_flyoutGroup == group)
		{
			_flyoutGroup = null;
		}

		return ChangeAsync(node?.Shown, _state.Dock(group), Words.Docked);
	}

	internal Task ToggleCollapsedAsync(string group)
	{
		MokaDockGroupNode? node = _state.FindGroup(group);
		return node is null ? Task.CompletedTask : CommitAsync(_state.SetCollapsed(group, !node.Collapsed));
	}

	internal Task ToggleMaximizedAsync(string group)
	{
		if (_state.Floating.FirstOrDefault(w => w.Group.Id == group) is { } window)
		{
			return CommitAsync(_state.SetFloatingMaximized(group, !window.Maximized));
		}

		return CommitAsync(_state.Maximize(_state.Maximized == group ? null : group));
	}

	internal Task AutoHideGroupAsync(string group, MokaDockPosition side) =>
		ChangeAsync(_state.FindGroup(group)?.Shown, _state.AutoHide(group, side), Words.Pinned);

	internal Task MoveFromMenuAsync(string panel, MokaDockTarget target) => MovePanelAsync(panel, target);

	internal void OpenFlyout(string group)
	{
		if (_flyoutGroup == group)
		{
			return;
		}

		_flyoutGroup = group;
		Recompute();
		_frame?.Refresh();
	}

	internal void CloseFlyout()
	{
		if (_flyoutGroup is null)
		{
			return;
		}

		_flyoutGroup = null;
		Recompute();
		_frame?.Refresh();
	}

	internal void ToggleFlyout(string group)
	{
		if (_flyoutGroup == group)
		{
			CloseFlyout();
		}
		else
		{
			OpenFlyout(group);
		}
	}

	internal void OpenSheet(string? panel)
	{
		_sheetPanel = panel;
		_frame?.Refresh();
	}

	internal void ToggleStack(string group)
	{
		if (!_stackClosed.Remove(group))
		{
			_stackClosed.Add(group);
		}

		Recompute();
		_frame?.Refresh();
	}

	internal async Task SetPanelCollapsedAsync(MokaDockPanel panel, bool collapsed)
	{
		EnsureCurrent();
		if (_state.Locate(IdOf(panel)).Group is { } group)
		{
			await CommitAsync(_state.SetCollapsed(group, collapsed));
		}
	}

	internal async Task SetPanelFloatingAsync(MokaDockPanel panel, bool floating)
	{
		EnsureCurrent();
		string id = IdOf(panel);
		MokaDockPanelLocation location = _state.Locate(id);
		if (floating && location.Place == MokaDockPlace.Docked)
		{
			await CommitAsync(_state.FloatPanel(id, panel.FloatingX, panel.FloatingY,
				Pixels(panel.FloatingWidth, 300), Pixels(panel.FloatingHeight, 400)));
		}
		else if (!floating && location is { Place: MokaDockPlace.Floating, Group: { } group })
		{
			await CommitAsync(_state.Dock(group));
		}
	}

	internal async Task SetPanelWindowAsync(MokaDockPanel panel)
	{
		EnsureCurrent();
		if (_state.Locate(IdOf(panel)) is { Place: MokaDockPlace.Floating, Group: { } group })
		{
			await CommitAsync(_state.MoveFloating(group, panel.FloatingX, panel.FloatingY,
				Pixels(panel.FloatingWidth, 300), Pixels(panel.FloatingHeight, 400)));
		}
	}

	internal async Task SetPanelSizeAsync(MokaDockPanel panel, string? size)
	{
		EnsureCurrent();
		if (_state.Locate(IdOf(panel)) is not { Place: MokaDockPlace.Docked, Group: { } group } || _state.Root is null
		    || MokaDockState.FindParent(_state.Root, group) is not var (parent, index))
		{
			return;
		}

		string?[] sizes = [.. parent.Children.Select(c => c.Size)];
		sizes[index] = size ?? LegacySize;
		await CommitAsync(_state.Resize(parent.Id, sizes));
	}

	internal void PanelMetaChanged() => _frame?.Refresh();

	/// <summary>A split's children were resized by a splitter. Called by the layout's script, once per drag.</summary>
	/// <param name="split">The split's id.</param>
	/// <param name="sizes">Every child's size in pixels.</param>
	/// <returns>A task that completes once the state is updated.</returns>
	[JSInvokable]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public Task OnSplitResized(string split, double[] sizes)
	{
		ArgumentNullException.ThrowIfNull(sizes);
		if (_state.FindNode(split) is not MokaDockSplitNode node || node.Children.Count != sizes.Length)
		{
			return Task.CompletedTask;
		}

		double[] shares = [.. sizes.Where((_, i) => IsShare(node.Children[i]))];
		double average = shares.Length > 0 ? shares.Average() : 1;
		string?[] next = new string?[sizes.Length];
		for (int i = 0; i < sizes.Length; i++)
		{
			MokaDockNode child = node.Children[i];
			next[i] = IsShare(child)
				? MokaDockState.Share(average > 0 ? sizes[i] / average : 1)
				: string.Create(CultureInfo.InvariantCulture, $"{Math.Round(sizes[i], 1)}px");
		}

		return CommitAsync(_state.Resize(split, next));
	}

	/// <summary>A splitter was double-clicked or Enter was pressed on it: the two children beside it go back to their starting sizes.</summary>
	/// <param name="split">The split's id.</param>
	/// <param name="index">The splitter's number: it sits between child index and child index + 1.</param>
	/// <returns>A task that completes once the state is updated.</returns>
	[JSInvokable]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public Task OnSplitReset(string split, int index)
	{
		if (_state.FindNode(split) is not MokaDockSplitNode node || index < 0 || index + 1 >= node.Children.Count)
		{
			return Task.CompletedTask;
		}

		string?[] sizes = [.. node.Children.Select(c => c.Size)];
		for (int i = index; i <= index + 1; i++)
		{
			sizes[i] = _defaults.FindNode(node.Children[i].Id)?.Size ?? (IsShare(node.Children[i]) ? null : node.Children[i].Size);
		}

		if (sizes[index] is null && sizes[index + 1] is null)
		{
			sizes[index] = null;
		}

		return CommitAsync(_state.Resize(split, sizes));
	}

	/// <summary>A dragged tab or group was dropped on a target. Called by the layout's script, once per drag.</summary>
	/// <param name="kind"><c>panel</c> or <c>group</c>.</param>
	/// <param name="id">The panel's or group's id.</param>
	/// <param name="node">The node dropped on, or null for the layout's edge.</param>
	/// <param name="zone"><c>center</c>, <c>left</c>, <c>right</c>, <c>top</c> or <c>bottom</c>.</param>
	/// <param name="index">The tab position for a drop into a tab strip, else -1.</param>
	/// <returns>A task that completes once the state is updated.</returns>
	[JSInvokable]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public Task OnDrop(string kind, string id, string? node, string zone, int index)
	{
		MokaDockZone where = zone switch
		{
			"left" => MokaDockZone.Left,
			"right" => MokaDockZone.Right,
			"top" => MokaDockZone.Top,
			"bottom" => MokaDockZone.Bottom,
			_ => MokaDockZone.Center
		};
		MokaDockTarget target = new(node, where, index >= 0 ? index : null);
		if (kind == "group")
		{
			MokaDockGroupNode? group = _state.FindGroup(id);
			return ChangeAsync(group?.Shown, _state.MoveGroup(id, target), Words.Moved);
		}

		return ChangeAsync(id, _state.Move(id, target), Words.Moved);
	}

	/// <summary>A dragged tab or group was dropped away from every target, so it floats there. Called by the layout's script.</summary>
	/// <param name="kind"><c>panel</c> or <c>group</c>.</param>
	/// <param name="id">The panel's or group's id.</param>
	/// <param name="x">The window's left edge, in pixels from the layout's.</param>
	/// <param name="y">The window's top edge.</param>
	/// <param name="width">The window's width.</param>
	/// <param name="height">The window's height.</param>
	/// <returns>A task that completes once the state is updated.</returns>
	[JSInvokable]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public Task OnDropFloat(string kind, string id, double x, double y, double width, double height)
	{
		if (kind == "group")
		{
			if (!IsFloatable(id))
			{
				return Task.CompletedTask;
			}

			return ChangeAsync(_state.FindGroup(id)?.Shown, _state.FloatGroup(id, x, y, width, height), Words.Floated);
		}

		return _state.Locate(id).Group is { } group && !IsFloatable(group) && _state.FindGroup(group)?.Panels.Count == 1
			? Task.CompletedTask
			: ChangeAsync(id, _state.FloatPanel(id, x, y, width, height), Words.Floated);
	}

	/// <summary>A floating window was moved or resized. Called by the layout's script, once per drag.</summary>
	/// <param name="group">The group the window shows.</param>
	/// <param name="x">The left edge in pixels.</param>
	/// <param name="y">The top edge in pixels.</param>
	/// <param name="width">The width in pixels.</param>
	/// <param name="height">The height in pixels.</param>
	/// <returns>A task that completes once the state is updated.</returns>
	[JSInvokable]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public Task OnFloatMoved(string group, double x, double y, double width, double height) =>
		CommitAsync(_state.MoveFloating(group, Math.Round(x), Math.Round(y), Math.Round(width), Math.Round(height)));

	/// <summary>Focus or a press went into a floating window behind another. Called by the layout's script.</summary>
	/// <param name="group">The group the window shows.</param>
	/// <returns>A task that completes once the state is updated.</returns>
	[JSInvokable]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public Task OnFloatFocused(string group) => CommitAsync(_state.BringToFront(group));

	/// <summary>Escape was pressed in the layout: a maximized group comes back, else an open side flyout closes.</summary>
	/// <returns>A task that completes once it is done.</returns>
	[JSInvokable]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public async Task OnEscape()
	{
		if (_state.Maximized is not null)
		{
			await CommitAsync(_state.Maximize(null));
			return;
		}

		if (_state.Floating.FirstOrDefault(w => w.Maximized) is { } window)
		{
			await CommitAsync(_state.SetFloatingMaximized(window.Group.Id, false));
			return;
		}

		CloseFlyout();
	}

	/// <summary>The layout crossed <see cref="NarrowBelow" />. Called by the layout's script.</summary>
	/// <param name="narrow">Whether it is now narrower.</param>
	[JSInvokable]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public void OnNarrowChanged(bool narrow)
	{
		if (_narrow == narrow)
		{
			return;
		}

		_narrow = narrow;
		_sheetPanel = null;
		_flyoutGroup = null;
		Recompute();
		_frame?.Refresh();
	}

	/// <summary>Ctrl+Tab or Ctrl+Shift+Tab in a group. Called by the layout's script.</summary>
	/// <param name="group">The group's id.</param>
	/// <param name="delta">1 for the next tab, -1 for the one before.</param>
	/// <returns>A task that completes once the tab shows.</returns>
	[JSInvokable]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public Task OnCycleTab(string group, int delta)
	{
		MokaDockGroupNode? node = group == NarrowGroupId ? NarrowGroup() : _state.FindGroup(group);
		if (node is null || node.Panels.Count < 2 || node.Shown is not { } shown)
		{
			return Task.CompletedTask;
		}

		int index = (node.Panels.ToList().IndexOf(shown) + delta + node.Panels.Count) % node.Panels.Count;
		return ActivateFromViewAsync(node.Panels[index]);
	}

	/// <summary>Delete on a tab that can close. Called by the layout's script.</summary>
	/// <param name="panel">The panel's id.</param>
	/// <returns>A task that completes once it is done.</returns>
	[JSInvokable]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public Task OnCloseKey(string panel) => IsClosable(panel) ? ClosePanelAsync(panel) : Task.CompletedTask;

	/// <summary>A pinned group's tab was hovered long enough to open it. Called by the layout's script.</summary>
	/// <param name="group">The group's id.</param>
	[JSInvokable]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public void OnFlyoutHover(string group) => OpenFlyout(group);

	/// <summary>A press landed outside the open side flyout. Called by the layout's script.</summary>
	[JSInvokable]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public void OnFlyoutDismiss() => CloseFlyout();

	/// <summary>A panel's other window was closed. Called by the layout's script, which watches the window.</summary>
	/// <param name="panel">The panel's id.</param>
	/// <returns>A task that completes once the panel is back.</returns>
	[JSInvokable]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public Task OnWindowClosed(string panel)
	{
		if (_windows.Remove(panel, out MokaDockWindowSession? session))
		{
			session.ReturnRequested -= OnWindowReturnRequested;
			session.End();
		}

		return _state.Locate(panel).Place == MokaDockPlace.PoppedOut
			? ChangeAsync(panel, _state.ReturnPoppedOut(panel), Words.Docked)
			: Task.CompletedTask;
	}

	/// <summary>Whether the browser can open another window, from the layout's script once it is bound.</summary>
	/// <param name="supported">Whether <c>window.open</c> is there and this isn't an app's web view.</param>
	[JSInvokable]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public void OnPopOutSupport(bool supported)
	{
		bool value = supported && RendererInfo.Name != "WebView" && Services.GetService<MokaDockWindows>() is not null;
		if (value == _popOutSupported)
		{
			return;
		}

		_popOutSupported = value;
		ViewVersion++;
		_frame?.Refresh();
	}

	private void OnWindowReturnRequested(object? sender, EventArgs e)
	{
		if (sender is MokaDockWindowSession session)
		{
			RunDetached(() => BringBackAsync(session.Panel));
		}
	}

	internal void UpdateWindowSession(MokaDockPanel panel)
	{
		if (panel.Component is { } component && _windows.TryGetValue(IdOf(panel), out MokaDockWindowSession? session))
		{
			session.Update(panel.Title ?? IdOf(panel), component);
		}
	}

	// A new window steps down from the layout's corner, one step per window already floating, and fits the area windows
	// float over, which only the browser can measure.
	private async Task<(double X, double Y, double Width, double Height)> NewWindowAsync()
	{
		double offset = 32 * (_state.Floating.Count % 6);
		double x = 48 + offset;
		double y = 48 + offset;
		double width = NewWindowWidth;
		double height = NewWindowHeight;
		if (_bound && await SafeModuleInvokeAsync<double[]>(ModulePath, "measureMain", _root) is [var areaWidth, var areaHeight]
		           && areaWidth > 0 && areaHeight > 0)
		{
			width = Math.Min(width, areaWidth);
			height = Math.Min(height, areaHeight);
			x = Math.Clamp(x, 0, areaWidth - width);
			y = Math.Clamp(y, 0, areaHeight - height);
		}

		return (x, y, width, height);
	}

	private async Task ChangeAsync(string? panel, MokaDockState next, Func<string, string> announce)
	{
		if (ReferenceEquals(next, _state))
		{
			return;
		}

		await CommitAsync(next);
		if (panel is null)
		{
			return;
		}

		Announce(announce(TitleOf(panel)));
		await PanelMoved.InvokeAsync(new MokaDockPanelChange(panel, _state.Locate(panel)));
	}

	private void Announce(string text) => _announcer?.Say(text);

	private void SetState(MokaDockState next, bool notify)
	{
		bool changed = !ReferenceEquals(next, _state);
		_state = next;
		if (_flyoutGroup is not null && !_state.AutoHidden.Any(a => a.Group.Id == _flyoutGroup))
		{
			_flyoutGroup = null;
		}

		Recompute();
		if (changed || notify)
		{
			_frame?.Refresh();
		}
	}

	// Takes a new state: redraws what it touches, tells the app, saves it and raises the declared panels' own
	// callbacks (CollapsedChanged, FloatingChanged, SizeChanged, the floating bounds) for what changed.
	private async Task CommitAsync(MokaDockState next, bool save = true)
	{
		EnsureCurrent();
		if (ReferenceEquals(next, _state))
		{
			return;
		}

		MokaDockState before = _state;
		_stateChanged = true;
		SetState(next, notify: true);
		_lastStateParameter = State;

		await StateChanged.InvokeAsync(_state);
		if (save)
		{
			await SaveAsync(_state);
		}

		foreach (MokaDockPanel panel in _panels.Values.ToList())
		{
			await panel.StateChangedAsync(before, _state);
		}
	}
}
