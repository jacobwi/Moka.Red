using System.Globalization;
using Moka.Red.Forms.TimePicker;

namespace Moka.Red.Forms.Tests.Components;

public class MokaTimeParserTests
{
	private static TimeSpan At(string time) => TimeSpan.ParseExact(time, @"hh\:mm", CultureInfo.InvariantCulture);

	[Theory]
	[InlineData("21:30", "21:30")]
	[InlineData("9:30 pm", "21:30")]
	[InlineData("9.30p", "21:30")]
	[InlineData("21h30", "21:30")]
	[InlineData("2130", "21:30")]
	[InlineData("930pm", "21:30")]
	[InlineData("9 pm", "21:00")]
	[InlineData("21", "21:00")]
	[InlineData("9:30 PM", "21:30")]
	[InlineData("9:30 p.m.", "21:30")]
	[InlineData("  21:30  ", "21:30")]
	[InlineData("9:30 am", "09:30")]
	[InlineData("9 a.m.", "09:00")]
	[InlineData("930a", "09:30")]
	[InlineData("12 am", "00:00")]
	[InlineData("12:30a", "00:30")]
	[InlineData("12 pm", "12:00")]
	[InlineData("12:30 PM", "12:30")]
	[InlineData("0", "00:00")]
	[InlineData("00:05", "00:05")]
	[InlineData("0905", "09:05")]
	[InlineData("23:59", "23:59")]
	public void Parse_ReadsTheSameTime_UnderEitherSetting(string text, string expected)
	{
		Assert.Equal(At(expected), MokaTimeParser.Parse(text, format24: true));
		Assert.Equal(At(expected), MokaTimeParser.Parse(text, format24: false));
	}

	[Theory]
	[InlineData("25:00")]
	[InlineData("abc")]
	[InlineData("")]
	[InlineData("   ")]
	[InlineData("12:60")]
	[InlineData("24:00")]
	[InlineData("13pm")]
	[InlineData("0am")]
	[InlineData("9:5")]
	[InlineData("12345")]
	[InlineData("9:30:00")]
	[InlineData("21h")]
	[InlineData("9m")]
	[InlineData("pm")]
	[InlineData("a")]
	[InlineData(":30")]
	[InlineData("-1")]
	public void Parse_TurnsDownNonsense_UnderEitherSetting(string text)
	{
		Assert.Null(MokaTimeParser.Parse(text, format24: true));
		Assert.Null(MokaTimeParser.Parse(text, format24: false));
		Assert.Null(MokaTimeParser.Parse(text, format24: false, shownAfternoon: true));
	}

	[Fact]
	public void Parse_TurnsDownNull() => Assert.Null(MokaTimeParser.Parse(null, format24: true));

	[Theory]
	[InlineData("9", null, "09:00")]
	[InlineData("9", false, "09:00")]
	[InlineData("9", true, "21:00")]
	[InlineData("9:30", true, "21:30")]
	[InlineData("930", true, "21:30")]
	[InlineData("12", null, "12:00")]
	[InlineData("12", true, "12:00")]
	[InlineData("12", false, "00:00")]
	[InlineData("21", true, "21:00")]
	[InlineData("21", false, "21:00")]
	[InlineData("0", true, "00:00")]
	[InlineData("9 am", true, "09:00")]
	[InlineData("9 pm", false, "21:00")]
	public void Parse_OnA12HourField_ABareHourKeepsTheHalfItIsIn(string text, bool? shownAfternoon, string expected) =>
		Assert.Equal(At(expected), MokaTimeParser.Parse(text, format24: false, shownAfternoon));

	[Theory]
	[InlineData("9", true, "09:00")]
	[InlineData("9", false, "09:00")]
	[InlineData("12", false, "12:00")]
	[InlineData("930", true, "09:30")]
	public void Parse_OnA24HourField_ABareHourIsThatHour(string text, bool shownAfternoon, string expected) =>
		Assert.Equal(At(expected), MokaTimeParser.Parse(text, format24: true, shownAfternoon));
}
