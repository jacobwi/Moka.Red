using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Forms.AutoComplete;
using Moka.Red.Forms.Calendar;
using Moka.Red.Forms.CodeEditor;
using Moka.Red.Forms.ColorWheel;
using Moka.Red.Forms.CreditCard;
using Moka.Red.Forms.DateRangePicker;
using Moka.Red.Forms.Knob;
using Moka.Red.Forms.Rating;
using Moka.Red.Forms.SchedulePicker;
using Moka.Red.Forms.SearchInput;
using Moka.Red.Forms.SelectField;
using Moka.Red.Forms.SignaturePad;
using Moka.Red.Forms.Slider;
using Moka.Red.Forms.TreeSelect;

namespace Moka.Red.Forms.Tests.Components;

// The inputs that aren't text boxes on the input bases, and the second value of the ones that bind two, each checked in
// every case of OwnedValueTests through its own UI and markup.
public sealed partial class OwnedValueTests
{
	private sealed class CodeEditorFamily : Family<MokaCodeEditor, string>
	{
		protected override string Initial => "a = 1";

		protected override string Picked => "a = 2";

		protected override string Second => "a = 3";

		protected override string Clamped => "a = 9";

		protected override string Default => "";

		protected override Task ChangeAsync(IRenderedComponent<IComponent> cut, string to) =>
			cut.Find("textarea").InputAsync(new ChangeEventArgs { Value = to });

		protected override string Shown(IRenderedComponent<IComponent> cut) => cut.Find("textarea").GetAttribute("value") ?? "";

		protected override string Text(string value) => value;
	}

	private sealed class RangeSliderFamily : Family<MokaRangeSlider, double>
	{
		protected override double Initial => 10;

		protected override double Picked => 40;

		protected override double Second => 70;

		protected override double Clamped => 50;

		protected override double Default => 0;

		protected override string ValueName => "ValueStart";

		protected override bool HasExpression => false;

		protected override IReadOnlyDictionary<string, object?> With => new Dictionary<string, object?> { ["ValueEnd"] = 90d };

		protected override Task ChangeAsync(IRenderedComponent<IComponent> cut, double to) =>
			StartThumb(cut).InputAsync(new ChangeEventArgs { Value = Text(to) });

		protected override string Shown(IRenderedComponent<IComponent> cut) => StartThumb(cut).GetAttribute("value") ?? "";

		protected override string Text(double value) => value.ToString(CultureInfo.InvariantCulture);

		private static IElement StartThumb(IRenderedComponent<IComponent> cut) => cut.Find(".moka-range-slider-input--start");
	}

	private sealed class RatingFamily : Family<MokaRating, int>
	{
		protected override int Initial => 1;

		protected override int Picked => 3;

		protected override int Second => 4;

		protected override int Clamped => 2;

		protected override int Default => 0;

		protected override bool HasExpression => false;

		protected override Task ChangeAsync(IRenderedComponent<IComponent> cut, int to) =>
			cut.FindAll(".moka-rating-star")[to - 1].ClickAsync(new MouseEventArgs());

		protected override string Shown(IRenderedComponent<IComponent> cut) =>
			cut.Find("[role=slider]").GetAttribute("aria-valuenow") ?? "";

		protected override string Text(int value) => value.ToString(CultureInfo.InvariantCulture);
	}

	// The arrows move the knob one step at a time, from what it shows.
	private sealed class KnobFamily : Family<MokaKnob, double>
	{
		protected override double Initial => 10;

		protected override double Picked => 11;

		protected override double Second => 12;

		protected override double Clamped => 50;

		protected override double Default => 0;

		protected override bool HasExpression => false;

		protected override async Task ChangeAsync(IRenderedComponent<IComponent> cut, double to)
		{
			double shown = double.Parse(Shown(cut), CultureInfo.InvariantCulture);
			string key = to > shown ? "ArrowRight" : "ArrowLeft";
			for (int step = 0; step < Math.Abs(to - shown); step++)
			{
				await cut.Find("[role=slider]").KeyDownAsync(new KeyboardEventArgs { Key = key });
			}
		}

		protected override string Shown(IRenderedComponent<IComponent> cut) =>
			cut.Find("[role=slider]").GetAttribute("aria-valuenow") ?? "";

