---
title: Settings Form
description: A section of settings with a real save flow, deep links to any field, and a page with search around it.
order: 143
---

# Settings Form

`MokaSettingsForm` is one section of settings: what's set now, an Unsaved tag and a sticky footer while edited, a question before leaving unsaved edits, and deep links that land on a field. `MokaSettingsGroup` is a heading and the fields under it. `MokaSettingsLayout` is the page around them, with a search box and a side list.

It keeps none of your save logic. You say whether it's `Dirty`, you hear `OnSave` and `OnReset`, and you hand back any `Refusal`.

## Setup

```razor
@using Moka.Red.Forms.SettingsForm
```

## Parameters

### MokaSettingsForm

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Title` | `string` | **required** | The section's name, for its heading and for screen readers |
| `Lead` | `string?` | - | What the section is for. `MokaTheme.WithDescriptions(false)` hides it |
| `Status` | `RenderFragment?` | - | What just happened: at the right of the header, or beside the title in `Cards` |
| `Appearance` | `MokaSettingsAppearance` | `Plain` | `Plain` is one box with each group as a heading over its fields. `Cards` puts the header and each group in a card of their own. See [Cards](#cards) |
| `Icon` | `MokaIconDefinition?` | - | An icon in a 56 px accent tile before the title |
| `HeaderActions` | `RenderFragment?` | - | Controls at the end of the header, such as a segmented choice |
| `Now` | `string?` | - | What's set now, in a line. It's state, so it always shows |
| `NowContent` | `RenderFragment?` | - | Instead of `Now`, for a line with bold parts |
| `NowLabel` | `string` | `"Now:"` | The word in front of that line |
| `ChildContent` | `RenderFragment?` | - | The groups and fields |
| `Dirty` | `bool` | `false` | Whether there are edits nobody saved. It shows the tag and the footer, and arms the question before leaving |
| `CanSave` | `bool` | `true` | Off, Save is disabled |
| `Saving` | `bool` | `false` | Both footer buttons are disabled, and Save says so |
| `Refusal` | `string?` | - | Why the last save was refused, shown under the fields |
| `OnSave` | `EventCallback` | - | Raised by Save |
| `OnReset` | `EventCallback` | - | Raised by the button that puts the edits back. Without a listener there is no such button |
| `HasAdvanced` | `bool` | `false` | Shows "Show advanced" with no group marked `Advanced`, for an app that hides single fields itself |
| `ShowAdvanced` | `bool` | `false` | Whether advanced settings are showing |
| `ShowAdvancedChanged` | `EventCallback<bool>` | - | Raised when the person, or a deep link to an advanced setting, changes it |
| `Entries` | `IReadOnlyList<MokaSettingsEntry>?` | - | The settings a deep link can land on, for the ones inside an advanced part. Defaults to the layout's |
| `UnsavedText` | `string` | `"Unsaved"` | The tag's text while edited |
| `DirtyText` | `string` | `"Changes not saved yet"` | The footer's sentence while edited |
| `ResetText` | `string` | `"Put back"` | The text of the button that puts the edits back |
| `SaveText` | `string` | `"Save"` | The text of the save button |
| `SavingText` | `string` | `"Saving"` | The text of the save button during a save |
| `ShowAdvancedText` | `string` | `"Show advanced"` | The toggle's text while advanced settings are hidden |
| `HideAdvancedText` | `string` | `"Hide advanced"` | The toggle's text while they show |
| `LeavePrompt` | `string?` | - | What the footer asks before leaving. Left out, it names the section |
| `StayText` | `string` | `"Stay"` | The text of the button that stays |
| `LeaveText` | `string` | `"Leave without saving"` | The text of the button that leaves anyway |
| `Class` | `string?` | - | Additional CSS classes on the form |
| `Style` | `string?` | - | Additional inline styles on the form |

`Margin` and `Rounded` land on the form, and `Padding` on its body, above the footer.

### MokaSettingsGroup

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Id` | `string?` | - | The anchor a deep link lands on, and what the heading links to |
| `Title` | `string` | **required** | The group's name |
| `Help` | `string?` | - | What the group is for. `MokaTheme.WithDescriptions(false)` hides it |
| `Advanced` | `bool` | `false` | Hides the group until the form's "Show advanced" is on |
| `Tag` | `string?` | - | A short word beside the title, like "New" |
| `Columns` | `int` | `2` | The most columns the grid has. A column is never narrower than 16rem of the group's own width, so a narrow group has fewer |
| `Span` | `MokaSettingsSpan` | `Auto` | In a form drawn as cards, `Full` takes the whole row |
| `AdvancedText` | `string` | `"Advanced"` | The tag beside the title of an advanced group |
| `ChildContent` | `RenderFragment?` | - | The fields |

