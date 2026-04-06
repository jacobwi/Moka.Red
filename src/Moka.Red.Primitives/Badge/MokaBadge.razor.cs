using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.Badge;

/// <summary>
///     Notification badge that wraps content and shows a count or dot indicator.
/// </summary>
public partial class MokaBadge
{
	/// <summary>The element to badge.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Badge text (e.g., "3", "99+").</summary>
	[Parameter]
	public string? Content { get; set; }

	/// <summary>Show just a dot, no text.</summary>
	[Parameter]
	public bool Dot { get; set; }

	/// <summary>Whether the badge is visible. Default true.</summary>
	[Parameter]
	public bool Visible { get; set; } = true;

	/// <summary>Maximum count before showing "N+". Default 99.</summary>
	[Parameter]
	public int MaxCount { get; set; } = 99;

	/// <summary>Whether the badge overlaps the wrapped content. Default true.</summary>
	[Parameter]
	public bool Overlap { get; set; } = true;

	/// <summary>Position of the badge indicator. Default TopRight.</summary>
	[Parameter]
	public MokaBadgePosition Position { get; set; } = MokaBadgePosition.TopRight;

	/// <inheritdoc />
	protected override string RootClass => "moka-badge";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-badge--{ColorToKebab(ResolvedColor)}")
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin)
		.AddStyle("padding", ResolvedPadding)
		.AddStyle(Style)
		.Build();

	private MokaColor ResolvedColor => Color ?? MokaColor.Error;

	private string PositionClass => Position switch
	{
		MokaBadgePosition.TopRight => "moka-badge__indicator--top-right",
		MokaBadgePosition.TopLeft => "moka-badge__indicator--top-left",
		MokaBadgePosition.BottomRight => "moka-badge__indicator--bottom-right",
		MokaBadgePosition.BottomLeft => "moka-badge__indicator--bottom-left",
		_ => "moka-badge__indicator--top-right"
	};

	private string? DisplayContent
	{
		get
		{
			if (Dot || string.IsNullOrEmpty(Content))
			{
				return null;
			}

			if (int.TryParse(Content, NumberStyles.Integer, CultureInfo.InvariantCulture, out int count) &&
			    count > MaxCount)
			{
				return $"{MaxCount}+";
			}

			return Content;
		}
	}
}
