using System.Diagnostics;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Moka.Red.Primitives.Utility;

namespace Moka.Red.Primitives.Tests.Components;

// The button copied from a .NET click handler, a module import and a call later. On Blazor Server that is a round trip
// after the click, and Safari, iOS and Firefox refuse a copy once the click is over, so it said "Couldn't copy" there.
// The browser now copies inside the click and .NET hears the result.
public class MokaCopyButtonTests : BunitContext
{
	private static IElement Button(IRenderedComponent<MokaCopyButton> cut) => cut.Find("button");

	[Fact]
	public void TheButton_CarriesItsText_AndLeavesTheClickToTheBrowser()
	{
		IRenderedComponent<MokaCopyButton> cut = Render<MokaCopyButton>(p => p.Add(x => x.Text, "npm i moka"));

		Assert.Equal("npm i moka", Button(cut).CopyText());
		Assert.False(Button(cut).HasAttribute("data-moka-copy-ask"));
		Assert.Throws<MissingEventHandlerException>(() => Button(cut).Click());
		Assert.Empty(JSInterop.Invocations);
	}

	[Fact]
	public async Task ACopy_ShowsTheCopiedState_ThenResets()
	{
		IRenderedComponent<MokaCopyButton> cut = Render<MokaCopyButton>(p => p.Add(x => x.Text, "npm i moka"));

		await Button(cut).CopiedAsync();

		Assert.Contains("moka-copy-btn--copied", Button(cut).ClassName, StringComparison.Ordinal);
		Assert.Equal("Copied!", Button(cut).GetAttribute("aria-label"));
		cut.WaitForAssertion(() => Assert.Equal("Copy", Button(cut).GetAttribute("aria-label")), TimeSpan.FromSeconds(5));
		Assert.DoesNotContain("moka-copy-btn--copied", Button(cut).ClassName, StringComparison.Ordinal);
	}

	// Saying nothing left a person pasting whatever they copied last.
	[Fact]
	public async Task ARefusedCopy_SaysItCouldntCopy_ThenResets()
	{
		IRenderedComponent<MokaCopyButton> cut = Render<MokaCopyButton>(p => p
			.Add(x => x.Text, "abc")
			.Add(x => x.FailedText, "Nope"));

		await Button(cut).CopiedAsync(false);

		Assert.DoesNotContain("moka-copy-btn--copied", Button(cut).ClassName, StringComparison.Ordinal);
		Assert.Contains("moka-copy-btn--failed", Button(cut).ClassName, StringComparison.Ordinal);
		Assert.Equal("Nope", Button(cut).GetAttribute("aria-label"));
		cut.WaitForAssertion(() => Assert.Equal("Copy", Button(cut).GetAttribute("aria-label")), TimeSpan.FromSeconds(5));
		Assert.DoesNotContain("moka-copy-btn--failed", Button(cut).ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public async Task ACopyAfterARefusal_ClearsTheRefusal()
	{
		IRenderedComponent<MokaCopyButton> cut = Render<MokaCopyButton>(p => p.Add(x => x.Text, "abc"));
		await Button(cut).CopiedAsync(false);

		await Button(cut).CopiedAsync();

		Assert.DoesNotContain("moka-copy-btn--failed", Button(cut).ClassName, StringComparison.Ordinal);
		Assert.Contains("moka-copy-btn--copied", Button(cut).ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void TheDefaultLook_HasNoTagAndNoStatusRegion()
	{
		IRenderedComponent<MokaCopyButton> cut = Render<MokaCopyButton>(p => p.Add(x => x.Text, "abc"));

		Assert.DoesNotContain("moka-copy-btn--quiet", Button(cut).ClassName, StringComparison.Ordinal);
		Assert.Empty(cut.FindAll("[role=status]"));
	}

	// A timer left from an earlier copy reset the button in the middle of a newer copy's two seconds.
	[Fact]
	public async Task OnlyTheLatestCopy_ResetsTheButton()
	{
		IRenderedComponent<MokaCopyButton> cut = Render<MokaCopyButton>(p => p.Add(x => x.Text, "abc"));

		await Button(cut).CopiedAsync();
		await Button(cut).CopiedAsync();
		await Task.Delay(TimeSpan.FromSeconds(1), Xunit.TestContext.Current.CancellationToken);
		await Button(cut).CopiedAsync();
		var sinceLatest = Stopwatch.StartNew();

		// A timer left from the first two copies would fire about a second from now.
		while (sinceLatest.Elapsed < TimeSpan.FromSeconds(1.7))
		{
			Assert.Equal("Copied!", await cut.InvokeAsync(() => Button(cut).GetAttribute("aria-label")));
			await Task.Delay(50, Xunit.TestContext.Current.CancellationToken);
		}

		cut.WaitForAssertion(() => Assert.Equal("Copy", Button(cut).GetAttribute("aria-label")), TimeSpan.FromSeconds(3));
	}

	// Callers leave Text empty beside a TextSource. Copied where nothing answers the question (a static page), it
	// would empty the person's clipboard, so the button never offers it.
	[Fact]
	public async Task TextSource_IsReadWhenTheBrowserAsks_NotWhenRendered()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(CopyEvents.Module);
		module.SetupVoid("answerCopy", _ => true).SetVoidResult();
		int reads = 0;
		IRenderedComponent<MokaCopyButton> cut = Render<MokaCopyButton>(p => p
			.Add(x => x.Text, "")
			.Add(x => x.TextSource, () =>
			{
				reads++;
				return "a long log";
			}));

		string? button = Button(cut).GetAttribute("blazor:elementReference");
		Assert.Null(Button(cut).CopyText());
		Assert.True(Button(cut).HasAttribute("data-moka-copy-ask"));
		Assert.Equal(0, reads);

		await Button(cut).AskAsync();

		Assert.Equal(1, reads);
		Assert.Equal("a long log", module.AnsweredText());
		Assert.Equal(button, ((ElementReference)module.Invocations.Single(i => i.Identifier == "answerCopy").Arguments[0]!).Id);
	}

	// Rendering loads nothing: a page of terminals or data lists makes no JS calls until someone copies.
	[Fact]
	public void TextSource_LoadsNoModuleUntilAsked()
	{
		Render<MokaCopyButton>(p => p.Add(x => x.Text, "").Add(x => x.TextSource, () => "log"));

		Assert.Empty(JSInterop.Invocations);
	}

	[Fact]
	public async Task ALostCircuit_WhileAnswering_DoesNotEscape()
	{
		JSInterop.SetupModule(CopyEvents.Module)
			.SetupVoid("answerCopy", _ => true).SetException(new JSDisconnectedException("Circuit gone"));
		IRenderedComponent<MokaCopyButton> cut = Render<MokaCopyButton>(p => p.Add(x => x.Text, "").Add(x => x.TextSource, () => "log"));

		await Button(cut).AskAsync();

		Assert.Equal("Copy", Button(cut).GetAttribute("aria-label"));
	}

	// The browser stops waiting and reports the failure, which is what says so.
	[Fact]
	public async Task AScriptError_WhileAnswering_DoesNotEscape()
	{
		JSInterop.SetupModule(CopyEvents.Module)
			.SetupVoid("answerCopy", _ => true).SetException(new JSException("Module failed"));
		IRenderedComponent<MokaCopyButton> cut = Render<MokaCopyButton>(p => p.Add(x => x.Text, "").Add(x => x.TextSource, () => "log"));

		await Button(cut).AskAsync();

		Assert.Equal("Copy", Button(cut).GetAttribute("aria-label"));
	}
}
