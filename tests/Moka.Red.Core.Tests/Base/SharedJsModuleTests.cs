using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;
using Moka.Red.Core.Base;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Core.Tests.Base;

public class SharedJsModuleTests : BunitContext
{
	private const string PathA = "./_content/Tests/a.js";
	private const string PathB = "./_content/Tests/b.js";

	public SharedJsModuleTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		JSInterop.SetupModule(PathA).Mode = JSRuntimeMode.Loose;
		JSInterop.SetupModule(PathB).Mode = JSRuntimeMode.Loose;
	}

	[Fact]
	public async Task TwoComponents_ImportOnce()
	{
		IRenderedComponent<ModuleUser> first = Render<ModuleUser>();
		IRenderedComponent<ModuleUser> second = Render<ModuleUser>();

		IJSObjectReference a = await first.Instance.Module(PathA);
		IJSObjectReference b = await second.Instance.Module(PathA);

		Assert.Same(a, b);
		Assert.Single(JSInterop.Invocations["import"]);
	}

	[Fact]
	public async Task ComponentsAskingAtTheSameTime_ShareTheImportInFlight()
	{
		using ControlledImport pending = new("./_content/Tests/slow.js");
		JSInterop.AddInvocationHandler(pending);
		IRenderedComponent<ModuleUser> first = Render<ModuleUser>();
		IRenderedComponent<ModuleUser> second = Render<ModuleUser>();

		Task<IJSObjectReference> a = first.Instance.Module("./_content/Tests/slow.js").AsTask();
		Task<IJSObjectReference> b = second.Instance.Module("./_content/Tests/slow.js").AsTask();
		Assert.False(a.IsCompleted);
		Assert.False(b.IsCompleted);

		await using FakeModule module = new();
		pending.Complete(module);

		Assert.Same(await a, await b);
		Assert.Single(JSInterop.Invocations["import"]);
	}

	[Fact]
	public async Task OneComponent_GetsEachPathItAsksFor()
	{
		// It used to keep the first module it imported and hand that back for any path.
		IRenderedComponent<ModuleUser> cut = Render<ModuleUser>();

		IJSObjectReference a = await cut.Instance.Module(PathA);
		IJSObjectReference b = await cut.Instance.Module(PathB);

		Assert.NotSame(a, b);
		Assert.Equal(2, JSInterop.Invocations["import"].Count);
	}

	[Fact]
	public async Task AFailedImport_IsTriedAgain()
	{
		const string path = "./_content/Tests/flaky.js";
		using ControlledImport import = new(path);
		JSInterop.AddInvocationHandler(import);
		IRenderedComponent<ModuleUser> cut = Render<ModuleUser>();

		Task<IJSObjectReference> failing = cut.Instance.Module(path).AsTask();
		import.Cancel();
		await Assert.ThrowsAsync<TaskCanceledException>(() => failing);

		await using FakeModule module = new();
		import.Complete(module);

		Assert.Same(module, await cut.Instance.Module(path));
		Assert.Equal(2, JSInterop.Invocations["import"].Count);
	}

	[Fact]
	public async Task AnImportThatThrows_IsNotKept()
	{
		// That's how prerendering fails: the runtime throws from InvokeAsync, with no task to attach to.
		FailingOnce runtime = new() { Failure = FailingOnce.How.Throws };
		Assert.Throws<InvalidOperationException>(() =>
		{
			_ = MokaJsModules.Get(runtime, PathA, out _);
		});

		IJSObjectReference module = await MokaJsModules.Get(runtime, PathA, out bool started);

		Assert.True(started);
		Assert.NotNull(module);
	}

	[Fact]
	public async Task AnImportAlreadyFailedWhenItReturns_IsNotKept()
	{
		FailingOnce runtime = new() { Failure = FailingOnce.How.ReturnsFailed };
		await Assert.ThrowsAsync<InvalidOperationException>(() => MokaJsModules.Get(runtime, PathA, out _));

		IJSObjectReference module = await MokaJsModules.Get(runtime, PathA, out bool started);

		Assert.True(started);
		Assert.NotNull(module);
	}

	[Fact]
	public async Task DisposingAComponent_LeavesItsNeighbourWorking()
	{
		const string path = "./_content/Tests/kept.js";
		await using FakeModule module = new();
		using ControlledImport import = new(path);
		JSInterop.AddInvocationHandler(import);
		import.Complete(module);
		IRenderedComponent<ModuleUser> gone = Render<ModuleUser>();
		IRenderedComponent<ModuleUser> kept = Render<ModuleUser>();
		await gone.Instance.Module(path);

		await gone.Instance.DisposeAsync();

		Assert.Equal(0, module.DisposeCalls);
		Assert.Same(module, await kept.Instance.Module(path));
		Assert.Single(JSInterop.Invocations["import"]);
	}

	[Fact]
	public async Task ARuntimeOfItsOwn_ImportsForItself()
	{
		// Each circuit has its own runtime, and a handle from one is meaningless in another.
		FailingOnce first = new();
		FailingOnce second = new();

		IJSObjectReference a = await MokaJsModules.Get(first, PathA, out bool startedFirst);
		IJSObjectReference b = await MokaJsModules.Get(second, PathA, out bool startedSecond);

		Assert.True(startedFirst);
		Assert.True(startedSecond);
		Assert.NotSame(a, b);
	}

	/// <summary>Exposes the protected module lookup.</summary>
	public sealed class ModuleUser : MokaComponentBase
	{
		protected override string RootClass => "module-user";

		public ValueTask<IJSObjectReference> Module(string path) => GetJsModuleAsync(path);

		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
			ArgumentNullException.ThrowIfNull(builder);
			builder.OpenElement(0, "div");
			builder.CloseElement();
		}
	}

	private sealed class FailingOnce : IJSRuntime
	{
		private readonly FakeModule _module = new();

		public enum How
		{
			Never,
			Throws,
			ReturnsFailed
		}

		public How Failure { get; set; }

		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
			InvokeAsync<TValue>(identifier, CancellationToken.None, args);

		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
		{
			How failure = Failure;
			Failure = How.Never;
			var unavailable = new InvalidOperationException("JavaScript interop calls cannot be issued at this time.");
			return failure switch
			{
				How.Throws => throw unavailable,
				How.ReturnsFailed => ValueTask.FromException<TValue>(unavailable),
				_ => ValueTask.FromResult((TValue)(object)_module)
			};
		}
	}

	private sealed class FakeModule : IJSObjectReference
	{
		public int DisposeCalls { get; private set; }

		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => ValueTask.FromResult(default(TValue)!);

		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
			ValueTask.FromResult(default(TValue)!);

		public ValueTask DisposeAsync()
		{
			DisposeCalls++;
			return ValueTask.CompletedTask;
		}
	}
}
