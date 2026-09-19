using System.Diagnostics;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Moka.Red.Primitives.Utility;

namespace Moka.Red.Primitives.Tests.Components;

// The click handler caught only JSException, so a lost circuit or a cancelled call escaped it and
// Blazor treated it as an unhandled error.
public class MokaCopyButtonTests : BunitContext
{
	private const string Module = "./_content/Moka.Red.Core/moka-drag.js";

	private readonly BunitJSModuleInterop _module;

	public MokaCopyButtonTests() => _module = JSInterop.SetupModule(Module);

	private static IElement Button(IRenderedComponent<MokaCopyButton> cut) => cut.Find("button");

	[Fact]
	public async Task ACopy_ShowsTheCopiedState_ThenResets()
	{
		_module.Setup<bool>("copyToClipboard", "npm i moka").SetResult(true);
		IRenderedComponent<MokaCopyButton> cut = Render<MokaCopyButton>(p => p.Add(x => x.Text, "npm i moka"));

		await Button(cut).ClickAsync(new MouseEventArgs());

		Assert.Contains("moka-copy-btn--copied", Button(cut).ClassName, StringComparison.Ordinal);
		Assert.Equal("Copied!", Button(cut).GetAttribute("title"));
		cut.WaitForAssertion(() => Assert.Equal("Copy", Button(cut).GetAttribute("title")), TimeSpan.FromSeconds(5));
		Assert.DoesNotContain("moka-copy-btn--copied", Button(cut).ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public async Task ALostCircuit_DoesNotEscapeTheClick()
	{
		_module.Setup<bool>("copyToClipboard", _ => true).SetException(new JSDisconnectedException("Circuit gone"));
		IRenderedComponent<MokaCopyButton> cut = Render<MokaCopyButton>(p => p.Add(x => x.Text, "abc"));

		await Button(cut).ClickAsync(new MouseEventArgs());

		Assert.Equal("Copy", Button(cut).GetAttribute("title"));
	}

	[Fact]
	public async Task ACancelledCall_DoesNotEscapeTheClick()
	{
		_module.Setup<bool>("copyToClipboard", _ => true).SetCanceled();
		IRenderedComponent<MokaCopyButton> cut = Render<MokaCopyButton>(p => p.Add(x => x.Text, "abc"));

		await Button(cut).ClickAsync(new MouseEventArgs());

		Assert.Equal("Copy", Button(cut).GetAttribute("title"));
	}

	[Fact]
	public async Task AScriptError_LeavesTheButtonAsItWas()
	{
		_module.Setup<bool>("copyToClipboard", _ => true).SetException(new JSException("Clipboard blocked"));
		IRenderedComponent<MokaCopyButton> cut = Render<MokaCopyButton>(p => p.Add(x => x.Text, "abc"));

		await Button(cut).ClickAsync(new MouseEventArgs());

		Assert.Equal("Copy", Button(cut).GetAttribute("title"));
	}

	[Fact]
	public async Task ARefusedCopy_LeavesTheButtonAsItWas()
	{
		_module.Setup<bool>("copyToClipboard", _ => true).SetResult(false);
		IRenderedComponent<MokaCopyButton> cut = Render<MokaCopyButton>(p => p.Add(x => x.Text, "abc"));

		await Button(cut).ClickAsync(new MouseEventArgs());

		Assert.DoesNotContain("moka-copy-btn--copied", Button(cut).ClassName, StringComparison.Ordinal);
	}

	// Two copies that finished together both cancelled the first copy's timer, and the second then
	// disposed the timer the first had just started without cancelling it. That timer later reset
	// the button in the middle of a newer copy's two seconds.
	[Fact]
	public async Task OnlyTheLatestCopy_ResetsTheButton()
	{
		using var copies = new PendingCopies();
		_module.AddInvocationHandler(copies);
		IRenderedComponent<MokaCopyButton> cut = Render<MokaCopyButton>(p => p.Add(x => x.Text, "abc"));

		await CopyAsync(cut, copies, 0);
		Task second = Button(cut).ClickAsync(new MouseEventArgs());
		Task third = Button(cut).ClickAsync(new MouseEventArgs());
		copies.WaitFor(3);
		copies.Complete(1);
		copies.Complete(2);
		await Task.WhenAll(second, third);

		await Task.Delay(TimeSpan.FromSeconds(1), Xunit.TestContext.Current.CancellationToken);
		await CopyAsync(cut, copies, 3);
		var sinceLatest = Stopwatch.StartNew();

		// A timer left from the second or third copy would fire about a second from now.
		while (sinceLatest.Elapsed < TimeSpan.FromSeconds(1.7))
		{
			Assert.Equal("Copied!", await cut.InvokeAsync(() => Button(cut).GetAttribute("title")));
			await Task.Delay(50, Xunit.TestContext.Current.CancellationToken);
		}

		cut.WaitForAssertion(() => Assert.Equal("Copy", Button(cut).GetAttribute("title")), TimeSpan.FromSeconds(3));
	}

	private static async Task CopyAsync(IRenderedComponent<MokaCopyButton> cut, PendingCopies copies, int call)
	{
		Task click = Button(cut).ClickAsync(new MouseEventArgs());
		copies.WaitFor(call + 1);
		copies.Complete(call);
		await click;
	}
}
