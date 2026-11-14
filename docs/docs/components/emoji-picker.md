---
title: Emoji Picker
description: An emoji panel with search, group tabs, skin tones and a frequently used group, as a form field, a field's end button, a chat composer or a colon typeahead.
order: 151
---

# Emoji Picker

`MokaEmojiPanel` picks an emoji from every one Unicode lists: a search box, group tabs and a grid, with skin tones, a frequently used group the app keeps, a preview strip and the arrow keys across the grid. The same panel shows up in several places:

| Where | Component |
|---|---|
| A form field whose value is one emoji | `MokaEmojiPicker` |
| A smile at the end of a text field or text area | `MokaTextField.EmojiButton`, `MokaTextArea.EmojiButton` |
| A chat's input row, as a popup or docked | `MokaChat.ShowEmoji` |
| Reactions under a message | [`MokaReactions`](/components/reactions) |
| Typing `:ta` in a text field | `MokaEmojiTypeahead` |
| Anywhere else | `MokaEmojiPanel` itself |

## Setup

```razor
@using Moka.Red.Primitives.Emoji
@using Moka.Red.Forms.EmojiPicker
@using Moka.Red.Feedback.EmojiTypeahead
```

Nothing to register. The emoji data is a static file in `Moka.Red.Primitives` (`_content/Moka.Red.Primitives/moka-emoji.en.json`, 1,923 emoji from Unicode Emoji 18.0, 180 KB, 42 KB with Brotli), fetched the first time a panel opens and never before, so it adds nothing to a WebAssembly download until then.

## A form field

`MokaEmojiPicker` is a button showing the chosen emoji, or a smile with a plus while empty. Bind it like any field: labels, `Required`, validation messages and a static page's post work as they do on `MokaTextField`.

```razor
<MokaEmojiPicker @bind-Value="_status" Label="Status emoji" Clearable />

<EditForm Model="_team">
    <DataAnnotationsValidator />
    <MokaEmojiPicker @bind-Value="_team.Icon" Label="Team icon" Required Size="MokaSize.Lg" />
</EditForm>
```

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Value` | `string?` | -- | The emoji, with its skin tone. Two-way bindable |
| `Label`, `HelperText`, `ErrorText`, `Required`, `Optional` | | | As on every field |
| `Actions` | `RenderFragment?` | - | Content beside the box, in its row, such as a button. See [A button beside the box](/components/forms#a-button-beside-the-box) |
| `Clearable` | `bool` | `false` | A cross beside the button clears the choice |
| `ClearLabel` | `string` | `"Clear emoji"` | The clear button's name and tooltip |
| `NoneText` | `string` | `"None"` | What the button says to a screen reader while empty |
| `Options` | `MokaEmojiOptions?` | default | The panel's options, its shape among them |
| `OptionsChanged` | `EventCallback<MokaEmojiOptions>` | -- | The new skin tone and frequently used list, for the app to store |
| `Placement` | `MokaTooltipPosition` | `BottomStart` | Where the popup opens against the button |
| `Size` | `MokaSize` | `Md` | The button: `Sm` 32 px, `Md` 40 px, `Lg` 44 px |

The button is named by the label and the emoji's name, such as "Status emoji, airplane". The panel is a dialog in the browser's top layer, so a card or dialog that hides its overflow can't cut it off. Escape or a click outside closes it, and a pick closes it and puts focus back on the button.

## The panel's options

Every component that holds a panel takes the same `MokaEmojiOptions` record, so a choice made once applies everywhere. It also carries the two things a panel changes, the skin tone and the frequently used list: the component hands back a new record through its `OptionsChanged` callback, and the app stores it however it likes (a cookie, local storage, the user's profile).

```razor
<MokaEmojiPicker @bind-Value="_icon" @bind-Options="_emoji" />
<MokaTextField @bind-Value="_title" EmojiButton @bind-EmojiOptions="_emoji" />

