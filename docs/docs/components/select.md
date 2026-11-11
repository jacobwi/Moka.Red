---
title: Select
description: Dropdown select with single/multi selection, search, grouping, loading state, and chip templates.
order: 9
---

# Select

`MokaSelect<TValue>` is a full-featured dropdown component. It supports keyboard navigation, single and multiple selection, searchable filtering, option grouping, a "Select All" checkbox, custom chip rendering in multi-select mode, and a loading state for async option sources.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Value` | `TValue?` | -- | Selected value (two-way bindable) |
| `ValueChanged` | `EventCallback<TValue?>` | -- | Notified when value changes |
| `SelectedValues` | `IList<TValue>?` | -- | Multi-select values (two-way bindable) |
| `SelectedValuesChanged` | `EventCallback<IList<TValue>>` | -- | Notified when multi-selection changes |
| `Items` | `IEnumerable<TValue>` | **required** | Available options |
| `Label` | `string?` | -- | Field label |
| `HelperText` | `string?` | -- | Help text below the field |
| `ErrorText` | `string?` | -- | Error message (puts field in error state) |
| `Required` | `bool` | `false` | Marks the field as required |
| `Optional` | `bool` | `false` | Shows a quiet "Optional" beside the label. `Required` wins |
| `Disabled` | `bool` | `false` | Disables interaction |
| `Placeholder` | `string?` | -- | Placeholder text when nothing is selected |
| `ValueSelector` | `Func<TValue, string>?` | -- | Converts an item to display string. Default: `ToString()` |
| `Searchable` | `bool` | `false` | Shows a search input inside the dropdown |
| `Clearable` | `bool` | `false` | Shows an X to clear the selection |
| `Multiple` | `bool` | `false` | Enables multi-selection with chips |
| `SelectAll` | `bool` | `false` | "Select All" checkbox (requires `Multiple`) |
| `GroupBy` | `Func<TValue, string?>?` | -- | Groups options under labeled headers |
| `IsOptionDisabled` | `Func<TValue, bool>?` | -- | Per-option disabled predicate |
| `ChipTemplate` | `RenderFragment<TValue>?` | -- | Custom chip renderer for multi-select |
| `ItemTemplate` | `RenderFragment<TValue>?` | -- | Draws each option in the list, an icon beside the text for example. See [Icons and Custom Items](#icons-and-custom-items) |
| `ValueTemplate` | `RenderFragment<TValue>?` | -- | Draws the chosen item in the closed box of a single select, so an icon stays once picked. The placeholder is unaffected |
| `Loading` | `bool` | `false` | Shows loading indicator in dropdown |
| `NoResultsText` | `string` | `"No options found"` | Empty search results message |
| `IsOpen` | `bool` | `false` | Whether the list is open (two-way bindable). The list still opens and closes itself; a value from the parent takes effect when it differs from the last one passed |
| `IsOpenChanged` | `EventCallback<bool>` | -- | Notified when the list opens or closes |
| `Look` | `MokaListLook?` | `null` | How the list's rows are drawn. Null takes the theme's choice. See [Looks](#looks) |
| `MoveHintText` | `string` | `"move"` | The word after the arrow keys in the `Compact` look's key hints |
| `PickHintText` | `string` | `"pick"` | The word after Enter in the `Compact` look's key hints |
| `CloseHintText` | `string` | `"close"` | The word after Esc in the `Compact` look's key hints |
| `SearchText` | `Func<TValue, string>?` | -- | What search matches for each item, when that should be more than the shown text. See [Search that finds more](#search-that-finds-more) |
| `IgnoreAccents` | `bool` | `false` | Whether search sets accents and apostrophes aside as well as case, so "cote" finds "Côte d'Ivoire" |
| `SearchItems` | `Func<string, IEnumerable<TValue>>?` | -- | Runs the search itself and returns the results in order, best first. Wins over `SearchText` and `IgnoreAccents`, and its results show without `GroupBy`'s headings |
| `SearchTag` | `Func<TValue, string, string?>?` | -- | A short tag at the end of a row while searching, saying why it matched |
| `SearchPlaceholder` | `string` | `"Search..."` | The search box's placeholder |
| `SearchLabel` | `string` | `"Search options"` | The search box's accessible name |
| `PlaceholderTemplate` | `RenderFragment?` | -- | Draws the closed box while nothing is picked, such as an icon, in place of the placeholder's text |
| `TypeToJump` | `bool` | `false` | Lets a closed single select change its choice from the keyboard as a native select does. See [Type to jump](#type-to-jump) |
| `BackspaceRemovesLast` | `bool` | `false` | In a `Multiple` select, Backspace in the empty search box takes the last chip off |
| `Size` | `MokaSize` | `Md` | Field size |
| `Variant` | `MokaVariant` | `Outlined` | Field visual variant |

## Single Select

```blazor-preview
@code {
    string? _role;
    string[] _roles = ["Admin", "Editor", "Viewer", "Guest"];
}

