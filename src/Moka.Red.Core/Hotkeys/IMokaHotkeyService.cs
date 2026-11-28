using System.Diagnostics.CodeAnalysis;

namespace Moka.Red.Core.Hotkeys;

/// <summary>
///     The app's keyboard shortcuts, in one place. Register with <c>AddMokaHotkeys()</c> and put one
///     <c>MokaHotkeyHost</c> in the layout; the host is what listens, so without it nothing fires.
///     Registering a shortcut here both binds it and lists it, so a shortcut cannot work without being
///     documented or be documented without working.
/// </summary>
[SuppressMessage("Design", "CA1003:Use generic event handler instances",
	Justification = "A parameterless Action is simpler for this lightweight state-changed event.")]
public interface IMokaHotkeyService
{
	/// <summary>Raised when the registered set changes, so the host can rebind and lists can redraw.</summary>
	event Action OnChanged;

	/// <summary>Every registered hotkey, in registration order.</summary>
	IReadOnlyList<MokaHotkey> Hotkeys { get; }

	/// <summary>
	///     Registers a hotkey, replacing any with the same <see cref="MokaHotkey.Id" />.
	/// </summary>
	/// <param name="hotkey">The hotkey.</param>
	/// <returns>
	///     A registration that unregisters the hotkey when disposed, so a component can bind one in
	///     <c>OnInitialized</c> and let it go with the component.
	/// </returns>
	IDisposable Register(MokaHotkey hotkey);

	/// <summary>Unregisters the hotkey with this id. Does nothing when there is none.</summary>
	/// <param name="id">The <see cref="MokaHotkey.Id" />.</param>
	void Unregister(string id);

	/// <summary>
	///     Runs the hotkey with this id, as a press of its keys would. Lets a command list or a menu item run the
	///     same action the shortcut does without duplicating it.
	/// </summary>
	/// <param name="id">The <see cref="MokaHotkey.Id" />.</param>
	/// <returns>A task that completes when the hotkey's action has run.</returns>
	Task InvokeAsync(string id);

	/// <summary>
	///     Whether a <c>MokaHotkeyHost</c> is listening, so a registered hotkey fires. A component with a shortcut of its
	///     own can register it here while this is true and keep its own listener otherwise, so it works in an app with
	///     no host. An implementation that doesn't track hosts answers false.
	/// </summary>
	bool IsListening => false;

	/// <summary>Raised when <see cref="IsListening" /> changes.</summary>
	event Action? ListeningChanged
	{
		add { }
		remove { }
	}

	/// <summary>
	///     Tells the registry a listener is bound. <c>MokaHotkeyHost</c> calls it once its listener is in the page and
	///     disposes the result when it goes, so <see cref="IsListening" /> stays true while any host is.
	/// </summary>
	/// <returns>A registration that takes the listener away when disposed.</returns>
	IDisposable AddListener() => MokaHotkeyNoListener.Instance;
}
