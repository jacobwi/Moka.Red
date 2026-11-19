using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.HtmlRendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using Moka.Red.Core.Theming;

namespace Moka.Red.Core.Tests.Theming;

/// <summary>
///     A provider drawn by ASP.NET Core's static renderer, as prerendering and static pages draw it. bUnit's markup doesn't
///     HTML-encode text the way that renderer does, so only this shows what reaches a style element.
/// </summary>
internal static partial class StaticProviderHtml
{
	public static async Task<string> RenderAsync(Dictionary<string, object?> parameters)
	{
		await using ServiceProvider services = new ServiceCollection()
			.AddLogging()
			.AddSingleton<IJSRuntime>(new NoJs())
			.BuildServiceProvider();
		await using HtmlRenderer renderer = new(services, NullLoggerFactory.Instance);

		return await renderer.Dispatcher.InvokeAsync(async () =>
		{
			HtmlRootComponent root = await renderer.RenderComponentAsync<MokaThemeProvider>(ParameterView.FromDictionary(parameters));
			return root.ToHtmlString();
		});
	}

	/// <summary>The text of every style element, in page order.</summary>
	public static IReadOnlyList<string> Styles(string html) => [.. Style().Matches(html).Select(match => match.Groups[1].Value)];

	[GeneratedRegex("<style[^>]*>(.*?)</style>", RegexOptions.Singleline)]
	private static partial Regex Style();

	private sealed class NoJs : IJSRuntime
	{
		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => new(default(TValue)!);

		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
			new(default(TValue)!);
	}
}
