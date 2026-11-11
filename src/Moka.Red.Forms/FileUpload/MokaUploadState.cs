namespace Moka.Red.Forms.FileUpload;

/// <summary>Where the app's upload of one file stands.</summary>
public enum MokaUploadState
{
	/// <summary>Chosen, and waiting its turn.</summary>
	Waiting,

	/// <summary>On its way. <see cref="MokaUploadStatus.Percent" /> says how far.</summary>
	Uploading,

	/// <summary>Uploaded.</summary>
	Done,

	/// <summary>Didn't make it. <see cref="MokaUploadStatus.Message" /> can say why, and Retry shows when the upload has an <c>OnRetry</c>.</summary>
	Failed
}
