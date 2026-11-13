using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.List;

/// <summary>
///     Structured list for navigation, selection, or data display.
///     Wraps <see cref="MokaListItem" /> children.
/// </summary>
public partial class MokaList
{
	private const string KeysModule = "./_content/Moka.Red.Core/moka-keys.js";

	// MokaTreeItem's toggle column, which its rows keep whether or not they have children. Its stylesheet holds the
	// same width, so tree rows here line their icons up with the tree's.
	private const string TreeToggleColumn = "20px";

	private ElementReference _root;
	private bool _keyboardBound;

	/// <summary>Child content containing <see cref="MokaListItem" /> elements.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Reduces vertical padding for compact layouts. Default true.</summary>
	[Parameter]
	public bool Dense { get; set; } = true;

	/// <summary>Adds a border around the list. Default false.</summary>
	[Parameter]
	public bool Bordered { get; set; }

	/// <summary>Enables hover highlight on items. Default true.</summary>
	[Parameter]
	public bool Hoverable { get; set; } = true;

	/// <summary>
	///     How the rows are sized. <see cref="MokaListRowStyle.Standard" /> by default; <see cref="MokaListRowStyle.Tree" />
	///     matches <c>MokaTree</c>'s rows, for a list that sits with a tree in one sidebar. <see cref="Dense" /> doesn't apply
	///     to tree rows.
	/// </summary>
	[Parameter]
	public MokaListRowStyle RowStyle { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-list";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-list--dense", Dense)
		.AddClass("moka-list--bordered", Bordered)
		.AddClass("moka-list--hoverable", Hoverable)
		.AddClass("moka-list--tree-rows", RowStyle == MokaListRowStyle.Tree)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => RowStyle == MokaListRowStyle.Tree
		? new StyleBuilder().AddStyle("--_list-tree-toggle", TreeToggleColumn).AddStyle(Style).Build()
		: Style;

	internal MokaSize ItemIconSize => RowStyle == MokaListRowStyle.Tree ? MokaSize.Xs : MokaSize.Sm;

	/// <summary>
	///     Turns on Enter and Space activation for the list's clickable rows, with one listener for
	///     all of them. Clickable items call this after they render, so a list without any never
	///     loads the module.
	/// </summary>
	internal async Task EnableKeyboardActivationAsync()
	{
		// Every clickable item asks after its first render; the first one binds.
		if (_keyboardBound)
		{
			return;
		}

		_keyboardBound = true;
		await SafeModuleInvokeVoidAsync(KeysModule, "bindActivation", _root, ".moka-list-item--interactive");
	}
}
