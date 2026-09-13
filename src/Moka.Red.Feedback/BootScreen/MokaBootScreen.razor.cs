using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Feedback.BootScreen;

/// <summary>
///     A full-screen terminal-style boot/splash screen: brand mark with glow, pulsing status dot,
///     spec chips, an indeterminate progress bar, a faint accent grid, and a sweeping scanline.
///     Follows the active theme, so it suits in-app loading states (data bootstrapping,
///     workspace switching, reconnect screens).
///     For the pre-boot splash shown before Blazor starts, use the static markup +
///     <c>_content/Moka.Red.Core/moka-boot.css</c> instead (see that file's header comment).
/// </summary>
public partial class MokaBootScreen : MokaVisualComponentBase
{
	/// <summary>Extra content rendered below the progress bar.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Whether the boot screen is rendered. Defaults to true.</summary>
	[Parameter]
	public bool Visible { get; set; } = true;

	/// <summary>The brand mark text. Defaults to "MOKA".</summary>
	[Parameter]
	public string Brand { get; set; } = "MOKA";

	/// <summary>Optional second brand segment, separated from <see cref="Brand" /> by a dot.</summary>
	[Parameter]
	public string? BrandSecondary { get; set; }

	/// <summary>Status line under the brand mark, shown with a pulsing dot. Defaults to "initializing".</summary>
	[Parameter]
	public string? StatusText { get; set; } = "initializing";

	/// <summary>Spec chips row (key/value pairs). Use <c>&lt;span&gt;label&lt;span class="moka-boot-screen-v"&gt;VALUE&lt;/span&gt;&lt;/span&gt;</c> per chip.</summary>
	[Parameter]
	public RenderFragment? ChipsContent { get; set; }

	/// <summary>Whether to render the faint accent grid backdrop. Defaults to true.</summary>
	[Parameter]
	public bool ShowGrid { get; set; } = true;

	/// <summary>Whether to render the animated scanline. Defaults to true.</summary>
	[Parameter]
	public bool ShowScanline { get; set; } = true;

	/// <summary>Whether to render the indeterminate progress bar. Defaults to true.</summary>
	[Parameter]
	public bool ShowBar { get; set; } = true;

	/// <summary>
	///     When true (default) the screen covers the viewport with <c>position: fixed</c>.
	///     When false it fills its parent container instead (useful for demos and panels).
	/// </summary>
	[Parameter]
	public bool FullScreen { get; set; } = true;

	/// <inheritdoc />
	protected override string RootClass => "moka-boot-screen";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-boot-screen--grid", ShowGrid)
		.AddClass("moka-boot-screen--scanline", ShowScanline)
		.AddClass("moka-boot-screen--embedded", !FullScreen)
		.AddClass(Class)
		.Build();
}
