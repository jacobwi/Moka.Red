using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Theming;
using Moka.Red.Core.Utilities;

namespace Moka.Red.ThemeGen.Editors;

/// <summary>
///     Editor for <see cref="MokaTypography" /> tokens including font families, sizes, line heights, and weights.
/// </summary>
public partial class MokaTypographyEditor : ComponentBase
{
	private readonly string _idPrefix = $"moka-typography-editor-{Guid.NewGuid():N}";

	/// <summary>The typography being edited.</summary>
	[Parameter]
	public MokaTypography Typography { get; set; } = MokaTypography.Default;

	/// <summary>Fires when any typography token changes.</summary>
	[Parameter]
	public EventCallback<MokaTypography> TypographyChanged { get; set; }

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	// The fields take any text, and the preview writes it into a declaration.
	private static string? FontSizeStyle(string value) => new StyleBuilder()
		.AddStyle("font-size", value)
		.Build();

	private async Task HandleChange(Func<MokaTypography, MokaTypography> updater)
	{
		MokaTypography updated = updater(Typography);
		await TypographyChanged.InvokeAsync(updated);
	}
}
