namespace Moka.Red.Forms.FileUpload;

/// <summary>
///     The app's report on one file's upload, which a <see cref="MokaFileUpload" /> asks for through
///     <see cref="MokaFileUpload.Status" /> every time it draws.
/// </summary>
/// <param name="State">Where the upload stands.</param>
/// <param name="Percent">How far it has got, from 0 to 100. Read while <see cref="MokaUploadState.Uploading" />.</param>
/// <param name="Message">A short line about it, such as why it failed. Shown in place of the size when set.</param>
public sealed record MokaUploadStatus(MokaUploadState State, double Percent = 0, string? Message = null);
