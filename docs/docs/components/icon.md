---
title: Icon
description: Inline SVG icon component backed by the MokaIcons library (Lucide-style, 24x24 viewBox).
order: 2
---

# Icon

`MokaIcon` renders an inline SVG from a `MokaIconDefinition`. Icons use a 24x24 viewBox with stroke-based paths (Lucide/Feather style). The built-in library is organized into six categories: `Action`, `Navigation`, `Status`, `Content`, `Toggle`, and `File`.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Icon` | `MokaIconDefinition` | **required** | The icon definition to render |
| `Size` | `MokaSize` | `Md` | `Xs`, `Sm`, `Md`, `Lg` |
| `Color` | `MokaColor?` | - | Sets the icon color via CSS custom property |
| `Class` | `string?` | - | Additional CSS classes |
| `Style` | `string?` | - | Additional inline styles |

## Basic Usage

```blazor-preview
<MokaIcon Icon="MokaIcons.Action.Save" />
<MokaIcon Icon="MokaIcons.Status.CheckCircle" Color="MokaColor.Success" />
<MokaIcon Icon="MokaIcons.Status.Error" Color="MokaColor.Error" />
```

## Sizes

```blazor-preview
<div style="display:flex;gap:12px;align-items:center">
    <MokaIcon Icon="MokaIcons.Navigation.Home" Size="MokaSize.Xs" />
    <MokaIcon Icon="MokaIcons.Navigation.Home" Size="MokaSize.Sm" />
    <MokaIcon Icon="MokaIcons.Navigation.Home" Size="MokaSize.Md" />
    <MokaIcon Icon="MokaIcons.Navigation.Home" Size="MokaSize.Lg" />
</div>
```

## Colors

```blazor-preview
<div style="display:flex;gap:12px;align-items:center">
    <MokaIcon Icon="MokaIcons.Status.Info" Color="MokaColor.Info" />
    <MokaIcon Icon="MokaIcons.Status.Warning" Color="MokaColor.Warning" />
    <MokaIcon Icon="MokaIcons.Status.Error" Color="MokaColor.Error" />
    <MokaIcon Icon="MokaIcons.Status.CheckCircle" Color="MokaColor.Success" />
    <MokaIcon Icon="MokaIcons.Toggle.Star" Color="MokaColor.Primary" />
</div>
```

## Icon Library

Each name below is a field on its group, as in `MokaIcons.Action.Save` or `MokaIcons.File.FolderPlus`.

### Action

`Save`, `Delete`, `Edit`, `Add`, `Remove`, `Search`, `Settings`, `Refresh`, `Download`, `Upload`, `Sun`, `Moon`, `Send`, `GripVertical`, `Pencil`, `Eraser`, `Broom`, `Sliders`, `Undo`, `Redo`, `Restore`, `History`, `Retry`, `RotateCw`, `RotateCcw`, `FlipHorizontal`, `FlipVertical`, `Crop`, `Replace`, `Swap`, `Share`, `Import`, `DownloadAll`, `Power`, `SignIn`, `SignOut`, `Play`, `Pause`, `Stop`, `Record`, `Eject`, `PlayCircle`, `PauseCircle`, `StopCircle`, `SkipBack`, `SkipForward`, `Rewind`, `FastForward`, `Repeat`, `Shuffle`

```blazor-preview
<div style="display:flex;gap:12px;flex-wrap:wrap">
    <MokaIcon Icon="MokaIcons.Action.Save" />
    <MokaIcon Icon="MokaIcons.Action.Delete" />
    <MokaIcon Icon="MokaIcons.Action.Edit" />
    <MokaIcon Icon="MokaIcons.Action.Add" />
    <MokaIcon Icon="MokaIcons.Action.Search" />
    <MokaIcon Icon="MokaIcons.Action.Settings" />
    <MokaIcon Icon="MokaIcons.Action.Refresh" />
    <MokaIcon Icon="MokaIcons.Action.Download" />
    <MokaIcon Icon="MokaIcons.Action.Upload" />
</div>
```

### Navigation

`ArrowLeft`, `ArrowRight`, `ArrowUp`, `ArrowDown`, `ChevronLeft`, `ChevronRight`, `ChevronUp`, `ChevronDown`, `Menu`, `Close`, `Home`, `ChevronsLeft`, `ChevronsRight`, `ChevronsUpDown`, `Dock`, `Undock`, `MoreHorizontal`, `MoreVertical`, `ArrowUpDown`, `ArrowUpToLine`, `ArrowDownToLine`, `ArrowUpCircle`, `ArrowDownCircle`, `Dashboard`, `Grid`, `Template`, `AppWindow`, `PanelLeft`, `PanelRight`, `PanelTop`, `PanelBottom`, `SplitView`, `SplitStacked`, `SplitThree`, `SplitGrid`, `Maximize`, `Minimize`, `Resize`, `Pan`, `Split`, `Route`

```blazor-preview
<div style="display:flex;gap:12px;flex-wrap:wrap">
    <MokaIcon Icon="MokaIcons.Navigation.ArrowLeft" />
    <MokaIcon Icon="MokaIcons.Navigation.ArrowRight" />
    <MokaIcon Icon="MokaIcons.Navigation.ChevronDown" />
    <MokaIcon Icon="MokaIcons.Navigation.Menu" />
    <MokaIcon Icon="MokaIcons.Navigation.Close" />
    <MokaIcon Icon="MokaIcons.Navigation.Home" />
    <MokaIcon Icon="MokaIcons.Navigation.MoreVertical" />
