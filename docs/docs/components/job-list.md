---
title: Job List
description: Several running jobs or transfers at once, each with a status dot, its actions, the numbers and a thick bar.
order: 156
---

# Job List

`MokaJobList` shows several jobs at once, such as uploads, downloads or exports. Each job gets two lines: a status dot, the name and its actions, then where it's going with the numbers and a thick bar. A failed job says why on its own line. A header gives a summary and Clear finished.

The list only shows what it's given. A button raises a callback, and the app changes `Jobs`. For one job told in words, use [`MokaJobProgress`](/components/job-progress).

## Setup

```razor
@using Moka.Red.Feedback.JobList
@using Moka.Red.Feedback.JobProgress
```

The second one is only for `MokaByteUnits`.

## Basic

```razor
<MokaJobList Jobs="_jobs"
             OnCancel="Cancel"
             OnRetry="Retry"
             OnRemove="Remove"
             OnClearFinished="ClearFinished" />

@code {
    private IReadOnlyList<MokaJob> _jobs =
    [
        new("video", "video-final.mp4", MokaJobState.Running)
        {
            Detail = "To Videos",
            Bytes = 1_200_000_000,
            TotalBytes = 1_900_000_000,
            BytesPerSecond = 12_000_000,
            TimeRemaining = TimeSpan.FromMinutes(1)
        },
        new("report", "report.pdf", MokaJobState.Failed)
        {
            Error = "Disk full. Free some space and try again."
        }
    ];

    private void Cancel(MokaJob job) { }
    private void Retry(MokaJob job) { }
    private void Remove(MokaJob job) { }
    private void ClearFinished() { }
}
```

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Jobs` | `IReadOnlyList<MokaJob>?` | - | The jobs, in the order to show them. Null is the same as none |
| `OnCancel` | `EventCallback<MokaJob>` | - | Raised by the cancel button |
| `OnRetry` | `EventCallback<MokaJob>` | - | Raised by the retry button |
| `OnRemove` | `EventCallback<MokaJob>` | - | Raised by the remove button |
| `OnClearFinished` | `EventCallback` | - | Raised by Clear finished. The app decides what goes |
| `ShowHeader` | `bool` | `true` | Whether the summary and Clear finished show |
| `MaxVisible` | `int?` | - | How many jobs show before a button offers the rest, from the top. Null or 0 shows them all |
| `StartContent` | `RenderFragment<MokaJob>?` | - | Something before each job that takes the height of its row, such as a thumbnail |
| `EndContent` | `RenderFragment<MokaJob>?` | - | The app's own badges and actions, between the name and the buttons |
| `EmptyContent` | `RenderFragment?` | - | Shown in place of the list when there are no jobs |
| `Text` | `MokaJobListText` | English | Every word the list writes |
| `ByteUnits` | `MokaByteUnits` | `Decimal` | `Decimal` counts sizes in powers of 1000, `Binary` in powers of 1024 |
| `ByteFormatter` | `Func<double, string>?` | - | Writes a size in bytes, unit included. Wins over `ByteUnits`, and a speed adds "/s" |
| `Disabled` | `bool` | `false` | Disables every button |
| `Class` | `string?` | - | Additional CSS classes on the root |
| `Style` | `string?` | - | Additional inline styles on the root |

Other attributes land on the root, and so do `Margin`, `Padding` and `Rounded`.

## The job

`MokaJob` is a record. `Id`, `Name` and `State` are positional, and the rest are set by name.

| Name | Type | Description |
|------|------|-------------|
| `Id` | `string` | Tells this job from the others, and keeps its row when the list reorders |
| `Name` | `string` | What the job is working on, such as a file name |
| `State` | `MokaJobState` | `Queued` (the default), `Running`, `Done`, `Failed` or `Canceled` |
| `Detail` | `string?` | Where it's going or what it's part of, such as "To Videos" |
| `Percent` | `double?` | How much is done, 0 to 100 |
| `Bytes` | `long?` | Bytes done so far |
| `TotalBytes` | `long?` | Bytes the whole job takes, when known |
| `BytesPerSecond` | `double?` | How fast it's going. Shows only while running |
| `TimeRemaining` | `TimeSpan?` | How long is left. Shows only while running |
| `Error` | `string?` | What went wrong, in a sentence |
| `Icon` | `MokaIconDefinition?` | An icon before the name, such as an arrow for its direction |

With `Percent` left null the bar works it out from `Bytes` and `TotalBytes`. When neither says how far it is, a short bar slides along the track, and with reduced motion asked for it stands still across the whole track, faint.

## What each state shows

| State | Second line | Bar | Buttons |
|-------|-------------|-----|---------|
| `Queued` | Detail, size, "Queued" | No | Remove, Cancel |
| `Running` | Detail, amount, speed, time left | Yes | Cancel |
| `Done` | Detail, size, "Done" | No | Remove |
| `Failed` | `Error`, or "It didn't finish." | No | Remove, Retry |
| `Canceled` | Detail, how far it got, "Canceled" | No | Remove, Retry |

A button whose callback isn't set doesn't show. When `TotalBytes` isn't known the percent joins the numbers, since the amount doesn't say how far it is. Sizes read the way they do on [`MokaJobProgress`](/components/job-progress): "1.2 of 1.9 GB" when they share a unit, and "900 KB of 1.2 GB" when they don't.

The second click of a double click is ignored, because a button can turn into a different one as soon as the state changes. Cancel and Retry sit in the same place, the last one in the row, so focus stays on it when one turns into the other.

## Your own badges and actions

`EndContent` sits between the name and the buttons, and `StartContent` before the whole job. A row with a slot redraws whenever the list does, since a slot can read anything.

```razor
<MokaJobList Jobs="_jobs" OnRemove="Remove">
    <StartContent Context="job">
        <img src="@Thumbnail(job)" alt="" width="36" height="36" />
    </StartContent>
    <EndContent Context="job">
        @if (job.State == MokaJobState.Done)
        {
            <MokaTag Text="Verified" Color="MokaColor.Success" Size="MokaSize.Xs" />
            <MokaButton Variant="MokaVariant.Text" Size="MokaSize.Xs" OnClick="() => Save(job)">Save</MokaButton>
        }
    </EndContent>
