namespace Moka.Red.Core.Utilities;

/// <summary>
///     Reads a keyboard shortcut hint such as <c>"Ctrl+Shift+P"</c> as the keys it names, so a component can
///     draw one key cap per key instead of a run of text.
/// </summary>
public static class ShortcutKeys
{
	/// <summary>
	///     Names the keys in a shortcut hint. Keys are separated by <c>+</c> and surrounding spaces are dropped,
	///     so <c>"Ctrl + Shift + P"</c> and <c>"Ctrl+Shift+P"</c> read the same. An empty piece between two keys
	///     is the plus key itself: <c>"Ctrl++"</c> is Ctrl and plus, not Ctrl and two separators.
	/// </summary>
	/// <param name="shortcut">The hint to read. <c>null</c> and empty name no keys.</param>
	/// <returns>The keys in the order they are written, empty when the hint names none.</returns>
	public static IReadOnlyList<string> Split(string? shortcut)
	{
		if (string.IsNullOrEmpty(shortcut))
		{
			return [];
		}

		string[] pieces = shortcut.Split('+');
		List<string> keys = new(pieces.Length);
		for (int index = 0; index < pieces.Length; index++)
		{
			string key = pieces[index].Trim();
			if (key.Length > 0)
			{
				keys.Add(key);
			}
			else if (index > 0 && pieces[index - 1].Trim().Length > 0)
			{
				keys.Add("+");
			}
		}

		return keys;
	}
}
