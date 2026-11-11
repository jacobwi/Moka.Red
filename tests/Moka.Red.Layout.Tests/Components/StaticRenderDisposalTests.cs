using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using Moka.Red.Layout.StatusBar;

namespace Moka.Red.Layout.Tests.Components;

// The docs site draws its previews with a static renderer whose JS runtime loads no modules, so an import answers null.
// MokaStatusBar's disposal asked that null module to unbind, and every docs deploy failed from 0.3.15 on with
// "Value cannot be null. (Parameter 'jsObjectReference')".
public sealed class StaticRenderDisposalTests
{
	[Fact]
	public async Task AStatusBarDrawnStatically_DisposesWithoutJs()
	{
		await using ServiceProvider services = new ServiceCollection()
			.AddLogging()
			.AddSingleton<IJSRuntime>(new NoModulesJs())
			.BuildServiceProvider();
		HtmlRenderer renderer = new(services, NullLoggerFactory.Instance);

		string html = await renderer.Dispatcher.InvokeAsync(async () =>
			(await renderer.RenderComponentAsync<MokaStatusBar>(ParameterView.FromDictionary(
				new Dictionary<string, object?> { [nameof(MokaStatusBar.Expandable)] = true }))).ToHtmlString());

		Assert.Contains("moka-statusbar", html, StringComparison.Ordinal);
		await renderer.DisposeAsync();
	}

	private sealed class NoModulesJs : IJSRuntime
	{
		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => new(default(TValue)!);

		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
			new(default(TValue)!);
	}
}
