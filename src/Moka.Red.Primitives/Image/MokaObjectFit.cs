namespace Moka.Red.Primitives.Image;

/// <summary>
///     Controls how image content is resized to fit its container.
///     Maps to CSS object-fit values.
/// </summary>
public enum MokaObjectFit
{
	/// <summary>Image covers the container, cropping as needed.</summary>
	Cover,

	/// <summary>Image is contained within the container, preserving aspect ratio.</summary>
	Contain,

	/// <summary>Image stretches to fill the container.</summary>
	Fill,

	/// <summary>Image renders at its natural size.</summary>
	None
}
