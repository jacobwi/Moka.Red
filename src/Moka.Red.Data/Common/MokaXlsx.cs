using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace Moka.Red.Data.Common;

/// <summary>
///     Writes a one-sheet Excel workbook (SpreadsheetML in a zip, ECMA-376) with no package: typed cells (numbers, dates,
///     yes or no, text), a bold header row on a light fill, column widths and the header frozen above the rows.
/// </summary>
internal static class MokaXlsx
{
	private const string Main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
	private const string Relations = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
	private const string PackageRelations = "http://schemas.openxmlformats.org/package/2006/relationships";

	// The cell formats styles.xml defines, by their place in cellXfs.
	private const string HeaderStyle = "1";
	private const string DateTimeStyle = "2";
	private const string DateStyle = "3";

	private const double MinWidth = 6;
	private const double MaxWidth = 60;

	// Widths are measured on the first rows only, which says enough about a column without reading a million cells.
	private const int MeasuredRows = 200;

	private static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false);

	/// <summary>Writes the workbook into a stream, positioned at its start.</summary>
	/// <param name="sheetName">The sheet's name. Characters a sheet name can't hold are dropped, and it is cut to 31.</param>
	/// <param name="headers">The column headings.</param>
	/// <param name="rows">The rows, each a cell per heading: a number, a <see cref="DateTime" />, a <see cref="DateOnly" />, a <see cref="bool" />, text, or null for an empty cell.</param>
	/// <returns>The workbook.</returns>
	public static MemoryStream Write(string sheetName, IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<object?>> rows)
	{
		List<IReadOnlyList<object?>> all = rows as List<IReadOnlyList<object?>> ?? rows.ToList();
		MemoryStream stream = new();
		using (ZipArchive zip = new(stream, ZipArchiveMode.Create, leaveOpen: true))
		{
			WriteText(zip, "[Content_Types].xml", ContentTypes);
			WriteText(zip, "_rels/.rels", RootRelations);
			WriteText(zip, "xl/workbook.xml", Workbook(SheetName(sheetName)));
			WriteText(zip, "xl/_rels/workbook.xml.rels", WorkbookRelations);
			WriteText(zip, "xl/styles.xml", Styles);
			ZipArchiveEntry sheet = zip.CreateEntry("xl/worksheets/sheet1.xml", CompressionLevel.Fastest);
			using Stream entry = sheet.Open();
			WriteSheet(entry, headers, all);
		}

		stream.Position = 0;
		return stream;
	}

	/// <summary>The letters of a column, from 0: A, B, …, Z, AA.</summary>
	/// <param name="index">The column's position.</param>
	/// <returns>Its letters.</returns>
	public static string ColumnName(int index)
	{
		StringBuilder name = new();
		for (int n = index + 1; n > 0; n = (n - 1) / 26)
		{
			name.Insert(0, (char)('A' + (n - 1) % 26));
		}

		return name.ToString();
	}

	private static void WriteSheet(Stream stream, IReadOnlyList<string> headers, List<IReadOnlyList<object?>> rows)
	{
		using XmlWriter xml = XmlWriter.Create(stream, new XmlWriterSettings { Encoding = Utf8, CheckCharacters = false });
		xml.WriteStartDocument(standalone: true);
		xml.WriteStartElement("worksheet", Main);
		xml.WriteAttributeString("xmlns", "r", null, Relations);

		int lastColumn = Math.Max(headers.Count - 1, 0);
		xml.WriteStartElement("dimension", Main);
		xml.WriteAttributeString("ref", $"A1:{ColumnName(lastColumn)}{rows.Count + 1}");
		xml.WriteEndElement();

		xml.WriteStartElement("sheetViews", Main);
		xml.WriteStartElement("sheetView", Main);
		xml.WriteAttributeString("workbookViewId", "0");
		xml.WriteStartElement("pane", Main);
		xml.WriteAttributeString("ySplit", "1");
		xml.WriteAttributeString("topLeftCell", "A2");
		xml.WriteAttributeString("activePane", "bottomLeft");
		xml.WriteAttributeString("state", "frozen");
		xml.WriteEndElement();
		xml.WriteStartElement("selection", Main);
		xml.WriteAttributeString("pane", "bottomLeft");
		xml.WriteAttributeString("activeCell", "A2");
		xml.WriteAttributeString("sqref", "A2");
		xml.WriteEndElement();
		xml.WriteEndElement();
		xml.WriteEndElement();

		if (headers.Count > 0)
		{
			xml.WriteStartElement("cols", Main);
			for (int i = 0; i < headers.Count; i++)
			{
				string column = (i + 1).ToString(CultureInfo.InvariantCulture);
				xml.WriteStartElement("col", Main);
				xml.WriteAttributeString("min", column);
				xml.WriteAttributeString("max", column);
				xml.WriteAttributeString("width", WidthOf(i, headers[i], rows).ToString("0.##", CultureInfo.InvariantCulture));
				xml.WriteAttributeString("customWidth", "1");
				xml.WriteEndElement();
			}

			xml.WriteEndElement();
		}

		xml.WriteStartElement("sheetData", Main);
		xml.WriteStartElement("row", Main);
		xml.WriteAttributeString("r", "1");
		for (int i = 0; i < headers.Count; i++)
		{
			WriteCell(xml, i, 1, headers[i], HeaderStyle);
		}

		xml.WriteEndElement();
		for (int r = 0; r < rows.Count; r++)
		{
			int number = r + 2;
			xml.WriteStartElement("row", Main);
			xml.WriteAttributeString("r", number.ToString(CultureInfo.InvariantCulture));
			IReadOnlyList<object?> row = rows[r];
			for (int i = 0; i < row.Count; i++)
			{
				WriteCell(xml, i, number, row[i], null);
			}

			xml.WriteEndElement();
		}

		xml.WriteEndElement();
		xml.WriteEndElement();
		xml.WriteEndDocument();
	}

	private static void WriteCell(XmlWriter xml, int column, int row, object? value, string? style)
	{
		if (value is null || value is double d && (double.IsNaN(d) || double.IsInfinity(d)))
		{
			return;
		}

		xml.WriteStartElement("c", Main);
		xml.WriteAttributeString("r", ColumnName(column) + row.ToString(CultureInfo.InvariantCulture));
		switch (value)
		{
			case bool yes:
				xml.WriteAttributeString("t", "b");
				xml.WriteElementString("v", Main, yes ? "1" : "0");
				break;
			case DateTime moment:
				xml.WriteAttributeString("s", moment.TimeOfDay == TimeSpan.Zero ? DateStyle : DateTimeStyle);
				xml.WriteElementString("v", Main, moment.ToOADate().ToString("R", CultureInfo.InvariantCulture));
				break;
			case DateTimeOffset moment:
				xml.WriteAttributeString("s", DateTimeStyle);
				xml.WriteElementString("v", Main, moment.DateTime.ToOADate().ToString("R", CultureInfo.InvariantCulture));
				break;
			case DateOnly day:
				xml.WriteAttributeString("s", DateStyle);
				xml.WriteElementString("v", Main, day.ToDateTime(TimeOnly.MinValue).ToOADate().ToString("R", CultureInfo.InvariantCulture));
				break;
			case double or float or decimal or int or long or short or byte or uint or ulong or ushort or sbyte:
				if (style is not null)
				{
					xml.WriteAttributeString("s", style);
				}

				xml.WriteElementString("v", Main, Convert.ToString(value, CultureInfo.InvariantCulture));
				break;
			default:
				if (style is not null)
				{
					xml.WriteAttributeString("s", style);
				}

				xml.WriteAttributeString("t", "inlineStr");
				xml.WriteStartElement("is", Main);
				xml.WriteStartElement("t", Main);
				xml.WriteAttributeString("xml", "space", null, "preserve");
				xml.WriteString(XmlSafe(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty));
				xml.WriteEndElement();
				xml.WriteEndElement();
				break;
		}

		xml.WriteEndElement();
	}

	// A column as wide as its heading or its longest value among the first rows, in characters, within sensible bounds.
	private static double WidthOf(int column, string header, List<IReadOnlyList<object?>> rows)
	{
		int widest = header.Length;
		int measured = Math.Min(rows.Count, MeasuredRows);
		for (int r = 0; r < measured; r++)
		{
			if (column < rows[r].Count && rows[r][column] is { } value)
			{
				int length = value switch
				{
					DateTime or DateTimeOffset => 16,
					DateOnly => 10,
					bool => 5,
					_ => (Convert.ToString(value, CultureInfo.CurrentCulture) ?? string.Empty).Length
				};
				widest = Math.Max(widest, length);
			}
		}

		return Math.Clamp(widest + 2, MinWidth, MaxWidth);
	}

	// XML 1.0 can't hold most control characters, and Excel refuses a sheet with one.
	private static string XmlSafe(string text)
	{
		if (!text.Any(static c => c < ' ' && c is not '\t' and not '\n' and not '\r'))
		{
			return text;
		}

		return string.Concat(text.Where(static c => c >= ' ' || c is '\t' or '\n' or '\r'));
	}

	// Excel refuses a sheet name with any of these, or longer than 31 characters.
	private static string SheetName(string name)
	{
		string clean = string.Concat(name.Where(static c => c is not ('[' or ']' or ':' or '*' or '?' or '/' or '\\'))).Trim('\'').Trim();
		return clean.Length == 0 ? "Sheet1" : clean.Length > 31 ? clean[..31] : clean;
	}

	private static void WriteText(ZipArchive zip, string path, string text)
	{
		ZipArchiveEntry entry = zip.CreateEntry(path, CompressionLevel.Fastest);
		using Stream stream = entry.Open();
		byte[] bytes = Utf8.GetBytes(text);
		stream.Write(bytes, 0, bytes.Length);
	}

	private static string Workbook(string sheetName)
	{
		StringBuilder text = new();
		using (XmlWriter xml = XmlWriter.Create(text, new XmlWriterSettings { OmitXmlDeclaration = true }))
		{
			xml.WriteStartElement("workbook", Main);
			xml.WriteAttributeString("xmlns", "r", null, Relations);
			xml.WriteStartElement("sheets", Main);
			xml.WriteStartElement("sheet", Main);
			xml.WriteAttributeString("name", sheetName);
			xml.WriteAttributeString("sheetId", "1");
			xml.WriteAttributeString("id", Relations, "rId1");
			xml.WriteEndElement();
			xml.WriteEndElement();
			xml.WriteEndElement();
		}

		return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" + text;
	}

	private const string ContentTypes =
		"<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
		"<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
		"<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
		"<Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
		"<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>" +
		"<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>" +
		"<Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/>" +
		"</Types>";

	private const string RootRelations =
		"<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
		"<Relationships xmlns=\"" + PackageRelations + "\">" +
		"<Relationship Id=\"rId1\" Type=\"" + Relations + "/officeDocument\" Target=\"xl/workbook.xml\"/>" +
		"</Relationships>";

	private const string WorkbookRelations =
		"<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
		"<Relationships xmlns=\"" + PackageRelations + "\">" +
		"<Relationship Id=\"rId1\" Type=\"" + Relations + "/worksheet\" Target=\"worksheets/sheet1.xml\"/>" +
		"<Relationship Id=\"rId2\" Type=\"" + Relations + "/styles\" Target=\"styles.xml\"/>" +
		"</Relationships>";

	// Cell formats: 0 plain, 1 the header (bold, light fill, a line under it), 2 a date and time, 3 a date.
	private const string Styles =
		"<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
		"<styleSheet xmlns=\"" + Main + "\">" +
		"<numFmts count=\"2\"><numFmt numFmtId=\"164\" formatCode=\"yyyy-mm-dd hh:mm\"/><numFmt numFmtId=\"165\" formatCode=\"yyyy-mm-dd\"/></numFmts>" +
		"<fonts count=\"2\"><font><sz val=\"11\"/><name val=\"Calibri\"/><family val=\"2\"/></font>" +
		"<font><b/><sz val=\"11\"/><name val=\"Calibri\"/><family val=\"2\"/></font></fonts>" +
		"<fills count=\"3\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill>" +
		"<fill><patternFill patternType=\"solid\"><fgColor rgb=\"FFF2F2F2\"/><bgColor indexed=\"64\"/></patternFill></fill></fills>" +
		"<borders count=\"2\"><border><left/><right/><top/><bottom/><diagonal/></border>" +
		"<border><left/><right/><top/><bottom style=\"thin\"><color rgb=\"FFBFBFBF\"/></bottom><diagonal/></border></borders>" +
		"<cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs>" +
		"<cellXfs count=\"4\">" +
		"<xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/>" +
		"<xf numFmtId=\"0\" fontId=\"1\" fillId=\"2\" borderId=\"1\" xfId=\"0\" applyFont=\"1\" applyFill=\"1\" applyBorder=\"1\"/>" +
		"<xf numFmtId=\"164\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyNumberFormat=\"1\"/>" +
		"<xf numFmtId=\"165\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyNumberFormat=\"1\"/>" +
		"</cellXfs>" +
		"<cellStyles count=\"1\"><cellStyle name=\"Normal\" xfId=\"0\" builtinId=\"0\"/></cellStyles>" +
		"</styleSheet>";
}
