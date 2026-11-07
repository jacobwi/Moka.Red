---
title: Native Select
description: A real select element, for a phone's own picker and for long lists, with the field's label, helper text and validation.
order: 120
---

# Native Select

`MokaNativeSelect<TValue>` draws a real `<select>`. A phone opens its own picker for it, the browser handles the keyboard and type-ahead, and a long list costs nothing to open. Use `MokaSelect` when you need search, groups, several choices or an option that shows more than text.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Value` | `TValue?` | -- | Selected value (two-way bindable) |
| `ValueChanged` | `EventCallback<TValue?>` | -- | Notified when the value changes |
| `Items` | `IReadOnlyList<TValue>` | empty | The options, in the order they are listed |
| `TextSelector` | `Func<TValue, string>?` | -- | The text of an item's option. Default: `ToString()` |
| `Placeholder` | `string?` | -- | Text of a first option that can't be chosen, shown while the value matches no item |
| `Label` | `string?` | -- | Field label |
| `HelperText` | `string?` | -- | Help text below the field |
| `ErrorText` | `string?` | -- | Error message (puts the field in error state) |
| `Required` | `bool` | `false` | Marks the field as required |
| `Optional` | `bool` | `false` | Shows a quiet "Optional" beside the label. `Required` wins |
| `Disabled` | `bool` | `false` | Disables the select |
| `Size` | `MokaSize` | `Md` | Field size |

`Id` and other attributes go on the `<select>`, so `name` or `aria-describedby` end up there. `Class` and `Style` go on the element around it. `Margin` goes on the field, `Padding` and `Rounded` on the select. The padding replaces the select's own, so leave room for the chevron at the right.

## Basic

```razor
@code {
    string? _zone = "Home";
    string[] _zones = ["Home", "Guests", "Lab"];
}

<MokaNativeSelect @bind-Value="_zone" Items="_zones" Label="Zone" />
```

## Any item type

The options carry their position in `Items` as their value, so the item can be any type. `TextSelector` writes the text.

```razor
@code {
    record Zone(int Id, string Name);

    Zone? _zone;
    Zone[] _zones = [new(1, "Home"), new(2, "Guests")];
}

<MokaNativeSelect @bind-Value="_zone" Items="_zones" TextSelector="z => z.Name" Label="Zone" />
```

## A null item

A null item is a real option, for a choice such as "use the default". Selecting it sets the value to null.

```razor
@code {
    string? _dns;
    string?[] _servers = [null, "1.1.1.1", "9.9.9.9"];
}

<MokaNativeSelect @bind-Value="_dns" Items="_servers" TextSelector='s => s ?? "Use the default"' Label="DNS server" />
```

## Placeholder

`Placeholder` adds a first option that can't be chosen, shown while the value matches no item. Without one, a value that matches no item shows an empty select, not a first option nobody picked.

```razor
<MokaNativeSelect @bind-Value="_zone" Items="_zones" Placeholder="Choose a zone" Label="Zone" />
```

## On a static page

A native select works on a page with no interactivity, which most of the library's lists can't. Bound to a field with `@bind-Value` in an `EditForm`, its options carry the items' own values as text, so the browser posts the chosen one and the server reads it back. That needs a type that reads back from text: strings, numbers, enums, dates and `Guid`s. Over any other type the options keep their positions and nothing is posted. See [Forms on static pages](/components/forms).

## Validation

It takes part in `EditForm` like the other inputs. A DataAnnotations message shows below the select, the select gets `aria-invalid`, and the element around it carries the `valid` and `invalid` classes.

```razor
<EditForm Model="_device" OnValidSubmit="Save">
    <DataAnnotationsValidator />
    <MokaNativeSelect @bind-Value="_device.Zone" Items="_zones" Label="Zone" Required Placeholder="Choose a zone" />
    <MokaButton Type="submit">Save</MokaButton>
</EditForm>
```
