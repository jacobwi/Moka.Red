using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Feedback.Drawer;

/// <summary>
///     A slide-in overlay panel from any screen edge.
///     Use for temporary content such as filters, settings, or navigation.
///     Unlike <c>MokaSidebar</c>, the drawer floats above page content with an optional backdrop.
/// </summary>
public partial class MokaDrawer : MokaComponentBase
{
	/// <summary>The drawer body content.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Whether the drawer is currently visible. Two-way bindable.</summary>
	[Parameter]
	public bool Open { get; set; }

	/// <summary>Callback invoked when the open state changes.</summary>
	[Parameter]
	public EventCallback<bool> OpenChanged { get; set; }

	/// <summary>The edge from which the drawer slides in. Defaults to <see cref="MokaDrawerPosition.Left" />.</summary>
	[Parameter]
	public MokaDrawerPosition Position { get; set; } = MokaDrawerPosition.Left;

	/// <summary>
	///     Drawer width for <see cref="MokaDrawerPosition.Left" /> and <see cref="MokaDrawerPosition.Right" /> positions.
	///     Accepts any CSS width value. Defaults to "320px".
	/// </summary>
	[Parameter]
	public string Width { get; set; } = "320px";

	/// <summary>
	///     Drawer height for <see cref="MokaDrawerPosition.Top" /> and <see cref="MokaDrawerPosition.Bottom" /> positions.
	///     Accepts any CSS height value. Defaults to "40vh".
	/// </summary>
	[Parameter]
	public string Height { get; set; } = "40vh";

	/// <summary>Optional title displayed in the drawer header.</summary>
	[Parameter]
	public string? Title { get; set; }

	/// <summary>Whether to show a close button in the header. Defaults to true.</summary>
	[Parameter]
	public bool ShowCloseButton { get; set; } = true;

	/// <summary>Whether clicking the backdrop closes the drawer. Defaults to true.</summary>
	[Parameter]
	public bool CloseOnBackdropClick { get; set; } = true;

	/// <summary>Whether pressing Escape closes the drawer. Defaults to true.</summary>
	[Parameter]
	public bool CloseOnEscape { get; set; } = true;

	/// <summary>Whether to show a backdrop overlay behind the drawer. Defaults to true.</summary>
	[Parameter]
	public bool Overlay { get; set; } = true;

	/// <inheritdoc />
	protected override string RootClass => "moka-drawer";

	private bool IsHorizontal => Position is MokaDrawerPosition.Left or MokaDrawerPosition.Right;

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-drawer--{PositionToKebab(Position)}")
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("width", Width, IsHorizontal)
		.AddStyle("height", Height, !IsHorizontal)
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

	private static string PositionToKebab(MokaDrawerPosition position) => position switch
	{
		MokaDrawerPosition.Left => "left",
		MokaDrawerPosition.Right => "right",
		MokaDrawerPosition.Top => "top",
		MokaDrawerPosition.Bottom => "bottom",
		_ => "left"
	};
}
