using Moka.Red.Core.Enums;

namespace Moka.Red.Primitives.Avatar;

// What a MokaAvatarGroup hands its avatars: the Size and SizeValue the page gave the group, null where it gave none.
internal sealed record MokaAvatarGroupSizing(MokaSize? Size, string? SizeValue)
{
	public static readonly MokaAvatarGroupSizing None = new(null, null);
}
