using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.Timeline;

/// <summary>
///     Vertical timeline for displaying events or history.
///     Contains <see cref="MokaTimelineItem" /> children.
/// </summary>
public partial class MokaTimeline
{
	/// <summary>Timeline items (MokaTimelineItem children).</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Alternating left/right layout. Default false.</summary>
	[Parameter]
	public bool Alternate { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-timeline";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-timeline--alternate", Alternate)
		.AddClass(Class)
		.Build();
}
