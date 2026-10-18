using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Navigation.MenuBar;

/// <summary>A line between groups of items in a <see cref="MokaMenuBarMenu" />.</summary>
public partial class MokaMenuBarDivider : MokaVisualComponentBase
{
	/// <inheritdoc />
	protected override string RootClass => "moka-menubar-divider";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass(Class)
		.Build();
}
