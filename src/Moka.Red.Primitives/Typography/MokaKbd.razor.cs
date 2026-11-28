using Microsoft.AspNetCore.Components;

namespace Moka.Red.Primitives.Typography;

/// <summary>
///     Keyboard key display component with a raised key-cap style.
///     Renders a semantic &lt;kbd&gt; element for representing keyboard input.
/// </summary>
public partial class MokaKbd
{
	/// <summary>Content to render inside the keyboard key. <see cref="Key" /> wins when both are set.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>
	///     The key, named as a shortcut names it: <c>"K"</c>, <c>"Esc"</c>, <c>"Shift"</c>. <c>"Mod"</c>, the key a
	///     shortcut such as <c>"Mod+K"</c> means on every platform, reads Ctrl, and Cmd on an Apple platform once the
	///     page has started, matching what the hotkey registry and the command palette bind. Every key cap the library
	///     draws for a shortcut sets this, one per key from <c>ShortcutKeys.Split</c>.
	/// </summary>
	[Parameter]
	public string? Key { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-kbd";

	private bool IsModKey => string.Equals(Key, "Mod", StringComparison.OrdinalIgnoreCase);
}