</div>
```

### Status

`Check`, `CheckCircle`, `Warning`, `Error`, `AlertCircle`, `Info`, `HelpCircle`, `Clock`, `Loading`, `Bell`, `BadgeCheck`, `Ban`, `Activity`, `Gauge`, `TrendingUp`, `TrendingDown`, `Shield`, `ShieldCheck`, `ShieldAlert`, `ShieldOff`, `ShieldX`, `ShieldBan`, `ShieldPlus`, `ShieldQuestion`, `Wifi`, `WifiOff`, `Signal`, `CloudOff`, `ServerOff`, `ServerCrash`, `Hourglass`, `Timer`, `TimerOff`, `AlarmClock`

### Content

`Copy`, `Paste`, `Link`, `Calendar`, `EyeDropper`, `ExternalLink`, `ZoomIn`, `Unlink`, `Image`, `Attachment`, `Filter`, `Sort`, `Columns`, `RowsComfortable`, `RowsDense`, `SortNeutral`, `Globe`, `Camera`, `QrCode`, `Scale`, `Cut`, `ZoomOut`, `User`, `Users`, `UserPlus`, `UserMinus`, `UserCheck`, `UserX`, `UserCog`, `UserLock`, `UserCircle`, `IdCard`, `Crown`, `Key`, `KeyRound`, `Fingerprint`, `ScanFace`, `Certificate`, `Signature`, `Inbox`, `Comment`, `Monitor`, `Laptop`, `Smartphone`, `Tablet`, `Watch`, `Tv`, `Gamepad`, `Printer`, `Speaker`, `Keyboard`, `Command`, `Cursor`, `Cpu`, `Server`, `Router`, `EthernetPort`, `Network`, `Firewall`, `Tunnel`, `Broadcast`, `RadioTower`, `Bluetooth`, `Rss`, `Cable`, `Plug`, `Unplug`, `Waypoints`, `Webhook`, `GlobeLock`, `Cloud`, `CloudUpload`, `CloudDownload`, `CloudCheck`, `CloudSync`, `RefreshOff`, `Zap`, `Sparkles`, `Bot`, `Puzzle`, `Wrench`, `Hammer`, `Toolbox`, `Palette`, `Paintbrush`, `PaintBucket`, `Droplet`, `Highlighter`, `PenTool`, `Wand`, `Stamp`, `Ruler`, `Shapes`, `Diamond`, `Cube`, `Layers`, `Images`, `ImagePlus`, `ImageOff`, `CameraOff`, `Film`, `Clapperboard`, `Music`, `AudioLines`, `Headphones`, `Cast`, `ScreenShare`, `PictureInPicture`, `AspectRatio`, `Captions`, `Ear`, `BarChart`, `BarChartHorizontal`, `LineChart`, `AreaChart`, `PieChart`, `Timeline`, `Kanban`, `Table`, `ColumnsView`, `Percent`, `Sigma`, `Type`, `Bold`, `Italic`, `Underline`, `Strikethrough`, `Heading`, `Heading1`, `Heading2`, `Heading3`, `Quote`, `Hash`, `Regex`, `AlignLeft`, `AlignCenter`, `AlignRight`, `AlignJustify`, `List`, `ListOrdered`, `ListChecks`, `ListTree`, `IndentIncrease`, `IndentDecrease`, `SpellCheck`, `StickyNote`, `Book`, `Tag`, `CalendarDays`, `CalendarCheck`, `CalendarPlus`, `CalendarX`, `CalendarClock`, `CalendarRange`

### Toggle

`Eye`, `EyeOff`, `Lock`, `Unlock`, `Star`, `StarOutline`, `Heart`, `HeartOutline`, `ThumbUp`, `ThumbDown`, `Pin`, `PinOff`, `Bookmark`, `Flag`, `Mic`, `MicOff`, `Video`, `VideoOff`, `Volume`, `VolumeOff`, `Square`, `CheckSquare`

### File

`Document`, `Folder`, `FolderOpen`, `FileText`, `Code`, `Terminal`, `Database`, `Files`, `FilePlus`, `FileMinus`, `FileCheck`, `FileX`, `FilePen`, `FileSearch`, `FileLock`, `FileKey`, `FileClock`, `FileWarning`, `FileUpload`, `FileDownload`, `FileSymlink`, `FileImage`, `FileVideo`, `FileAudio`, `FileCode`, `FileJson`, `FileTerminal`, `FileType`, `FileSpreadsheet`, `FileArchive`, `Folders`, `FolderPlus`, `FolderMinus`, `FolderCheck`, `FolderX`, `FolderInput`, `FolderUp`, `FolderDown`, `FolderSearch`, `FolderLock`, `FolderCog`, `FolderTree`, `Archive`, `ArchiveRestore`, `ArchiveX`, `Package`, `PackageOpen`, `PackageCheck`, `Logs`, `Braces`, `HardDrive`, `HardDriveUpload`, `HardDriveDownload`, `DatabaseBackup`

## Custom SVG

Pass a `MokaIconDefinition` constructed with a name and SVG path data to render any custom icon.

```blazor-preview
@code {
    static readonly MokaIconDefinition CustomIcon = new("custom",
        "M12 2L2 7l10 5 10-5-10-5z M2 17l10 5 10-5 M2 12l10 5 10-5");
}
<MokaIcon Icon="CustomIcon" Size="MokaSize.Lg" Color="MokaColor.Primary" />
```
