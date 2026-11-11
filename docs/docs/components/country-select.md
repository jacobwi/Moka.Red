---
title: Country Select
description: Pick a country by its flag and name, bound to its ISO code, as a dropdown, a flag button or a grid of flags, with search by name, code and dialing code.
order: 121
---

# Country Select

`MokaCountrySelect` picks a country and binds its ISO 3166-1 code, such as `"SE"`. It is a [`MokaSelect`](select.md) with the country list, the flags and the search already set up, so an app doesn't repeat the same templates. The flags are drawn by [`MokaFlag`](flag.md), so they look the same on Windows.

```razor
@using Moka.Red.Forms.CountrySelect
```

```razor
<MokaCountrySelect @bind-Value="_country" Label="Country" HelperText="Where the invoice goes." />

@code {
    private string? _country = "SE";
}
```

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Value` | `string?` | -- | The picked country's two-letter code, in upper case. Two-way bindable, and works in an `EditForm` |
| `Shape` | `MokaCountrySelectShape` | `Dropdown` | `Dropdown`, `FlagButton`, `Grid` or `Prefix`. See [Shapes](#shapes) |
| `RowContent` | `MokaCountryRowContent?` | `null` | What each row shows beside the flag: `Name`, `NameAndCode`, `NameAndDialingCode` or `OwnName`. Null is the dialing code in `Prefix` and the code everywhere else |
| `Order` | `MokaCountryOrder` | `SuggestedFirst` | `Alphabetical`, `SuggestedFirst`, `ByRegion` or `SuggestedThenByRegion`. The grid always lists A to Z. See [Order](#order) |
| `Suggested` | `IReadOnlyList<string>?` | -- | Codes listed first under Suggested |
| `SuggestBrowserCountry` | `bool` | `true` | Whether the browser's country goes first among the suggestions, read from its language once the page is interactive |
| `Countries` | `IReadOnlyList<string>?` | -- | Limits the list to these codes |
| `Names` | `Func<MokaCountry, string>?` | -- | The name shown for each country, for names in another language. Search still finds the English one |
| `ExtendedSearch` | `bool` | `true` | Whether search also finds codes, dialing codes, other names and own names, and tags a row with why it matched. Off, search matches names only |
| `MatchText` | `Func<MokaCountryMatch, string?>?` | -- | Writes the tags. Null writes "code SE", "dialing +46", "also Holland" or the own name |
| `Multiple` | `bool` | `false` | Several countries, as flag chips. Binds `SelectedValues` |
| `SelectedValues` | `IList<string>?` | -- | The codes picked with `Multiple`. Two-way bindable |
| `FlagInField` | `bool` | `true` | Whether the closed field keeps the flag beside the name |
| `TypeToJump` | `bool` | `true` | Whether typing on the closed field picks a country and Up and Down step through the list, as a native select does |
| `Label` | `string?` | -- | The label. In `FlagButton` and `Prefix` it names the button instead, and defaults to "Country" and "Country code" |
| `HelperText`, `ErrorText`, `Required`, `Optional` | | | As on every field. Without `ErrorText` the `EditForm`'s message for the field shows |
| `Placeholder` | `string` | `"Choose a country"` | The field's text while nothing is picked |
| `Clearable` | `bool` | `false` | Whether the choice can be cleared |
| `Look` | `MokaListLook?` | `null` | The list's rows, as on `MokaSelect`. A `Multiple` list on the theme's standard rows starts each with a check box |
| `SearchPlaceholder` | `string?` | -- | Null is "Search countries", or "Country, code or +46" in `Prefix` |
| `NoResultsText` | `string` | `"No country matches"` | What a search that finds nothing says |
| `SuggestedText`, `AllCountriesText` | `string` | `"Suggested"`, `"All countries"` | The headings of the suggested order |
| `RegionText` | `Func<MokaWorldRegion, string>?` | -- | The continents' headings. Null uses English names |
| `GridHeight` | `string?` | -- | The grid's height, any CSS length. Null is about five rows of tiles |
| `TooltipPosition` | `MokaTooltipPosition` | `Bottom` | Where the button shapes' tooltip opens |

## Shapes

`Dropdown` is a field whose list opens under it, with a search box at the top and the flag kept in the field.

`FlagButton` is only the flag and a chevron, for a toolbar or a card header where a field is too wide. It has no visible label: the label and the country are its accessible name ("Region, Sweden") and its tooltip ("Region: Sweden"). With nothing picked it shows a globe.

```razor
<MokaCountrySelect Shape="MokaCountrySelectShape.FlagButton" Label="Region" @bind-Value="_region" />
```

`Grid` is tiles with the flag and the code, always open, for a list people scan by eye, such as the countries a shop ships from. The search box above it filters the tiles. It has no headings, so it lists A to Z whatever `Order` says. The arrow keys move in both directions: Down from the search box goes into the grid, Up from its first row comes back, Home and End go to the ends of a row and Ctrl with them to the first and last tile, and Enter or Space picks. The grid's script keeps the highlight in the browser and calls .NET once per pick, and reports how many tiles fit in a row only when that changes.

```razor
<MokaCountrySelect Shape="MokaCountrySelectShape.Grid" Label="Ships from" @bind-Value="_from"
                   Countries="_warehouses" GridHeight="14rem" />