<MokaSelect @bind-Value="_role" Label="Role" Items="_roles" Placeholder="Select a role" Clearable />
```

## Optional

`Optional` says the field can be left empty. The word sits inside the label, so the trigger's name reads it too.

```razor
<MokaSelect @bind-Value="_zone" Label="Zone" Items="_zones" Optional />
```

For a real `<select>` with the phone's own picker, see [`MokaNativeSelect`](native-select.md).

## With ValueSelector

When `TValue` is a complex object, use `ValueSelector` to control what text appears in the trigger.

```blazor-preview
@code {
    record Country(int Id, string Name, string Code);
    Country? _selected;
    Country[] _countries = [
        new(1, "Germany", "DE"),
        new(2, "France", "FR"),
        new(3, "Italy", "IT"),
    ];
}

<MokaSelect @bind-Value="_selected"
            Label="Country"
            Items="_countries"
            ValueSelector="c => c.Name"
            Clearable />
```

## Searchable

```blazor-preview
@code {
    string? _city;
    string[] _cities = ["Berlin", "Paris", "Rome", "Madrid", "Warsaw", "Vienna", "Lisbon"];
}

<MokaSelect @bind-Value="_city" Label="City" Items="_cities" Searchable Clearable />
```

## Search that finds more

Search matches the text `ValueSelector` shows, ignoring case. `SearchText` gives it more to match, such as a code or other names, and `IgnoreAccents` sets accents and apostrophes aside on both sides. The fold is the library's own table (`MokaTextFold` in Core), since an accent-blind `CompareInfo` needs ICU, which a WebAssembly app with invariant globalization leaves out.

```razor
<MokaSelect @bind-Value="_airport" Label="Airport" Items="_airports" Searchable IgnoreAccents
            ValueSelector="a => a.City" SearchText="@(a => $"{a.City} {a.Code}")" />
```

For a search that ranks its results, `SearchItems` takes what was typed and returns the items to show, best first, and `SearchTag` says on a row why it matched. [`MokaCountrySelect`](country-select.md) uses both, so "se" puts Sweden first with a "code SE" tag.

```razor
<MokaSelect @bind-Value="_airport" Items="_airports" Searchable ValueSelector="a => a.City"
            SearchItems="query => Rank(query)"
            SearchTag="@((a, query) => a.Code.Equals(query, StringComparison.OrdinalIgnoreCase) ? $"code {a.Code}" : null)" />
```

## Type to jump

With `TypeToJump`, a closed single select changes its choice from the keyboard as a native select does: typing picks the first item whose text starts with what was typed (accents set aside), the same letter again moves on to the next item starting with it, and a pause of a second starts over. Up and Down step to the next or previous item, Home and End to the first and last, and Alt with Down or Up opens the list. Enter and Space still open it, and Space inside a name being typed ("new z") is part of it. A hidden status says the new choice. It does nothing in a `Multiple` select.

```razor
<MokaSelect @bind-Value="_city" Label="City" Items="_cities" TypeToJump />
```

## Multiple Selection

```blazor-preview
@code {
    IList<string> _selected = new List<string>();
    string[] _skills = ["Blazor", "C#", "TypeScript", "CSS", "SQL", "Docker"];
}

<MokaSelect Multiple
            @bind-SelectedValues="_selected"
            Label="Skills"
            Items="_skills"
            Searchable
            SelectAll
            Placeholder="Choose skills" />
