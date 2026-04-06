using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Layout.StatusBar;

/// <summary>
///     Bottom status bar similar to VS Code's status bar. Fixed to the bottom of the viewport
///     with compact height and small font. Supports start (left) and end (right) content areas.
/// </summary>
public partial class MokaStatusBar : MokaVisualComponentBase
{
	/// <summary>Generic status bar content.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Left-aligned status items.</summary>
	[Parameter]
	public RenderFragment? StartContent { get; set; }

	/// <summary>Right-aligned status items.</summary>
	[Parameter]
	public RenderFragment? EndContent { get; set; }

	/// <summary>Whether to show a top border. Default true.</summary>
	[Parameter]
	public bool Bordered { get; set; } = true;

	/// <inheritdoc />
	protected override string RootClass => "moka-statusbar";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-statusbar--bordered", Bordered)
		.AddClass(Class)
		.Build();
}
