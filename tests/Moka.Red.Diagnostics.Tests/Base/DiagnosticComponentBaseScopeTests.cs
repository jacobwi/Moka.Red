using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Moka.Red.Diagnostics.Extensions;

namespace Moka.Red.Diagnostics.Tests.Base;

// Prerendering renders into a scoped HtmlRenderer, and disposing the request's scope disposes the
// renderer and its components. A tracked component that first looked the diagnostics service up while
// disposing hit the disposed provider and failed the request.
public class DiagnosticComponentBaseScopeTests
{
	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task StaticRender_ThenScopeDisposal_DoesNotThrow(bool registerDiagnostics)
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddScoped<IJSRuntime, NoJsRuntime>();
		services.AddScoped<HtmlRenderer>();
		if (registerDiagnostics)
		{
			services.AddMokaDiagnostics();
		}

		await using ServiceProvider provider = services.BuildServiceProvider();
		AsyncServiceScope scope = provider.CreateAsyncScope();
		HtmlRenderer renderer = scope.ServiceProvider.GetRequiredService<HtmlRenderer>();
		await renderer.Dispatcher.InvokeAsync(() =>
			renderer.RenderComponentAsync<DiagnosticComponentBaseTests.TestDiagComponent>());

		Exception? error = await Record.ExceptionAsync(async () => await scope.DisposeAsync());

		Assert.Null(error);
	}

	// Prerendering has no JS runtime to call either.
	private sealed class NoJsRuntime : IJSRuntime
	{
		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
			throw new InvalidOperationException("No JS runtime while rendering statically.");

		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken,
			object?[]? args) =>
			throw new InvalidOperationException("No JS runtime while rendering statically.");
	}
}
