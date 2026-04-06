using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Forms.SchedulePicker;

/// <summary>
///     Weekly time-slot grid for selecting availability. Displays days as columns
///     and time slots as rows. Click to toggle individual slots, or drag to select multiple.
/// </summary>
public partial class MokaSchedulePicker : MokaVisualComponentBase
{
	private bool _dragSelectMode;
	private bool _isDragging;

	/// <summary>Currently selected time slots. Two-way bindable.</summary>
	[Parameter]
	public IList<MokaTimeSlot> SelectedSlots { get; set; } = [];

	/// <summary>Callback fired when the selected slots change.</summary>
	[Parameter]
	public EventCallback<IList<MokaTimeSlot>> SelectedSlotsChanged { get; set; }

	/// <summary>First visible hour in 24-hour format. Default is 8 (8:00 AM).</summary>
	[Parameter]
	public int StartHour { get; set; } = 8;

	/// <summary>Last visible hour in 24-hour format. Default is 18 (6:00 PM).</summary>
	[Parameter]
	public int EndHour { get; set; } = 18;

	/// <summary>Duration of each slot in minutes. Default is 60.</summary>
	[Parameter]
	public int SlotDuration { get; set; } = 60;

	/// <summary>Days of the week to display. Default is Monday through Friday.</summary>
	[Parameter]
	public IReadOnlyList<DayOfWeek>? Days { get; set; }

	/// <summary>Whether the picker is read-only (no interaction).</summary>
	[Parameter]
	public bool ReadOnly { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-schedule-picker";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-schedule-picker--readonly", ReadOnly)
		.AddClass($"moka-schedule-picker--{MokaEnumHelpers.ToCssClass(Color ?? MokaColor.Primary)}")
		.AddClass(Class)
		.Build();

	private IReadOnlyList<DayOfWeek> VisibleDays => Days ??
	[
		DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
		DayOfWeek.Thursday, DayOfWeek.Friday
	];

	private List<(int Hour, int Minute)> TimeSlots
	{
		get
		{
			var slots = new List<(int, int)>();
			int totalStartMinutes = StartHour * 60;
			int totalEndMinutes = EndHour * 60;
			for (int m = totalStartMinutes; m < totalEndMinutes; m += SlotDuration)
			{
				slots.Add((m / 60, m % 60));
			}

			return slots;
		}
	}

	/// <summary>Has internal drag state.</summary>
	protected override bool ShouldRender() => true;

	private bool IsSelected(DayOfWeek day, int hour, int minute) =>
		SelectedSlots.Any(s => s.Day == day && s.StartHour == hour && s.StartMinute == minute);

	private async Task ToggleSlot(DayOfWeek day, int hour, int minute)
	{
		if (ReadOnly)
		{
			return;
		}

		MokaTimeSlot? existing = SelectedSlots.FirstOrDefault(s =>
			s.Day == day && s.StartHour == hour && s.StartMinute == minute);

		if (existing is not null)
		{
			SelectedSlots.Remove(existing);
		}
		else
		{
			SelectedSlots.Add(new MokaTimeSlot(day, hour, minute, SlotDuration));
		}

		await SelectedSlotsChanged.InvokeAsync(SelectedSlots);
	}

	private async Task HandleMouseDown(DayOfWeek day, int hour, int minute)
	{
		if (ReadOnly)
		{
			return;
		}

		_isDragging = true;
		_dragSelectMode = !IsSelected(day, hour, minute);
		await ApplyDrag(day, hour, minute);
	}

	private async Task HandleMouseEnter(DayOfWeek day, int hour, int minute)
	{
		if (!_isDragging || ReadOnly)
		{
			return;
		}

		await ApplyDrag(day, hour, minute);
	}

	private void HandleMouseUp() => _isDragging = false;

	private async Task ApplyDrag(DayOfWeek day, int hour, int minute)
	{
		bool isSelected = IsSelected(day, hour, minute);

		if (_dragSelectMode && !isSelected)
		{
			SelectedSlots.Add(new MokaTimeSlot(day, hour, minute, SlotDuration));
			await SelectedSlotsChanged.InvokeAsync(SelectedSlots);
		}
		else if (!_dragSelectMode && isSelected)
		{
			MokaTimeSlot existing = SelectedSlots.First(s =>
				s.Day == day && s.StartHour == hour && s.StartMinute == minute);
			SelectedSlots.Remove(existing);
			await SelectedSlotsChanged.InvokeAsync(SelectedSlots);
		}
	}

	private static string FormatTime(int hour, int minute) =>
		$"{hour:D2}:{minute:D2}";

	private static string FormatDayShort(DayOfWeek day) => day switch
	{
		DayOfWeek.Monday => "Mon",
		DayOfWeek.Tuesday => "Tue",
		DayOfWeek.Wednesday => "Wed",
		DayOfWeek.Thursday => "Thu",
		DayOfWeek.Friday => "Fri",
		DayOfWeek.Saturday => "Sat",
		DayOfWeek.Sunday => "Sun",
		_ => day.ToString()[..3]
	};
}