</MokaJobList>
```

## Long lists

`MaxVisible` shows that many jobs from the top, with a button for the rest and a way back. The order is yours, so put what matters first. A failure in the hidden part still counts in the summary and is still announced.

A height on the root scrolls the jobs under a header that stays put.

```razor
<MokaJobList Jobs="_jobs" MaxVisible="10" Style="max-height: 20rem;" />
```

## Updating many times a second

Build a new list with new records for the jobs that moved. A row draws again only when its job differs from the last one it drew, so a tick costs the rows that moved and no others. Keep the record of a job that hasn't moved, or make an equal one: either way it skips. Changing `Text`, `ByteUnits`, `ByteFormatter` or `Disabled`, or gaining or losing a callback, draws every row once.

The fill moves by `transform`, with a 120 ms transition, so a tick costs no layout and the bar keeps up with the numbers.

## Words

`Text` holds every word. Set the ones that should say something about your own app, or all of them to translate it. Anything holding a name or a count is a function, so the words can go around it and plurals can follow it.

```razor
<MokaJobList Jobs="_jobs" Text="Words" OnCancel="Cancel" />

@code {
    private static readonly MokaJobListText Words = new()
    {
        ListLabel = "Transfers",
        Empty = "No transfers yet",
        EmptyHint = "Uploads and downloads show up here.",
        Cancel = name => $"Stop {name}",
        Summary = counts => $"{counts.Running} going, {counts.Finished} finished"
    };
}
```

| Name | Type | Default |
|------|------|---------|
| `ListLabel` | `string` | "Jobs" |
| `Queued`, `Running`, `Done`, `Failed`, `Canceled` | `string` | The state's name |
| `NoReason` | `string` | "It didn't finish." |
| `ClearFinished` | `string` | "Clear finished" |
| `Empty` | `string` | "No jobs" |
| `EmptyHint` | `string?` | - |
| `ShowLess` | `string` | "Show less" |
| `Separator` | `string` | ", " |
| `Cancel`, `Retry`, `Remove` | `Func<string, string>` | "Cancel {name}" and so on |
| `ShowMore` | `Func<int, string>` | "Show {hidden} more" |
| `Summary` | `Func<MokaJobCounts, string>` | "2 running, 1 failed", leaving out what's zero |
| `Amount` | `Func<string, string, string>` | "{done} of {whole}" |
| `TimeLeft` | `Func<TimeSpan, string>` | "1 min left" |
| `FailureAnnouncement` | `Func<MokaJob, string>` | "{name} failed: {error}" |

`MokaJobCounts` has `Queued`, `Running`, `Done`, `Failed`, `Canceled` and `Finished`, the three ways of having stopped added up.

## Accessibility

- It's a list with a name, one item per job, and each status dot is named for its state, so a state is never only a color.
- A running job's bar is a `progressbar` named for the job, with its percent and its amount. Nothing reads those out until someone asks.
- A job that fails is announced politely, once, with its reason, including one hidden by `MaxVisible`. A job already failed when the list appears isn't news, and one that fails again is announced again. Percents aren't announced.
- Each button is named for its job, "Cancel video-final.mp4", and shows that name in a tooltip.
- When a press takes away the button that had focus, focus moves to that job's row, or to the row now in its place if the job has gone, unless the person has already moved on.
- A long name is cut off with an ellipsis and stays whole in a `title`.