@code {
    private MokaEmojiOptions _emoji = new() { Navigation = MokaEmojiNavigation.Rail, ShowKeyHints = true };
}
```

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `Shape` | `MokaEmojiShape` | `Popover` | How the panel is laid out and shown (below) |
| `Navigation` | `MokaEmojiNavigation` | `Tabs` | How people move between groups (below) |
| `ShowSearch` | `bool` | `true` | A search box over the grid |
| `SearchKeywords` | `bool` | `true` | Search also matches keywords and shortcodes. Off matches names only |
| `SkinTones` | `bool` | `true` | A hand beside the search box picks the skin tone |
| `SkinTone` | `MokaSkinTone` | `Default` | The tone applied to every emoji that takes one, ZWJ sequences included |
| `ShowFrequent` | `bool` | `true` | A first group with the emoji picked most |
| `Recent` | `IReadOnlyList<MokaEmojiUse>?` | -- | What was picked, how often and when. The panel adds every pick and hands the list back |
| `FrequentCount` | `int` | `16` | How many the Frequently used group shows |
| `ShowPreview` | `bool` | `true` | The lit emoji large at the bottom with its name and `:shortcode:` |
| `ShowKeyHints` | `bool` | `false` | A row saying what the keys do. The keys work either way |
| `Quick` | `IReadOnlyList<string>?` | 8 everyday ones | The quick picks of the `Strip` shape |
| `MaxVersion` | `double?` | -- | The newest emoji version to show. Null finds out what the system draws (below) |
| `Columns` | `int?` | -- | Columns in the grid. Null fits the width |
| `DataSource` | `string?` | -- | An app's own data file in the same format, for another language |
| `Text` | `MokaEmojiText?` | English | Every word the panel shows or says |

### Shapes

| `MokaEmojiShape` | What it is |
|---|---|
| `Popover` | A grid with search and group tabs, in a popup from a button. The default |
| `Strip` | Eight quick picks in a row with More, which opens the full grid in place under them. Suits a status or a mood |
| `Composer` | Docked as wide as its host, the groups along the bottom like a phone keyboard. It stays open for several picks; its button or Escape closes it |
| `List` | A list like the command palette: each row the emoji, its name and its shortcode, most used first |

A field's emoji button and `MokaReactions` always open a popup: `List` opens the list and the others the grid. `MokaChat` docks a `Composer` and opens a popup for the rest.

### Groups

| `MokaEmojiNavigation` | What it is |
|---|---|
| `Tabs` | A row of icons over the grid, one per group, plus Frequently used. Each is named in a tooltip. Click one and the grid jumps there; scroll and the lit tab follows |
| `Rail` | The same icons down the left edge |
| `Headings` | One long scroll with sticky group names and no tabs |
| `SearchOnly` | No groups at all: the most used emoji (or a popular few) until something is typed, then the matches |

### Frequently used

The panel counts every pick by the emoji without its skin tone and ranks by count, then by how recent. It keeps its own list while it lives; to keep it across visits, store what `OptionsChanged` hands back:

```razor
<MokaEmojiPanel Options="_emoji" OptionsChanged="Store" OnPick="Use" />

@code {
    private MokaEmojiOptions _emoji = new();

    private async Task Store(MokaEmojiOptions changed)
    {
        _emoji = changed;
        await Profile.SaveEmojiAsync(changed.Recent, changed.SkinTone);
    }
}
```

`MokaEmojiUse` is `(string Emoji, int Count, DateTimeOffset LastUsed)`. The time comes from a registered `TimeProvider`, else the system clock.

### Search

Search runs in the browser as you type. With `SearchKeywords` on, whole matches rank first (a shortcode, then a name, then a keyword), then what starts with the text, so `lol` finds 😂, `+1` finds 👍 and `:tada` finds 🎉. Names and keywords come from Unicode CLDR, shortcodes from GitHub's gemoji. The match count is announced to screen readers.

## The panel on its own

`MokaEmojiPanel` is inline unless it gets an `Anchor`, which makes it a popup against that element.

```razor
<MokaEmojiPanel Options="_emoji" OptionsChanged="Store" OnPick="pick => _picked = pick" />

<button @ref="_button" @onclick="() => _open = !_open">Emoji</button>
<MokaEmojiPanel Anchor="_button" @bind-Open="_open" OnPick="Insert" />
```

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Options` | `MokaEmojiOptions?` | default | As above |
| `OptionsChanged` | `EventCallback<MokaEmojiOptions>` | -- | The new skin tone and frequently used list |
| `OnPick` | `EventCallback<MokaEmojiPick>` | -- | Once per pick: `Emoji` (with its tone), `Name`, `Shortcode`, `Base` (without a tone) |
| `Selected` | `string?` | -- | The chosen emoji, marked in the grid and pressed in the strip |
| `Anchor` | `ElementReference?` | -- | Makes the panel a popup in the top layer against this element |
| `Open` | `bool` | `true` | Whether it shows. Two-way bindable |
| `Placement` | `MokaTooltipPosition` | `BottomStart` | Where a popup opens, flipping when there's no room |
| `CloseOnPick` | `bool` | `true` | Whether a pick closes a popup, or the grid a strip opened |
| `FocusOnClose` | `ElementReference?` | `Anchor` | Where focus goes when it closes |
| `FocusAfterPick` | `bool` | `true` | Off when `OnPick` moves focus itself |
| `AutoFocus` | `bool` | `false` | An inline panel focuses its search box when it appears. A popup always does |
| `Height` | `string?` | -- | The panel's height, such as `"24rem"` |
| `AriaLabel` | `string?` | `"Choose an emoji"` | The panel's name |

## Keyboard

One tab stop for the grid. From the search box, Down steps into the grid.

- The arrows move in both directions, Page Up and Page Down jump a group, Home and End go to the ends.
- Enter (or Space in the grid) picks the lit emoji.
- Escape clears the search first, then closes a popup and puts focus back where it came from.
- In the tabs, the arrows walk the groups.
- In the tone list, the arrows walk the six tones and Escape closes only the list.

The grid is a `role="grid"` (a `listbox` in the `List` shape) with rows, labelled row groups and cells named by the emoji's CLDR name, and the search box is a combobox pointing at the lit cell through `aria-activedescendant`.

