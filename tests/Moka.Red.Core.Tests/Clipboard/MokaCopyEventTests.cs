using System.Reflection;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Clipboard;

namespace Moka.Red.Core.Tests.Clipboard;

// Copying happens in the browser, inside the click: Safari, iOS and Firefox refused the copy a .NET click handler made a
// round trip later. The browser half can't run here, so these check the .NET half and that the browser half is wired
// where Blazor and the components look for it.
public class MokaCopyEventTests
{
	[Fact]
	public void BothEvents_AreRegisteredForRazor_WithTheirArguments()
	{
		Dictionary<string, EventHandlerAttribute> events = typeof(EventHandlers)
			.GetCustomAttributes<EventHandlerAttribute>()
			.ToDictionary(a => a.AttributeName, StringComparer.Ordinal);

		Assert.Equal(2, events.Count);
		Assert.Equal(typeof(MokaCopyEventArgs), events["onmokacopied"].EventArgsType);
		Assert.Equal(typeof(EventArgs), events["onmokacopyask"].EventArgsType);
		Assert.All(events.Values, e => Assert.True(e.EnableStopPropagation && e.EnablePreventDefault));
	}

	// Blazor builds the arguments with System.Text.Json, which the trimmer can't follow.
	[Fact]
	public void TheTrimmerDescriptor_KeepsTheArguments()
	{
		Assembly core = typeof(MokaCopyEventArgs).Assembly;
		using Stream? descriptor = core.GetManifestResourceStream("ILLink.Descriptors.xml");
		Assert.NotNull(descriptor);

		using StreamReader reader = new(descriptor);
		Assert.Contains("fullname=\"Moka.Red.Core.Clipboard.MokaCopyEventArgs\" preserve=\"all\"", reader.ReadToEnd(),
			StringComparison.Ordinal);
	}

	[Fact]
	public void TheInitializer_RegistersBothEvents_AndStartsTheListener()
	{
		string text = Script("Moka.Red.Core.lib.module.js");

		Assert.Contains("import { listenForCopies } from './moka-copy.js';", text, StringComparison.Ordinal);
		Assert.Contains("registerCustomEventType('mokacopied'", text, StringComparison.Ordinal);
		Assert.Contains("copied: e.detail?.copied === true", text, StringComparison.Ordinal);
		Assert.Contains("registerCustomEventType('mokacopyask'", text, StringComparison.Ordinal);
		Assert.Contains("listenForCopies();", text, StringComparison.Ordinal);
	}

	// A listener on the document in the capture phase runs inside the click whatever the page's own handlers do, and
	// no copy waits on .NET before it starts.
	[Fact]
	public void TheListener_CopiesInsideTheClick()
	{
		string text = Script("moka-copy.js");

		Assert.Contains("document.addEventListener('click', onClick, true);", text, StringComparison.Ordinal);
		Assert.Contains("navigator.clipboard.writeText(text)", text, StringComparison.Ordinal);
		Assert.Contains("new ClipboardItem({ 'text/plain': blob })", text, StringComparison.Ordinal);
		Assert.Contains("new CustomEvent('mokacopied', { bubbles: true, detail: { copied } })", text, StringComparison.Ordinal);
		Assert.Contains("new CustomEvent('mokacopyask', { bubbles: true })", text, StringComparison.Ordinal);
		Assert.Contains("export function answerCopy(element, text)", text, StringComparison.Ordinal);
		Assert.DoesNotContain("invokeMethodAsync", text, StringComparison.Ordinal);
	}

	// The old name stays for code that imported it, and there is one copy of the copying code.
	[Fact]
	public void TheDragModule_HandsCopyToClipboardOn()
	{
		string text = Script("moka-drag.js");

		Assert.Contains("export { copyText as copyToClipboard } from './moka-copy.js';", text, StringComparison.Ordinal);
		Assert.DoesNotContain("execCommand", text, StringComparison.Ordinal);
	}

	[Fact]
	public void TheArguments_SayWhetherItCopied() =>
		Assert.False(new MokaCopyEventArgs().Copied);

	private static string Script(string name) =>
		File.ReadAllText(Path.Combine(RepositoryRoot(), "src", "Moka.Red.Core", "wwwroot", name)).ReplaceLineEndings("\n");

	private static string RepositoryRoot()
	{
		for (DirectoryInfo? folder = new(AppContext.BaseDirectory); folder is not null; folder = folder.Parent)
		{
			if (File.Exists(Path.Combine(folder.FullName, "Moka.Red.slnx")))
			{
				return folder.FullName;
			}
		}

		throw new InvalidOperationException("The repository root was not found.");
	}
}
