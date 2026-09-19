using System.Diagnostics;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Moka.Red.Primitives.CodeBlock;

namespace Moka.Red.Primitives.Tests.Components;

// The copy handler caught only JSException, so a lost circuit or a cancelled call escaped it and Blazor
// treated it as an unhandled error. The reset ran through ContinueWith on a thread-pool thread and
// changed the component's state there.
public class MokaCodeBlockTests : BunitContext
{
	private const string Module = "./_content/Moka.Red.Core/moka-drag.js";
	private const string Code = "dotnet add package Moka.Red";

	private readonly BunitJSModuleInterop _module;

	public MokaCodeBlockTests() => _module = JSInterop.SetupModule(Module);

	private static IElement CopyButton(IRenderedComponent<MokaCodeBlock> cut) => cut.Find(".moka-code-block__copy");

	private IRenderedComponent<MokaCodeBlock> RenderBlock() =>
		Render<MokaCodeBlock>(p => p.Add(x => x.Code, Code));

	private static void AssertNotCopied(IRenderedComponent<MokaCodeBlock> cut)
	{
		IElement button = CopyButton(cut);
		Assert.Equal("Copy", button.GetAttribute("title"));
		Assert.DoesNotContain("moka-code-block__copy--copied", button.ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public async Task ACopy_ShowsTheCopiedState_ThenResets()
	{
		_module.Setup<bool>("copyToClipboard", Code).SetResult(true);
		IRenderedComponent<MokaCodeBlock> cut = RenderBlock();

		await CopyButton(cut).ClickAsync(new MouseEventArgs());

		Assert.Equal("Copied!", CopyButton(cut).GetAttribute("title"));
		Assert.Contains("moka-code-block__copy--copied", CopyButton(cut).ClassName, StringComparison.Ordinal);
		cut.WaitForAssertion(() => AssertNotCopied(cut), TimeSpan.FromSeconds(5));
	}

	[Fact]
	public async Task ALostCircuit_DoesNotEscapeTheClick()
	{
		_module.Setup<bool>("copyToClipboard", _ => true).SetException(new JSDisconnectedException("Circuit gone"));
		IRenderedComponent<MokaCodeBlock> cut = RenderBlock();

		await CopyButton(cut).ClickAsync(new MouseEventArgs());

		AssertNotCopied(cut);
	}

	[Fact]
	public async Task ACancelledCall_DoesNotEscapeTheClick()
	{
		_module.Setup<bool>("copyToClipboard", _ => true).SetCanceled();
		IRenderedComponent<MokaCodeBlock> cut = RenderBlock();

		await CopyButton(cut).ClickAsync(new MouseEventArgs());

		AssertNotCopied(cut);
	}

	[Fact]
	public async Task ATornDownRuntime_DoesNotEscapeTheClick()
	{
		_module.Setup<bool>("copyToClipboard", _ => true).SetException(new ObjectDisposedException("JSRuntime"));
		IRenderedComponent<MokaCodeBlock> cut = RenderBlock();

		await CopyButton(cut).ClickAsync(new MouseEventArgs());

		AssertNotCopied(cut);
	}

	[Fact]
	public async Task AScriptError_LeavesTheButtonAsItWas()
	{
		_module.Setup<bool>("copyToClipboard", _ => true).SetException(new JSException("Clipboard blocked"));
		IRenderedComponent<MokaCodeBlock> cut = RenderBlock();

		await CopyButton(cut).ClickAsync(new MouseEventArgs());

		AssertNotCopied(cut);
	}

	[Fact]
	public async Task ARefusedCopy_LeavesTheButtonAsItWas()
	{
		_module.Setup<bool>("copyToClipboard", _ => true).SetResult(false);
		IRenderedComponent<MokaCodeBlock> cut = RenderBlock();

		await CopyButton(cut).ClickAsync(new MouseEventArgs());

		AssertNotCopied(cut);
	}

	// Two copies that finished together both cancelled the first copy's timer, and the second then
	// disposed the timer the first had just started without cancelling it. That timer later reset
	// the button in the middle of a newer copy's two seconds.
	[Fact]
	public async Task OnlyTheLatestCopy_ResetsTheButton()
	{
		using var copies = new PendingCopies();
		_module.AddInvocationHandler(copies);
		IRenderedComponent<MokaCodeBlock> cut = RenderBlock();

		await CopyAsync(cut, copies, 0);
		Task second = CopyButton(cut).ClickAsync(new MouseEventArgs());
		Task third = CopyButton(cut).ClickAsync(new MouseEventArgs());
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
			Assert.Equal("Copied!", await cut.InvokeAsync(() => CopyButton(cut).GetAttribute("title")));
			await Task.Delay(50, Xunit.TestContext.Current.CancellationToken);
		}

		cut.WaitForAssertion(() => Assert.Equal("Copy", CopyButton(cut).GetAttribute("title")), TimeSpan.FromSeconds(3));
	}

	private static async Task CopyAsync(IRenderedComponent<MokaCodeBlock> cut, PendingCopies copies, int call)
	{
		Task click = CopyButton(cut).ClickAsync(new MouseEventArgs());
		copies.WaitFor(call + 1);
		copies.Complete(call);
		await click;
	}
}
