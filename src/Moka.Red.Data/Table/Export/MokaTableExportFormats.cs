namespace Moka.Red.Data.Table;

/// <summary>The files a <see cref="MokaTable{TItem}" /> export button writes. With more than one the button opens a menu.</summary>
[Flags]
public enum MokaTableExportFormats
{
	/// <summary>None.</summary>
	None = 0,

	/// <summary>A CSV file, in UTF-8 with a byte order mark, each column as it exports.</summary>
	Csv = 1,

	/// <summary>An Excel workbook (.xlsx), with numbers, dates and yes or no as their own cell types and the header frozen.</summary>
	Excel = 2
}

/// <summary>One file an export writes.</summary>
public enum MokaTableExportFormat
{
	/// <summary>A CSV file.</summary>
	Csv,

	/// <summary>An Excel workbook.</summary>
	Excel
}
