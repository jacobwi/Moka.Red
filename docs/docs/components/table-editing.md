---
title: Table Editing
description: Editing MokaTable rows in place with editors that fit each column, checks before a save, cell and row modes, adding and deleting rows, undo, and pasting blocks of cells.
order: 164
---

# Table Editing

A `MokaTable` edits rows in place. Each column opts in with `Editable` and opens an editor that fits it: a text field, a number field, a date picker, a list, a check box, a switch or stars, all the library's own inputs. The table checks what is entered, and hands the row to `OnRowSave`, which saves it. It changes none of your objects: it writes into a copy and gives you the row as it was, the row with the changes, and the list of changes.

```blazor-preview
@code {
    record Part(int Id, string Name, int Stock, bool Active, string State);

    List<Part> _parts =
    [
        new(1, "Bolt M6", 120, true, "Ordered"),
        new(2, "Nut M6", 80, true, "Shipped"),
        new(3, "Washer M6", 45, false, "Ordered"),
    ];

    string _note = "Double-click a cell, or focus it and press Enter or F2.";

    void Save(MokaTableRowSave<Part> save)
    {
        _parts = _parts.Select(p => p.Id == save.Item.Id ? save.Updated : p).ToList();
        _note = $"Saved {save.Updated.Name}: {string.Join(", ", save.Changes.Select(c => $"{c.Column} {c.OldValue} to {c.NewValue}"))}";
    }
}

<MokaTable Items="_parts" ItemKey="p => p.Id" OnRowSave="Save">
    <MokaColumn Title="Name" Field="p => p.Name" Editable SetField="@((p, v) => p with { Name = (string?)v ?? "" })" />
    <MokaNumberColumn Title="Stock" Value="p => p.Stock" Editable SetValue="@((p, v) => p with { Stock = (int)(v ?? 0) })" />
    <MokaYesNoColumn Title="Active" Value="p => p.Active" Editable SetValue="@((p, v) => p with { Active = v ?? false })" />
    <MokaStatusColumn Title="State" Status="p => p.State" Editable SetStatus="@((p, v) => p with { State = v ?? "" })" />
</MokaTable>
<MokaCaption>@_note</MokaCaption>
```

## Editors

The editor comes from the column's `Editor`, `Auto` by default:

| Column | Editor |
|--------|--------|
| `MokaNumberColumn`, `MokaMoneyColumn`, `MokaBytesColumn`, a `Field` of a number | `MokaNumericField`. A column that can hold no value opens an empty box for one, and emptying the box saves null; a `Field` whose type can't be null refuses an empty box with a message. A row whose value is empty opens it too |
| `MokaDateColumn`, a `Field` of `DateTime`, `DateTimeOffset` or `DateOnly` | `MokaDatePicker`. The time of day and the offset the value had are kept |
| `MokaYesNoColumn` | `MokaCheckbox`, or `MokaSwitch` with `Look="MokaYesNoLook.Switch"`. A `Field` of `bool` takes the check box |
| `MokaStatusColumn`, a column with `EditOptions` or `FilterOptions`, a `Field` of an enum | `MokaNativeSelect` |
| `MokaRatingColumn` | `MokaRating`, up to the column's `Max` |
| Anything else | `MokaTextField`. Text that doesn't read as the column's type, "forty" in a number column, stays as typed with a message, and the row can't be saved until it reads |

A `Field` reads its values through `object`, which loses their declared type, so a `Field` column's type comes from its values: the row's own, else the first value among the table's rows. It takes an empty value when the row or another row leaves it empty, so an `int?` column opens a number box on an empty row and lets a value be cleared. A column that no row gives a value opens a text box, unless `Editor` names another; a column kind always knows its type.

Set `Editor` to choose another: `MokaColumnEditor.Text`, `Number`, `Date`, `Select`, `Checkbox`, `Switch` or `Rating`. A list offers `EditOptions` (`MokaFilterOption(Value, Text)`), else `FilterOptions`, else an enum's names, else the values the loaded rows have, up to 200. A choice's `Value` is written the way the column's value reads back: an enum's name, a number or a date in the invariant culture.

For an editor of your own, give the column an `EditTemplate`. It receives a `MokaCellEdit<TItem>` with the row as edited so far (`Item`), the `Value`, an `Error`, a `Label` for its `aria-label`, and `SetValue(object?)`, which takes a value of the column's type:

```razor
<MokaColumn Title="Color" Field="p => p.Color" Editable SetField="@((p, v) => p with { Color = (string)v! })">
    <EditTemplate Context="edit">
        <MokaColorInput Value="@((string?)edit.Value)" ValueChanged="@(c => edit.SetValue(c))" aria-label="@edit.Label" />
    </EditTemplate>
</MokaColumn>
```

