#pragma warning disable CA1031 // A component that can't draw with no parameters is skipped, whatever it throws.
#pragma warning disable CA2000 // Each renderer is disposed by the loop, which is what the test checks.

using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using Moka.Red.ContextMenu;
using Moka.Red.ContextMenu.Extensions;
using Moka.Red.Core.Base;
using Moka.Red.Core.Extensions;
using Moka.Red.Data.Table;
using Moka.Red.Feedback.Dialog;
using Moka.Red.Feedback.Extensions;
using Moka.Red.Forms.TextField;
using Moka.Red.Layout.Card;
using Moka.Red.Navigation.Menu;
using Moka.Red.Primitives.Button;

namespace Moka.Red.Data.Tests.Conventions;

// The docs site draws its previews with a static renderer whose JS runtime loads no modules, so an import answers null,
// and disposes them afterwards. From 0.3.15 to 0.3.22 one component's disposal threw there, and every docs deploy
// failed without anyone noticing. Every component that renders this way has to dispose this way too.
public sealed class StaticRenderDisposalTests
{
	private static readonly Assembly[] Packages =
	[
		typeof(MokaComponentBase).Assembly,
		typeof(MokaButton).Assembly,
		typeof(MokaCard).Assembly,
		typeof(MokaTextField).Assembly,
		typeof(MokaDialog).Assembly,
		typeof(MokaTable<>).Assembly,
		typeof(MokaMenu).Assembly,
		typeof(MokaContextMenu).Assembly
	];

	[Fact]
	public async Task EveryComponentDrawnWithoutJs_DisposesWithoutJs()
	{
		List<string> failures = [];
		int disposed = 0;

		foreach (Type component in Components())
		{
			await using ServiceProvider services = Services();
			HtmlRenderer renderer = new(services, NullLoggerFactory.Instance);
			try
			{
				await renderer.Dispatcher
					.InvokeAsync(() => renderer.RenderComponentAsync(component, ParameterView.Empty))
					.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
			}
			catch (Exception)
			{
				// Most components that can't draw with no parameters need one, or a service only a host has.
				await DisposeQuietly(renderer);
				continue;
			}

			try
			{
				await renderer.DisposeAsync();
				disposed++;
			}
			catch (Exception ex)
			{
				failures.Add($"{component.Name}: {ex.GetBaseException().Message}");
			}
		}

		Assert.Empty(failures);
		Assert.True(disposed > 150, $"Only {disposed} components drew with no parameters, so the check covers too little.");
	}

	private static IEnumerable<Type> Components()
	{
		foreach (Type type in Packages.SelectMany(static assembly => assembly.GetExportedTypes()))
		{
			if (type.IsAbstract || !typeof(IComponent).IsAssignableFrom(type) || type.GetConstructor(Type.EmptyTypes) is null)
			{
				continue;
			}

			if (!type.IsGenericTypeDefinition)
			{
				yield return type;
				continue;
			}

			Type? closed = null;
			try
			{
				closed = type.MakeGenericType(type.GetGenericArguments().Select(static _ => typeof(string)).ToArray());
			}
			catch (ArgumentException)
			{
			}

			if (closed is not null)
			{
				yield return closed;
			}
		}
	}

	private static ServiceProvider Services() => new ServiceCollection()
		.AddLogging()
		.AddSingleton<IJSRuntime>(new NoModulesJs())
		.AddMokaRedCore()
		.AddMokaHotkeys()
		.AddMokaPageTitles()
		.AddMokaFeedback()
		.AddMokaContextMenu()
		.BuildServiceProvider();

	private static async Task DisposeQuietly(HtmlRenderer renderer)
	{
		try
		{
			await renderer.DisposeAsync();
		}
		catch (Exception)
		{
		}
	}

	private sealed class NoModulesJs : IJSRuntime
	{
		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => new(default(TValue)!);

		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
			new(default(TValue)!);
	}
}
