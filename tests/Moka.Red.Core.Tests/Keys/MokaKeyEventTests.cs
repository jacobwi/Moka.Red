using System.Reflection;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Keys;

namespace Moka.Red.Core.Tests.Keys;

// @onmokakeydown is a keydown whose arguments say who owns the press. The browser works the owner out, so these check
// the .NET half and that the browser half is where Blazor looks for it.
public class MokaKeyEventTests
{
	[Fact]
	public void TheEvent_IsRegisteredForRazor_WithItsArguments()
	{
		EventHandlerAttribute attribute = Assert.Single(typeof(EventHandlers).GetCustomAttributes<EventHandlerAttribute>());

		Assert.Equal("onmokakeydown", attribute.AttributeName);
		Assert.Equal(typeof(MokaKeyEventArgs), attribute.EventArgsType);
		Assert.True(attribute.EnableStopPropagation);
		Assert.True(attribute.EnablePreventDefault);
	}

	[Theory]
	[InlineData(MokaKeyOwner.None, false)]
	[InlineData(MokaKeyOwner.Field, true)]
	[InlineData(MokaKeyOwner.Control, true)]
	[InlineData(MokaKeyOwner.Modal, true)]
	public void Owned_IsAnyOwnerButNone(MokaKeyOwner owner, bool owned) =>
		Assert.Equal(owned, new MokaKeyEventArgs { Owner = owner }.Owned);

	// The browser sends the owner as a number, so the enum's values are the contract.
	[Fact]
	public void TheOwnersNumbers_MatchTheBrowsers()
	{
		Assert.Equal(0, (int)MokaKeyOwner.None);
		Assert.Equal(1, (int)MokaKeyOwner.Field);
		Assert.Equal(2, (int)MokaKeyOwner.Control);
		Assert.Equal(3, (int)MokaKeyOwner.Modal);
	}

	// Blazor builds the arguments with System.Text.Json, which the trimmer can't follow: without the descriptor a trimmed
	// WebAssembly app lost the constructor and every key event failed (scripts/wasm-smoke.cjs checks the real thing).
	[Fact]
	public void TheTrimmerDescriptor_KeepsTheArguments()
	{
		Assembly core = typeof(MokaKeyEventArgs).Assembly;
		using Stream? descriptor = core.GetManifestResourceStream("ILLink.Descriptors.xml");
		Assert.NotNull(descriptor);

		using StreamReader reader = new(descriptor);
		Assert.Contains("fullname=\"Moka.Red.Core.Keys.MokaKeyEventArgs\" preserve=\"all\"", reader.ReadToEnd(), StringComparison.Ordinal);
	}

	// Blazor loads a package's JavaScript initializer only by this exact name, and calls one hook or the other by host.
	[Fact]
	public void TheInitializer_HasTheNameBlazorLoads_AndBothHooks()
	{
		string text = File.ReadAllText(Path.Combine(RepositoryRoot(), "src", "Moka.Red.Core", "wwwroot", "Moka.Red.Core.lib.module.js"));

		Assert.Contains("export function afterWebStarted(blazor)", text, StringComparison.Ordinal);
		Assert.Contains("export function afterStarted(blazor)", text, StringComparison.Ordinal);
		Assert.Contains("registerCustomEventType('mokakeydown'", text, StringComparison.Ordinal);
		Assert.Contains("browserEventName: 'keydown'", text, StringComparison.Ordinal);
	}

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
