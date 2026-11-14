using System.Collections;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;

namespace Moka.Red.Core.Theming;

// The tokens a theme holds at values of its own. Equal when they pin the same tokens to the same values, so two themes
// built the same way compare equal: a record compares a dictionary by reference.
internal sealed class MokaPinnedTokens : IReadOnlyDictionary<string, string>, IEquatable<MokaPinnedTokens>
{
	private readonly FrozenDictionary<string, string> _pins;

	private MokaPinnedTokens(FrozenDictionary<string, string> pins) => _pins = pins;

	public static MokaPinnedTokens Empty { get; } = new(FrozenDictionary<string, string>.Empty);

	/// <inheritdoc />
	public int Count => _pins.Count;

	/// <inheritdoc />
	public IEnumerable<string> Keys => _pins.Keys;

	/// <inheritdoc />
	public IEnumerable<string> Values => _pins.Values;

	/// <inheritdoc />
	public string this[string key] => _pins[key];

	/// <inheritdoc />
	public bool ContainsKey(string key) => _pins.ContainsKey(key);

	/// <inheritdoc />
	public bool TryGetValue(string key, [MaybeNullWhen(false)] out string value) => _pins.TryGetValue(key, out value);

	/// <inheritdoc />
	public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => ((IEnumerable<KeyValuePair<string, string>>)_pins).GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	/// <inheritdoc />
	public bool Equals(MokaPinnedTokens? other)
	{
		if (other is null || Count != other.Count)
		{
			return false;
		}

		foreach ((string key, string value) in _pins)
		{
			if (!other.TryGetValue(key, out string? theirs) || !string.Equals(value, theirs, StringComparison.Ordinal))
			{
				return false;
			}
		}

		return true;
	}

	/// <inheritdoc />
	public override bool Equals(object? obj) => Equals(obj as MokaPinnedTokens);

	/// <inheritdoc />
	public override int GetHashCode()
	{
		int hash = Count;
		foreach ((string key, string value) in _pins)
		{
			hash ^= HashCode.Combine(StringComparer.Ordinal.GetHashCode(key), StringComparer.Ordinal.GetHashCode(value));
		}

		return hash;
	}

	internal MokaPinnedTokens With(string name, string value)
	{
		Dictionary<string, string> pins = new(_pins, StringComparer.Ordinal) { [name] = value };
		return new MokaPinnedTokens(pins.ToFrozenDictionary(StringComparer.Ordinal));
	}

	internal MokaPinnedTokens Without(string name)
	{
		if (!_pins.ContainsKey(name))
		{
			return this;
		}

		Dictionary<string, string> pins = new(_pins, StringComparer.Ordinal);
		pins.Remove(name);
		return pins.Count == 0 ? Empty : new MokaPinnedTokens(pins.ToFrozenDictionary(StringComparer.Ordinal));
	}
}
