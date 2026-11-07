---
title: Error Boundary
description: An ErrorBoundary that says so in the app's look, with a way to try again.
order: 141
---

# Error Boundary

`MokaErrorBoundary` catches what its content throws and shows it in the app's look. Most unhandled exceptions end a Blazor Server circuit, so without a boundary one failing part takes the whole app with it.

While the content works there's no wrapper at all, so it changes nothing about your layout.

## Setup

```razor
@using Moka.Red.Feedback.ErrorBoundary
```

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | - | What it guards |
| `What` | `string` | `"This page"` | What stopped, for the sentence: "This card", "The menu" |
| `Compact` | `bool` | `false` | One line, for a part of a page rather than all of it |
| `Links` | `RenderFragment?` | - | Beside the buttons of the full look, for links such as where the logs are |
| `RecoverOnNavigation` | `bool` | `false` | Tries the content again when the location changes |
| `Detail` | `string?` | `"The rest of the app is fine."` | The line under the title of the full look |
| `ShowMessage` | `bool` | `true` | Whether the full look has its fold with the exception's message |
| `MessageLabel` | `string` | `"What went wrong"` | The label of that fold |
| `TryAgainText` | `string` | `"Try again"` | The text of the button that tries the content again |
| `ReloadText` | `string` | `"Reload the page"` | The text of the button that reloads the page |
| `ErrorContent` | `RenderFragment<Exception>?` | - | Replaces the look altogether. The wrapper and its class stay |
| `MaximumErrorCount` | `int` | `100` | From `ErrorBoundaryBase`. Past this many failures with no Try again, one is treated as fatal |
| `Class` | `string?` | - | Additional CSS classes on what's drawn while failed |
| `Style` | `string?` | - | Additional inline styles on what's drawn while failed |
| `Id` | `string?` | - | The id of what's drawn while failed |

## Around a page

Put it around the layout's body. `RecoverOnNavigation` keeps a failed page from standing in for the next one.

```razor
<MokaErrorBoundary RecoverOnNavigation>
    <ChildContent>@Body</ChildContent>
    <Links><a href="logs">See the logs</a></Links>
</MokaErrorBoundary>
```

## Around a part

```razor
<MokaCard Title="Traffic">
    <MokaErrorBoundary Compact What="This chart">
        <TrafficChart/>
    </MokaErrorBoundary>
</MokaCard>
```

## What it shows

The full look is a card with a warning tile, "This page hit a problem", Try again, Reload the page, and a fold with the exception's message. It shows `Exception.Message` to whoever is looking at the page, so turn `ShowMessage` off if your messages can hold anything private.

The failure still reaches the server's log, through Blazor's own `IErrorBoundaryLogger`. A logger that fails is ignored, so logging can't bring down the page the boundary just saved.

## In tests

Whatever it draws while failed carries the class `moka-error-boundary--failed`, with `ErrorContent` too. A browser test can fail on it:

```js
const broke = await page.locator('.moka-error-boundary--failed').count();
if (broke > 0) throw new Error(`${url} shows a failure`);
```

## Try again

`Recover()` is public, from `ErrorBoundaryBase`, so a parent can call it through a reference. If the content fails again the failure shows again, and nothing is rethrown.
