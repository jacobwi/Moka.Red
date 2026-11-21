using AngleSharp.Dom;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Feedback.Banner;
using Moka.Red.Feedback.Extensions;

namespace Moka.Red.Feedback.Tests.Components;

// A banner's link could only open in this tab: a status page or release notes outside the app replaced the app.
public sealed class MokaBannerTargetTests : BunitContext
{
	public MokaBannerTargetTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddMokaFeedback();
	}

	private IMokaBannerService Banners => Services.GetRequiredService<IMokaBannerService>();

	private IElement ShowLink(string? target, MokaBannerText? text = null)
	{
		IRenderedComponent<MokaBannerHost> cut = text is null
			? Render<MokaBannerHost>()
			: Render<MokaBannerHost>(p => p.Add(x => x.Text, text));
		Banners.Show(new MokaBanner
		{
			Id = "maintenance",
			Message = "Down from 22:00 to 23:00.",
			LinkText = "Details",
			Href = "https://status.example.com",
			Target = target
		});
		return cut.Find(".moka-banner__link");
	}

	[Fact]
	public void ALinkForANewTab_HasRel_AndSaysWhereItOpens()
	{
		IElement link = ShowLink("_blank");

		Assert.Equal("_blank", link.GetAttribute("target"));
		Assert.Equal("noopener noreferrer", link.GetAttribute("rel"));
		Assert.Equal("Details, opens in a new tab", link.TextContent);
		Assert.Equal(", opens in a new tab", link.QuerySelector(".moka-visually-hidden")!.TextContent);
	}

	[Fact]
	public void TheWords_ComeFromTheHostsText() =>
		Assert.Equal("Details, öppnas i en ny flik",
			ShowLink("status", new MokaBannerText { NewTab = "öppnas i en ny flik" }).TextContent);

	[Theory]
	[InlineData("_self")]
	[InlineData("_top")]
	public void ATargetInThisTab_AddsNoRelOrWords(string target)
	{
		IElement link = ShowLink(target);

		Assert.Equal(target, link.GetAttribute("target"));
		Assert.False(link.HasAttribute("rel"));
		Assert.Equal("Details", link.TextContent);
	}

	[Fact]
	public void ALinkWithoutTarget_KeepsItsMarkup()
	{
		IElement link = ShowLink(null);

		Assert.Equal(["class", "href"], link.Attributes.Select(a => a.Name)
			.Where(name => !name.StartsWith("b-", StringComparison.Ordinal)));
		Assert.Equal("Details", link.InnerHtml);
	}

	[Fact]
	public void TheDefaultWords_SayItOpensInANewTab() =>
		Assert.Equal("opens in a new tab", new MokaBannerText().NewTab);
}
