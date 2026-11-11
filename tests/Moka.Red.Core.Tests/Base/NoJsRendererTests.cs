using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using Moka.Red.Core.Base;

namespace Moka.Red.Core.Tests.Base;

// A renderer with no JS behind it, such as the docs site's static preview renderer, answers an import with null.
// The module helpers treat that as no JS, so a component that unbinds on disposal doesn't throw.
public sealed class NoJsRendererTests
{
	[Fact]
	public async Task AModuleCall_WithNoModule_DoesNothing()
	{
		await using ServiceProvider services = new ServiceCollection()
			.AddLogging()
			.AddSingleton<IJSRuntime>(new NoModulesJs())
			.BuildServiceProvider();
		HtmlRenderer renderer = new(services, NullLoggerFactory.Instance);

		await renderer.Dispatcher.InvokeAsync(() => renderer.RenderComponentAsync<UnbindsOnDispose>(ParameterView.Empty));

		await renderer.DisposeAsync();
	}

	private sealed class UnbindsOnDispose : MokaComponentBase
	{
		protected override string RootClass => "test-unbinds";

		protected override async ValueTask DisposeAsyncCore()
		{
			await SafeModuleInvokeVoidAsync("./_content/Tests/unbind.js", "unbind");
			await SafeModuleInvokeAsync<bool>("./_content/Tests/unbind.js", "isBound");
			await base.DisposeAsyncCore();
		}
	}

	private sealed class NoModulesJs : IJSRuntime
	{
		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => new(default(TValue)!);

		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
			new(default(TValue)!);
	}
}