The editors sit under an empty `EditContext`, so a table inside an `EditForm` doesn't add them to that form's fields.

## Writing the changes

The table writes each change into a copy of the row through the column's setter, and hands that copy to `OnRowSave` as `Updated`:

| Column | Setter |
|--------|--------|
| `MokaColumn` | `SetField`: `Func<TItem, object?, TItem>`, the value of the column's type, boxed |
| `MokaNumberColumn`, `MokaRatingColumn` | `SetValue`: `Func<TItem, double?, TItem>` |
| `MokaMoneyColumn` | `SetValue`: `Func<TItem, decimal?, TItem>` |
| `MokaBytesColumn` | `SetValue`: `Func<TItem, long?, TItem>` |
| `MokaDateColumn` | `SetValue`: `Func<TItem, DateTimeOffset?, TItem>` |
| `MokaYesNoColumn` | `SetValue`: `Func<TItem, bool?, TItem>` |
| `MokaStatusColumn` | `SetStatus`: `Func<TItem, string?, TItem>` |

A setter returns the row: `(p, v) => p with { Stock = (int)(v ?? 0) }` makes a new record, so your item never changes. For a class whose properties the setter sets in place, give the table `EditCopy`, which copies the row before an edit, and the setters write into the copy. A column without a setter edits all the same; its change is only in `Changes`.

`OnRowSave` receives a `MokaTableRowSave<TItem>`:

| Member | Description |
|--------|-------------|
| `Item` | The row as it was: your item, or for an added row the one `NewItem` made |
| `Updated` | The row with every change written through the setters |
| `Changes` | `MokaTableCellChange(Column, OldValue, NewValue)` for each value that changed, in the order the columns stand. Values are of the column's type |
| `Kind` | `Edit`, `Add`, `Paste` or `Undo` |
| `IsNew` | True for an added row |
| `NewValue<TValue>(column)` | One column's new value |
| `AddError(message)`, `AddError(column, message)` | Refuses the save. The row stays open with the message, under the column's editor or over the row |

An exception thrown by the handler does the same: its message shows over the row and the row stays open for another try. While the handler runs, the row's Save button and the editors are disabled.

After a save, rows in memory go through the search, filters and sort again, so a list you changed in place shows the change. A `ServerData` row shows `Updated` where it stands, with no request; an added row loads the page again.

## Checks

A column's `Validate` checks a value as it changes and again before a save. It receives the row as edited so far and the value, and returns what is wrong or null:

```razor
<MokaNumberColumn Title="Stock" Value="p => p.Stock" Editable
                  SetValue="@((p, v) => p with { Stock = (int)(v ?? 0) })"
                  Validate="@((p, v) => v is double n && n < 0 ? "Stock can't be below zero" : null)" />
```

For DataAnnotations or any validator component, put it in `Validators`. The table runs it against an `EditContext` of the copy before each save, so the attributes on your model check the edited values:

```razor
<MokaTable Items="_people" OnRowSave="SaveAsync" EditMode="MokaTableEditMode.Row">
    <Validators><DataAnnotationsValidator /></Validators>
    <ChildContent>
        <MokaColumn Title="Name" Field="p => p.Name" Editable SetField="@((p, v) => p with { Name = (string?)v ?? "" })" />
    </ChildContent>
</MokaTable>
```

A column finds its messages under its `Property`, the name of the property it edits, which defaults to its `Title`. A message under no column's property is about the whole row and shows over the row's editors in row mode. In cell mode only the open cell's messages count, so a value someone didn't touch doesn't stop a save. A record's setter makes a new copy, so the validators are drawn again for it before they are asked.

## Cell and row modes

`EditMode` says how cells open:

| Mode | What opens | Saves | Puts back |
|------|------------|-------|-----------|
| `Cell` (default) | The one cell | Enter, Tab to the next editable cell, Shift+Tab to the previous, or a click elsewhere | Escape |
| `Row` | Every editable cell of the row | Enter, or the row's Save button | Escape, or the row's Cancel button |

Enter, F2 or a double click on an editable cell opens it. What is typed on the cell after Enter or F2, before the editor is drawn, waits for the editor and reaches it as if typed there, Enter, Escape and Tab included, so a slow connection loses no keys. In row mode Tab moves between the row's editors as it does anywhere. Focus goes back to the cell when the editor closes, found by the row's key, so a save that moves the row under a sort, or adds a row at the bottom, takes focus with it; Tab goes on from where the saved row now stands. Opening another row saves the open one first; a save that fails keeps it open.

