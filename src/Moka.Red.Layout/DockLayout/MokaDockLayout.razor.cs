using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Moka.Red.Core.Base;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Layout.DockLayout;

/// <summary>
///     A docked panel layout container that arranges child <see cref="MokaDockPanel" /> components
///     around a central <see cref="MokaDockContent" /> area using CSS grid.
///     Supports resizable splitters between panels.
/// </summary>
public partial class MokaDockLayout : MokaComponentBase
{
	private readonly List<MokaDockPanel> _panels = [];
	private GridTemplate _grid;
	private IJSObjectReference? _jsModule;

	/// <summary>Child content containing <see cref="MokaDockPanel" /> and <see cref="MokaDockContent" /> elements.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	[Inject] private IJSRuntime JsRuntime { get; set; } = default!;

	/// <inheritdoc />
	protected override string RootClass => "moka-dock-layout";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("display", "grid")
		.AddStyle("grid-template-columns", _grid.Columns)
		.AddStyle("grid-template-rows", _grid.Rows)
		.AddStyle("grid-template-areas", _grid.Areas)
		.AddStyle(Style)
		.Build();

	/// <inheritdoc />
	protected override void OnInitialized()
	{
		base.OnInitialized();
		_grid = BuildGrid();
	}

	internal void RegisterPanel(MokaDockPanel panel)
	{
		if (!_panels.Contains(panel))
		{
			_panels.Add(panel);
			RefreshGrid();
		}
	}

	internal void UnregisterPanel(MokaDockPanel panel)
	{
		if (_panels.Remove(panel))
		{
			RefreshGrid();
		}
	}

	/// <summary>
	///     Called by a panel whenever its dock edge, size, collapsed or floating state may have changed,
	///     including after every parameter set.
	/// </summary>
	internal void NotifyPanelChanged() => RefreshGrid();

	internal async ValueTask<IJSObjectReference> EnsureJsModuleAsync()
	{
		if (_jsModule is not null)
		{
			return _jsModule;
		}

		_jsModule = await JsRuntime.InvokeAsync<IJSObjectReference>(
			"import", "./_content/Moka.Red.Core/moka-drag.js");
		return _jsModule;
	}

	// The grid renders before the panels inside it receive their parameters, so a change a parent
	// makes to a panel only reaches the grid through this call, which queues a second pass in the
	// same render batch. Re-rendering only when the grid differs from the last one stops the
	// layout -> panel -> layout cycle after that pass.
	private void RefreshGrid()
	{
		GridTemplate grid = BuildGrid();
		if (grid == _grid)
		{
			return;
		}

		_grid = grid;
		ForceRender();
	}

	private GridTemplate BuildGrid() => new(BuildGridColumns(), BuildGridRows(), BuildGridAreas());

	private MokaDockPanel? GetPanel(MokaDockPosition position)
		=> _panels.FirstOrDefault(p => p.Dock == position && !p.IsFloating);

	private string GetPanelSize(MokaDockPosition position)
	{
		MokaDockPanel? panel = GetPanel(position);
		if (panel is null)
		{
			return "";
		}

		return panel.IsCollapsed ? panel.CollapsedSize ?? "0px" : panel.CurrentSize;
	}

	private bool HasPanel(MokaDockPosition position)
		=> _panels.Any(p => p.Dock == position && !p.IsFloating);

	private string BuildGridColumns()
	{
		bool hasLeft = HasPanel(MokaDockPosition.Left);
		bool hasRight = HasPanel(MokaDockPosition.Right);

		string left = hasLeft ? GetPanelSize(MokaDockPosition.Left) : "";
		string right = hasRight ? GetPanelSize(MokaDockPosition.Right) : "";

		return string.Join(" ",
			new[] { left, "1fr", right }
				.Where(s => !string.IsNullOrEmpty(s)));
	}

	private string BuildGridRows()
	{
		bool hasTop = HasPanel(MokaDockPosition.Top);
		bool hasBottom = HasPanel(MokaDockPosition.Bottom);

		string top = hasTop ? GetPanelSize(MokaDockPosition.Top) : "";
		string bottom = hasBottom ? GetPanelSize(MokaDockPosition.Bottom) : "";

		return string.Join(" ",
			new[] { top, "1fr", bottom }
				.Where(s => !string.IsNullOrEmpty(s)));
	}

	private string BuildGridAreas()
	{
		bool hasLeft = HasPanel(MokaDockPosition.Left);
		bool hasRight = HasPanel(MokaDockPosition.Right);
		bool hasTop = HasPanel(MokaDockPosition.Top);
		bool hasBottom = HasPanel(MokaDockPosition.Bottom);

		List<string> rows = [];

		if (hasTop)
		{
			string topRow = BuildAreaRow("top", hasLeft, hasRight);
			rows.Add($"'{topRow}'");
		}

		string contentRow = BuildAreaRow("content", hasLeft, hasRight);
		rows.Add($"'{contentRow}'");

		if (hasBottom)
		{
			string bottomRow = BuildAreaRow("bottom", hasLeft, hasRight);
			rows.Add($"'{bottomRow}'");
		}

		return string.Join(" ", rows);
	}

	private static string BuildAreaRow(string center, bool hasLeft, bool hasRight)
	{
		List<string> cols = [];
		if (hasLeft)
		{
			cols.Add("left");
		}

		cols.Add(center);
		if (hasRight)
		{
			cols.Add("right");
		}

		return string.Join(" ", cols);
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		if (_jsModule is not null)
		{
			try
			{
				await _jsModule.DisposeAsync();
			}
			catch (JSDisconnectedException)
			{
			}

			_jsModule = null;
		}

		await base.DisposeAsyncCore();
	}

	private readonly record struct GridTemplate(string Columns, string Rows, string Areas);
}
