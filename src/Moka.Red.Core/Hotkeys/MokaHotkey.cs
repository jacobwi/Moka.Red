namespace Moka.Red.Core.Hotkeys;

/// <summary>
///     One keyboard shortcut and what it does. Register it with <see cref="IMokaHotkeyService" /> and the
///     shortcut works wherever focus is, and shows up in anything that lists the app's shortcuts.
/// </summary>
public sealed record MokaHotkey
{
	/// <summary>
	///     Identifies the hotkey, for <see cref="IMokaHotkeyService.Unregister" /> and for replacing it. Defaults
	///     to a new value, so a hotkey that is registered once and disposed never needs one.
	/// </summary>
	public string Id { get; init; } = Guid.NewGuid().ToString("N");

	/// <summary>
	///     The keys, written the way a hint is: modifiers then one key, joined by <c>+</c>, as in "Ctrl+K" or
	///     "Mod+Shift+P". <c>Mod</c> is Ctrl, or Cmd on a Mac. Null, empty, or a modifier the browser side does
	///     not know turns the hotkey off (with a console warning), rather than binding the bare key.
	/// </summary>
	public string? Keys { get; init; }

	/// <summary>What the hotkey does, in the words a shortcut list should show. Required for it to be listed.</summary>
	public string? Description { get; init; }

	/// <summary>Heading this hotkey is listed under, such as "Navigation". Null groups it under "General".</summary>
	public string? Group { get; init; }

	/// <summary>
	///     Whether the hotkey still fires while focus is in a text field. Default false, which matters only for a
	///     shortcut that types a character (no Ctrl, Cmd or Alt, such as "/" or "Shift+K"): one that cannot be
	///     typed is never in a field's way and always fires.
	/// </summary>
	public bool AllowInInput { get; init; }

	/// <summary>
	///     Whether a matching press has its default action cancelled. Default true. Turn it off for a shortcut
	///     that should also do whatever the browser does with it.
	/// </summary>
	public bool PreventDefault { get; init; } = true;

	/// <summary>Runs when the hotkey fires. Takes precedence over <see cref="OnInvokeSync" />.</summary>
	public Func<Task>? OnInvoke { get; init; }

	/// <summary>Runs when the hotkey fires and <see cref="OnInvoke" /> is not set.</summary>
	public Action? OnInvokeSync { get; init; }

	/// <summary>Whether this hotkey has keys to bind and something to run.</summary>
	internal bool IsBindable =>
		!string.IsNullOrWhiteSpace(Keys) && (OnInvoke is not null || OnInvokeSync is not null);
}
