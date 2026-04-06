namespace Moka.Red.Core.Utilities;

/// <summary>
///     Fluent builder for composing CSS class strings. Skips null/whitespace values.
///     Uses inline storage for up to 8 classes to avoid List allocation in the common case.
/// </summary>
public sealed class CssBuilder
{
	private const int InlineCapacity = 8;

	private readonly string?[] _inline = new string?[InlineCapacity];
	private int _count;
	private List<string>? _overflow;

	public CssBuilder(string? defaultClass = null)
	{
		if (!string.IsNullOrWhiteSpace(defaultClass))
		{
			_inline[0] = defaultClass;
			_count = 1;
		}
	}

	public CssBuilder AddClass(string? value)
	{
		if (!string.IsNullOrWhiteSpace(value))
		{
			Append(value);
		}

		return this;
	}

	public CssBuilder AddClass(string? value, bool when)
	{
		if (when && !string.IsNullOrWhiteSpace(value))
		{
			Append(value);
		}

		return this;
	}

	public CssBuilder AddClass(string? value, Func<bool> when)
	{
		ArgumentNullException.ThrowIfNull(when);

		if (when() && !string.IsNullOrWhiteSpace(value))
		{
			Append(value);
		}

		return this;
	}

	public string Build()
	{
		if (_count == 0)
		{
			return string.Empty;
		}

		if (_count == 1)
		{
			return _inline[0]!;
		}

		if (_overflow is null)
		{
			// Fast path: all classes in inline array — use Span join
			return string.Join(' ', _inline.AsSpan(0, _count));
		}

		// Slow path: overflow list exists
		return string.Join(' ', EnumerateAll());
	}

	public override string ToString() => Build();

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
