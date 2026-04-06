using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Theming;

namespace Moka.Red.ThemeGen.Editors;

/// <summary>
///     Editor for <see cref="MokaTypography" /> tokens including font families, sizes, line heights, and weights.
/// </summary>
public partial class MokaTypographyEditor : ComponentBase
{
	/// <summary>The typography being edited.</summary>
	[Parameter]
	public MokaTypography Typography { get; set; } = MokaTypography.Default;

	/// <summary>Fires when any typography token changes.</summary>
	[Parameter]
	public EventCallback<MokaTypography> TypographyChanged { get; set; }

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	private async Task HandleChange(Func<MokaTypography, MokaTypography> updater)
	{
		MokaTypography updated = updater(Typography);
		await TypographyChanged.InvokeAsync(updated);
	}
}
