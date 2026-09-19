using System.Diagnostics.CodeAnalysis;
using Bunit;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Core.Base;
using Moka.Red.Diagnostics.Components.Panels;
using Moka.Red.Diagnostics.Extensions;
using Moka.Red.Diagnostics.Services;

namespace Moka.Red.Diagnostics.Tests.Components;

// Nothing called RecordJsInteropCall, so the Network tab was always empty while its empty state said
// calls made through SafeJsInvokeAsync were tracked.
public class JsInteropTrackingTests : BunitContext
{
	private const string ModulePath = "./_content/Tests/interop-test.js";

	public JsInteropTrackingTests()
	{
		Services.AddMokaDiagnostics();
		JSInterop.Mode = JSRuntimeMode.Loose;
		JSInterop.SetupModule(ModulePath).Mode = JSRuntimeMode.Loose;
	}

	[Fact]
	public void SafeJsCalls_AreRecorded()
	{
		Render<InteropComponent>();

		IReadOnlyList<JsInteropEntry> entries = Services.GetRequiredService<IMokaDiagnosticsService>().GetJsInteropEntries();
		Assert.Contains(entries, e => e.Identifier == "interopTest.ping" && e.CallCount == 1);
		Assert.Contains(entries, e => e.Identifier == "interopTest.read" && e.CallCount == 1);
	}

	[Fact]
	public void ModuleCalls_RecordTheImportAndTheFunction()
	{
		Render<InteropComponent>();

		IReadOnlyList<JsInteropEntry> entries = Services.GetRequiredService<IMokaDiagnosticsService>().GetJsInteropEntries();
		Assert.Contains(entries, e => e.Identifier == "import" && e.CallCount == 1);
		Assert.Contains(entries, e => e.Identifier == "bindThing" && e.CallCount == 2);
	}

	[Fact]
	public void NetworkPanel_ListsTheCalls()
	{
		Render<InteropComponent>();

		IRenderedComponent<NetworkPanel> cut = Render<NetworkPanel>();

		Assert.Contains("interopTest.ping", cut.Markup, StringComparison.Ordinal);
	}

	// MokaInputBase has interop helpers of its own, which reported nothing, so the calls form
	// inputs make never reached the Network tab.
	[Fact]
	public void InputModuleCalls_RecordTheImportAndTheFunction()
	{
		Render<InteropInput>();

		IReadOnlyList<JsInteropEntry> entries = Services.GetRequiredService<IMokaDiagnosticsService>().GetJsInteropEntries();
		Assert.Contains(entries, e => e.Identifier == "import" && e.CallCount == 1);
		Assert.Contains(entries, e => e.Identifier == "bindInput" && e.CallCount == 2);
	}

	/// <summary>Makes one call through each of MokaComponentBase's interop helpers.</summary>
	public sealed class InteropComponent : MokaComponentBase
	{
		protected override string RootClass => "interop-test";

		protected override async Task OnAfterRenderAsync(bool firstRender)
		{
			if (!firstRender)
			{
				return;
			}

			await SafeJsInvokeVoidAsync("interopTest.ping");
			await SafeJsInvokeAsync<string>("interopTest.read");
			await SafeModuleInvokeVoidAsync(ModulePath, "bindThing");
			await SafeModuleInvokeAsync<string>(ModulePath, "bindThing");
		}

		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
			ArgumentNullException.ThrowIfNull(builder);
			builder.OpenElement(0, "div");
			builder.AddAttribute(1, "class", CssClass);
			builder.CloseElement();
		}
	}

	/// <summary>Makes two calls through MokaInputBase's module helper, which imports the module once.</summary>
	public sealed class InteropInput : MokaInputBase<string>
	{
		protected override string RootClass => "interop-input";

		protected override async Task OnAfterRenderAsync(bool firstRender)
		{
			if (!firstRender)
			{
				return;
			}

			await SafeModuleInvokeVoidAsync(ModulePath, "bindInput");
			await SafeModuleInvokeVoidAsync(ModulePath, "bindInput");
		}

		protected override bool TryParseValueFromString(string? value, out string result,
			[NotNullWhen(false)] out string? validationErrorMessage)
		{
			result = value ?? string.Empty;
			validationErrorMessage = null;
			return true;
		}

		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
			ArgumentNullException.ThrowIfNull(builder);
			builder.OpenElement(0, "input");
			builder.AddAttribute(1, "class", ComponentCssClass);
			builder.CloseElement();
		}
	}
}
