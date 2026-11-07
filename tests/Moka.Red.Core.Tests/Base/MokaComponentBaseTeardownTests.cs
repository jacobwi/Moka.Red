using Bunit;
using Microsoft.AspNetCore.Components.Rendering;
using Moka.Red.Core.Base;
using Moka.Red.Tests.Shared;

#pragma warning disable CA2000 // The runtime keeps each stand-in module, as it keeps a real one, and the tests check nothing disposes it.

namespace Moka.Red.Core.Tests.Base;

// Blazor disposes every component of a circuit that has ended, and a JS call made then fails with a
// TaskCanceledException. From a component's disposal that was logged as an unhandled exception of the
// circuit, so the base swallows it for the cleanup a derived class does.
public class MokaComponentBaseTeardownTests : BunitContext
{
	private const string ModulePath = "./cleanup.js";

	[Fact]
	public async Task ACleanupCanceledAsTheCircuitEnds_ThrowsNothing_AndLeavesTheSharedModuleAlone()
	{
		EndingModule module = new();
		using ControlledImport import = new(ModulePath);
		IRenderedComponent<CleanupComponent> cut = RenderWithModule(import, module);
		cut.Instance.CleanupFailure = new TaskCanceledException();

		await DisposeComponentsAsync();

		Assert.False(Renderer.UnhandledException.IsCompleted);
		Assert.Equal(0, module.DisposeAttempts);
	}

	// Only a lost circuit is quiet: any other failure in the cleanup is still a bug worth hearing about.
	[Fact]
	public async Task ACleanupThatFailsForAnotherReason_StillFails_AndLeavesTheSharedModuleAlone()
	{
		EndingModule module = new();
		using ControlledImport import = new(ModulePath);
		IRenderedComponent<CleanupComponent> cut = RenderWithModule(import, module);
		InvalidOperationException failure = new("A bug in the cleanup.");
		cut.Instance.CleanupFailure = failure;

		Exception? thrown = await Record.ExceptionAsync(DisposeComponentsAsync);

		Exception reported = thrown ?? await Renderer.UnhandledException.WaitAsync(TimeSpan.FromSeconds(5),
			Xunit.TestContext.Current.CancellationToken);
		Assert.Same(failure, reported);
		Assert.Equal(0, module.DisposeAttempts);
	}

	private IRenderedComponent<CleanupComponent> RenderWithModule(ControlledImport import, EndingModule module)
	{
		JSInterop.AddInvocationHandler(import);
		import.Complete(module);

		IRenderedComponent<CleanupComponent> cut = Render<CleanupComponent>();
		cut.WaitForAssertion(() => Assert.True(cut.Instance.ModuleLoaded));
		return cut;
	}

	public sealed class CleanupComponent : MokaComponentBase
	{
		public Exception? CleanupFailure { get; set; }

		public bool ModuleLoaded { get; private set; }

		protected override string RootClass => "cleanup-component";

		protected override async Task OnAfterRenderAsync(bool firstRender)
		{
			if (firstRender)
			{
				await GetJsModuleAsync(ModulePath);
				ModuleLoaded = true;
			}
		}

		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
			ArgumentNullException.ThrowIfNull(builder);
			builder.OpenElement(0, "div");
			builder.AddAttribute(1, "class", CssClass);
			builder.CloseElement();
		}

		protected override ValueTask DisposeAsyncCore() =>
			CleanupFailure is null ? base.DisposeAsyncCore() : ValueTask.FromException(CleanupFailure);
	}
}
