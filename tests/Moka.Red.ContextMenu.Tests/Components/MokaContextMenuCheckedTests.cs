using AngleSharp.Dom;
using Bunit;
using static Moka.Red.ContextMenu.Tests.MenuDom;

namespace Moka.Red.ContextMenu.Tests.Components;

// A checked item drew a check, but screen readers heard a plain menu item.
public class MokaContextMenuCheckedTests : BunitContext
{
	public MokaContextMenuCheckedTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	private IElement RenderMenu(params MokaContextMenuItem[] items) =>
		Render<MokaContextMenu>(p => p
			.Add(x => x.Items, items)
			.Add(x => x.Visible, true)).Find("[role=menu]");

	[Fact]
	public void ACheckedItem_IsACheckedMenuItemCheckbox()
	{
		IElement row = Row(RenderMenu(
			new MokaContextMenuItem { Text = "Wrap lines", Checked = true },
			new MokaContextMenuItem { Text = "Show hidden" }), "Wrap lines");

		Assert.Equal("menuitemcheckbox", row.GetAttribute("role"));
		Assert.Equal("true", row.GetAttribute("aria-checked"));
	}

	[Fact]
	public void AnItemThatIsntChecked_StaysAPlainMenuItem()
	{
		IElement row = Row(RenderMenu(new MokaContextMenuItem { Text = "Show hidden" }), "Show hidden");

		Assert.Equal("menuitem", row.GetAttribute("role"));
		Assert.False(row.HasAttribute("aria-checked"));
	}

	[Fact]
	public void ACheckedItemWithChildren_StaysAPlainMenuItem()
	{
		IElement row = Row(RenderMenu(new MokaContextMenuItem
		{
			Text = "Sort",
			Checked = true,
			Children = [new MokaContextMenuItem { Text = "Name" }]
		}), "Sort");

		Assert.Equal("menuitem", row.GetAttribute("role"));
		Assert.False(row.HasAttribute("aria-checked"));
		Assert.Equal("true", row.GetAttribute("aria-haspopup"));
	}
}
