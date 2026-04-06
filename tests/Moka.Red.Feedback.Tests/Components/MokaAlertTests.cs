using AngleSharp.Dom;
using Bunit;
using Moka.Red.Feedback.Alert;
using Moka.Red.Feedback.Toast;

namespace Moka.Red.Feedback.Tests.Components;

public class MokaAlertTests : BunitContext
{
	[Fact]
	public void Renders_RoleAlert()
	{
		IRenderedComponent<MokaAlert> cut = Render<MokaAlert>(p => p
			.AddChildContent("Test message"));

		IElement el = cut.Find("[role='alert']");
		Assert.NotNull(el);
	}

	[Fact]
	public void Renders_RootClass()
	{
		IRenderedComponent<MokaAlert> cut = Render<MokaAlert>(p => p
			.AddChildContent("Test"));

		IElement el = cut.Find(".moka-alert");
		Assert.NotNull(el);
	}

	[Fact]
	public void DefaultSeverity_IsInfo()
	{
		IRenderedComponent<MokaAlert> cut = Render<MokaAlert>(p => p
			.AddChildContent("Info alert"));

		IElement el = cut.Find(".moka-alert");
		Assert.Contains("moka-alert--info", el.ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void Severity_AppliesClass()
	{
		IRenderedComponent<MokaAlert> cut = Render<MokaAlert>(p => p
			.Add(x => x.Severity, MokaToastSeverity.Error)
			.AddChildContent("Error"));

		IElement el = cut.Find(".moka-alert");
		Assert.Contains("moka-alert--error", el.ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void Title_Renders()
	{
		IRenderedComponent<MokaAlert> cut = Render<MokaAlert>(p => p
			.Add(x => x.Title, "Important")
			.AddChildContent("Message body"));

		IElement title = cut.Find(".moka-alert-title");
		Assert.Equal("Important", title.TextContent);
	}

	[Fact]
	public void Message_Renders()
	{
		IRenderedComponent<MokaAlert> cut = Render<MokaAlert>(p => p
			.AddChildContent("Alert body text"));

		IElement message = cut.Find(".moka-alert-message");
		Assert.Equal("Alert body text", message.TextContent);
	}

	[Fact]
	public void Outlined_AppliesClass()
	{
		IRenderedComponent<MokaAlert> cut = Render<MokaAlert>(p => p
			.Add(x => x.Outlined, true)
			.AddChildContent("Outlined"));

		IElement el = cut.Find(".moka-alert");
		Assert.Contains("moka-alert--outlined", el.ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void Dense_AppliesClass()
	{
		IRenderedComponent<MokaAlert> cut = Render<MokaAlert>(p => p
			.Add(x => x.Dense, true)
			.AddChildContent("Dense"));

		IElement el = cut.Find(".moka-alert");
		Assert.Contains("moka-alert--dense", el.ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void Closable_ShowsCloseButton()
	{
		IRenderedComponent<MokaAlert> cut = Render<MokaAlert>(p => p
			.Add(x => x.Closable, true)
			.AddChildContent("Closable"));

		IElement close = cut.Find(".moka-alert-close");
		Assert.NotNull(close);
	}

	[Fact]
	public void NotClosable_NoCloseButton()
	{
		IRenderedComponent<MokaAlert> cut = Render<MokaAlert>(p => p
			.AddChildContent("Not closable"));

		Assert.Empty(cut.FindAll(".moka-alert-close"));
	}

	[Fact]
	public void Close_HidesAlert()
	{
		IRenderedComponent<MokaAlert> cut = Render<MokaAlert>(p => p
			.Add(x => x.Closable, true)
			.AddChildContent("Will close"));

		cut.Find(".moka-alert-close").Click();

		Assert.Empty(cut.FindAll(".moka-alert"));
	}

	[Fact]
	public void Close_FiresCallback()
	{
		bool closed = false;
		IRenderedComponent<MokaAlert> cut = Render<MokaAlert>(p => p
			.Add(x => x.Closable, true)
			.Add(x => x.OnClose, () => closed = true)
			.AddChildContent("Callback test"));

		cut.Find(".moka-alert-close").Click();
		Assert.True(closed);
	}

	[Fact]
	public void HasIcon()
	{
		IRenderedComponent<MokaAlert> cut = Render<MokaAlert>(p => p
			.AddChildContent("Has icon"));

		IElement icon = cut.Find(".moka-alert-icon");
		Assert.NotNull(icon);
	}
}
