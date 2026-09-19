namespace Moka.Red.Tests.Shared;

/// <summary>
///     Splits a style attribute or a declaration list the way a browser does, so a test can check that a
///     value it passed added no declarations of its own. Linked into the test projects that need it.
/// </summary>
internal static class CssDeclarations
{
	/// <summary>
	///     The property names in <paramref name="style" />, in order. Semicolons split declarations only
	///     outside strings, parentheses and escapes, and a string ends at a newline, as it does in CSS.
	/// </summary>
	public static List<string> PropertyNames(string style)
	{
		List<string> names = [];
		int depth = 0;
		char quote = '\0';
		int start = 0;
		for (int i = 0; i < style.Length; i++)
		{
			char c = style[i];
			if (c == '\\')
			{
				i++;
			}
			else if (quote != '\0')
			{
				if (c == quote || c is '\n' or '\r' or '\f')
				{
					quote = '\0';
				}
			}
			else if (c is '"' or '\'')
			{
				quote = c;
			}
			else if (c == '(')
			{
				depth++;
			}
			else if (c == ')')
			{
				// An unmatched parenthesis is an ordinary token, it opens nothing.
				depth = Math.Max(0, depth - 1);
			}
			else if (c == ';' && depth == 0)
			{
				AddName(names, style[start..i]);
				start = i + 1;
			}
		}

		AddName(names, style[start..]);
		return names;
	}

	private static void AddName(List<string> names, string declaration)
	{
		int colon = declaration.IndexOf(':', StringComparison.Ordinal);
		if (colon > 0)
		{
			names.Add(declaration[..colon].Trim());
		}
	}
}
