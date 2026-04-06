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
	private readonly string _inputId = $"moka-fileupload-{Guid.NewGuid():N}";
	private bool _isDragging;

	/// <summary>Label text displayed above the upload area.</summary>
	[Parameter]
	public string? Label { get; set; }

	/// <summary>Helper text displayed below the upload area.</summary>
	[Parameter]
	public string? HelperText { get; set; }

	/// <summary>Error text displayed below the upload area when in error state.</summary>
	[Parameter]
	public string? ErrorText { get; set; }

	/// <summary>Accepted file types (e.g., "image/*", ".pdf,.docx").</summary>
	[Parameter]
	public string? Accept { get; set; }

	/// <summary>Whether to allow multiple file selection. Default is false.</summary>
	[Parameter]
	public bool Multiple { get; set; }

	/// <summary>Maximum file size in bytes. Default is 10 MB.</summary>
	[Parameter]
	public long MaxFileSize { get; set; } = 10 * 1024 * 1024;

	/// <summary>Maximum number of files allowed. Default is 10.</summary>
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

	/// <summary>Whether to enable drag-and-drop zone. Default is true.</summary>
	[Parameter]
	public bool DragDrop { get; set; } = true;

	/// <inheritdoc />
	protected override string RootClass => "moka-fileupload";

	private string DropzoneCssClass => new CssBuilder("moka-fileupload-dropzone")
		.AddClass("moka-fileupload-dropzone--dragging", _isDragging)
		.AddClass("moka-fileupload-dropzone--compact", Compact)
		.AddClass("moka-fileupload-dropzone--disabled", Disabled)
		.Build();

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	private async Task HandleFileSelected(InputFileChangeEventArgs e)
	{
		_errors.Clear();

		IReadOnlyList<IBrowserFile> files = e.GetMultipleFiles(MaxFiles);

		foreach (IBrowserFile file in files)
		{
			if (file.Size > MaxFileSize)
			{
				_errors.Add($"{file.Name} exceeds maximum size ({FormatSize(MaxFileSize)})");
				continue;
			}

			if (!Multiple)
			{
				_files.Clear();
			}

			_files.Add(file);
		}

		if (OnFilesSelected.HasDelegate)
		{
			await OnFilesSelected.InvokeAsync(_files.AsReadOnly());
		}
	}

	private async Task RemoveFile(IBrowserFile file)
	{
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

	private void HandleDragEnter()
	{
		if (!Disabled)
		{
			_isDragging = true;
		}
	}

	private void HandleDragLeave() => _isDragging = false;

	/// <summary>Formats a byte count into a human-readable size string.</summary>
	private static string FormatSize(long bytes) => bytes switch
	{
		< 1024 => $"{bytes} B",
		< 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
		< 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024.0):F1} MB",
		_ => $"{bytes / (1024.0 * 1024.0 * 1024.0):F1} GB"
	};
}
