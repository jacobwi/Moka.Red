namespace Moka.Red.Data.Table;

/// <summary>What a person column draws in the avatar of a row with no picture.</summary>
public enum MokaAvatarFallback
{
	/// <summary>The person's initials on a hue picked from them.</summary>
	Initials,

	/// <summary>An identicon drawn from the person's name.</summary>
	Identicon
}
