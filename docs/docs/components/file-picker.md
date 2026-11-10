---
title: File Picker
description: Let your app pick files for every Moka component that would open the browser's file dialog, such as a native picker in a MAUI app.
order: 153
---

# File Picker

Three components open the browser's file dialog: [`MokaFileUpload`](/components/forms#fileupload), the Upload control of [`MokaFileBrowser`](/components/file-browser) and the image button of [`MokaQRScanner`](/components/qr-scanner). `IMokaFilePicker` lets your app pick the files instead. Register one and all three ask it, and the browser's dialog never opens.

The reason to do it is Blazor Hybrid on Windows. In a MAUI app, the WebView2 file dialog that a file input opens can end the whole app once it has been open for 10 to 30 seconds (a crash in `EmbeddedBrowserWebView.dll`, [WebView2Feedback #5741](https://github.com/MicrosoftEdge/WebView2Feedback/issues/5741), still open). A native picker never opens that dialog. The [MAUI picker](#a-maui-picker) below is complete and ready to copy.

With no picker registered, every component works exactly as before: same markup, no extra listeners, no extra script.

## Setup

```csharp
using Moka.Red.Core.Extensions;

builder.Services.AddMokaFilePicker<MauiFilePicker>();   // scoped; AddMokaRed() doesn't call it
```

```razor
@using Moka.Red.Core.Files
```

## IMokaFilePicker

```csharp
public interface IMokaFilePicker
{
    Task<IReadOnlyList<IBrowserFile>> PickAsync(MokaFilePickRequest request, CancellationToken cancellationToken);
}
```

| What | Meaning |
|------|---------|
| The files returned | What the person picked. Each is an `IBrowserFile`, so it reaches your code the way a file from the browser does |
| An empty list | The person cancelled. Nothing changes |
| `OperationCanceledException` | Also a cancel |
| Any other exception | A failure, shown the way the component shows its own (below) |
| `cancellationToken` | Cancelled when the component that asked is disposed. Files that arrive after that are ignored |

### MokaFilePickRequest

| Member | Type | Description |
|--------|------|-------------|
| `Multiple` | `bool` | Whether the person may pick several files. A component that wants one keeps the first |
| `Accept` | `string?` | The component's types, written as a file input's `accept`: `".pdf,.docx"`, `"image/*"`, `"application/pdf"`. Null takes any file |
| `AcceptedTypes()` | `IReadOnlyList<string>` | The tokens of `Accept`, trimmed, for mapping to a native picker's file types |

The picker doesn't have to filter. Every component checks the files it gets as it checks the browser's, so a picker that can't express a type can show every file.

## One component, or one page

Each of the three components has a `FilePicker` parameter that wins over the registered picker. Use it in tests and demos, or in an app that wants a picker on some pages only:

```razor
<MokaFileUpload Label="Receipt" Accept="image/*" FilePicker="_picker" />

@code {
    private readonly IMokaFilePicker _picker = new MauiFilePicker();
}
```

## What each component does with it

| Component | Asks for | Then |
|-----------|----------|------|
| `MokaFileUpload` | `Multiple` and `Accept`, as set on the component | The files go through `Accept`, `MaxFileSize`, `MaxFiles`, the one-file rule and their error lines, then `OnFilesSelected` and the list, exactly as a pick from the browser does |
| `MokaFileBrowser` | Several files, any type | The files go to `Upload` with the folder that was on screen when Upload was pressed, and the folder reloads |
| `MokaQRScanner` | One file, `image/*` | The image is streamed to the browser, which reads the code as it does for its own pick, up to 32 MB. Its states and texts don't change, and a bigger image counts as holding no code |

A click, Enter or Space on the upload zone, its labels or the QR scanner's image button still lands on the hidden file input. With a picker, the component cancels that click in the browser before the dialog can open, and asks the picker. While a pick is open, another click does nothing.

Dropped files don't go through the picker. They still arrive through the components' file inputs, and [`MokaDropOverlay`](/components/drop-overlay) only takes drops. With a picker registered it also cancels clicks on its layer, which stays up for a moment after a drag ends off the page, so a stray click then can't open the browser's dialog either.

### When the picker fails

- `MokaFileUpload` and `MokaQRScanner` let the exception go, so it reaches the nearest error boundary, like an exception from `OnFilesSelected` or `OnScanned`.
- `MokaFileBrowser` shows it in its failure alert, titled "That didn't work", and the alert's retry asks the picker again. `OnFailureChanged` hears it like any failed action.

## A MAUI picker

`FilePicker.Default` wrapped into `IBrowserFile`. The picked file is read from disk, so its stream seeks, which a browser file's doesn't. Put both classes in your MAUI app project:

```csharp
using Microsoft.AspNetCore.Components.Forms;
using Moka.Red.Core.Files;

namespace MyApp;

public sealed class MauiFilePicker : IMokaFilePicker
{
    private static readonly Dictionary<string, string[]> Extensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/*"] = [".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp", ".svg"],
        ["video/*"] = [".mp4", ".mov", ".m4v", ".webm", ".mkv", ".avi"],
        ["audio/*"] = [".mp3", ".wav", ".m4a", ".aac", ".ogg", ".flac"],
        ["text/*"] = [".txt", ".csv", ".md", ".log"],
        ["application/pdf"] = [".pdf"],
        ["application/json"] = [".json"],
        ["application/zip"] = [".zip"],
        ["text/plain"] = [".txt"],
        ["text/csv"] = [".csv"]
    };

    public async Task<IReadOnlyList<IBrowserFile>> PickAsync(MokaFilePickRequest request, CancellationToken cancellationToken)
    {
        PickOptions options = new() { FileTypes = FileTypes(request) };
        IReadOnlyList<FileResult> picked = await MainThread.InvokeOnMainThreadAsync(() => ShowAsync(request.Multiple, options));
        cancellationToken.ThrowIfCancellationRequested();
        return [.. picked.Select(result => new MauiPickedFile(result))];
    }

    private static async Task<IReadOnlyList<FileResult>> ShowAsync(bool multiple, PickOptions options)
    {
        // The click reached .NET inside a WebView event, and a dialog shown inside one can misbehave on Windows,
        // so the dialog opens once that event has returned.
        await Task.Yield();
        if (multiple)
        {
            IEnumerable<FileResult?>? many = await FilePicker.Default.PickMultipleAsync(options);
            return [.. (many ?? []).OfType<FileResult>()];
        }

        FileResult? one = await FilePicker.Default.PickAsync(options);
        return one is null ? [] : [one];
    }

    // Null shows every file. A token the platform can't name does that too: the component checks each file anyway,
    // and filtering by the other tokens alone would hide files it takes.
    private static FilePickerFileType? FileTypes(MokaFilePickRequest request)
    {
        IReadOnlyList<string> accepted = request.AcceptedTypes();
        List<string> types = [];
        foreach (string token in accepted)
        {
            IEnumerable<string>? platform = PlatformTypes(token);
            if (platform is null)
            {
                return null;
            }

            types.AddRange(platform);
        }

        if (types.Count == 0)
        {
            return null;
        }

        return new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
        {
            [DeviceInfo.Current.Platform] = types.Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
        });
    }

    // Windows filters by extension, Android by MIME type, iOS and Mac Catalyst by uniform type identifier.
    private static IEnumerable<string>? PlatformTypes(string token)
    {
#if WINDOWS
        return token.StartsWith('.') ? [token] : Extensions.GetValueOrDefault(token);
#elif ANDROID
        if (!token.StartsWith('.'))
        {
            return [token];
        }

        string? mime = Android.Webkit.MimeTypeMap.Singleton?.GetMimeTypeFromExtension(token[1..].ToLowerInvariant());
        return mime is null ? null : [mime];
#elif IOS || MACCATALYST
        string? identifier = token switch
        {
            "image/*" => "public.image",
            "video/*" => "public.movie",
            "audio/*" => "public.audio",
            "text/*" => "public.text",
            _ when token.StartsWith('.') => UniformTypeIdentifiers.UTType.CreateFromExtension(token[1..])?.Identifier,
            _ => UniformTypeIdentifiers.UTType.CreateFromMimeType(token)?.Identifier
        };
        return identifier is null ? null : [identifier];
#else
        return null;
#endif
    }
}

public sealed class MauiPickedFile(FileResult result) : IBrowserFile
{
    private readonly FileInfo _file = new(result.FullPath);

    public string Name => result.FileName;

    public DateTimeOffset LastModified => _file.LastWriteTimeUtc;

    public long Size => _file.Length;

    public string ContentType => string.IsNullOrEmpty(result.ContentType) ? "application/octet-stream" : result.ContentType;

    public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default)
    {
        if (Size > maxAllowedSize)
        {
            throw new IOException($"{Name} is {Size} bytes, more than the {maxAllowedSize} allowed.");
        }

        return new FileStream(_file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous);
    }
}
```

The `Extensions` table only serves Windows, which can't filter by MIME type. Add the types your app takes.

Then register it in `MauiProgram`:

```csharp
using Moka.Red.Core.Extensions;
using Moka.Red.Extensions;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        MauiAppBuilder builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

        builder.Services.AddMauiBlazorWebView();
        builder.Services.AddMokaRed();
        builder.Services.AddMokaFilePicker<MauiFilePicker>();

        return builder.Build();
    }
}
```

A Blazor Server or WebAssembly app has only the page's file input, so it registers no picker.

## In tests

A picker that hands back fixed files lets a test pick without a browser:

```csharp
public sealed class ChosenFiles(params IBrowserFile[] files) : IMokaFilePicker
{
    public List<MokaFilePickRequest> Requests { get; } = [];

    public Task<IReadOnlyList<IBrowserFile>> PickAsync(MokaFilePickRequest request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        return Task.FromResult<IReadOnlyList<IBrowserFile>>(files);
    }
}

public sealed class MemoryFile(string name, byte[] content, string contentType = "application/octet-stream") : IBrowserFile
{
    public string Name => name;

    public DateTimeOffset LastModified => DateTimeOffset.UnixEpoch;

    public long Size => content.Length;

    public string ContentType => contentType;

    public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default) =>
        new MemoryStream(content, writable: false);
}
```

```csharp
ChosenFiles picker = new(new MemoryFile("report.pdf", "%PDF"u8.ToArray(), "application/pdf"));
var cut = Render<MokaFileUpload>(p => p.Add(x => x.FilePicker, picker));

cut.Find(".moka-fileupload-dropzone").Click();
```
