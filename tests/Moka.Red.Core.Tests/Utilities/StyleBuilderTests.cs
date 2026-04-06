using Moka.Red.Core.Utilities;

namespace Moka.Red.Core.Tests.Utilities;

public class StyleBuilderTests
{
	[Fact]
	public void Build_WithNoStyles_ReturnsNull()
	{
		string? result = new StyleBuilder().Build();

		Assert.Null(result);
	}

	[Fact]
	public void AddStyle_PropertyValue_FormatsCorrectly()
	{
		string? result = new StyleBuilder()
			.AddStyle("color", "red")
			.Build();

		Assert.Equal("color: red", result);
	}

	[Fact]
	public void AddStyle_MultipleProperties_JoinedWithSemicolon()
	{
		string? result = new StyleBuilder()
			.AddStyle("color", "red")
			.AddStyle("font-size", "14px")
			.Build();

		Assert.Equal("color: red; font-size: 14px", result);
	}

	[Fact]
	public void AddStyle_NullValue_Skipped()
	{
		string? result = new StyleBuilder()
			.AddStyle("color", null)
			.Build();

		Assert.Null(result);
	}

	[Fact]
	public void AddStyle_WithCondition_WhenFalse_Skipped()
	{
		string? result = new StyleBuilder()
			.AddStyle("color", "red", false)
			.Build();

		Assert.Null(result);
	}

	[Fact]
	public void AddStyle_WithCondition_WhenTrue_Added()
	{
		string? result = new StyleBuilder()
			.AddStyle("color", "red", true)
			.Build();

		Assert.Equal("color: red", result);
	}

	[Fact]
	public void AddStyle_RawString_TrimsSemicolon()
	{
		string? result = new StyleBuilder()
			.AddStyle("color: blue;")
			.Build();

		Assert.Equal("color: blue", result);
	}

	[Fact]
	public void AddStyle_RawNull_Skipped()
	{
		string? result = new StyleBuilder()
			.AddStyle(null)
			.Build();

		Assert.Null(result);
	}

	[Fact]
	public void ToString_ReturnsEmptyString_WhenNoStyles()
	{
		var builder = new StyleBuilder();

		Assert.Equal(string.Empty, builder.ToString());
	}
}
