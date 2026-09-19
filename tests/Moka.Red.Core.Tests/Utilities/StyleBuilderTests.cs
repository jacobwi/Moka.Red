using Moka.Red.Core.Utilities;
using Moka.Red.Tests.Shared;

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

	// A value went into the style as it was, so a parameter could end its declaration and add others,
	// or leave a string or bracket open and swallow the declarations after it.
	[Theory]
	[InlineData("0 auto; position: fixed; inset: 0")]
	[InlineData("1px} body { display: none")]
	[InlineData("url(\"x.png\"); background: red")]
	[InlineData("\"open string")]
	[InlineData("calc(100% - 2px")]
	[InlineData("red /* open comment")]
	[InlineData("red\\")]
	public void AValueThatCouldLeaveItsDeclaration_IsDropped(string value)
	{
		string? style = new StyleBuilder()
			.AddStyle("margin", value)
			.AddStyle("width", "10px", true)
			.AddStyle("color", value, true)
			.AddStyle("height", "20px")
			.Build();

		Assert.Equal("width: 10px; height: 20px", style);
		Assert.Equal(["width", "height"], CssDeclarations.PropertyNames(style!));
	}

	// Data URIs keep their semicolons inside quotes or the url( token.
	[Theory]
	[InlineData("url(\"data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg'/>\")")]
	[InlineData("url(data:image/png;base64,iVBORw0KGgo=)")]
	[InlineData("var(--moka-shadow-1), 0 0 0 1px var(--moka-color-primary-border)")]
	[InlineData("'Inter', -apple-system, 'Segoe UI', sans-serif")]
	public void AValueThatStaysInItsDeclaration_IsKept(string value)
	{
		string? style = new StyleBuilder()
			.AddStyle("background-image", value)
			.AddStyle("color", "red")
			.Build();

		Assert.Equal($"background-image: {value}; color: red", style);
	}

	// The raw overload carries the consumer's own Style text, several declarations included.
	[Fact]
	public void TheRawOverload_IsWrittenAsItIs()
	{
		string? style = new StyleBuilder()
			.AddStyle("margin: 0 auto; position: sticky")
			.Build();

		Assert.Equal("margin: 0 auto; position: sticky", style);
	}
}
