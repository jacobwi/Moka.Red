using AngleSharp.Dom;
using Bunit;
using Moka.Red.Feedback.Tooltip;
using Moka.Red.Primitives.Utility;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Primitives.Tests.Components;

public class MokaCopyableTests : BunitContext
{
	private IRenderedComponent<MokaCopyable> Copyable(string text = "192.168.1.24", string? label = null) =>
		Render<MokaCopyable>(p => p.Add(x => x.Text, text).Add(x => x.Label, label));

	[Fact]
	public void TheValue_ShowsBesideAQuietButton()
	{
		IRenderedComponent<MokaCopyable> cut = Copyable();

		Assert.Equal("192.168.1.24", cut.Find(".moka-copyable__value").TextContent.Trim());
		Assert.Contains("moka-copy-btn--quiet", cut.Find("button").ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public async Task ChildContent_ReplacesWhatShows_AndTheFullValueIsStillCopied()
	{
		IRenderedComponent<MokaCopyable> cut = Render<MokaCopyable>(p => p
			.Add(x => x.Text, "sk-live-1234")
			.Add(x => x.Label, "VPN key")
			.AddChildContent("sk-live-..."));

		Assert.Equal("sk-live-1234", cut.Find("button").CopyText());
		await cut.Find("button").CopiedAsync();

		Assert.Equal("sk-live-...", cut.Find(".moka-copyable__value").TextContent.Trim());
		Assert.Contains("moka-copy-btn--copied", cut.Find("button").ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void TheButton_IsNamedFromTheLabel()
	{
		IRenderedComponent<MokaCopyable> cut = Copyable(label: "IPv4");

		IconTooltip.AssertNamed(cut.Find("button"), "Copy IPv4", MokaTooltipPosition.Bottom);
	}

	[Fact]
	public void WithoutALabel_TheButtonIsNamedFromTheValue()
	{
		IRenderedComponent<MokaCopyable> cut = Copyable();

		Assert.Equal("Copy 192.168.1.24", cut.Find("button").GetAttribute("aria-label"));
	}

	[Fact]
	public void CopyLabel_ReplacesTheName()
	{
		IRenderedComponent<MokaCopyable> cut = Render<MokaCopyable>(p => p
			.Add(x => x.Text, "abc")
			.Add(x => x.CopyLabel, "Kopieren"));

		Assert.Equal("Kopieren", cut.Find("button").GetAttribute("aria-label"));
	}

	[Fact]
	public void ValueIsMonoByDefault_AndCanBePlain()
	{
		IRenderedComponent<MokaCopyable> mono = Copyable();
		IRenderedComponent<MokaCopyable> plain = Render<MokaCopyable>(p => p.Add(x => x.Text, "abc").Add(x => x.Mono, false));

		Assert.Contains("moka-copyable--mono", mono.Find(".moka-copyable").ClassName, StringComparison.Ordinal);
		Assert.DoesNotContain("moka-copyable--mono", plain.Find(".moka-copyable").ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void Wrap_IsAClassOnTheRoot()
	{
		IRenderedComponent<MokaCopyable> cut = Render<MokaCopyable>(p => p.Add(x => x.Text, "abc").Add(x => x.Wrap, true));

		Assert.Contains("moka-copyable--wrap", cut.Find(".moka-copyable").ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public async Task AfterACopy_ATagSaysSo_AndTheTooltipStepsAside()
	{
		IRenderedComponent<MokaCopyable> cut = Copyable("abc");

		await cut.Find("button").CopiedAsync();

		Assert.Equal("Copied", cut.Find(".moka-copy-btn__tag").TextContent);
		Assert.Empty(cut.FindAll("[role=tooltip]"));
		Assert.Equal("Copied", cut.Find("[role=status]").TextContent);
	}

	[Fact]
	public async Task WhenTheCopyIsRefused_ATagSaysThat()
	{
		IRenderedComponent<MokaCopyable> cut = Copyable("abc");

		await cut.Find("button").CopiedAsync(false);

		IElement button = cut.Find("button");
		Assert.Contains("moka-copy-btn--failed", button.ClassName, StringComparison.Ordinal);
		Assert.Equal("Couldn't copy", cut.Find(".moka-copy-btn__tag").TextContent);
		Assert.Equal("Couldn't copy", button.GetAttribute("aria-label"));
	}

	[Fact]
	public void TheStatusRegion_IsThereBeforeAnythingHappens_SoAScreenReaderHearsTheChange()
	{
		IRenderedComponent<MokaCopyable> cut = Copyable();

		Assert.Equal(string.Empty, cut.Find("[role=status]").TextContent);
	}

	[Fact]
	public void ClassAndAttributes_LandOnTheRoot()
	{
		IRenderedComponent<MokaCopyable> cut = Render<MokaCopyable>(p => p
			.Add(x => x.Text, "abc")
			.Add(x => x.Class, "mine")
			.AddUnmatched("data-row", "1"));

		IElement root = cut.Find(".moka-copyable");
		Assert.Contains("mine", root.ClassName, StringComparison.Ordinal);
		Assert.Equal("1", root.GetAttribute("data-row"));
	}

	// copyable.md's own example shows a key masked. Copy buttons carry their text in the page since 0.4.3, so a
	// Secret one asks for it at the click and keeps it out of its name too.
	[Fact]
	public async Task Secret_KeepsTheValueOutOfThePage_AndHandsItOverWhenAsked()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(CopyEvents.Module);
		module.SetupVoid("answerCopy", _ => true).SetVoidResult();
		IRenderedComponent<MokaCopyable> cut = Render<MokaCopyable>(p => p
			.Add(x => x.Text, "sk-live-1234")
			.Add(x => x.Secret, true)
			.AddChildContent("sk-live-..."));

		Assert.DoesNotContain("sk-live-1234", cut.Markup, StringComparison.Ordinal);
		Assert.Null(cut.Find("button").CopyText());
		Assert.True(cut.Find("button").HasAttribute("data-moka-copy-ask"));
		Assert.Equal("Copy", cut.Find("button").GetAttribute("aria-label"));

		await cut.Find("button").AskAsync();

		Assert.Equal("sk-live-1234", module.AnsweredText());
	}

	[Fact]
	public void Secret_WithALabel_IsNamedFromTheLabel()
	{
		IRenderedComponent<MokaCopyable> cut = Render<MokaCopyable>(p => p
			.Add(x => x.Text, "sk-live-1234")
			.Add(x => x.Label, "VPN key")
			.Add(x => x.Secret, true)
			.AddChildContent("sk-live-..."));

		Assert.Equal("Copy VPN key", cut.Find("button").GetAttribute("aria-label"));
		Assert.DoesNotContain("sk-live-1234", cut.Markup, StringComparison.Ordinal);
	}
}
