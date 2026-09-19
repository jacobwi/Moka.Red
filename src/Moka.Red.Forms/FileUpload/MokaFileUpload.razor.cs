using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Forms.FileUpload;

/// <summary>
///     A file upload component with drag-and-drop support.
///     Uses Blazor's built-in <see cref="InputFile" /> component and HTML5 drag events.
/// </summary>
public partial class MokaFileUpload
{
	private readonly List<string> _errors = [];
	private readonly List<IBrowserFile> _files = [];
	private readonly string _generatedId = $"moka-fileupload-{Guid.NewGuid():N}";

	// dragenter and dragleave also fire when the pointer crosses from the zone's border onto the
	// input that covers it, so the zone counts them rather than trusting the last one.
	private int _dragDepth;

	// The consumer's Id names the file input, which also takes the unmatched attributes: it is the
	// control, and attributes such as capture, name or aria-describedby only work there. The labels
	// that open it follow the Id.
	private string InputId => string.IsNullOrEmpty(Id) ? _generatedId : Id;

	/// <summary>Label text displayed above the upload area.</summary>
	[Parameter]
	public string? Label { get; set; }

	/// <summary>Helper text displayed below the upload area.</summary>
	[Parameter]
	public string? HelperText { get; set; }

	/// <summary>Error text displayed below the upload area when in error state.</summary>
	[Parameter]
	public string? ErrorText { get; set; }

	/// <summary>
	///     Accepted file types (e.g., "image/*", ".pdf,.docx"). Dropped files are checked against it too,
	///     and a file of another type is listed as an error.
	/// </summary>
	[Parameter]
	public string? Accept { get; set; }

	/// <summary>Whether to allow multiple file selection. Default is false.</summary>
	[Parameter]
	public bool Multiple { get; set; }

	/// <summary>Maximum file size in bytes. Default is 10 MB.</summary>
	[Parameter]
	public long MaxFileSize { get; set; } = 10 * 1024 * 1024;

	/// <summary>
	///     Maximum number of files in the list when <see cref="Multiple" /> is on, across every pick and
	///     drop. Default is 10.
	/// </summary>
	[Parameter]
	public int MaxFiles { get; set; } = 10;

	/// <summary>Callback invoked when files are selected or changed.</summary>
	[Parameter]
	public EventCallback<IReadOnlyList<IBrowserFile>> OnFilesSelected { get; set; }

	/// <summary>Callback invoked when a file is removed from the list.</summary>
	[Parameter]
	public EventCallback<IBrowserFile> OnFileRemoved { get; set; }

	/// <summary>Whether to show the selected file list. Default is true.</summary>
	[Parameter]
	public bool ShowFileList { get; set; } = true;

	/// <summary>Whether to use compact single-line mode. Default is false.</summary>
	[Parameter]
	public bool Compact { get; set; }

	/// <summary>Whether files can be dropped on the zone. Default is true.</summary>
	[Parameter]
	public bool DragDrop { get; set; } = true;

	/// <inheritdoc />
	protected override string RootClass => "moka-fileupload";

	// A disabled upload's input takes no drop, so the file would fall through to the page, which opens it.
	private bool AcceptsDrops => DragDrop && !Disabled;

	private string DropzoneCssClass => new CssBuilder("moka-fileupload-dropzone")
		.AddClass("moka-fileupload-dropzone--dragging", _dragDepth > 0)
		.AddClass("moka-fileupload-dropzone--compact", Compact)
		.AddClass("moka-fileupload-dropzone--disabled", Disabled)
		.Build();

