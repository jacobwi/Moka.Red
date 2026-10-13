using AngleSharp.Dom;
using Moka.Red.Core.Utilities;
using Moka.Red.Feedback.Tooltip;

namespace Moka.Red.Tests.Shared;

/// <summary>
///     Checks an icon-only control the library draws: the browser's own <c>title</c> is gone, the control is named by
///     <c>aria-label</c>, and the same name shows in the MokaTooltip around it, on the side the control's place calls
///     for. Linked into the test projects that need it.
/// </summary>
internal static class IconTooltip
{
	/// <summary>Asserts the rule for <paramref name="control" /> and returns the tooltip's popup.</summary>
	public static IElement AssertNamed(IElement control, string name, MokaTooltipPosition position)
	{
		Assert.False(control.HasAttribute("title"), $"The control still carries title=\"{control.GetAttribute("title")}\".");
		Assert.Equal(name, control.GetAttribute("aria-label"));

		// The nearest trigger is this control's own tooltip; the popup is that trigger's child, never a nested one's.
		IElement? trigger = control.Closest(".moka-tooltip-trigger");
		Assert.NotNull(trigger);
		IElement popup = Assert.Single(trigger.Children, child => child.ClassList.Contains("moka-tooltip-popup"));
		Assert.Equal(name, popup.TextContent.Trim());
		Assert.Contains($"moka-tooltip-popup--{MokaEnumHelpers.ToCssClass(position)}", popup.ClassList);
		return popup;
	}
}
