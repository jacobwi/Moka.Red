using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;

namespace Moka.Red.Layout.StatusBar;

/// <summary>
///     A thin vertical rule between a bar's items: what an app would otherwise draw by styling the items'
///     neighbours itself. Sits in the bar's content beside <see cref="MokaStatusBarItem" />s and takes no room
///     beyond its line and the bar's own spacing.
/// </summary>
public partial class MokaStatusBarSeparator : MokaComponentBase
{
	/// <summary>The bar's own rule class; the CSS sizes and colours the line.</summary>
	protected override string RootClass => "moka-statusbar-separator";
}
