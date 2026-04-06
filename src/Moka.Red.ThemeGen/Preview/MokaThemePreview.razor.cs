using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Theming;

namespace Moka.Red.ThemeGen.Preview;

/// <summary>
///     Live preview panel that renders sample components with the edited theme.
///     Applies theme CSS variables directly via inline style for isolated rendering.
/// </summary>
public partial class MokaThemePreview : ComponentBase
{
	/// <summary>The theme to preview.</summary>
	[Parameter]
	public MokaTheme PreviewTheme { get; set; } = MokaTheme.Light;

	/// <summary>Inline CSS variables for the preview scope.</summary>
	private string PreviewStyle => PreviewTheme.ToCssVariables();

	/// <inheritdoc />
	protected override bool ShouldRender() => true;
}
