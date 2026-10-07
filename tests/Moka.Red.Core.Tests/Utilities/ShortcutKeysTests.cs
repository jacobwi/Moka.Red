using Moka.Red.Core.Utilities;

namespace Moka.Red.Core.Tests.Utilities;

// Shortcut hints are drawn as one key cap per key, so the context menu, the command palette and the
// search input all have to read "Ctrl+Shift+P" the same way.
public class ShortcutKeysTests
{
	[Theory]
	[InlineData("Ctrl+C", new[] { "Ctrl", "C" })]
	[InlineData("Ctrl+Shift+W", new[] { "Ctrl", "Shift", "W" })]
	[InlineData("Enter", new[] { "Enter" })]
	// Spaces around the separator are the writer's, not part of a key name.
	[InlineData("Ctrl + Shift + P", new[] { "Ctrl", "Shift", "P" })]
	// The plus key itself, which naive splitting turns into separators and loses.
	[InlineData("Ctrl++", new[] { "Ctrl", "+" })]
	[InlineData("+", new string[0])]
	[InlineData("", new string[0])]
	[InlineData(null, new string[0])]
	public void Split_NamesTheKeys(string? shortcut, string[] expected) =>
		Assert.Equal(expected, ShortcutKeys.Split(shortcut));
}