```

## Grouped Options

```blazor-preview
@code {
    record MenuItem(string Name, string Category);
    MenuItem? _item;
    MenuItem[] _menu = [
        new("Espresso", "Coffee"),
        new("Latte", "Coffee"),
        new("Cappuccino", "Coffee"),
        new("Green Tea", "Tea"),
        new("Earl Grey", "Tea"),
        new("Orange Juice", "Juice"),
    ];
}

<MokaSelect @bind-Value="_item"
            Label="Drink"
            Items="_menu"
            ValueSelector="x => x.Name"
            GroupBy="x => x.Category"
            Clearable />
```

## Disabled Options

```blazor-preview
@code {
    string? _plan;
    string[] _plans = ["Free", "Pro", "Enterprise", "Custom"];
    bool IsDisabled(string p) => p == "Custom";
}

<MokaSelect @bind-Value="_plan"
            Label="Plan"
            Items="_plans"
            IsOptionDisabled="IsDisabled" />
```

## Loading State

```blazor-preview
@code {
    string? _user;
    string[] _users = [];
    bool _loading = true;

    protected override async Task OnInitializedAsync()
    {
        await Task.Delay(1500);
        _users = ["Alice", "Bob", "Carol", "Dave"];
        _loading = false;
    }
}

<MokaSelect @bind-Value="_user" Label="Assignee" Items="_users" Loading="_loading" />
```

## Icons and Custom Items

`ItemTemplate` draws each option in the list, and `ValueTemplate` draws the chosen one in the closed box, so an icon you show in the list stays once it's picked. Without `ValueTemplate` the box shows the item's text from `ValueSelector`. Give the list and the box the same markup so they read alike.

```razor
<MokaSelect TValue="Device" Items="_devices" @bind-Value="_device" ValueSelector="d => d.Name" Label="Device">
    <ItemTemplate Context="device">
        <span style="display: inline-flex; align-items: center; gap: var(--moka-spacing-sm);">
            <MokaIcon Icon="device.Icon" SizeValue="16px"/>@device.Name
        </span>
    </ItemTemplate>
    <ValueTemplate Context="device">
        <span style="display: inline-flex; align-items: center; gap: var(--moka-spacing-sm);">
            <MokaIcon Icon="device.Icon" SizeValue="16px"/>@device.Name
        </span>
    </ValueTemplate>
</MokaSelect>
```

Keep `ValueSelector`: search and the chips of a multi-select read the text from it. Keep the text in the templates too, since an option is named by what it shows, and a lone icon would leave it nameless.

`MokaNativeSelect` can't do this. It is a real `<select>`, whose options hold text only in every browser but the newest Chrome, so use `MokaSelect` when items need icons.

## Custom Chip Template

Use `ChipTemplate` to control how selected items render as chips in multi-select mode.

```blazor-preview
@code {
    IList<string> _selected = new List<string>();
    string[] _tags = ["urgent", "bug", "enhancement", "wontfix", "duplicate"];
}

<MokaSelect Multiple
            @bind-SelectedValues="_selected"
            Label="Labels"
            Items="_tags">
    <ChipTemplate Context="tag">
        <MokaChip Color="MokaColor.Primary" Size="MokaSize.Sm">@tag</MokaChip>
    </ChipTemplate>
</MokaSelect>
```

## Error State

```blazor-preview
<MokaSelect Label="Department"
            Items='new[] { "Engineering", "Design", "Product" }'
            ErrorText="Please select a department." />
```

## A Null Option

A `null` in `Items` is a normal option, for "Any" or "None". Picking it shows its text instead of the placeholder, so give `ValueSelector` a name for it:

```razor
<MokaSelect TValue="string" @bind-Value="_owner" Label="Owner"
            Items="@(new string?[] { null, "Ana", "Ben" })"
            ValueSelector="@(o => o ?? "Anyone")" />
```

The placeholder shows only while the value is `null` and no option is `null`.

## Id, Class and Attributes

`Id` goes on the trigger, and the ids built from it follow: the label is `{Id}-label`, the option list `{Id}-listbox` and each option `{Id}-listbox-option-0` and on. Other attributes go on the trigger as well, so `aria-describedby` or a `data-` attribute lands on the control, and inside an `EditForm` the trigger gets `aria-invalid="true"` while the field fails validation. `Class` and `Style` go on the element around the trigger and the list.

```razor
<MokaSelect @bind-Value="_country" Id="country" Label="Country" Items="_countries"
            aria-describedby="country-note" />
