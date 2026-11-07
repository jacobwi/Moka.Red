using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Feedback.Tooltip;
using Moka.Red.Primitives.Utility;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Primitives.Tests.Components;

public class MokaCopyableTests : BunitContext
{
	private const string Module = "./_content/Moka.Red.Core/moka-drag.js";

	private readonly BunitJSModuleInterop _module;

	public MokaCopyableTests() => _module = JSInterop.SetupModule(Module);

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
	public void ChildContent_ReplacesWhatShows_AndTheFullValueIsStillCopied()
	{
		_module.Setup<bool?>("copyToClipboard", "sk-live-1234").SetResult(true);
		IRenderedComponent<MokaCopyable> cut = Render<MokaCopyable>(p => p
			.Add(x => x.Text, "sk-live-1234")
			.Add(x => x.Label, "VPN key")
			.AddChildContent("sk-live-..."));

		cut.Find("button").Click();

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
		_module.Setup<bool?>("copyToClipboard", "abc").SetResult(true);
		IRenderedComponent<MokaCopyable> cut = Copyable("abc");

		await cut.Find("button").ClickAsync(new MouseEventArgs());

		Assert.Equal("Copied", cut.Find(".moka-copy-btn__tag").TextContent);
		Assert.Empty(cut.FindAll("[role=tooltip]"));
		Assert.Equal("Copied", cut.Find("[role=status]").TextContent);
	}

	[Fact]
	public async Task WhenTheCopyIsRefused_ATagSaysThat()
	{
		_module.Setup<bool?>("copyToClipboard", "abc").SetResult(false);
		IRenderedComponent<MokaCopyable> cut = Copyable("abc");

		await cut.Find("button").ClickAsync(new MouseEventArgs());

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
}