## Text fields and text areas

`EmojiButton` puts a smile at the end of `MokaTextField` or in the corner of `MokaTextArea`. The pick goes in at the caret, over any selection, and focus goes back to the field; the field hears it as typing, so `@bind-Value` and validation follow.

```razor
<MokaTextField @bind-Value="_title" Label="Title" EmojiButton />
<MokaTextArea @bind-Value="_notes" Label="Release notes" EmojiButton EmojiButtonLabel="Add emoji" />
```

## The colon typeahead

Wrap a field in `MokaEmojiTypeahead`. Type a colon and two letters and a `MokaSlashMenu` opens under it with the matches, each row the emoji in a tile, its `:shortcode:` and its name. Up and Down move, Enter or Tab puts the emoji in with a space after it, Escape closes the list and leaves the text. A whole code such as `:tada:` turns into 🎉 as its closing colon is typed.

```razor
<MokaEmojiTypeahead>
    <MokaTextArea @bind-Value="_comment" Label="Comment" />
</MokaEmojiTypeahead>

<textarea @ref="_plain"></textarea>
<MokaEmojiTypeahead For="_plainRef" />
```

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | -- | The field, or anything holding it. The first textarea or text input inside is watched |
| `For` | `ElementReference?` | -- | A field outside the wrapper. The list then opens against it |
| `Options` | `MokaEmojiOptions?` | default | The data file, newest version, skin tone and keyword search |
| `MaxResults` | `int` | `8` | How many matches the list shows |
| `MinLength` | `int` | `2` | Letters after the colon before it opens, so "12:30" never does |
| `ConvertShortcodes` | `bool` | `true` | Whether `:tada:` turns into 🎉 |
| `Header` | `string?` | `"Emoji"` | The list's header |
| `EmptyText`, `EmptyHint` | `string?` | | What the list says when nothing matches |
| `Placement` | `MokaPopoverPosition` | `BottomStart` | Where the list opens |
| `Disabled` | `bool` | `false` | Turns it off |

The field keeps focus throughout. A colon only opens the list after a space, a bracket or a quote, or at the start.

## Emoji the system can't draw

Each emoji carries the version it arrived in. The first time a panel opens it draws one emoji of each newer version on a canvas; a version that comes out as a box, or as two pictures where one should be, is hidden, with everything after it. Windows 11 keeps its emoji font close to current; Windows 10 stopped around Emoji 12. `MaxVersion` skips the check and shows up to the version you name. A browser that blurs canvas reads against fingerprinting makes the check show everything rather than hide by mistake.

## Flags on Windows

Windows has no flag emoji. The panel draws in MokaFlag's Twemoji Country Flags font first, whose `unicode-range` covers only the flag characters, so flags show everywhere and the font is fetched only once one is on screen. With `MokaThemeProvider LoadFonts="false"`, Windows shows letter pairs, as `MokaFlag` does.

## Another language

The shipped file is English. For another language, generate a file in the same format and pass its address in `DataSource`, and replace the words with `MokaEmojiText`:

```razor
<MokaEmojiPicker @bind-Value="_icon"
                 Options="@(new MokaEmojiOptions { DataSource = "emoji/moka-emoji.sv.json", Text = Swedish })" />
```

## The data file

Format 1, one JSON file: `format`, `language`, `unicode` (the emoji version), `starter`, `groups` (`id` and `name` of Unicode's nine groups, in order) and `emoji`, one row per emoji in Unicode's order, without skin tone copies:

```json
["👍", 1, "thumbs up", "+1|thumbsup", "+1|good|hand|like|thumb|up|yes|approve|ok", 0.6, 1]
```

The fields are the emoji (fully qualified), its group, its CLDR name, its shortcodes (`|`-separated, the first is the main one), its keywords (CLDR's and gemoji's tags), its emoji version, and its skin tone: `0` takes none, `1` puts the modifier after the first code point, and a list such as `[0,4]` after each of those code points (🧑‍🤝‍🧑 tones both people). An FE0F right after a modifier's place is dropped, as Unicode writes the toned sequences.

`tools/Moka.Red.EmojiGen` writes it from Unicode's `emoji-test.txt`, CLDR's `annotations/en.xml` and `annotationsDerived/en.xml`, and gemoji's `db/emoji.json`:

```bash
dotnet run --project tools/Moka.Red.EmojiGen -- --download
dotnet run --project tools/Moka.Red.EmojiGen -- --sources path/to/folder
```

The Unicode and CLDR data are under the Unicode License v3 and gemoji's under the MIT License; both notices ship beside the file as `moka-emoji.LICENSE.txt`.

## Why the grid is drawn by script

About 1,900 emoji as Blazor components would make every render of the panel a long one, and search, hover and the arrow keys would each be a round trip. Blazor draws the frame (search box, tabs, tone button, preview strip) and a collocated module draws the grid, keeps the highlight and calls .NET once per pick. This follows the Blazor docs' "Avoid thousands of component instances" (`blazor/performance/rendering.md`) and "Avoid excessively fine-grained calls" (`blazor/performance/javascript-interoperability.md`).
