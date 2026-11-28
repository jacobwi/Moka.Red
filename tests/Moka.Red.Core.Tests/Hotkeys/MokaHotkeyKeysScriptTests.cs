using System.Text.Json;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Core.Tests.Hotkeys;

/// <summary>
///     How moka-hotkeys.js reads a shortcut and matches a press, run in Node.js (<c>Script/hotkey-keys.mjs</c>). A shortcut
///     named by a punctuation mark such as <c>"?"</c> never fired, since typing it takes Shift and the shortcut didn't say
///     Shift; which keys type it depends on the layout, so it's matched by the character typed. Skipped where Node.js
///     isn't installed.
/// </summary>
public sealed class MokaHotkeyKeysScriptTests
{
	private static readonly Lazy<JsonElement?> Windows = new(() => Run("Win32"));
	private static readonly Lazy<JsonElement?> Mac = new(() => Run("MacIntel"));

	[Theory]
	[InlineData("question-us", true)]
	[InlineData("question-fr", true)]
	[InlineData("question-slash-pressed", false)]
	[InlineData("slash-us", true)]
	[InlineData("slash-de", true)]
	[InlineData("shift-question-with-shift", true)]
	[InlineData("shift-question-without-shift", false)]
	[InlineData("ctrl-question", true)]
	[InlineData("ctrl-question-without-ctrl", false)]
	public void APunctuationMark_MatchesTheCharacterTyped_WithShiftOnlyWhenNamed(string press, bool matches) =>
		Assert.Equal(matches, Matches(Windows, press));

	[Theory]
	[InlineData("letter", true)]
	[InlineData("letter-with-shift", false)]
	public void ALetter_StillNeedsItsModifiersExactly(string press, bool matches) =>
		Assert.Equal(matches, Matches(Windows, press));

	[Theory]
	[InlineData("mod-with-ctrl", true, false)]
	[InlineData("mod-with-meta", false, true)]
	public void Mod_IsCtrl_AndCmdOnAnApplePlatform(string press, bool onWindows, bool onMac)
	{
		Assert.Equal(onWindows, Matches(Windows, press));
		Assert.Equal(onMac, Matches(Mac, press));
	}

	[Fact]
	public void AHeldShortcut_RunsOnce_AndEveryRepeatIsTakenFromTheBrowser()
	{
		JsonElement? result = Windows.Value;
		Assert.SkipUnless(result is not null, "Node.js isn't installed, so the hotkey script can't be run.");
		JsonElement held = result!.Value.GetProperty("held");

		Assert.True(held.GetProperty("firstPrevented").GetBoolean());
		Assert.True(held.GetProperty("repeatPrevented").GetBoolean());
		Assert.True(held.GetProperty("repeatPrevented2").GetBoolean());
		Assert.Equal(1, held.GetProperty("runs").GetInt32());
	}

	private static JsonElement? Run(string platform) => ScriptRunner.Run(
		["tests", "Moka.Red.Core.Tests", "Hotkeys", "Script", "hotkey-keys.mjs"],
		["src", "Moka.Red.Core", "wwwroot", "moka-hotkeys.js"],
		platform);

	private static bool Matches(Lazy<JsonElement?> run, string press)
	{
		JsonElement? result = run.Value;
		Assert.SkipUnless(result is not null, "Node.js isn't installed, so the hotkey script can't be run.");
		return result!.Value.GetProperty(press).GetBoolean();
	}
}
