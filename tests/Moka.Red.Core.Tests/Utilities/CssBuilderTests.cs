using Moka.Red.Core.Utilities;

namespace Moka.Red.Core.Tests.Utilities;

public class CssBuilderTests
{
	[Fact]
	public void Build_WithDefaultClass_ReturnsClass()
	{
		string result = new CssBuilder("moka-button").Build();

		Assert.Equal("moka-button", result);
	}

	[Fact]
	public void Build_WithNoClasses_ReturnsEmpty()
	{
		string result = new CssBuilder().Build();

		Assert.Equal(string.Empty, result);
	}

	[Fact]
	public void AddClass_AppendsMultipleClasses()
	{
		string result = new CssBuilder("base")
			.AddClass("extra")
			.AddClass("another")
			.Build();

		Assert.Equal("base extra another", result);
	}

	[Fact]
	public void AddClass_SkipsNull()
	{
		string result = new CssBuilder("base")
			.AddClass(null)
			.AddClass("valid")
			.Build();

		Assert.Equal("base valid", result);
	}

	[Fact]
	public void AddClass_SkipsWhitespace()
	{
		string result = new CssBuilder("base")
			.AddClass("  ")
			.AddClass("")
			.Build();

		Assert.Equal("base", result);
	}

	[Fact]
	public void AddClass_WithBoolCondition_WhenTrue_AddsClass()
	{
		string result = new CssBuilder("base")
			.AddClass("active", true)
			.Build();

		Assert.Equal("base active", result);
	}

	[Fact]
	public void AddClass_WithBoolCondition_WhenFalse_SkipsClass()
	{
		string result = new CssBuilder("base")
			.AddClass("active", false)
			.Build();

		Assert.Equal("base", result);
	}

	[Fact]
	public void AddClass_WithFuncCondition_EvaluatesLazily()
	{
		string result = new CssBuilder("base")
			.AddClass("lazy", () => true)
			.AddClass("skipped", () => false)
			.Build();

		Assert.Equal("base lazy", result);
	}

	[Fact]
	public void AddClass_WithFuncCondition_NullFunc_Throws()
	{
		var builder = new CssBuilder("base");

		Assert.Throws<ArgumentNullException>(() => builder.AddClass("test", null!));
	}

	[Fact]
	public void ToString_ReturnsSameAsBuild()
	{
		CssBuilder builder = new CssBuilder("moka-root").AddClass("extra");

		Assert.Equal(builder.Build(), builder.ToString());
	}

	[Fact]
	public void NullDefaultClass_ProducesEmpty()
	{
		string result = new CssBuilder().Build();

		Assert.Equal(string.Empty, result);
	}
}
