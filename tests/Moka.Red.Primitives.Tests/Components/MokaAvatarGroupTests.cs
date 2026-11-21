using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Enums;
using Moka.Red.Primitives.Avatar;

namespace Moka.Red.Primitives.Tests.Components;

// The group's Size never reached its avatars, and the avatars never told the group they were there, so Max hid nothing
// and the "+N" avatar never showed.
public class MokaAvatarGroupTests : BunitContext
{
	[Fact]
	public void WithoutASize_EachAvatarDrawsAsItDoesAlone()
	{
		Avatar[] avatars = [new("AB"), new("CD", MokaSize.Sm), new("EF", SizeValue: "3rem")];

		IRenderedComponent<MokaAvatarGroup> cut = Render<MokaAvatarGroup>(p => p.Add(x => x.ChildContent, Draw(avatars)));

		IReadOnlyList<IElement> drawn = cut.FindAll(".moka-avatar");
		Assert.Equal(avatars.Length, drawn.Count);
		for (int i = 0; i < avatars.Length; i++)
		{
			drawn[i].MarkupMatches(Alone(avatars[i]).Find(".moka-avatar").OuterHtml);
		}

		Assert.Empty(cut.FindAll(".moka-avatar-group__overflow"));
	}

	[Fact]
	public void TheGroupsSize_ReachesEveryAvatarThatSetsNoSizeOfItsOwn()
	{
		IRenderedComponent<MokaAvatarGroup> cut = Render<MokaAvatarGroup>(p => p
			.Add(x => x.Size, MokaSize.Lg)
			.Add(x => x.ChildContent, Draw([new("AB"), new("CD", MokaSize.Sm), new("EF", SizeValue: "20px")])));

		IReadOnlyList<IElement> drawn = cut.FindAll(".moka-avatar");
		AssertSize(drawn[0], "lg", "56px");
		AssertSize(drawn[1], "sm", "32px");
		AssertSize(drawn[2], "md", "20px");
	}