		protected override string Text(double value) => value.ToString(CultureInfo.InvariantCulture);
	}

	private sealed class CalendarFamily : Family<MokaCalendar, DateOnly?>
	{
		protected override DateOnly? Initial => new DateOnly(2026, 3, 5);

		protected override DateOnly? Picked => new DateOnly(2026, 3, 10);

		protected override DateOnly? Second => new DateOnly(2026, 3, 12);

		protected override DateOnly? Clamped => new DateOnly(2026, 3, 20);

		protected override DateOnly? Default => null;

		protected override bool HasExpression => false;

		protected override IReadOnlyDictionary<string, object?> With =>
			new Dictionary<string, object?> { ["DisplayMonth"] = new DateOnly(2026, 3, 1) };

		protected override Task ChangeAsync(IRenderedComponent<IComponent> cut, DateOnly? to) =>
			cut.FindAll(".moka-calendar__day:not(.moka-calendar__day--adjacent)")
				.Single(day => day.TextContent.Trim() == Text(to))
				.ClickAsync(new MouseEventArgs());

		protected override string Shown(IRenderedComponent<IComponent> cut) =>
			cut.FindAll(".moka-calendar__day--selected").SingleOrDefault()?.TextContent.Trim() ?? "";

		protected override string Text(DateOnly? value) => value?.Day.ToString(CultureInfo.InvariantCulture) ?? "";
	}

	private sealed class ColorWheelFamily : Family<MokaColorWheel, string>
	{
		protected override string Initial => "#ff0000";

		protected override string Picked => "#00ff00";

		protected override string Second => "#0000ff";

		protected override string Clamped => "#ffff00";

		protected override string Default => "#ef5350";

		protected override bool HasExpression => false;

		protected override Task ChangeAsync(IRenderedComponent<IComponent> cut, string to) =>
			cut.Find(".moka-color-wheel-hex-input").ChangeAsync(new ChangeEventArgs { Value = to });

		protected override string Shown(IRenderedComponent<IComponent> cut) =>
			cut.Find(".moka-color-wheel-hex-input").GetAttribute("value") ?? "";

		protected override string Text(string value) => value;
	}

	// Types the fruit, waits for the suggestion and picks it.
	private sealed class AutoCompleteFamily : Family<MokaAutoComplete<string>, string?>
	{
		private static readonly string[] Fruits = ["apple", "banana", "cherry", "date"];

		protected override string? Initial => "apple";

		protected override string? Picked => "banana";

		protected override string? Second => "cherry";

		protected override string? Clamped => "date";

		protected override string? Default => null;

		protected override bool HasExpression => false;

		protected override IReadOnlyDictionary<string, object?> With => new Dictionary<string, object?>
		{
			["Debounce"] = 0,
			["SearchFunc"] = (Func<string, Task<IEnumerable<string>>>)(query =>
				Task.FromResult(Fruits.Where(fruit => fruit.StartsWith(query, StringComparison.Ordinal))))
		};

		protected override async Task ChangeAsync(IRenderedComponent<IComponent> cut, string? to)
		{
			await cut.Find("input").InputAsync(new ChangeEventArgs { Value = to });
			cut.WaitForAssertion(() => Assert.Single(cut.FindAll("[role=option]"), option => option.TextContent.Trim() == to));
			await cut.FindAll("[role=option]").Single(option => option.TextContent.Trim() == to).ClickAsync(new MouseEventArgs());
		}

		protected override string Shown(IRenderedComponent<IComponent> cut) => cut.Find("input").GetAttribute("value") ?? "";

		protected override string Text(string? value) => value ?? "";
	}

	private sealed class SearchFamily : Family<MokaSearchInput, string>
	{
		protected override string Initial => "ada";

		protected override string Picked => "grace";

		protected override string Second => "linus";

		protected override string Clamped => "gra";

		protected override string Default => "";

		protected override bool HasExpression => false;

		protected override Task ChangeAsync(IRenderedComponent<IComponent> cut, string to) =>
			cut.Find("input").InputAsync(new ChangeEventArgs { Value = to });

		protected override string Shown(IRenderedComponent<IComponent> cut) => cut.Find("input").GetAttribute("value") ?? "";

