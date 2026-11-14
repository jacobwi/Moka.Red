---
title: Table Export
description: Exporting MokaTable rows as CSV or an Excel workbook the table writes itself, copying rows and blocks of cells for a spreadsheet, and pasting them back.
order: 165
---

# Table Export

`Exportable` puts an export button in the toolbar. It writes every row that passes the search, the filters and the quick filter, in the sort order, not just the page on screen, with the visible columns in the order they stand. With `ServerData` the table asks for page 1 with a page size that covers the whole result set.

```razor
<MokaTable Items="_orders" Exportable ExportFormats="MokaTableExportFormats.Csv | MokaTableExportFormats.Excel"
           ExportFileName="orders">
    <MokaColumn Title="Number" Field="o => o.Number" />
    <MokaMoneyColumn Title="Total" Value="o => o.Total" Currency="EUR" />
    <MokaDateColumn Title="Placed" Value="o => o.Placed" />
</MokaTable>
```

## Formats

`ExportFormats` says what the button writes: `Csv` (the default), `Excel`, or both, which makes the button open a menu with "Export as CSV" and "Export as Excel". `ExportAsync(format)` exports from code.

| Format | File |
|--------|------|
| `Csv` | UTF-8 with a byte order mark, so Excel reads it as UTF-8. Each column as it exports: the cell's text for a plain column, and a column kind's own export text, such as numbers in the invariant culture and dates in ISO 8601. A cell that starts with `=`, `+`, `-`, `@`, a tab or a carriage return gets a leading `'`, so a spreadsheet shows it as text instead of running it (CSV injection); a plain number such as `-5` stays a number |
| `Excel` | An .xlsx workbook the table writes itself, with no package: one sheet named after the table's `Title` or `Label`, a bold heading row on a light fill frozen above the rows, each column as wide as its heading or its longest value among the first 200 rows, and typed cells. Numbers stay numbers, dates are dates (`yyyy-mm-dd hh:mm`, or `yyyy-mm-dd` without a time), yes or no is a boolean, and the rest is text. A text cell is never a formula, so nothing in it runs. A table with footer totals ends the sheet with a row of them, as numbers, over every exported row or as a server sent them |

What a cell holds in a workbook comes from the column's `ExportValueOf`: a plain column's `Field` value when it is a number, a date or a `bool`, else its export text. `MokaNumberColumn`, `MokaMoneyColumn`, `MokaBytesColumn`, `MokaRatingColumn` and `MokaYesNoColumn` write their value; `MokaDateColumn` writes the moment's clock time in its `TimeZone`, since a spreadsheet date has no zone. A column kind of your own overrides `ExportValueOf` and `ExportTextOf`.

The file is saved as `ExportFileName` with the format's extension, or `export-` and the date and time without one. It reaches the browser as a stream it reads into a blob, as the ASP.NET Core file download docs show, rather than one long string in a message.

## Your own export

With `OnExport` set the button writes nothing and hands you a `MokaTableExportContext<TItem>`:

| Property | Type | Description |
|----------|------|-------------|
| `Items` | `IReadOnlyList<TItem>` | Every row the search and filters keep, in the sort order |
| `Columns` | `IReadOnlyList<MokaColumn<TItem>>` | The visible columns |
| `IsCompleteSet` | `bool` | `false` when a `ServerData` source returned fewer rows than its `TotalItems` |
| `Format` | `MokaTableExportFormat` | The format picked |
| `FileName` | `string` | The name the table would save it under, extension included |

## Copying rows

With `Selectable` and `CopySelectedRows`, a Copy button copies the picked rows, and Ctrl+C on a cell copies them too. The button sits in the bulk bar when the table has `BulkActions`, else in the selection bar, which shows while rows are picked whether or not the table has `SelectionActions`. They go to the clipboard as tab-separated text with the headings, the rows on screen first, which pastes into a spreadsheet as cells. A cell holding a tab, a quote or a line break is quoted the way spreadsheets quote it, and a cell that would run as a formula gets a leading `'`, as in a CSV export.

The copy happens inside the click or the key press, through the library's in-click copy (`moka-copy.js`): the table works out the text while the clipboard write is already under way, so Safari and Firefox take it too.

## Blocks of cells

`CellRanges` lets people mark a block of cells: drag over them with the mouse, or hold Shift and press the arrows from a cell. The marked cells are tinted and say `aria-selected="true"`; a plain arrow key or Escape lets go of the block. Ctrl+C copies the block as tab-separated text without headings, and Ctrl+C on a single cell copies that cell. Ctrl+V pastes a block from a spreadsheet into the editable cells, through the editing path: see [Pasting cells](/components/table-editing#pasting-cells).

The block lives in the browser. One pointer listener on the page serves every table that marks blocks, the cells are marked there, and .NET hears nothing until a copy or a paste, so marking a block renders nothing. Rows a virtualized table draws later are marked as they appear.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Exportable` | `bool` | `false` | The toolbar's export button |
| `ExportFormats` | `MokaTableExportFormats` | `Csv` | The files the button offers. With both, it opens a menu |
| `ExportFileName` | `string?` | -- | The file's name, without its extension |
| `OnExport` | `EventCallback<MokaTableExportContext<TItem>>` | -- | Your own export in place of the file |
| `CopySelectedRows` | `bool` | `false` | A Copy button for the picked rows, and Ctrl+C |
| `CellRanges` | `bool` | `false` | Marking, copying and pasting blocks of cells |

The words are in `MokaTableText`: `Export`, `ExportCsv`, `ExportExcel`, `SheetName`, `CopyRows`, `CopiedRows`, `CopyFailed` and `Pasted`.
