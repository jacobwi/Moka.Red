---
title: Cascader
description: A drill-down selector over a tree of options, with the chosen path as a breadcrumb.
order: 90
---

# Cascader

`MokaCascader` drills down a tree of options: each choice opens the column of its children, and
the value is the path from the root to what was chosen - a province/city/district picker, a
category tree, a folder chooser. The trigger shows the path as a breadcrumb.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Options` | `IReadOnlyList<MokaCascaderOption>?` | -- | The tree of options to choose from |
| `Value` | `IReadOnlyList<string>` | `[]` | The chosen path, root to leaf. Two-way bindable |
| `ValueChanged` | `EventCallback<IReadOnlyList<string>>` | -- | Notified when a path is chosen or cleared |
| `IsOpen` | `bool` | `false` | Whether the panel is open. Two-way bindable |
| `IsOpenChanged` | `EventCallback<bool>` | -- | Notified when the panel opens or closes |
| `ChangeOnSelect` | `bool` | `false` | Choosing a node with children selects its path right away, instead of waiting for a leaf |
| `Placeholder` | `string?` | `"Select..."` | Text shown before anything is chosen |
| `Clearable` | `bool` | `false` | Whether the selection can be cleared |
| `Label` | `string?` | -- | Label text displayed above the field |
| `HelperText` | `string?` | -- | Helper text displayed below the field |
| `ErrorText` | `string?` | -- | Error text displayed below the field in error state |
| `Required` | `bool` | `false` | Whether the field is required |
| `Disabled` | `bool` | `false` | Whether the field is disabled |

## Basic

```blazor-preview
@code {
    IReadOnlyList<MokaCascaderOption> _options =
    [
        new()
        {
            Value = "earth", Label = "Earth",
            Children =
            [
                new()
                {
                    Value = "europe", Label = "Europe",
                    Children =
                    [
                        new MokaCascaderOption { Value = "czechia", Label = "Czechia" },
                        new MokaCascaderOption { Value = "germany", Label = "Germany" }
                    ]
                },
                new()
                {
                    Value = "asia", Label = "Asia",
                    Children = [new MokaCascaderOption { Value = "japan", Label = "Japan" }]
                }
            ]
        }
    ];

    IReadOnlyList<string> _value = [];
}

<MokaCascader @bind-Value="_value" Options="_options" Clearable="true" />
```

## Choosing early

By default only a leaf completes a path. `ChangeOnSelect` selects whatever was clicked, parent or
not, and closes:

```razor
<MokaCascader @bind-Value="_path" Options="_options" ChangeOnSelect="true" />
```

## Keyboard

The trigger holds focus while the panel is open. The arrows walk the columns - up and down inside
one, right into the highlighted option's children, left back out - Enter chooses what is
highlighted (or opens the panel when it is closed), and Escape and Tab close it. A value set from
code opens drilled down to it, with the chosen node expanded.

`Disabled` options are skipped by the keyboard and refuse a click. The field works inside an
`EditForm` like the other selects.

## Above the page

The open panel sits in the browser's top layer, so a card that hides its overflow no longer cuts it off and nothing later on the page paints over it, whatever its `z-index`. It stays against the field as the page scrolls and flips to the other side when the window has no room there. While it's open the library sets its position, so a stylesheet can restyle it but not move it.

## Looks

The panel follows the theme's popup look, like every other floating panel, and so do its options and the lines between its columns. An open branch keeps the accent under every look. [Popover](/components/popover#looks) lists the seven looks.

```razor
<MokaThemeProvider Theme="MokaTheme.Default.WithPopups(MokaPopupLook.AccentTint)">
    @Body
</MokaThemeProvider>
```

The looks are drawn only on the new design. On `MokaTheme.Dark` and `Light` the panel looks as it always has.
