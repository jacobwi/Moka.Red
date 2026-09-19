using AngleSharp.Dom;
using Bunit;
using Moka.Red.Forms.Calendar;

namespace Moka.Red.Forms.Tests.Components;

// aria-selected was a bare bool. Blazor writes true as an empty value, which ARIA reads as not set,
// and leaves false out, so no day was announced as selected (gotcha #10).
public class MokaCalendarTests : BunitContext
{
	[Fact]
	public void AriaSelected_IsTrueOnTheChosenDay_AndFalseOnTheOthers()
	{
		var value = new DateOnly(2026, 4, 15);
		IRenderedComponent<MokaCalendar> cut = Render<MokaCalendar>(p => p
			.Add(x => x.Value, value)
			.Add(x => x.DisplayMonth, value));

		IReadOnlyList<IElement> days = cut.FindAll("button[role=gridcell]");
		IElement chosen = Assert.Single(days, day => day.GetAttribute("aria-selected") == "true");

		Assert.Equal("15", chosen.TextContent.Trim());
		Assert.All(days.Where(day => day != chosen), day => Assert.Equal("false", day.GetAttribute("aria-selected")));
	}

	[Fact]
	public void AriaSelected_IsFalseEverywhereWithoutAValue()
	{
		IRenderedComponent<MokaCalendar> cut = Render<MokaCalendar>(p => p
			.Add(x => x.DisplayMonth, new DateOnly(2026, 4, 1)));

		Assert.All(cut.FindAll("button[role=gridcell]"), day => Assert.Equal("false", day.GetAttribute("aria-selected")));
	}
}
