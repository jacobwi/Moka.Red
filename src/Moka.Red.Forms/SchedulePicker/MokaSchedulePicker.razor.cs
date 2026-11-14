using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Utilities;
using Moka.Red.Forms.Common;

namespace Moka.Red.Forms.SchedulePicker;

/// <summary>
///     Weekly time-slot grid for selecting availability. Displays days as columns
///     and time slots as rows. Click to toggle individual slots, or drag to select multiple.
/// </summary>
public partial class MokaSchedulePicker : MokaVisualComponentBase
{
	// The slots the grid shows, apart from SelectedSlots. A change goes out as a new list, never by editing the list
	// the parent passed, which threw on an array and left a parent that kept its list unable to put it back.
	private readonly MokaInputValue<IList<MokaTimeSlot>> _slots;
	private bool _dragSelectMode;
	private bool _isDragging;

	/// <summary>Creates the picker. It shows its own slots until the parent passes a list.</summary>
	public MokaSchedulePicker() => _slots = new MokaInputValue<IList<MokaTimeSlot>>(StateHasChanged,
		DispatchExceptionAsync, [], MokaSequenceComparer<MokaTimeSlot>.Default);

	/// <summary>
	///     Currently selected time slots. Two-way bindable. A change is reported as a new list; the list passed in is
	///     never edited.
	/// </summary>
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

	private string? GridStyle => new StyleBuilder()
		.AddStyle("grid-template-columns", $"auto {string.Join(" ", VisibleDays.Select(_ => "1fr"))}")
		.Build();

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

	private IList<MokaTimeSlot> Slots => _slots.Shown ?? [];

	/// <summary>Has internal drag state.</summary>
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	public override Task SetParametersAsync(ParameterView parameters)
	{
		_slots.Take(parameters, nameof(SelectedSlots));
		return base.SetParametersAsync(parameters);
	}

	/// <inheritdoc />
	protected override void OnAfterRender(bool firstRender)
	{
		base.OnAfterRender(firstRender);
		_slots.AfterRender();
	}

	/// <inheritdoc />
	protected override ValueTask DisposeAsyncCore()
	{
		_slots.Stop();
		return base.DisposeAsyncCore();
	}

	private bool IsSelected(DayOfWeek day, int hour, int minute) =>
		Slots.Any(s => s.Day == day && s.StartHour == hour && s.StartMinute == minute);

	private void HandleMouseDown(DayOfWeek day, int hour, int minute)
	{
		if (ReadOnly)
		{
			return;
		}

		_isDragging = true;
		_dragSelectMode = !IsSelected(day, hour, minute);
		ApplyDrag(day, hour, minute);
	}

	// Only a drag uses these. A default EventCallback renders no listener, and a week of half hours is 336 slots
	// that would otherwise each be a round trip and a render for the mouse just passing over.
	private EventCallback SlotEnterHandler(DayOfWeek day, int hour, int minute) => _isDragging
		? EventCallback.Factory.Create(this, () => ApplyDrag(day, hour, minute))
		: default;

	private EventCallback DragLeaveHandler => _isDragging
		? EventCallback.Factory.Create(this, HandleMouseUp)
		: default;

	private void HandleMouseUp() => _isDragging = false;

	private void ApplyDrag(DayOfWeek day, int hour, int minute)
	{
		bool isSelected = IsSelected(day, hour, minute);

		if (_dragSelectMode && !isSelected)
		{
			_slots.Pick([.. Slots, new MokaTimeSlot(day, hour, minute, SlotDuration)], SelectedSlotsChanged);
		}
		else if (!_dragSelectMode && isSelected)
		{
			_slots.Pick([.. Slots.Where(s => s.Day != day || s.StartHour != hour || s.StartMinute != minute)],
				SelectedSlotsChanged);
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
