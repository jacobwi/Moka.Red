namespace Moka.Red.Primitives.QRScanner;

/// <summary>Where a <see cref="MokaQRScanner" /> is: off, starting, scanning, or the reason the camera didn't start.</summary>
public enum MokaQRScannerState
{
	/// <summary>The camera is off.</summary>
	Off,

	/// <summary>Asking for the camera, which may wait on the person allowing it.</summary>
	Starting,

	/// <summary>The camera is on and frames are being read.</summary>
	Scanning,

	/// <summary>The person or a browser policy refused the camera.</summary>
	Denied,

	/// <summary>No camera was found.</summary>
	NoCamera,

	/// <summary>A camera exists but another app holds it.</summary>
	CameraBusy,

	/// <summary>The browser can't use a camera here, such as a page served over plain HTTP, or can't read QR codes.</summary>
	Unsupported,

	/// <summary>The camera failed to start for another reason.</summary>
	Failed
}
