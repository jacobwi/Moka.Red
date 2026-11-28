using System.Text.Json;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Core.Tests.Hotkeys;

/// <summary>
///     How moka-hotkeys.js treats a press in an editable field for each <c>MokaHotkey.InFields</c>, run in Node.js over a
///     stand-in document (<c>Script/hotkey-fields.mjs</c>). A Ctrl shortcut used to fire in every field, so an app-wide
///     Ctrl+Z took a text box's own undo. Skipped where Node.js isn't installed.
/// </summary>
public sealed class MokaHotkeyFieldsScriptTests
{
	private static readonly Lazy<JsonElement?> Result = new(() => ScriptRunner.Run(
		["tests", "Moka.Red.Core.Tests", "Hotkeys", "Script", "hotkey-fields.mjs"],
		["src", "Moka.Red.Core", "wwwroot", "moka-hotkeys.js"]));

	[Theory]
	[InlineData("modifier-unless-typing", true)]
	[InlineData("modifier-never", false)]
	[InlineData("modifier-always", true)]
	[InlineData("modifier-unset", true)]
	[InlineData("typing-unless-typing", false)]
	[InlineData("typing-never", false)]
	[InlineData("typing-always", true)]
	public void InAField_AHotkeyFiresAsItsSettingSays(string hotkey, bool fires)
	{
		JsonElement result = Press(hotkey);

		Assert.Equal(fires, result.GetProperty("inField").GetBoolean());
		Assert.Equal(fires, result.GetProperty("inTextArea").GetBoolean());
	}

	[Theory]
	[InlineData("modifier-unless-typing")]
	[InlineData("modifier-never")]
	[InlineData("modifier-always")]
	[InlineData("typing-unless-typing")]
	[InlineData("typing-never")]
	[InlineData("typing-always")]
	public void OutsideAField_EveryHotkeyFires(string hotkey) =>
		Assert.True(Press(hotkey).GetProperty("onButton").GetBoolean());

	private static JsonElement Press(string hotkey)
	{
		JsonElement? result = Result.Value;
		Assert.SkipUnless(result is not null, "Node.js isn't installed, so the hotkey script can't be run.");
		return result!.Value.GetProperty(hotkey);
	}
}
