namespace Moka.Red.Core.Utilities;

/// <summary>
///     Fluent builder for composing inline CSS style strings.
///     Uses inline storage for up to 8 styles to avoid List allocation in the common case.
/// </summary>
/// <remarks>
///     A value given with a property name is checked with <see cref="CssValues.IsSelfContained" /> and left
///     out when it could end its declaration or run into the next one, so a parameter cannot add
///     declarations of its own. The raw <see cref="AddStyle(string?)" /> overload is written as it is: it
///     carries style text the consumer wrote, such as a component's <c>Style</c> parameter.
/// </remarks>
public sealed class StyleBuilder
{
	private const int InlineCapacity = 8;

	private readonly string?[] _inline = new string?[InlineCapacity];
	private int _count;
	private List<string>? _overflow;

	/// <summary>
	///     Adds <c>property: value</c>. A null or blank value is skipped, and so is a value that fails
	///     <see cref="CssValues.IsSelfContained" />.
	/// </summary>
	/// <param name="property">The CSS property name, such as <c>width</c> or <c>--moka-swatch-size</c>.</param>
	/// <param name="value">The value, often straight from a component parameter.</param>
	/// <returns>This builder.</returns>
	public StyleBuilder AddStyle(string property, string? value)
	{
		if (!string.IsNullOrWhiteSpace(value) && CssValues.IsSelfContained(value))
		{
			Append($"{property}: {value}");
		}

		return this;
	}

	/// <summary>
	///     Adds <c>property: value</c> when <paramref name="when" /> is true, with the same checks as
	///     <see cref="AddStyle(string, string?)" />.
	/// </summary>
	/// <param name="property">The CSS property name.</param>
	/// <param name="value">The value.</param>
	/// <param name="when">Whether to add the declaration at all.</param>
	/// <returns>This builder.</returns>
	public StyleBuilder AddStyle(string property, string? value, bool when)
	{
		if (when && !string.IsNullOrWhiteSpace(value) && CssValues.IsSelfContained(value))
		{
			Append($"{property}: {value}");
		}

		return this;
	}

	/// <summary>
	///     Adds style text as it is, minus any trailing semicolons. Meant for the consumer's own
	///     <c>Style</c> parameter, which may hold several declarations. Nothing is checked.
	/// </summary>
	/// <param name="rawStyle">One or more declarations. Null or blank is skipped.</param>
	/// <returns>This builder.</returns>
	public StyleBuilder AddStyle(string? rawStyle)
	{
		if (!string.IsNullOrWhiteSpace(rawStyle))
		{
			Append(rawStyle.TrimEnd(';'));
		}

		return this;
	}

	/// <summary>The declarations joined with <c>"; "</c>, or <c>null</c> when none were added.</summary>
	/// <returns>The style attribute value.</returns>
	public string? Build()
	{
		if (_count == 0)
		{
			return null;
		}

		if (_count == 1)
		{
			return _inline[0];
		}

		if (_overflow is null)
		{
			return string.Join("; ", _inline.AsSpan(0, _count));
		}

		return string.Join("; ", EnumerateAll());
	}

	/// <summary>The same as <see cref="Build" />, with an empty string for no declarations.</summary>
	/// <returns>The style attribute value.</returns>
	public override string ToString() => Build() ?? string.Empty;

	private void Append(string value)
	{
		if (_count < InlineCapacity)
		{
			_inline[_count++] = value;
		}
		else
		{
			_overflow ??= [];
			_overflow.Add(value);
			_count++;
		}
	}

	private IEnumerable<string> EnumerateAll()
	{
		for (int i = 0; i < InlineCapacity; i++)
		{
			yield return _inline[i]!;
		}

		foreach (string s in _overflow!)
		{
			yield return s;
		}
	}
}