<p id="country-note">Where the invoice goes.</p>
```

`Margin` goes on the field, around the label and the helper text. `Padding` and `Rounded` go on the trigger.

Up to 0.1.12 the select ignored `Id`, and every attribute except `aria-label`.

## Looks

`MokaListLook` decides how the rows of the open list are drawn. The box around them follows the theme's popup look (`WithPopups`), so a list matches the app's menus. `MokaAutoComplete` and `MokaTreeSelect` take the same looks.

| Look | Rows |
|------|------|
| `Standard` | Edge to edge. The chosen row is tinted, with a check at its end |
| `Check` | Inset with rounded corners. The chosen row keeps a plain background and gets a check, so a tint only ever means where you are |
| `Solid` | Inset, with the chosen row filled with the accent |
| `Attached` | While open, the field and the list are one shape inside one focus ring. The list is as wide as the field, and the field squares whichever corners meet it, below or above |
| `Radio` | Roomier rows that start with a radio circle, filled for the chosen row. A multiple select gets check boxes instead |
| `Compact` | Short rows, with key hints under the list: the arrows, Enter and Esc |
| `Terminal` | Monospace, `[x]` on the chosen row and `[ ]` on the rest, a glow on the lit row. With a `Standard` popup look the box goes terminal too |

Set one for the whole app on the theme:

```razor
<MokaThemeProvider Theme="@(MokaTheme.Red.WithLists(MokaListLook.Check))">
    @Body
</MokaThemeProvider>
```

Or on one select, which beats the theme:

```razor
<MokaSelect @bind-Value="_team" Label="Team" Items="_teams" Look="MokaListLook.Attached" />

<MokaSelect @bind-Value="_zone" Label="Zone" Items="_zones" Look="MokaListLook.Compact"
            MoveHintText="bewegen" PickHintText="wählen" CloseHintText="schließen" />
```

Looks belong to the new design. On `MokaTheme.Dark` and `MokaTheme.Light` the list stays as it is, whatever `Look` or the theme says. Every look keeps the keyboard as described below, and the marks and key hints are hidden from screen readers: the hints sit outside the listbox, and the options still report whether they are selected.

## Accessibility

The trigger has `role="combobox"` and takes its accessible name from `Label`. Without a visible label, pass `aria-label`. The placeholder is the last fallback. The open option list is a `role="listbox"` with the same name, and the trigger points at it with `aria-controls`.

```razor
<MokaSelect @bind-Value="_priority" Items="_priorities" aria-label="Priority" Placeholder="Any" />
```

Keyboard:

- Enter, Space or Down Arrow opens the list. The arrow keys move the highlight, Enter or Space picks the highlighted option, and Escape closes the list. None of these keys scroll the page. With `TypeToJump`, Down and Up on the closed field change the choice instead and Alt with Down opens the list.
- Escape stops at an open list, so a `MokaDialog` around the select stays open.
- Focus stays on the field while you move through the options, and screen readers announce the highlighted one (`aria-activedescendant`). Options report whether they are selected.
- With `Searchable`, focus moves into the search box when the list opens, so you can type straight away. Enter there picks the highlighted option without submitting a surrounding form, and Escape returns focus to the field.
- With `GroupBy`, the arrow keys follow the order on screen, and each group is announced by its name.
- A disabled select is out of the tab order and announced as unavailable. Its clear and chip buttons are disabled too.

The clear button ("Clear selection") and each chip's remove button ("Remove *item*") show only a cross, so they are
named by `aria-label` and by a `MokaTooltip` below them, which they keep while disabled. While the list is open they show
no tooltip, which would cover its first options. Up to 0.1.19 the clear button was named by a `title`.

## Above the page

The open option list sits in the browser's top layer, so a card that hides its overflow no longer cuts it off and nothing later on the page paints over it, whatever its `z-index`. It stays against the field as the page scrolls and flips to the other side when the window has no room there. While it's open the library sets its position, so a stylesheet can restyle it but not move it.
