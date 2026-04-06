namespace Moka.Red.Layout.Resizable;

/// <summary>Event data provided when a resize operation completes.</summary>
/// <param name="Width">The new width in pixels.</param>
/// <param name="Height">The new height in pixels.</param>
public sealed record MokaResizeResult(double Width, double Height);
