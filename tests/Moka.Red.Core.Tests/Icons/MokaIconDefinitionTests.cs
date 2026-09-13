using Moka.Red.Core.Icons;

namespace Moka.Red.Core.Tests.Icons;

public class MokaIconDefinitionTests
{
	[Fact]
	public void GetHashCode_DoesNotThrowOnDefault()
	{
		// The struct has no field initializers, so Name is null on default(T).
		// Dereferencing it threw NullReferenceException.
		MokaIconDefinition icon = default;

		Exception? thrown = Record.Exception(() => icon.GetHashCode());

		Assert.Null(thrown);
	}

	[Fact]
	public void DefaultInstance_CanBeUsedInAHashSet()
	{
		var set = new HashSet<MokaIconDefinition> { default, default };

		Assert.Single(set);
	}

	[Fact]
	public void FilledAndOutline_AreNotEqual()
	{
		// Star and StarOutline carry identical path data; Filled is what distinguishes them.
		var solid = new MokaIconDefinition("star", "M12 2z", filled: true);
		var outline = new MokaIconDefinition("star", "M12 2z");

		Assert.NotEqual(solid, outline);
	}

	[Fact]
	public void Filled_DefaultsToFalse()
	{
		var icon = new MokaIconDefinition("save", "M1 1z");

		Assert.False(icon.Filled);
	}

	[Fact]
	public void SameNameAndFill_AreEqual()
	{
		var a = new MokaIconDefinition("save", "M1 1z");
		var b = new MokaIconDefinition("save", "M9 9z");

		Assert.Equal(a, b);
		Assert.True(a == b);
	}

	[Fact]
	public void ViewBox_DefaultsTo24Square()
	{
		var icon = new MokaIconDefinition("save", "M1 1z");

		Assert.Equal("0 0 24 24", icon.ViewBox);
	}
}
