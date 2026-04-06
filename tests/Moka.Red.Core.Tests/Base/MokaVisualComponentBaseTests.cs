using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components.Rendering;
using Moka.Red.Core.Base;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Core.Tests.Base;

public class MokaVisualComponentBaseTests : BunitContext
{
	[Fact]
	public void Size_DefaultsToMd()
	{
		IRenderedComponent<TestVisualComponent> cut = Render<TestVisualComponent>();

		Assert.Equal(MokaSize.Md, cut.Instance.Size);
	}

	[Fact]
	public void SizeValue_IsNullByDefault()
	{
		IRenderedComponent<TestVisualComponent> cut = Render<TestVisualComponent>();

		Assert.Null(cut.Instance.SizeValue);
	}

	[Fact]
	public void SizeValue_OverridesSizeEnum()
	{
		IRenderedComponent<TestVisualComponent> cut = Render<TestVisualComponent>(parameters => parameters
			.Add(p => p.Size, MokaSize.Lg)
			.Add(p => p.SizeValue, "32px"));

		Assert.Equal("32px", cut.Instance.ExposedResolvedSize);
	}

	[Fact]
	public void ResolvedSize_ReturnsMappedPixels_WhenSizeValueIsNull()
	{
		IRenderedComponent<TestVisualComponent> cut = Render<TestVisualComponent>(parameters => parameters
			.Add(p => p.Size, MokaSize.Xs));

		Assert.Equal("14px", cut.Instance.ExposedResolvedSize);
	}

	[Fact]
	public void ResolvedSize_ReturnsCorrectPixels_ForEachSize()
	{
		var expected = new Dictionary<MokaSize, string>
		{
			{ MokaSize.Xs, "14px" },
			{ MokaSize.Sm, "16px" },
			{ MokaSize.Md, "20px" },
			{ MokaSize.Lg, "24px" }
		};

		foreach ((MokaSize size, string pixels) in expected)
		{
			IRenderedComponent<TestVisualComponent> cut = Render<TestVisualComponent>(parameters => parameters
				.Add(p => p.Size, size));

			Assert.Equal(pixels, cut.Instance.ExposedResolvedSize);
		}
	}

	[Fact]
	public void Color_IsNullByDefault()
	{
		IRenderedComponent<TestVisualComponent> cut = Render<TestVisualComponent>();

		Assert.Null(cut.Instance.Color);
	}

	[Fact]
	public void Variant_DefaultsToFilled()
	{
		IRenderedComponent<TestVisualComponent> cut = Render<TestVisualComponent>();

		Assert.Equal(MokaVariant.Filled, cut.Instance.Variant);
	}

	[Fact]
	public void Disabled_DefaultsToFalse()
	{
		IRenderedComponent<TestVisualComponent> cut = Render<TestVisualComponent>();

		Assert.False(cut.Instance.Disabled);
	}

	[Theory]
	[InlineData(MokaSize.Xs, "xs")]
	[InlineData(MokaSize.Sm, "sm")]
	[InlineData(MokaSize.Md, "md")]
	[InlineData(MokaSize.Lg, "lg")]
	public void SizeToKebab_ReturnsCorrectString(MokaSize size, string expected) =>
		Assert.Equal(expected, TestVisualComponent.ExposedSizeToKebab(size));

	[Theory]
	[InlineData(MokaColor.Primary, "primary")]
	[InlineData(MokaColor.Secondary, "secondary")]
	[InlineData(MokaColor.Error, "error")]
	[InlineData(MokaColor.Warning, "warning")]
	[InlineData(MokaColor.Success, "success")]
	[InlineData(MokaColor.Info, "info")]
	[InlineData(MokaColor.Surface, "surface")]
	public void ColorToKebab_ReturnsCorrectString(MokaColor color, string expected) =>
		Assert.Equal(expected, TestVisualComponent.ExposedColorToKebab(color));

	[Theory]
	[InlineData(MokaVariant.Filled, "filled")]
	[InlineData(MokaVariant.Outlined, "outlined")]
	[InlineData(MokaVariant.Text, "text")]
	[InlineData(MokaVariant.Soft, "soft")]
	public void VariantToKebab_ReturnsCorrectString(MokaVariant variant, string expected) =>
		Assert.Equal(expected, TestVisualComponent.ExposedVariantToKebab(variant));

	[Fact]
	public void CssClass_IncludesSizeClass()
	{
		IRenderedComponent<TestVisualComponent> cut = Render<TestVisualComponent>(parameters => parameters
			.Add(p => p.Size, MokaSize.Lg));

		IElement div = cut.Find("div");
		Assert.Contains("test-visual--lg", div.ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void CssClass_IncludesColorClass_WhenColorIsSet()
	{
		IRenderedComponent<TestVisualComponent> cut = Render<TestVisualComponent>(parameters => parameters
			.Add(p => p.Color, MokaColor.Error));

		IElement div = cut.Find("div");
		Assert.Contains("test-visual--error", div.ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void CssClass_ExcludesColorClass_WhenColorIsNull()
	{
		IRenderedComponent<TestVisualComponent> cut = Render<TestVisualComponent>();

		IElement div = cut.Find("div");
		Assert.DoesNotContain("test-visual--primary", div.ClassName, StringComparison.Ordinal);
	}

	/// <summary>
	///     Minimal concrete component for testing MokaVisualComponentBase behavior.
	/// </summary>
	public sealed class TestVisualComponent : MokaVisualComponentBase
	{
		protected override string RootClass => "test-visual";

		public string ExposedResolvedSize => ResolvedSize;

		protected override string CssClass => new CssBuilder(RootClass)
			.AddClass($"test-visual--{SizeToKebab(Size)}")
			.AddClass(Color.HasValue ? $"test-visual--{ColorToKebab(Color.Value)}" : null)
			.AddClass(Class)
			.Build();

		public static string ExposedSizeToKebab(MokaSize size) => SizeToKebab(size);
		public static string ExposedColorToKebab(MokaColor color) => ColorToKebab(color);
		public static string ExposedVariantToKebab(MokaVariant variant) => VariantToKebab(variant);

		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
			ArgumentNullException.ThrowIfNull(builder);
			builder.OpenElement(0, "div");
			builder.AddAttribute(1, "class", CssClass);

			if (CssStyle is not null)
			{
				builder.AddAttribute(2, "style", CssStyle);
			}

			if (Id is not null)
			{
				builder.AddAttribute(3, "id", Id);
			}

			builder.AddMultipleAttributes(4, AdditionalAttributes);
			builder.CloseElement();
		}
	}
}
