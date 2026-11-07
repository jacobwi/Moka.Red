using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Moka.Red.Core.Base;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Utilities;
using Moka.Red.Forms.FileUpload;
using Moka.Red.Icons;

namespace Moka.Red.Forms.DropOverlay;

/// <summary>
///     A layer over the whole window that appears while files are dragged over it and takes the drop. Put one on a
///     page, or in a layout, and the window becomes the drop target. Window listeners exist only while it is on the
///     page and not disabled. For a drop zone inside a form, use <see cref="MokaFileUpload" />.
/// </summary>
public partial class MokaDropOverlay : MokaVisualComponentBase
{
	private const string ModulePath = "./_content/Moka.Red.Forms/DropOverlay/MokaDropOverlay.razor.js";

	private ElementReference _root;
	private bool _bound;
	private bool _handingOver;
	private int _inputKey;

	/// <summary>
	///     Accepted file types (e.g., "image/*", ".conf,.zip"). A drop skips the browser's file dialog, so every file
	///     is checked against it here, and a file of another type goes to <see cref="OnRejected" />.
	/// </summary>
	[Parameter]
	public string? Accept { get; set; }

	/// <summary>Whether a drop can bring several files. Off takes the first and rejects the rest. Default true.</summary>
	[Parameter]
	public bool Multiple { get; set; } = true;

	/// <summary>The most files one drop hands over when <see cref="Multiple" /> is on. Default 10.</summary>
	[Parameter]
	public int MaxFiles { get; set; } = 10;

	/// <summary>The largest file accepted, in bytes. Default 10 MB.</summary>
	[Parameter]
	public long MaxFileSize { get; set; } = 10 * 1024 * 1024;

	/// <summary>
	///     The files a drop brought that passed every check. Not raised when none did. Read them before the callback
	///     returns: the layer then swaps its file input for a fresh one, and a file can only be read through the input
	///     that took it. The callback may read them for as long as it runs, even if the overlay is disabled meanwhile.
	/// </summary>
	[Parameter]
	public EventCallback<IReadOnlyList<IBrowserFile>> OnFiles { get; set; }

	/// <summary>The files a drop brought that failed a check, with the reason. Not raised when none did.</summary>
	[Parameter]
	public EventCallback<IReadOnlyList<MokaRejectedFile>> OnRejected { get; set; }

	/// <summary>The large line in the middle of the layer. Default "Drop files here".</summary>
	[Parameter]
	public string Title { get; set; } = "Drop files here";

	/// <summary>A short line under the title, such as the types it takes, drawn in the monospace font.</summary>
	[Parameter]
	public string? Hint { get; set; }

	/// <summary>The icon above the title. Default <see cref="MokaIcons.Action.Upload" />.</summary>
	[Parameter]
	public MokaIconDefinition Icon { get; set; } = MokaIcons.Action.Upload;

	/// <summary>Replaces the icon, title and hint inside the panel.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-drop-overlay";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass(Class)
		.Build();

	// The root covers the window and draws nothing, so the panel takes the padding and the radius.

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin)
		.AddStyle(Style)
		.Build();

	private string? PanelStyle => new StyleBuilder()
		.AddStyle("padding", ResolvedPadding)
		.AddStyle("border-radius", ResolvedRounding)
		.Build();

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		bool wanted = !Disabled;
		if (wanted == _bound)
		{
			return;
		}

		_bound = wanted;
		await SafeModuleInvokeVoidAsync(ModulePath, wanted ? "bindDropOverlay" : "unbindDropOverlay", _root);
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		if (_bound)
		{
			_bound = false;
			await SafeModuleInvokeVoidAsync(ModulePath, "unbindDropOverlay", _root);
		}

		await base.DisposeAsyncCore();
	}

	private async Task HandleChange(InputFileChangeEventArgs e)
	{
		if (Disabled)
		{
			return;
		}

		// The files are read through the input that took them, so it stays on the page until the callbacks are done,
		// even if the parent disables the overlay meanwhile (MokaArk disabled it while an upload ran, and the next
		// file's read failed with no input to read from).
		_handingOver = true;
		try
		{
			await HandOverAsync(e);
		}
		finally
		{
			_handingOver = false;

			// Chrome sends no change for a drop of the files the input already holds, so a new input takes the next
			// drop.
			_inputKey++;
		}
	}

	private async Task HandOverAsync(InputFileChangeEventArgs e)
	{
		List<IBrowserFile> accepted = [];
		List<MokaRejectedFile> rejected = [];
		int limit = Multiple ? Math.Max(0, MaxFiles) : 1;

		// GetMultipleFiles throws past its limit, so the limit is the file count.
		foreach (IBrowserFile file in e.GetMultipleFiles(e.FileCount))
		{
			if (!MokaFileTypes.IsAccepted(file, Accept))
			{
				rejected.Add(new MokaRejectedFile(file, MokaFileRejection.Type));
			}
			else if (file.Size > MaxFileSize)
			{
				rejected.Add(new MokaRejectedFile(file, MokaFileRejection.Size));
			}
			else if (accepted.Count >= limit)
			{
				rejected.Add(new MokaRejectedFile(file, MokaFileRejection.Count));
			}
			else
			{
				accepted.Add(file);
			}
		}

		if (accepted.Count > 0)
		{
			await OnFiles.InvokeAsync(accepted.AsReadOnly());
		}

		if (rejected.Count > 0)
		{
			await OnRejected.InvokeAsync(rejected.AsReadOnly());
		}
	}
}
