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

	/// <summary>Creates an icon definition with the given name, SVG path, and optional viewBox.</summary>
	public MokaIconDefinition(string name, string svgPath, string viewBox = "0 0 24 24")
	{
		Name = name;
		SvgPath = svgPath;
		ViewBox = viewBox;
	}

	/// <summary>Creates an icon definition from a custom icon name with no built-in SVG.</summary>
	public static MokaIconDefinition FromString(string name) => new(name, string.Empty);

	/// <summary>Implicit conversion from string (for custom icon names with no built-in SVG).</summary>
	public static implicit operator MokaIconDefinition(string name) => FromString(name);

	/// <inheritdoc />
	public bool Equals(MokaIconDefinition other) => Name == other.Name;

	/// <inheritdoc />
	public override bool Equals(object? obj) => obj is MokaIconDefinition other && Equals(other);

	/// <inheritdoc />
	public override int GetHashCode() => Name.GetHashCode(StringComparison.Ordinal);

	/// <summary>Equality operator.</summary>
	public static bool operator ==(MokaIconDefinition left, MokaIconDefinition right) => left.Equals(right);

	/// <summary>Inequality operator.</summary>
	public static bool operator !=(MokaIconDefinition left, MokaIconDefinition right) => !left.Equals(right);
}