### MokaSettingsLayout

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Sections` | `IReadOnlyList<MokaSettingsSection>` | empty | The rows of the side list |
| `Active` | `string?` | - | The `Id` of the section showing. When the layout is narrow, null shows the list instead, or `Landing` if you set one |
| `Landing` | `RenderFragment?` | - | What shows while no section is active, such as a settings home page. See [the phone's list](#the-page-around-it) below |
| `ActiveChanged` | `EventCallback<string?>` | - | Raised when a row without an address is picked, or the way back to the list is, with null |
| `Entries` | `IReadOnlyList<MokaSettingsEntry>?` | - | Every setting that can be searched for. Without any, there's no search box |
| `OnPick` | `EventCallback<MokaSettingsEntry>` | - | Raised when a search result is picked, just before going to it |
| `MaxResults` | `int` | `8` | The most search results to show |
| `ChildContent` | `RenderFragment?` | - | The section showing |
| `SearchLabel` | `string` | `"Search settings"` | The search box's placeholder and name |
| `NoResultsText` | `string` | `"No setting matches that."` | What the results say when nothing matches |
| `ListLabel` | `string` | `"Settings sections"` | The side list's name for screen readers |
| `BackHref` | `string?` | - | The address of the list when the layout is narrow. Left out, the way back is a button that clears `Active` |
| `BackText` | `string` | `"All settings"` | The text of the way back |
| `UnsavedLabel` | `string` | `"Unsaved edits"` | Said for screen readers on the row of a section with unsaved edits |

### The models

| Record | Members |
|--------|---------|
| `MokaSettingsEntry` | `Label`, `Keywords`, `Anchor`, `Advanced`, and `Route`, `Section` and `Href`, for the search box and your own command palette |
| `MokaSettingsSection` | `Id`, `Label`, `Group`, `Href`, and `Icon` (set with `{ Icon = MokaIcons.Action.Settings }`): drawn before the label in the row and beside the section's name in search results, muted, in the accent on the current row and the accent's ink on a solid one. A row without one leaves no gap, so give every row of a group an icon or none |

## A form

```razor
<MokaSettingsForm Title="General"
                  Lead="How this router names itself and keeps time."
                  Dirty="_edited != _saved"
                  DirtyText="Your edits aren't saved yet"
                  Saving="_saving"
                  Refusal="@_refusal"
                  OnSave="SaveAsync"
                  OnReset="PutBack">
    <NowContent>this router is <strong>@_saved.Name</strong>, on @_saved.Zone time.</NowContent>
    <ChildContent>
        <MokaSettingsGroup Id="name" Title="Name">
            <div id="host-name">
                <MokaTextField Label="Host name" Mono @bind-Value="_edited.Name"/>
                <p class="moka-settings-help moka-about">What other devices see on the network.</p>
            </div>
            <MokaTextField Label="Domain" Optional Mono @bind-Value="_edited.Domain"/>
        </MokaSettingsGroup>
    </ChildContent>
</MokaSettingsForm>
```

Save stays in view on a long section, because the footer sticks to the bottom of the window. If something floats over the end of the page, such as a [`MokaActionBar`](/components/action-bar), set `--moka-settings-footer-offset` on the form to clear it.

## Cards

`Appearance="MokaSettingsAppearance.Cards"` draws the header and each group as a card of its own on the page, like a dashboard. The groups go two to a row once the form is 46rem wide, and one to a row below that. The form's own width decides, not the window's, so a settings section beside an app shell's rail stacks when it's narrow. A group with `Span="MokaSettingsSpan.Full"` takes the whole row, as does any other content with the class `moka-settings-full`, and an advanced group that's hidden leaves no empty card. Deep links land on a group inside its card as they do in the plain form.

```razor
<MokaSettingsForm Title="Storage" Lead="Where your libraries live and how full they are."
                  Appearance="MokaSettingsAppearance.Cards" Icon="MokaIcons.File.Database"
                  Dirty="Dirty" OnSave="SaveAsync">
    <Status><MokaTag Text="Healthy" Color="MokaColor.Success" Pill/></Status>
    <HeaderActions>
        <MokaSegmentedControl @bind-Value="_detail" Size="MokaSize.Xs" AriaLabel="Detail">
            <MokaSegment Value="simple" Text="Simple"/>
            <MokaSegment Value="detailed" Text="Detailed"/>
        </MokaSegmentedControl>
    </HeaderActions>
    <ChildContent>
        <MokaSettingsGroup Id="location" Title="Location">...</MokaSettingsGroup>
        <MokaSettingsGroup Id="limits" Title="Limits">...</MokaSettingsGroup>
        <MokaSettingsGroup Id="cleanup" Title="Cleanup" Span="MokaSettingsSpan.Full">...</MokaSettingsGroup>
    </ChildContent>