```

For a handful of countries, a [`MokaRadioGroup`](forms.md) with `Cards` and a `MokaFlag` preview works too.

`Prefix` is the flag and the dialing code without a border, for the start of another field. It's what [`MokaPhoneInput`](forms.md#phoneinput) draws when it has a `Country`.

`Multiple` applies to `Dropdown` and `Grid`. The button shapes pick one country.

## What a row shows

```razor
<MokaCountrySelect @bind-Value="_country" RowContent="MokaCountryRowContent.Name" />
<MokaCountrySelect @bind-Value="_country" RowContent="MokaCountryRowContent.NameAndCode" />
<MokaCountrySelect @bind-Value="_country" RowContent="MokaCountryRowContent.OwnName" />
```

`OwnName` puts the country's own name under the English one where they differ (Sverige, 日本, Deutschland), two lines a row, so fewer fit.

## Order

`Alphabetical` sorts by name with accents set aside, so Åland Islands sorts under A and Côte d'Ivoire under C. `SuggestedFirst` lists the browser's country and `Suggested` under a Suggested heading, then everyone else under All countries, without repeating them; with nothing to suggest, it is A to Z. `ByRegion` lists under Africa, Americas, Antarctica, Asia, Europe and Oceania headings that stay put while the list scrolls, and `SuggestedThenByRegion` does both.

```razor
<MokaCountrySelect @bind-Value="_country" Order="MokaCountryOrder.SuggestedThenByRegion"
                   Suggested='new[] { "SE", "NO", "DK" }' />
```

The browser's country comes from `Intl.Locale(navigator.language).maximize()`, so "sv-SE" and "sv" both give Sweden. It arrives after the first interactive render, so a prerendered list shows only the app's suggestions until then. Turn it off with `SuggestBrowserCountry="false"`.

## Search

With `ExtendedSearch` on, "se" finds Sweden by its code, "+1" finds the United States, Canada and Jamaica, "44" or "+44" the United Kingdom, a pasted "+46701234567" Sweden, "cote" Côte d'Ivoire, "uk", "holland" and "turkey" theirs, and "suomi" Finland by its own name. The best match comes first, and a row that matched something other than its name carries a tag saying why. Where several places share a dialing code, the one it belongs to comes first. Case, accents and apostrophes are set aside by the library's own table (`MokaTextFold` in Core), not by `CompareInfo`, which needs ICU: a WebAssembly app with invariant globalization has none (`blazor/globalization-localization.md`).

## Several countries

```razor
<MokaCountrySelect Multiple @bind-SelectedValues="_shipsTo" Label="Ships to"
                   HelperText="Pick as many as you like." />

@code {
    private IList<string> _shipsTo = new List<string> { "SE", "NO", "DK" };
}
```

Each picked country is a chip with its flag. The list stays open while you pick, and Backspace in the empty search box takes the last one off.

## The keyboard

On the closed field, Enter, Space and Alt with Down open the list. With `TypeToJump`, typing picks the first country starting with what you typed ("nor" then "w" for Norway, the same letter again for the next one), Up and Down step through the list and Home and End go to its ends, without opening it, as a native select does. A hidden status says the new country. In the open list the keys are `MokaSelect`'s.

## Forms and static pages

`Value`, `ValueChanged` and `ValueExpression` go straight to the select inside, so `@bind-Value` in an `EditForm` validates and shows messages like any field. Bound to a field, it posts the code under the field's name, so a static page keeps the value the server set; picking needs interactivity. The grid posts the same way.

## The country data

`MokaCountries` (in `Moka.Red.Primitives.Flag`) has every country `MokaFlag` draws a flag for: the 249 ISO 3166-1 countries and territories plus Kosovo (`XK`), 250 in all. It leaves out `EU`, `UN` and the exceptional reservations `AC`, `CP`, `DG`, `EA`, `IC` and `TA`, which are parts of other countries (Ascension's +247 is listed under St. Helena).

| Member | Description |
|--------|-------------|
| `All` | Every `MokaCountry`, sorted by English name with accents set aside |
| `Find(code)` | The country with a code, in any case, or null |
| `Search(text, countries, name)` | The countries matching what was typed, best first, as `MokaCountryMatch` (the country, what matched and the matched text) |
| `RegionName(region)` | A continent's English name |

`MokaCountry` holds `Code`, `Name` (English, as CLDR writes it), `NativeName` (the country's own, several separated by commas where it has more than one), `DialingCodes` (with their plus, the main one first, empty for a place with no telephones of its own) with `DialingCode` for the first, `Region` (`MokaWorldRegion`, by the UN's M49 regions) and `OtherNames`.

The table is built into the assembly as one string, read on first use, so names are there while prerendering and in a WebAssembly app, where `RegionInfo` gives no real names under invariant globalization, and nothing is downloaded. English names come from `RegionInfo` and own names from each region's default culture, read once on Windows; dialing codes were written by hand and checked against Windows and CLDR's telephone codes. Formatting a number per country is not included: `MokaPhoneInput` formats North American numbers only, as before.

## Accessibility

The dropdown and the button shapes are `MokaSelect`'s combobox and listbox. Flags in the rows and the field are hidden from screen readers, so a row reads as its name and code. The button shapes are named by their label and country. The grid is a `role="grid"` of rows of `role="gridcell"` tiles, each named by its country and marked `aria-selected`; its search box is a combobox that points at the highlighted tile with `aria-activedescendant` while the arrows are in the grid.
