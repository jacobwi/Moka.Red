namespace Moka.Red.Core.Utilities;

/// <summary>
///     Fluent builder for composing inline CSS style strings.
///     Uses inline storage for up to 8 styles to avoid List allocation in the common case.
/// </summary>
public sealed class StyleBuilder
{
	private const int InlineCapacity = 8;

	private readonly string?[] _inline = new string?[InlineCapacity];
	private int _count;
	private List<string>? _overflow;

	public StyleBuilder AddStyle(string property, string? value)
	{
		if (!string.IsNullOrWhiteSpace(value))
		{
			Append($"{property}: {value}");
		}

		return this;
	}

	public StyleBuilder AddStyle(string property, string? value, bool when)
	{
		if (when && !string.IsNullOrWhiteSpace(value))
		{
			Append($"{property}: {value}");
		}

		return this;
	}

	public StyleBuilder AddStyle(string? rawStyle)
	{
		if (!string.IsNullOrWhiteSpace(rawStyle))
		{
			Append(rawStyle.TrimEnd(';'));
		}

		return this;
	}

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
