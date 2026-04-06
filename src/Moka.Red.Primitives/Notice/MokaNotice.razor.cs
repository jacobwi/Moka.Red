using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.Notice;

/// <summary>
///     A full-width page-level banner notice. Unlike <c>MokaAlert</c> which is inline,
///     this notice spans the full viewport width and optionally sticks to the top or bottom.
/// </summary>
public partial class MokaNotice
{
	private bool _visible = true;

	/// <summary>Content displayed in the notice body.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Semantic color. Defaults to <see cref="MokaColor.Info" />.</summary>
	public override MokaColor? Color { get; set; } = MokaColor.Info;

	/// <summary>Position of the notice on the viewport. Default is <see cref="MokaNoticePosition.Top" />.</summary>
	[Parameter]
	public MokaNoticePosition Position { get; set; } = MokaNoticePosition.Top;

	/// <summary>Whether the notice can be dismissed. Default is true.</summary>
	[Parameter]
	public bool Closable { get; set; } = true;

	/// <summary>Callback invoked when the notice is closed.</summary>
	[Parameter]
	public EventCallback OnClose { get; set; }

	/// <summary>Optional leading icon.</summary>
	[Parameter]
	public MokaIconDefinition? Icon { get; set; }

	/// <summary>Whether the notice sticks to the viewport edge on scroll. Default is true.</summary>
	[Parameter]
	public bool Sticky { get; set; } = true;

	/// <inheritdoc />
	protected override string RootClass => "moka-notice";

	private MokaColor ResolvedColor => Color ?? MokaColor.Info;

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-notice--{ColorToKebab(ResolvedColor)}")
		.AddClass($"moka-notice--{PositionToKebab(Position)}")
		.AddClass("moka-notice--sticky", Sticky)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin)
		.AddStyle("padding", ResolvedPadding)
		.AddStyle(Style)
		.Build();

	/// <summary>Has internal visibility state.</summary>
	protected override bool ShouldRender() => true;

	private async Task HandleClose()
	{
		_visible = false;
		await OnClose.InvokeAsync();
	}

	private static string PositionToKebab(MokaNoticePosition position) => position switch
	{
		MokaNoticePosition.Top => "top",
		MokaNoticePosition.Bottom => "bottom",
		_ => "top"
	};
}
