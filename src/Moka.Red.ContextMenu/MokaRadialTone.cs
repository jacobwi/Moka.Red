namespace Moka.Red.ContextMenu;

/// <summary>What a button in a <see cref="MokaRadialMenu" /> does, which sets its color.</summary>
public enum MokaRadialTone
{
	/// <summary>An ordinary action, in the text color.</summary>
	Default,

	/// <summary>An action that blocks, stops or removes something, in the error color.</summary>
	Danger,

	/// <summary>An action that allows or restores something, in the success color.</summary>
	Good
}
