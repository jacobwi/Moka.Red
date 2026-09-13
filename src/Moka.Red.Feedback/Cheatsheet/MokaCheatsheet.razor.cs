using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Feedback.Cheatsheet;

/// <summary>
///     A keyboard-shortcut overlay: a centered modal listing shortcut groups in a
///     two-column grid, with descriptions on the left and key chips on the right.
///     Typically toggled by a "?" key handler in the host app. Renders nothing while closed.
/// </summary>
public partial class MokaCheatsheet : MokaVisualComponentBase
{
	/// <summary>Whether the cheatsheet is currently visible. Two-way bindable. Renders nothing when false.</summary>
	[Parameter]
	public bool Open { get; set; }

	/// <summary>Callback invoked when the open state changes.</summary>
	[Parameter]
	public EventCallback<bool> OpenChanged { get; set; }

	/// <summary>Heading shown in the header row. Defaults to "Keyboard Shortcuts".</summary>
	[Parameter]
	public string Title { get; set; } = "Keyboard Shortcuts";

	/// <summary>The shortcut groups rendered in the grid.</summary>
	[Parameter]
	public IReadOnlyList<MokaCheatsheetGroup>? Groups { get; set; }

	/// <summary>Whether clicking the backdrop closes the overlay. Defaults to true.</summary>
	[Parameter]
	public bool CloseOnBackdropClick { get; set; } = true;

	/// <summary>Whether pressing Escape closes the overlay. Defaults to true.</summary>
	[Parameter]
	public bool CloseOnEscape { get; set; } = true;

	/// <summary>Maximum modal width. Accepts any CSS width value. Defaults to "720px".</summary>
	[Parameter]
	public string MaxWidth { get; set; } = "720px";

	/// <summary>Optional hint line rendered under the shortcut grid.</summary>
	[Parameter]
	public RenderFragment? FooterContent { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-cheatsheet";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("max-width", MaxWidth)
		.AddStyle(Style)
		.Build();

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	private async Task HandleBackdropClick()
	{
		if (CloseOnBackdropClick)
		{
			await CloseAsync();
		}
	}

	private async Task HandleKeyDown(KeyboardEventArgs e)
	{
		if (CloseOnEscape && e.Key == "Escape")
		{
			await CloseAsync();
		}
	}

	private async Task CloseAsync()
	{
		Open = false;

		if (OpenChanged.HasDelegate)
		{
			await OpenChanged.InvokeAsync(false);
		}
	}
}
