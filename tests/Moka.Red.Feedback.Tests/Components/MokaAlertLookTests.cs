using AngleSharp.Dom;
using Bunit;
using Moka.Red.Feedback.Alert;
using Moka.Red.Feedback.Toast;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Feedback.Tests.Components;

public class MokaAlertLookTests : BunitContext
{
	private const string Stylesheet = "MokaAlert.razor.css";

	[Fact]
	public void Default_HasNoFixOrAccent()
	{
		IRenderedComponent<MokaAlert> cut = Render<MokaAlert>(p => p.AddChildContent("Message"));

		Assert.Equal("moka-alert moka-alert--info", cut.Find(".moka-alert").ClassName);
		Assert.Empty(cut.FindAll(".moka-alert-fix"));
	}

	[Fact]
	public void FixAction_RendersAfterTheText()
	{
		IRenderedComponent<MokaAlert> cut = Render<MokaAlert>(p => p
			.AddChildContent("Message")
			.Add(x => x.FixAction, "<button>Move it back</button>"));

		IElement alert = cut.Find(".moka-alert");

		Assert.Contains("moka-alert--has-fix", alert.ClassList);
		Assert.Equal("moka-alert-fix", alert.Children[^1].ClassName);
		Assert.Equal("moka-alert-content", alert.Children[^2].ClassName);
		Assert.Equal("Move it back", cut.Find(".moka-alert-fix button").TextContent);
	}

	[Fact]
	public void FixAction_RendersBeforeTheCloseButton()
	{
		IRenderedComponent<MokaAlert> cut = Render<MokaAlert>(p => p
			.Add(x => x.Closable, true)
			.Add(x => x.FixAction, "<button>Move it back</button>"));

		IElement alert = cut.Find(".moka-alert");

		Assert.Equal("moka-alert-fix", alert.Children[^2].ClassName);
		Assert.Single(alert.Children[^1].QuerySelectorAll(".moka-alert-close"));
	}

	[Fact]
	public void HasFixRule_WrapsTheActionUnderTheText()
	{
		Assert.Equal("wrap", ScopedCss.Declarations(Stylesheet, ".moka-alert--has-fix")["flex-wrap"]);
		Assert.Equal("1 1 15rem",
			ScopedCss.Declarations(Stylesheet, ".moka-alert--has-fix > .moka-alert-content")["flex"]);
	}

	[Theory]
	[InlineData(MokaToastSeverity.Info)]
	[InlineData(MokaToastSeverity.Success)]
	[InlineData(MokaToastSeverity.Warning)]
	[InlineData(MokaToastSeverity.Error)]
	public void Accent_ReplacesTheSeverityClass(MokaToastSeverity severity)
	{
		IElement alert = Render<MokaAlert>(p => p
			.Add(x => x.Severity, severity)
			.Add(x => x.Accent, true)).Find(".moka-alert");

		Assert.Equal("moka-alert moka-alert--accent", alert.ClassName);
	}

	[Fact]
	public void Accent_KeepsTheAlertRole()
	{
		IElement alert = Render<MokaAlert>(p => p.Add(x => x.Accent, true)).Find(".moka-alert");

		Assert.Equal("alert", alert.GetAttribute("role"));
	}

	[Fact]
	public void AccentRule_ReadsTheAccentTokens()
	{
		IReadOnlyDictionary<string, string> accent = ScopedCss.Declarations(Stylesheet, ".moka-alert--accent");

		Assert.Equal("var(--moka-color-primary-bg)", accent["background"]);
		Assert.Equal("var(--moka-color-primary-ring)", accent["border-color"]);
		Assert.Equal("var(--moka-color-primary-text)", accent["color"]);
		Assert.Equal("var(--moka-color-primary)",
			ScopedCss.Declarations(Stylesheet, ".moka-alert--accent ::deep .moka-alert-icon")["color"]);
	}

	[Fact]
	public void OutlinedAccent_HasNoFill()
	{
		Assert.Equal("transparent",
			ScopedCss.Declarations(Stylesheet, ".moka-alert--accent.moka-alert--outlined")["background"]);
	}

	[Fact]
	public void Class_StaysLast()
	{
		IElement alert = Render<MokaAlert>(p => p
			.Add(x => x.Accent, true)
			.Add(x => x.FixAction, "<button>Fix</button>")
			.Add(x => x.Class, "mine")).Find(".moka-alert");

		Assert.EndsWith("mine", alert.ClassName, StringComparison.Ordinal);
	}
}
