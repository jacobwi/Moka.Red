---
title: QR Scanner
description: Reads a QR code from the camera, or from an image the person picks, decoded in the browser.
order: 151
---

# QR Scanner

`MokaQRScanner` reads a QR code from the camera, or from an image the person picks. Decoding happens in the browser: its own `BarcodeDetector` where it reads QR codes, else the bundled [jsQR](https://github.com/cozmo/jsQR) in a worker, so frames never travel to .NET and nothing is added to your app's NuGet graph. jsQR (Apache-2.0, its license ships beside it) loads the first time something is scanned, not with the page. To draw a code, use [`MokaQRCode`](/components/qr-code).

It's an inline component. For a scan dialog, put it in a [`MokaDialog`](/components/dialog).

## Setup

```razor
@using Moka.Red.Primitives.QRScanner
```

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Active` | `bool` | `false` | Whether the camera is on. Two-way: after a scan the scanner turns itself off and raises `ActiveChanged` |
| `OnScanned` | `EventCallback<string>` | -- | The text of a code that was read and passed `Accept` |
| `Accept` | `Func<string, bool>?` | -- | Whether a code is one the app can use. A code turned down shows `RejectedText` and the scan goes on |
| `Continuous` | `bool` | `false` | Keeps scanning after a code, reporting each new one. A code is reported once while it stays in view |
| `OnStateChanged` | `EventCallback<MokaQRScannerState>` | -- | Off, Starting, Scanning, or why the camera didn't start: Denied, NoCamera, CameraBusy, Unsupported, Failed |
| `ShowImagePicker` | `bool` | `true` | Whether a button lets the person pick an image of a code instead |
| `PickImageText` | `string` | `"Use an image"` | The image button's text |
| `OffText`, `StartingText`, `ScanningText`, `FoundText` | `string` | English | The status line in each state |
| `RejectedText` | `string` | `"That QR code isn't one this app can use."` | Said when `Accept` turns a code down |
| `DeniedText`, `NoCameraText`, `CameraBusyText`, `UnsupportedText`, `FailedText` | `string` | English | Said when the camera can't start, and why |
| `ImageFallbackText` | `string` | `"You can pick an image of the code instead."` | Added after a camera problem while the image button shows |
| `NoCodeText` | `Func<string, string>?` | -- | Said when a picked image holds no code, given the file's name |
| `FilePicker` | `IMokaFilePicker?` | -- | Picks the image in place of the browser's dialog. Wins over a registered `IMokaFilePicker`. See [Your app's own picker](#your-apps-own-picker) |

`State` reads where the scanner is now.

## Basic

```razor
<MokaQRScanner @bind-Active="_scanning" OnScanned="text => _code = text" />
<MokaButton OnClick="() => _scanning = true">Scan</MokaButton>

@code {
    private bool _scanning;
    private string? _code;
}
```

## In a dialog

The dialog's open state turns the camera on and off, so closing it any way, Escape included, stops the camera.

```razor
<MokaDialog @bind-Open="_open" Title="Scan QR code" Icon="MokaIcons.Content.QrCode" DialogSize="MokaDialogSize.Small">
    <MokaQRScanner Active="_open" OnScanned="HandleScanned" />
</MokaDialog>

@code {
    private bool _open;

    private void HandleScanned(string text)
    {
        _open = false;
        // Use the text.
    }
}
```

## Only some codes

`Accept` turns codes away without ending the scan, and the status line says so. A code that's turned down isn't reported again while it stays in view.

```razor
<MokaQRScanner @bind-Active="_scanning"
               Accept="@(text => text.Contains("[Interface]"))"
               RejectedText="That QR code isn't a WireGuard config."
               OnScanned="ImportAsync" />
```

## The camera

The scanner asks for the rear camera where there is one. The camera runs only while `Active` is on: it stops when `Active` goes off, after a scan (unless `Continuous`), when `Disabled` is set and when the component is disposed. Frames are read about five times a second, scaled to 720 pixels on their long side, and skipped while the tab is hidden.

When the camera can't start, the status line says why and offers the image button: the person refused it or a permissions policy blocks it (`Denied`), there is none (`NoCamera`), another app holds it (`CameraBusy`), or the page isn't a secure context, which a camera needs, or the browser can't read QR codes (`Unsupported`).

## From an image

The image button is a label around a file input, so Tab reaches it and a click anywhere on it opens the picker. The image is read in the browser too, scaled to 1600 pixels on its long side and drawn on white, so a transparent PNG or an SVG reads. The status line names a file that holds no code.

## Your app's own picker

With an `IMokaFilePicker` registered, or one passed to `FilePicker`, the image button asks it for one `image/*` file instead of opening the browser's dialog. A Blazor Hybrid app on Windows wants this, since WebView2's file dialog can end the app. [File Picker](/components/file-picker) has a complete MAUI picker.

```razor
<MokaQRScanner @bind-Active="_scanning" OnScanned="text => _code = text" FilePicker="_picker" />
```

The click, Enter or Space that would open the browser's dialog is cancelled before it can. The picked image arrives in .NET, so it is streamed to the browser, which reads the code exactly as it does for its own pick, and the states and texts are the same. Images over 32 MB aren't streamed and count as holding no code. An empty list, or an `OperationCanceledException`, is a cancel and changes nothing. Any other exception goes to the nearest error boundary. While the picker is open another click does nothing.

## Screen readers

The status line is a polite `role="status"`, so a screen reader hears each state, a turned-down code and a code read. The video and the viewfinder are hidden from it.