		protected override string Text(string value) => value;
	}

	private sealed class TreeSelectFamily : Family<MokaTreeSelect<string>, string?>
	{
		private static readonly Dictionary<string, string> Names = new()
		{
			["a"] = "Alpha", ["b"] = "Beta", ["c"] = "Gamma", ["d"] = "Delta"
		};

		protected override string? Initial => "a";

		protected override string? Picked => "b";

		protected override string? Second => "c";

		protected override string? Clamped => "d";

		protected override string? Default => null;

		protected override bool HasExpression => false;

		protected override IReadOnlyDictionary<string, object?> With => new Dictionary<string, object?>
		{
			["Placeholder"] = "None",
			["Items"] = Names.Select(pair => new MokaTreeSelectItem<string>(pair.Key, pair.Value)).ToList()
		};

		protected override async Task ChangeAsync(IRenderedComponent<IComponent> cut, string? to)
		{
			if (cut.FindAll(".moka-tree-select-item").Count == 0)
			{
				await cut.Find(".moka-tree-select-trigger").ClickAsync(new MouseEventArgs());
			}

			await cut.FindAll(".moka-tree-select-item").Single(item => item.TextContent.Trim() == Text(to))
				.ClickAsync(new MouseEventArgs());
		}

		protected override string Shown(IRenderedComponent<IComponent> cut) =>
			cut.Find(".moka-tree-select-display").TextContent.Trim();

		protected override string Text(string? value) => value is null ? "None" : Names[value];
	}

	// Picks a start in this month: the popup opens again each time, since one start leaves it waiting for the end.
	private sealed class DateRangeFamily : Family<MokaDateRangePicker, DateOnly?>
	{
		private static readonly DateOnly Month = new(DateTime.Today.Year, DateTime.Today.Month, 1);

		protected override DateOnly? Initial => Month.AddDays(2);

		protected override DateOnly? Picked => Month.AddDays(9);

		protected override DateOnly? Second => Month.AddDays(11);

		protected override DateOnly? Clamped => Month.AddDays(19);

		protected override DateOnly? Default => null;

		protected override string ValueName => "StartDate";

		protected override bool HasExpression => false;

		protected override async Task ChangeAsync(IRenderedComponent<IComponent> cut, DateOnly? to)
		{
			if (cut.FindAll(".moka-daterange__popup").Count > 0)
			{
				await cut.Find("input").ClickAsync(new MouseEventArgs());
			}

			await cut.Find("input").ClickAsync(new MouseEventArgs());
			string day = to!.Value.Day.ToString(CultureInfo.InvariantCulture);
			await cut.FindAll(".moka-daterange__day:not(.moka-daterange__day--other-month)")
				.First(cell => cell.TextContent.Trim() == day)
				.ClickAsync(new MouseEventArgs());
		}

		protected override string Shown(IRenderedComponent<IComponent> cut) => cut.Find("input").GetAttribute("value") ?? "";

		protected override string Text(DateOnly? value) =>
			value is { } start ? $"{start.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)} - ..." : "";
	}

	// Presses the last slot of the list, Monday to Friday from eight.
	private sealed class ScheduleFamily : Family<MokaSchedulePicker, IList<MokaTimeSlot>>
	{
		private static readonly DayOfWeek[] Week =
			[DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday];

		protected override IList<MokaTimeSlot> Initial => [];

		protected override IList<MokaTimeSlot> Picked => [Slot(DayOfWeek.Monday, 8)];

		protected override IList<MokaTimeSlot> Second => [Slot(DayOfWeek.Monday, 8), Slot(DayOfWeek.Monday, 9)];

		protected override IList<MokaTimeSlot> Clamped => [Slot(DayOfWeek.Tuesday, 8)];

		protected override IList<MokaTimeSlot> Default => [];

		protected override string ValueName => "SelectedSlots";

		protected override bool HasExpression => false;

		protected override async Task ChangeAsync(IRenderedComponent<IComponent> cut, IList<MokaTimeSlot> to)
		{
			MokaTimeSlot slot = to[^1];
			int index = (slot.StartHour - 8) * Week.Length + Array.IndexOf(Week, slot.Day);
			await cut.FindAll(".moka-schedule-picker__slot")[index].MouseDownAsync(new MouseEventArgs());
			await cut.Find(".moka-schedule-picker").MouseUpAsync(new MouseEventArgs());
		}

