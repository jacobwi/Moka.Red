---
title: Inline Edit
description: Text that becomes a field, with a pencil on hover. Enter saves, Escape cancels, clicking away saves.
order: 156
---

# Inline Edit

`MokaInlineEdit` shows text that turns into a field when it's clicked. Enter saves, Escape cancels and clicking away saves. A pencil appears on hover and keyboard focus, and stays on a touch screen.

An error from `Validate` or from a failed `Save` shows under the field and keeps it open. After Enter or Escape, focus goes back to the text. After a click away it stays where the click put it.

## Setup

```razor
@using Moka.Red.Forms.InlineEdit
```

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Value` / `@bind-Value` | `string` | - | The text |
| `Editing` / `@bind-Editing` | `bool` | `false` | Whether the field is open. Bind it to start editing from a key or a menu |
| `Trigger` | `MokaInlineEditTrigger` | `Click` | `Click`, `DoubleClick` or `Manual`. With `DoubleClick`, Enter and Space on the focused text start it too. With `Manual` the text is plain text and only `Editing` opens it |
| `Validate` | `Func<string, string?>?` | - | Returns an error message that blocks the save, or null |
| `Save` | `Func<string, Task>?` | - | Saves the text. While it runs the field stays open and read-only. An exception shows its message as the error and keeps the field open. Without it the text just goes to `ValueChanged` |
| `AllowBlank` | `bool` | `false` | Lets the text be saved blank |
| `SaveUnchanged` | `bool` | `false` | Saves text that didn't change, for a name offered as a suggestion |
| `Trim` | `bool` | `true` | Removes spaces from both ends before saving |
| `Selection` | `MokaInlineEditSelection` | `All` | What opens selected: `All`, `Stem` (a file name before its extension) or `End` |
| `Label` | `string` | `"Text"` | The field's accessible name |
| `ButtonLabel` | `string?` | - | The text's accessible name as a button. Null is "Edit" and the text |
| `BlankText` | `string` | `"This can't be blank"` | The message when blank text is blocked |
| `ErrorText` | `string?` | - | An error from outside |
| `Placeholder` | `string?` | - | Shown in the field when empty, and in place of the text when there's none |
| `Mono` | `bool` | `false` | Draws the text in the monospace font |
| `DisplayTemplate` | `RenderFragment<string>?` | - | Replaces how the text is drawn while it isn't being edited |
| `MaxLength`, `ReadOnly`, `Disabled`, `InputType` | | | As on the other text inputs. Read-only and disabled text can't be edited and isn't a button |
| `Size` | `MokaSize` | `Md` | `Md` follows the text around it, and the others use the type scale |
| `SizeValue` | `string?` | - | A font size of its own, such as `"18px"` |

`Class` and `Style` land on the root. `Margin` lands on the root, and `Padding` and `Rounded` on the text and the field. `Id` and other attributes go on the field.

## Basic

```razor
<MokaInlineEdit @bind-Value="_title" Label="Title" />
```

## Checking and saving

`Validate` stops the save before it starts. `Save` can fail afterwards, and what it throws is what the person reads.

```razor
<MokaInlineEdit @bind-Value="_name"
                Label="Project name"
                Validate="@(text => text.Length > 40 ? "Keep it under 40 characters" : null)"
                Save="SaveNameAsync" />

@code {
	private string _name = "Atlas";

	private async Task SaveNameAsync(string name)
	{
		await Projects.RenameAsync(name);
	}
}
```

## Double click

```razor
<MokaInlineEdit @bind-Value="_note" Label="Note" Trigger="MokaInlineEditTrigger.DoubleClick" />
```

## Started by the parent

With `Manual` the text is plain text, for a row that starts editing from F2 or a menu.

```razor
<MokaInlineEdit @bind-Value="_name" @bind-Editing="_renaming" Label="Name" Trigger="MokaInlineEditTrigger.Manual" />
<MokaButton OnClick="() => _renaming = true">Rename</MokaButton>
```

## File names

`Stem` selects the name before its extension, so typing doesn't remove `.pdf`.

```razor
<MokaInlineEdit @bind-Value="_file" Label="File name" Selection="MokaInlineEditSelection.Stem" Mono />
```

## In a form

Inside an `EditForm` it takes part like any other input, and the form's validation message shows under it.

## Keyboard

| Key | Action |
|-----|--------|
| Enter or Space on the text | Starts editing |
| Enter | Saves, except while an input method is composing |
| Escape | Cancels |
| Tab | Moves on, which saves |
