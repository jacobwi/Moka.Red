using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Theming;
using Moka.Red.Core.Utilities;

namespace Moka.Red.ThemeGen.Editors;

/// <summary>
///     Editor for <see cref="MokaSpacing" /> tokens including spacing scale and border radius.
/// </summary>
public partial class MokaSpacingEditor : ComponentBase
{
	/// <summary>The spacing being edited.</summary>
	[Parameter]
	public MokaSpacing Spacing { get; set; } = MokaSpacing.Default;

	/// <summary>Fires when any spacing token changes.</summary>
	[Parameter]
	public EventCallback<MokaSpacing> SpacingChanged { get; set; }

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	// The fields take any text, and the preview writes it into a declaration.
	private static string? PreviewStyle(string property, string value) => new StyleBuilder()
		.AddStyle(property, value)
		.Build();

	private async Task HandleChange(Func<MokaSpacing, MokaSpacing> updater)
	{
		MokaSpacing updated = updater(Spacing);
		await SpacingChanged.InvokeAsync(updated);
	}
}
