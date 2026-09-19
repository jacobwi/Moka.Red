using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.Timeline;

/// <summary>
///     Individual item within a <see cref="MokaTimeline" />.
/// </summary>
public partial class MokaTimelineItem
{
	/// <summary>Content for the timeline item.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Title text for the event.</summary>
	[Parameter]
	public string? Title { get; set; }

	/// <summary>Timestamp string (e.g., "2 hours ago", "Mar 15").</summary>
	[Parameter]
	public string? Timestamp { get; set; }

	/// <summary>Icon displayed inside the timeline dot.</summary>
	[Parameter]
	public MokaIconDefinition? Icon { get; set; }

	/// <summary>
	///     Color of the timeline dot. The icon inside takes the matching on-color, and a
	///     <see cref="MokaColor.Surface" /> dot gets an outline so it stands out from the page.
	///     Default null, the primary color.
	/// </summary>
	[Parameter]
	public MokaColor? DotColor { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-timeline-item";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-timeline-item--has-icon", Icon is not null)
		.AddClass(Class)
		.Build();

	private string? DotStyle
	{
		get
		{
			if (DotColor is not { } color)
			{
				return null;
			}

			string name = MokaEnumHelpers.ToCssClass(color);

			// A surface dot has the page's own colour, so without the outline it disappears.
			string border = color == MokaColor.Surface ? "var(--moka-color-outline)" : $"var(--moka-color-{name})";

			return new StyleBuilder()
				.AddStyle("background-color", $"var(--moka-color-{name})")
				.AddStyle("border-color", border)
				.AddStyle("color", $"var(--moka-color-on-{name})")
				.Build();
		}
	}
}
