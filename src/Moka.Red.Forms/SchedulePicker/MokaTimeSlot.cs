namespace Moka.Red.Forms.SchedulePicker;

/// <summary>
///     Represents a selected time slot in the <see cref="MokaSchedulePicker" /> grid.
/// </summary>
/// <param name="Day">Day of the week for this slot.</param>
/// <param name="StartHour">Start hour in 24-hour format (0-23).</param>
/// <param name="StartMinute">Start minute (0-59).</param>
/// <param name="DurationMinutes">Duration of the slot in minutes.</param>
public sealed record MokaTimeSlot(
	DayOfWeek Day,
	int StartHour,
	int StartMinute,
	int DurationMinutes);