	// A length SizeValue scales the initials with the box, as it does on an avatar alone.
	[Fact]
	public void TheGroupsSizeValue_ReachesTheAvatars_WithTheGroupsSizeForTheirShares()
	{
		IRenderedComponent<MokaAvatarGroup> cut = Render<MokaAvatarGroup>(p => p
			.Add(x => x.Size, MokaSize.Sm)
			.Add(x => x.SizeValue, "3.5rem")
			.Add(x => x.ChildContent, Draw([new("AB"), new("CD", MokaSize.Lg)])));

		IReadOnlyList<IElement> drawn = cut.FindAll(".moka-avatar");
		AssertSize(drawn[0], "sm", "3.5rem");
		Assert.Contains("moka-avatar--sized", drawn[0].ClassName, StringComparison.Ordinal);
		Assert.Contains("--_avatar-size: 3.5rem", drawn[0].GetAttribute("style"), StringComparison.Ordinal);

		AssertSize(drawn[1], "lg", "56px");
		Assert.DoesNotContain("moka-avatar--sized", drawn[1].ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void AnAvatarInAGroup_DrawsAsAnAvatarAloneOfTheGroupsSize()
	{
		IRenderedComponent<MokaAvatarGroup> cut = Render<MokaAvatarGroup>(p => p
			.Add(x => x.Size, MokaSize.Xs)
			.Add(x => x.SizeValue, "30px")
			.Add(x => x.ChildContent, Draw([new("AB")])));

		cut.Find(".moka-avatar").MarkupMatches(Alone(new Avatar("AB", MokaSize.Xs, "30px")).Find(".moka-avatar").OuterHtml);
	}

	[Fact]
	public void ChangingTheGroupsSize_RedrawsItsAvatars()
	{
		RenderFragment content = Draw([new("AB"), new("CD")]);
		IRenderedComponent<MokaAvatarGroup> cut = Render<MokaAvatarGroup>(p => p
			.Add(x => x.Size, MokaSize.Sm)
			.Add(x => x.ChildContent, content));

		cut.Render(p => p
			.Add(x => x.Size, MokaSize.Lg)
			.Add(x => x.ChildContent, content));

		Assert.All(cut.FindAll(".moka-avatar"), avatar => AssertSize(avatar, "lg", "56px"));
	}

	[Fact]
	public void Max_ShowsTheFirstAvatars_AndAPlusNCountsTheRest()
	{
		IRenderedComponent<MokaAvatarGroup> cut = Render<MokaAvatarGroup>(p => p
			.Add(x => x.Max, 3)
			.Add(x => x.ChildContent, Draw([new("AB"), new("CD"), new("EF"), new("GH"), new("IJ")])));

		Assert.Equal(["AB", "CD", "EF", "+2"], cut.FindAll(".moka-avatar__initials").Select(i => i.TextContent));

		IElement more = cut.FindAll(".moka-avatar")[^1];
		Assert.Contains("moka-avatar-group__overflow", more.ClassName, StringComparison.Ordinal);
		Assert.DoesNotContain("background-color", more.GetAttribute("style"), StringComparison.Ordinal);
		Assert.DoesNotContain("moka-avatar--own", more.ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void MaxOfZero_ShowsOnlyTheCount()
	{
		IRenderedComponent<MokaAvatarGroup> cut = Render<MokaAvatarGroup>(p => p
			.Add(x => x.Max, 0)
			.Add(x => x.ChildContent, Draw([new("AB"), new("CD")])));

		Assert.Equal(["+2"], cut.FindAll(".moka-avatar__initials").Select(i => i.TextContent));
	}

	[Fact]
	public void ThePlusN_SizesLikeTheAvatars()
	{
		IRenderedComponent<MokaAvatarGroup> plain = Render<MokaAvatarGroup>(p => p
			.Add(x => x.Max, 1)
			.Add(x => x.ChildContent, Draw([new("AB"), new("CD")])));
		IRenderedComponent<MokaAvatarGroup> sized = Render<MokaAvatarGroup>(p => p
			.Add(x => x.Max, 1)
			.Add(x => x.Size, MokaSize.Sm)
			.Add(x => x.SizeValue, "48px")
			.Add(x => x.ChildContent, Draw([new("AB"), new("CD")])));

		AssertSize(plain.Find(".moka-avatar-group__overflow"), "md", "40px");

		IElement more = sized.Find(".moka-avatar-group__overflow");
		AssertSize(more, "sm", "48px");
		Assert.Contains("moka-avatar--sized", more.ClassName, StringComparison.Ordinal);
		Assert.Contains("--_avatar-size: 48px", more.GetAttribute("style"), StringComparison.Ordinal);
	}

	[Fact]
	public void AvatarsComingAndGoing_MoveTheCount()
	{
		IRenderedComponent<MokaAvatarGroup> cut = Render<MokaAvatarGroup>(p => p
			.Add(x => x.Max, 3)
			.Add(x => x.ChildContent, Draw([new("AB"), new("CD"), new("EF"), new("GH")])));
		Assert.Equal(["AB", "CD", "EF", "+1"], Initials(cut));

		cut.Render(p => p
			.Add(x => x.Max, 3)
			.Add(x => x.ChildContent, Draw([new("AB"), new("CD"), new("EF"), new("GH"), new("IJ"), new("KL")])));
		Assert.Equal(["AB", "CD", "EF", "+3"], Initials(cut));

		cut.Render(p => p
			.Add(x => x.Max, 3)
			.Add(x => x.ChildContent, Draw([new("AB"), new("CD")])));
		Assert.Equal(["AB", "CD"], Initials(cut));
	}

	[Fact]
	public void RaisingMax_ShowsTheAvatarsItLeftOut()
	{
		RenderFragment content = Draw([new("AB"), new("CD"), new("EF")]);
		IRenderedComponent<MokaAvatarGroup> cut = Render<MokaAvatarGroup>(p => p
			.Add(x => x.Max, 1)
			.Add(x => x.ChildContent, content));
		Assert.Equal(["AB", "+2"], Initials(cut));

		cut.Render(p => p
			.Add(x => x.Max, 5)
			.Add(x => x.ChildContent, content));
		Assert.Equal(["AB", "CD", "EF"], Initials(cut));
	}

	private static string[] Initials(IRenderedComponent<MokaAvatarGroup> cut) =>
		[.. cut.FindAll(".moka-avatar__initials").Select(i => i.TextContent)];

	private static void AssertSize(IElement avatar, string step, string box)
	{
		Assert.Contains($"moka-avatar--{step}", avatar.ClassName, StringComparison.Ordinal);
		Assert.StartsWith($"width: {box}; height: {box};", avatar.GetAttribute("style"), StringComparison.Ordinal);
	}

	private IRenderedComponent<MokaAvatar> Alone(Avatar avatar) => Render<MokaAvatar>(p =>
	{
		p.Add(x => x.Initials, avatar.Initials);
		if (avatar.Size is { } size)
		{
			p.Add(x => x.Size, size);
		}

		if (avatar.SizeValue is not null)
		{
			p.Add(x => x.SizeValue, avatar.SizeValue);
		}
	});

	private static RenderFragment Draw(Avatar[] avatars) => builder =>
	{
		foreach (Avatar avatar in avatars)
		{
			builder.OpenComponent<MokaAvatar>(0);
			builder.SetKey(avatar.Initials);
			builder.AddComponentParameter(1, nameof(MokaAvatar.Initials), avatar.Initials);
			if (avatar.Size is { } size)
			{
				builder.AddComponentParameter(2, nameof(MokaAvatar.Size), size);
			}

			if (avatar.SizeValue is not null)
			{
				builder.AddComponentParameter(3, nameof(MokaAvatar.SizeValue), avatar.SizeValue);
			}

			builder.CloseComponent();
		}
	};

	private sealed record Avatar(string Initials, MokaSize? Size = null, string? SizeValue = null);
}
