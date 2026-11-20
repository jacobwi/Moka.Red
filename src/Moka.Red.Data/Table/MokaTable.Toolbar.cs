using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Theming;
using Moka.Red.Icons;

namespace Moka.Red.Data.Table;

// The toolbar's density menu. The other toolbar parts belong to the feature they drive.
public partial class MokaTable<TItem>
{
	private static readonly MokaTableDensity[] Densities =
		[MokaTableDensity.Compact, MokaTableDensity.Standard, MokaTableDensity.Comfortable];

	private readonly string _densityMenuKey = $"moka-table-density-menu-{Guid.NewGuid():N}";
	private readonly string _densityGroupName = $"moka-table-density-{Guid.NewGuid():N}";
	private bool _showDensityMenu;
	private bool _densityMenuShown;
	private ElementReference _densityMenuRef;
	private ElementReference _densityToggleRef;

	private MokaIconDefinition DensityIcon =>
		_density == MokaTableDensity.Compact ? MokaIcons.Content.RowsDense : MokaIcons.Content.RowsComfortable;

	private string DensityName(MokaTableDensity density) => density switch
	{
		MokaTableDensity.Standard => Words.DensityStandard,
		MokaTableDensity.Comfortable => Words.DensityComfortable,
		_ => Words.DensityCompact
	};

	private void ToggleDensityMenu() => _showDensityMenu = !_showDensityMenu;

	private async Task PickDensityAsync(MokaTableDensity density)
	{
		_densityFromParameter = false;
		_density = density;
		_showDensityMenu = false;
		await NotifyStateChangedAsync();
	}

	// In the top layer like the column menu, so the table's rounded frame can't cut it off.
	private async Task SyncDensityMenuAsync()
	{
		bool open = _showDensityMenu && ShowDensityToggle && ShowToolbar && !SelectionBarShown;
		if (_densityMenuShown == open)
		{
			return;
		}

		_densityMenuShown = open;
		if (open)
		{
			await SafeModuleInvokeVoidAsync(PopupModule, "open", _densityMenuKey, _densityMenuRef, _densityToggleRef, null,
				new Dictionary<string, object?> { ["placement"] = "bottom-end" });
		}
		else
		{
			await SafeModuleInvokeVoidAsync(PopupModule, "close", _densityMenuKey);
		}
	}
}
