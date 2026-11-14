namespace Moka.Red.Core.Announcer;

/// <summary>How urgently a screen reader speaks an announcement.</summary>
public enum MokaPoliteness
{
	/// <summary>Waits until the screen reader has finished what it is saying. For news that can wait its turn.</summary>
	Polite,

	/// <summary>Interrupts what the screen reader is saying. Only for what needs attention now, such as an error.</summary>
	Assertive
}
