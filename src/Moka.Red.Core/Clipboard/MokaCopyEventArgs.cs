namespace Moka.Red.Core.Clipboard;

/// <summary>
///     The arguments of <c>@onmokacopied</c>: how a copy the browser made inside a click went. This package's click
///     listener copies for any element carrying <c>data-moka-copy</c> (the text), <c>data-moka-copy-from</c> (a selector
///     for the element or elements holding it) or <c>data-moka-copy-ask</c> (the text comes from .NET through
///     <c>@onmokacopyask</c>), and the element raises this once the clipboard has answered.
/// </summary>
public sealed class MokaCopyEventArgs : EventArgs
{
	/// <summary>Whether the text reached the clipboard. False when the browser refused it.</summary>
	public bool Copied { get; set; }
}
