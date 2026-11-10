using Microsoft.AspNetCore.Components.Forms;

namespace Moka.Red.Core.Files;

/// <summary>
///     Picks files in place of the browser's file dialog, for every Moka component that would open one:
///     <c>MokaFileUpload</c>, the Upload control of <c>MokaFileBrowser</c> and the image button of
///     <c>MokaQRScanner</c>. Register one with <c>AddMokaFilePicker</c>, or hand one to a component's
///     <c>FilePicker</c> parameter. With neither, the components open the browser's dialog as they always have.
/// </summary>
/// <remarks>
///     A Blazor Hybrid app on Windows wants one: WebView2's file dialog, opened from a file input, can end the whole
///     app once it has been open for a while (WebView2Feedback #5741), and a native picker never opens it.
///     Dropped files still arrive through the components' own file inputs.
/// </remarks>
public interface IMokaFilePicker
{
	/// <summary>Asks the person for files.</summary>
	/// <param name="request">Whether several files are wanted, and which types.</param>
	/// <param name="cancellationToken">Cancelled when the component that asked goes away.</param>
	/// <returns>
	///     The files picked, or an empty list when the person cancelled. Throwing
	///     <see cref="OperationCanceledException" /> counts as cancelled too. The component checks the files as it
	///     checks the browser's, so a picker that can't filter by type may return any file.
	/// </returns>
	Task<IReadOnlyList<IBrowserFile>> PickAsync(MokaFilePickRequest request, CancellationToken cancellationToken);
}