		protected override string Shown(IRenderedComponent<IComponent> cut) =>
			string.Join(",", cut.FindAll(".moka-schedule-picker__slot--selected").Select(slot => slot.GetAttribute("title")));

		protected override string Text(IList<MokaTimeSlot> value) => string.Join(",", value
			.OrderBy(slot => slot.StartHour).ThenBy(slot => Array.IndexOf(Week, slot.Day))
			.Select(slot => $"{slot.Day.ToString()[..3]} {slot.StartHour:D2}:{slot.StartMinute:D2}"));

		private static MokaTimeSlot Slot(DayOfWeek day, int hour) => new(day, hour, 0, 60);
	}

	// The drawing is the browser's, so the markup shows only whether the pad holds one.
	private sealed class SignatureFamily : Family<MokaSignaturePad, string?>
	{
		public override bool HasThreeValues => false;

		protected override string? Initial => null;

		protected override string? Picked => "data:image/png;base64,AAAA";

		protected override string? Default => null;

		protected override bool HasExpression => false;

		protected override Task ChangeAsync(IRenderedComponent<IComponent> cut, string? to)
		{
			IRenderedComponent<MokaSignaturePad> pad = cut.FindComponent<MokaSignaturePad>();
			return pad.InvokeAsync(() => pad.Instance.OnSignatureChanged(to));
		}

		protected override string Shown(IRenderedComponent<IComponent> cut) =>
			cut.FindAll(".moka-signature-pad__placeholder").Count == 0 ? "drawn" : "";

		protected override string Text(string? value) => string.IsNullOrEmpty(value) ? "" : "drawn";
	}

	// Types the digits; the box writes them in groups of four.
	private sealed class CreditCardFamily : Family<MokaCreditCardInput, string>
	{
		protected override string Initial => "4242 4242";

		protected override string Picked => "5555 5555";

		protected override string Second => "4000 1234";

		protected override string Clamped => "4111";

		protected override string Default => "";

		protected override string ValueName => "CardNumber";

		protected override bool HasExpression => false;

		protected override Task ChangeAsync(IRenderedComponent<IComponent> cut, string to) =>
			NumberBox(cut).InputAsync(new ChangeEventArgs { Value = to.Replace(" ", "", StringComparison.Ordinal) });

		protected override string Shown(IRenderedComponent<IComponent> cut) => NumberBox(cut).GetAttribute("value") ?? "";

		protected override string Text(string value) => value;

		private static IElement NumberBox(IRenderedComponent<IComponent> cut) => cut.Find(".moka-creditcard-input--number");
	}

	// A multiple select: each change picks the last letter of the list from the open list.
	private sealed class SelectValuesFamily : Family<MokaSelect<string>, IList<string>>
	{
		public override string Name => "MokaSelect.SelectedValues";

		protected override IList<string> Initial => [];

		protected override IList<string> Picked => ["a"];

		protected override IList<string> Second => ["a", "b"];

		protected override IList<string> Clamped => ["d"];

		protected override IList<string> Default => [];

		protected override string ValueName => "SelectedValues";

		protected override bool HasExpression => false;

		protected override IReadOnlyDictionary<string, object?> With => new Dictionary<string, object?>
		{
			["Items"] = new[] { "a", "b", "c", "d" },
			["Multiple"] = true
		};

		protected override async Task ChangeAsync(IRenderedComponent<IComponent> cut, IList<string> to)
		{
			if (cut.FindAll("[role=option]").Count == 0)
			{
				await cut.Find("[role=combobox]").ClickAsync(new MouseEventArgs());
			}

			await cut.FindAll("[role=option]").Single(option => option.TextContent.Trim() == to[^1])
				.ClickAsync(new MouseEventArgs());
		}

		protected override string Shown(IRenderedComponent<IComponent> cut) => string.Join(",",
			cut.FindAll(".moka-select-chip-remove").Select(button => button.GetAttribute("aria-label")!["Remove ".Length..]));

		protected override string Text(IList<string> value) => string.Join(",", value);
	}
}