	// The field wrapper is the outermost element and the only one that holds everything, so it
	// takes the margin, Class and Style. The drop zone draws the box, so it takes the padding and
	// radius.

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin)
		.AddStyle(Style)
		.Build();

	private string? DropzoneStyle => new StyleBuilder()
		.AddStyle("padding", ResolvedPadding)
		.AddStyle("border-radius", ResolvedRounding)
		.Build();

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	private async Task HandleFileSelected(InputFileChangeEventArgs e)
	{
		// The input is disabled, but a change already on its way when Disabled was set gets here.
		if (Disabled)
		{
			return;
		}

		_errors.Clear();

		// A drop skips the dialog's type filter and can bring any number of files, even to a single-file
		// input, so the rules are checked here, where both ways in meet. GetMultipleFiles throws past
		// its limit, so the limit is the file count.
		IReadOnlyList<IBrowserFile> files = e.GetMultipleFiles(e.FileCount);
		if (!Multiple && files.Count > 1)
		{
			files = [files[0]];
		}

		int overLimit = 0;
		foreach (IBrowserFile file in files)
		{
			if (!IsAccepted(file))
			{
				_errors.Add($"{file.Name} is not an accepted file type");
				continue;
			}

			if (file.Size > MaxFileSize)
			{
				_errors.Add($"{file.Name} exceeds maximum size ({FormatSize(MaxFileSize)})");
				continue;
			}

			if (!Multiple)
			{
				_files.Clear();
			}
			else if (_files.Count >= MaxFiles)
			{
				overLimit++;
				continue;
			}

			_files.Add(file);
		}

		// One line, not one per file: a drop can hold thousands.
		if (overLimit > 0)
		{
			_errors.Add(overLimit == 1
				? $"1 file was not added: the limit is {MaxFiles}"
				: $"{overLimit} files were not added: the limit is {MaxFiles}");
		}

		if (OnFilesSelected.HasDelegate)
		{
			await OnFilesSelected.InvokeAsync(_files.AsReadOnly());
		}
	}

	private async Task RemoveFile(IBrowserFile file)
	{
		// The buttons are disabled, but a click already on its way when Disabled was set gets here.
		if (Disabled)
		{
			return;
		}

		_files.Remove(file);

		if (OnFileRemoved.HasDelegate)
		{
			await OnFileRemoved.InvokeAsync(file);
		}

		if (OnFilesSelected.HasDelegate)
		{
			await OnFilesSelected.InvokeAsync(_files.AsReadOnly());
		}
	}

	// Without DragDrop the zone takes no drops, so it does not light up for one.
	private void HandleDragEnter()
	{
		if (DragDrop && !Disabled)
		{
			_dragDepth++;
		}
	}

	private void HandleDragLeave() => _dragDepth = Math.Max(0, _dragDepth - 1);

	// A drop ends the drag without a dragleave.
	private void HandleDrop() => _dragDepth = 0;

	// The dialog filters by Accept, but a dropped file meets it only here. As in the browser, a token
	// is an extension (".pdf"), a MIME type ("application/pdf") or a wildcard ("image/*"), compared
	// without case, and any other token is ignored, so an Accept with no valid token takes every file.
	private bool IsAccepted(IBrowserFile file)
	{
		string contentType = file.ContentType ?? string.Empty;
		string[] types = (Accept ?? string.Empty)
			.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		bool anyValid = false;

		foreach (string type in types)
		{
			bool matches;
			if (type.StartsWith('.'))
			{
				matches = file.Name.EndsWith(type, StringComparison.OrdinalIgnoreCase);
			}
			else if (type.EndsWith("/*", StringComparison.Ordinal))
			{
				matches = type == "*/*" || contentType.StartsWith(type[..^1], StringComparison.OrdinalIgnoreCase);
			}
			else if (type.Contains('/', StringComparison.Ordinal))
			{
				matches = string.Equals(contentType, type, StringComparison.OrdinalIgnoreCase);
			}
			else
			{
				continue;
			}

			anyValid = true;
			if (matches)
			{
				return true;
			}
		}

		return !anyValid;
	}

	/// <summary>Formats a byte count into a human-readable size string.</summary>
	private static string FormatSize(long bytes) => bytes switch
	{
		< 1024 => $"{bytes} B",
		< 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
		< 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024.0):F1} MB",
		_ => $"{bytes / (1024.0 * 1024.0 * 1024.0):F1} GB"
	};
}
