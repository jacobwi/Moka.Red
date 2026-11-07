namespace Moka.Red.Primitives.ChainCheck;

/// <summary>How one step of a <see cref="MokaChainCheck" /> stands.</summary>
public enum MokaChainTone
{
	/// <summary>Green. The step holds.</summary>
	Ok,

	/// <summary>Amber. The step holds, but something about it is worth knowing.</summary>
	Warn,

	/// <summary>Red. The chain breaks here. Steps after the first one that fails are drawn faded, since they can't be judged.</summary>
	Fail,

	/// <summary>Grey with a dashed line into it. It can't be judged yet, or it doesn't apply here.</summary>
	Wait,

	/// <summary>Grey with a dashed line into it. The step is turned off.</summary>
	Off
}