</MokaSettingsForm>
```

The current section in `MokaSettingsLayout`'s list follows the theme's current-place choice: `MokaTheme.WithCurrentPlace(MokaCurrentPlace.Solid)` fills it with the accent.

## Help and state

Two kinds of text are kept apart. Help says what a thing is for, and someone who knows the app can hide it. State says what's so, and always shows. `Lead` and `Help` are help, and the `Now` line and `Refusal` are state.

For your own lines among the fields, use these classes. `moka-about` is what `WithDescriptions(false)` hides.

```razor
<p class="moka-settings-help moka-about">What other devices see on the network.</p>
<p class="moka-settings-said">Right now it's 14:32 there.</p>
```

## Leaving with unsaved edits

While `Dirty`, a `NavigationLock` holds any link that leaves the page and asks in the footer, which it scrolls into view with the cursor on Stay. The browser asks before leaving the app. A link to another field on the same page isn't leaving, query string included.

A `MokaSettingsSection` with no `Href` only switches `Active`. No navigation happens, so nothing can ask. Give sections addresses, or check `Dirty` yourself.

## Deep links

A link such as `settings/general#time-zone` scrolls to the element with that id, marks it with a 3 px ring for 2.2 seconds and puts the cursor in its field. On a touch screen it leaves the cursor alone, because the keyboard would cover what it just scrolled to.

The target can be a group's `Id` or any element in the form. If it isn't there yet, because the page is still loading its data, it's tried again on the next renders.

An advanced setting turns "Show advanced" on first. A group marked `Advanced` is found by itself. For an element inside something you hide, list it in `Entries` with `Advanced` set.

## Advanced settings

"Show advanced" appears once a group is marked `Advanced`. To hide single fields, set `HasAdvanced` and bind `ShowAdvanced`.

```razor
<MokaSettingsForm Title="General" HasAdvanced @bind-ShowAdvanced="_advanced">
    <MokaSettingsGroup Id="time" Title="Time">
        <MokaTextField Label="Time zone" @bind-Value="_zone"/>
        @if (_advanced)
        {
            <MokaTextField Label="Time server" @bind-Value="_server"/>
        }
    </MokaSettingsGroup>
</MokaSettingsForm>
```

## The page around it

`/` puts the cursor in the search box, and the arrows and Enter pick a result. The form inside takes the layout's `Entries` and marks its row while edited.

```razor
<MokaSettingsLayout Sections="_sections" Entries="_entries" Active="@Section" BackHref="settings">
    <GeneralSettings/>
</MokaSettingsLayout>

@code {
    [Parameter] public string? Section { get; set; }

    private static readonly MokaSettingsSection[] _sections =
    [
        new("general", "General", "System", "settings/general"),
        new("updates", "Updates", "System", "settings/updates"),
        new("names", "Names", "Network", "settings/names")
    ];

    private static readonly MokaSettingsEntry[] _entries =
    [
        new("Time zone", "clock region", "time-zone") { Route = "settings/general", Section = "General" },
        new("Time server", "ntp clock", "time-server", Advanced: true) { Route = "settings/general", Section = "General" }
    ];
}
```

When the layout is narrower than 45rem the list is its own page. It shows while `Active` is null, and a section links back to it. With `Landing` set, that page shows instead of the list, and beside the list on a wider layout, so a home screen can be the first thing a phone sees. Give it links to the sections, since the list is hidden there. A section's way back returns to it. The layout's own width decides, not the window's, so it behaves the same in an app shell beside a rail, in a dialog or in a split view: a 45rem layout is a 720 px one with no shell around it.

## Above the page

The open list of search results sits in the browser's top layer, so a card that hides its overflow no longer cuts it off and nothing later on the page paints over it, whatever its `z-index`. It stays against the search box as the page scrolls and flips to the other side when the window has no room there. While it's open the library sets its position, so a stylesheet can restyle it but not move it.
