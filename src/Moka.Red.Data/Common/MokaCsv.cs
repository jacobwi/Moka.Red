using System.Globalization;

namespace Moka.Red.Data.Common;

/// <summary>Writes cells for a CSV export, shared by the table and the charts so both guard spreadsheets the same way.</summary>
internal static class MokaCsv
{
	/// <summary>
	///     A cell as CSV: quoted when it holds a comma, a quote or a line break, and made text when a spreadsheet would run
	///     it as a formula.
	/// </summary>
	/// <param name="value">The cell's text.</param>
	/// <returns>The cell, ready to join with commas.</returns>
	public static string Escape(string value)
	{
		ArgumentNullException.ThrowIfNull(value);

		// A spreadsheet runs a cell that starts with one of these as a formula, so data such as
		// =HYPERLINK(...) would execute when the export is opened (CSV injection, per OWASP). The
		// leading quote makes it text. A number such as -5 holds no formula, so it stays a number.
		if (value.Length > 0 && value[0] is '=' or '+' or '-' or '@' or '\t' or '\r' && !IsNumber(value))
		{
			value = "'" + value;
		}

		if (value.Contains(',', StringComparison.Ordinal) || value.Contains('"', StringComparison.Ordinal) ||
		    value.Contains('\n', StringComparison.Ordinal) || value.Contains('\r', StringComparison.Ordinal))
		{
			return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
		}

		return value;
	}

	// The cell text went through the column's Format in the current culture, so that culture is
	// tried first. No surrounding white space: a leading tab is one of the formula triggers.
	private static bool IsNumber(string value)
	{
		const NumberStyles Styles = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint
		                            | NumberStyles.AllowThousands | NumberStyles.AllowExponent;

		return double.TryParse(value, Styles, CultureInfo.CurrentCulture, out _)
		       || double.TryParse(value, Styles, CultureInfo.InvariantCulture, out _);
	}
}