`MokaEditColumn` draws each row's buttons: Edit and Delete, and while the row is open Save and Cancel. Like the actions column, its buttons have no click handler or button component per row. `ShowDelete="false"` leaves Delete out, and `Actions` adds buttons of your own after them.

```razor
<MokaTable Items="_parts" EditMode="MokaTableEditMode.Row" OnRowSave="Save" OnRowDelete="Delete">
    <MokaColumn Title="Name" Field="p => p.Name" Editable SetField="@((p, v) => p with { Name = (string?)v ?? "" })" />
    <MokaEditColumn TItem="Part" />
</MokaTable>
```

`StartEditAsync(item, column)`, `SaveEditAsync()`, `CancelEdit()` and `IsEditing` do the same from code.

## Adding and deleting

With `NewItem` and `OnRowSave` the toolbar has an Add button. It opens a new row made by `NewItem`, with every editable cell, at `AddRowPosition` (`Top`, the default, or `Bottom`). Save hands it to `OnRowSave` with `Kind` `Add`. `AddRowAsync()` does the same from code.

`OnRowDelete` receives a row to delete, from the edit column's Delete button. With `ConfirmDelete` (on by default) the table asks first in a dialog of the app's `IMokaDialogService`, "Delete this row?" with the row's `ItemName`, and runs the delete with the dialog open, so a failure shows there. Without the dialog service registered the row is deleted at once. The rows load again afterwards.

## Undo

After a save, a line under the rows says what changed, with an Undo button, and Ctrl+Z on a cell does the same. Undo raises `OnRowSave` with `Kind` `Undo`, the current row as `Item`, and the old values in `Changes` and `Updated`. `ShowUndo="false"` hides the line; Ctrl+Z still works. `UndoAsync()` and `CanUndo` do it from code.

## Pasting cells

With `CellRanges` someone can mark a block of cells and paste a block copied from a spreadsheet with Ctrl+V. The block goes in from the marked block's first cell, or the focused cell. Each cell is read in its column's type, in the current culture or the invariant one, and a yes or no column reads its own Yes and No. Each row's values are checked like an edit and saved through `OnRowSave` with `Kind` `Paste`. A cell that isn't editable, doesn't read or fails its check is left as it was, and a line under the rows says how many cells were pasted and how many weren't. One pasted value fills the whole marked block. See [Table Export](/components/table-export#blocks-of-cells) for marking and copying.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `EditMode` | `MokaTableEditMode` | `Cell` | One cell at a time, or a whole row |
| `OnRowSave` | `EventCallback<MokaTableRowSave<TItem>>` | -- | Saves an edited, added or pasted row, or an undo |
| `OnRowDelete` | `EventCallback<TItem>` | -- | Deletes a row, from the edit column's Delete button |
| `ConfirmDelete` | `bool` | `true` | Asks first through `IMokaDialogService` |
| `NewItem` | `Func<TItem>?` | -- | Makes the row Add opens. With `OnRowSave` the toolbar has an Add button |
| `AddRowPosition` | `MokaTableAddPosition` | `Top` | Where a new row opens |
| `EditCopy` | `Func<TItem, TItem>?` | -- | Copies a row before it is edited, for setters that set properties in place |
| `Validators` | `RenderFragment?` | -- | Validator components run against an `EditContext` of the row before a save |
| `ShowUndo` | `bool` | `true` | Shows what the last save changed, with Undo |

On `MokaColumn` and every column kind:

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Editable` | `bool` | `false` | The cells can be edited |
| `Editor` | `MokaColumnEditor` | `Auto` | The editor the cells open with |
| `EditOptions` | `IReadOnlyList<MokaFilterOption>?` | -- | The choices a list editor offers |
| `EditTemplate` | `RenderFragment<MokaCellEdit<TItem>>?` | -- | Your own editor |
| `SetField` | `Func<TItem, object?, TItem>?` | -- | Writes a value into a row. Column kinds have a typed setter as well |
| `Validate` | `Func<TItem, object?, string?>?` | -- | Checks a value: what is wrong, or null |
| `Property` | `string?` | the `Title` | The property the validators file the column's messages under |

The words are in `MokaTableText`: `AddRow`, `EditRow`, `SaveRow`, `CancelEdit`, `DeleteRow`, `DeleteRowTitle`, `DeleteRowMessage`, `Delete`, `InvalidValue`, `Saved`, `Undo`, `DismissSaved`, `UndoFailed` and `Pasted`.
