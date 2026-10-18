using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Navigation.MenuBar;

/// <summary>
///     One command in a <see cref="MokaMenuBarMenu" />. Choosing it closes the menu and then runs the action, so
///     an action that opens a dialog does not fight the menu for focus.
/// </summary>
public partial class MokaMenuBarItem : MokaVisualComponentBase
{
	private ElementReference _itemRef;

	/// <summary>The menu this belongs to. Supplied by <see cref="MokaMenuBarMenu" />.</summary>
	[CascadingParameter]
	public MokaMenuBarMenu? Menu { get; set; }

	/// <summary>The item's label.</summary>
	[Parameter]
	[EditorRequired]
	public string? Text { get; set; }

	/// <summary>Icon before the label. Items without one still line up with those that have one.</summary>
	[Parameter]
	public MokaIconDefinition? Icon { get; set; }

	/// <summary>
	///     Shortcut hint on the right, such as "Ctrl+N", drawn as one key cap per key. A label only: bind the key
	///     with <c>IMokaHotkeyService</c> and give its registration the same string.
	/// </summary>
	[Parameter]
	public string? Shortcut { get; set; }

	/// <summary>Runs when the item is chosen, after the menu has closed.</summary>
	[Parameter]
	public EventCallback OnClick { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-menubar-item";

	/// <summary>The highlight is the menu's state, not a parameter.</summary>
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-menubar-item--highlighted", Menu?.IsHighlighted(this) == true)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override void OnInitialized()
	{
		base.OnInitialized();
		Menu?.Register(this);
	}

	internal ValueTask FocusAsync() => SafeFocusAsync(_itemRef);

	internal async Task InvokeAsync()
	{
		if (OnClick.HasDelegate)
		{
			await OnClick.InvokeAsync();
		}
	}

	private async Task HandleClick()
	{
		if (Menu is not null)
		{
			await Menu.ChooseAsync(this);
			return;
		}

		await InvokeAsync();
	}
}
