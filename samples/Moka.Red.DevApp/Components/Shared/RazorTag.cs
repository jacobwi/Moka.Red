using System.Globalization;
using System.Text;

namespace Moka.Red.DevApp.Components.Shared;

internal sealed class RazorTag(string name)
{
	private readonly StringBuilder _attributes = new();

	public RazorTag Set(string attribute, string? value)
	{
		if (value is not null)
		{
			_attributes.Append(' ').Append(attribute).Append("=\"").Append(EscapeAttribute(value)).Append('"');
		}

		return this;
	}

	public RazorTag Set<TEnum>(string attribute, TEnum value, TEnum unset) where TEnum : struct, Enum =>
		Set(attribute, EqualityComparer<TEnum>.Default.Equals(value, unset) ? null : $"{typeof(TEnum).Name}.{value}");

	public RazorTag Set<TEnum>(string attribute, TEnum? value) where TEnum : struct, Enum =>
		Set(attribute, value is { } set ? $"{typeof(TEnum).Name}.{set}" : null);

	public RazorTag Set(string attribute, double value, double unset) =>
		Set(attribute, value.Equals(unset) ? null : value.ToString(CultureInfo.InvariantCulture));

	public RazorTag Text(string attribute, string? value) =>
		Set(attribute, string.IsNullOrEmpty(value) ? null : value);

	public RazorTag Flag(string attribute, bool on)
	{
		if (on)
		{
			_attributes.Append(' ').Append(attribute);
		}

		return this;
	}

	public override string ToString() => $"<{name}{_attributes}/>";

	public string Around(string? content) =>
		string.IsNullOrEmpty(content) ? ToString() : $"<{name}{_attributes}>{EscapeText(content)}</{name}>";

	public string Around(string inner, bool multiline) =>
		multiline ? $"<{name}{_attributes}>\n\t{inner.Replace("\n", "\n\t", StringComparison.Ordinal)}\n</{name}>" : $"<{name}{_attributes}>{inner}</{name}>";

	private static string EscapeAttribute(string value) =>
		value.Replace("&", "&amp;", StringComparison.Ordinal)
			.Replace("\"", "&quot;", StringComparison.Ordinal)
			.Replace("@", "@@", StringComparison.Ordinal);

	private static string EscapeText(string value) =>
		value.Replace("&", "&amp;", StringComparison.Ordinal)
			.Replace("<", "&lt;", StringComparison.Ordinal)
			.Replace("@", "@@", StringComparison.Ordinal);
}
