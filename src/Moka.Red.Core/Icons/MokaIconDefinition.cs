namespace Moka.Red.Core.Icons;

/// <summary>
///     Lightweight icon definition carrying an SVG path and metadata.
///     Used by components to render icons without depending on the full icon package.
/// </summary>
public readonly struct MokaIconDefinition : IEquatable<MokaIconDefinition>
{
	/// <summary>Icon name for identification and CSS class generation.</summary>
	public string Name { get; }

	/// <summary>SVG path data (the "d" attribute of a path element).</summary>
	public string SvgPath { get; }

	/// <summary>SVG viewBox. Defaults to "0 0 24 24".</summary>
	public string ViewBox { get; }

	/// <summary>
	///     True when the path should be painted with <c>fill="currentColor"</c> and no stroke.
	///     Distinguishes solid glyphs (Star, Heart) from their outline twins, which are otherwise
	///     identical path data.
	/// </summary>
	public bool Filled { get; }

	/// <summary>Creates an icon definition with the given name, SVG path, and optional viewBox.</summary>
	public MokaIconDefinition(string name, string svgPath, string viewBox = "0 0 24 24", bool filled = false)
	{
		Name = name;
		SvgPath = svgPath;
		ViewBox = viewBox;
		Filled = filled;
	}

	/// <summary>Creates an icon definition from a custom icon name with no built-in SVG.</summary>
	public static MokaIconDefinition FromString(string name) => new(name, string.Empty);

	/// <summary>Implicit conversion from string (for custom icon names with no built-in SVG).</summary>
	public static implicit operator MokaIconDefinition(string name) => FromString(name);

	/// <inheritdoc />
	public bool Equals(MokaIconDefinition other) =>
		string.Equals(Name, other.Name, StringComparison.Ordinal) && Filled == other.Filled;

	/// <inheritdoc />
	public override bool Equals(object? obj) => obj is MokaIconDefinition other && Equals(other);

	/// <inheritdoc />
	// Name is null on default(MokaIconDefinition) because the struct has no field initializers,
	// so this must not dereference it.
	public override int GetHashCode() => HashCode.Combine(Name is null ? 0 : Name.GetHashCode(StringComparison.Ordinal), Filled);

	/// <summary>Equality operator.</summary>
	public static bool operator ==(MokaIconDefinition left, MokaIconDefinition right) => left.Equals(right);

	/// <summary>Inequality operator.</summary>
	public static bool operator !=(MokaIconDefinition left, MokaIconDefinition right) => !left.Equals(right);
}
