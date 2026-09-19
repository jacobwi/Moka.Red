using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Moka.Red.Navigation.Extensions;
using Moka.Red.Navigation.Tabs;
using Moka.Red.Navigation.Tabs.Models;
using Moka.Red.Navigation.Tabs.Services;

namespace Moka.Red.Navigation.Tests.Components;

// ITabStorageProvider shipped with a browser implementation that nothing registered or called.
// StorageKey is the opt-in that uses it.
public class MokaTabContainerStorageTests : BunitContext
{
	private const string Key = "tabs";
	private readonly MemoryStorage _storage = new();

	public MokaTabContainerStorageTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		JSInterop.SetupModule("./_content/Moka.Red.Navigation/moka-tabs.js").Mode = JSRuntimeMode.Loose;

		// Registered first on purpose: AddMokaTabs only adds the browser provider when none exists.
		Services.AddSingleton<ITabStorageProvider>(_storage);
		Services.AddMokaTabs<string>();
	}

	private IMokaTabSessionState<string> State => Services.GetRequiredService<IMokaTabSessionState<string>>();

	[Fact]
	public async Task SavedTabs_AreRestoredOnTheFirstRender()
	{
		_storage.Items[Key] = await SavedSessionAsync("saved");

		IRenderedComponent<MokaTabContainer<string>> cut = RenderContainer(Key);

		Assert.Equal(["saved"], State.Tabs.Select(t => t.Id));
		Assert.Contains("Tab saved", cut.Markup, StringComparison.Ordinal);
	}

	[Fact]
	public async Task EveryChange_IsSaved()
	{
		IRenderedComponent<MokaTabContainer<string>> cut = RenderContainer(Key);

		await cut.InvokeAsync(() => State.AddTabAsync(new TabInfo<string> { Id = "one", Title = "One" }));
		Assert.Equal(["one"], await IdsInAsync(_storage.Items[Key]));

		await cut.InvokeAsync(() => State.AddTabAsync(new TabInfo<string> { Id = "two", Title = "Two" }));
		Assert.Equal(["one", "two"], await IdsInAsync(_storage.Items[Key]));
	}

	// Remounting after navigating away and back must keep the live tabs: the saved copies have
	// lost their ContentParameters.
	[Fact]
	public async Task AContainerMountedAgain_KeepsTheLiveTabs()
	{
		IRenderedComponent<MokaTabContainer<string>> first = RenderContainer(Key);
		await first.InvokeAsync(() => State.AddTabAsync(new TabInfo<string> { Id = "live", Title = "Live" }));
		await DisposeComponentsAsync();

		_storage.Items[Key] = await SavedSessionAsync("stale");
		RenderContainer(Key);

		Assert.Equal(["live"], State.Tabs.Select(t => t.Id));
		Assert.Equal(["live"], await IdsInAsync(_storage.Items[Key]));
	}

	[Fact]
	public async Task WithoutAStorageKey_NothingIsReadOrSaved()
	{
		IRenderedComponent<MokaTabContainer<string>> cut = RenderContainer(null);

		await cut.InvokeAsync(() => State.AddTabAsync(new TabInfo<string> { Id = "one", Title = "One" }));

		Assert.Equal(0, _storage.Loads);
		Assert.Equal(0, _storage.Saves);
	}

	[Fact]
	public async Task AnUnreadableSession_IsReplaced()
	{
		_storage.Items[Key] = "{ not json";

		RenderContainer(Key);

		Assert.Empty(State.Tabs);
		Assert.Empty(await IdsInAsync(_storage.Items[Key]));
	}

	// Saving after a failed load could overwrite a session the browser still holds.
	[Fact]
	public async Task WhenStorageCannotBeRead_NothingIsSaved()
	{
		_storage.FailLoads = true;
		IRenderedComponent<MokaTabContainer<string>> cut = RenderContainer(Key);

		await cut.InvokeAsync(() => State.AddTabAsync(new TabInfo<string> { Id = "one", Title = "One" }));

		Assert.Equal(0, _storage.Saves);
	}

	private IRenderedComponent<MokaTabContainer<string>> RenderContainer(string? storageKey) =>
		Render<MokaTabContainer<string>>(p => p
			.Add(x => x.StorageKey, storageKey)
			.Add(x => x.DefaultTabContent, tab => $"Tab {tab.Id}"));

	private static async Task<string> SavedSessionAsync(params string[] ids)
	{
		await using ServiceProvider provider = new ServiceCollection().AddMokaTabs<string>().BuildServiceProvider();
		IMokaTabSessionState<string> state = provider.GetRequiredService<IMokaTabSessionState<string>>();
		foreach (string id in ids)
		{
			await state.AddTabAsync(new TabInfo<string> { Id = id, Title = id });
		}

		return state.SerializeState();
	}

	private static async Task<List<string>> IdsInAsync(string json)
	{
		await using ServiceProvider provider = new ServiceCollection().AddMokaTabs<string>().BuildServiceProvider();
		IMokaTabSessionState<string> state = provider.GetRequiredService<IMokaTabSessionState<string>>();
		await state.RestoreStateAsync(json);
		return state.Tabs.Select(t => t.Id).ToList();
	}

	private sealed class MemoryStorage : ITabStorageProvider
	{
		public Dictionary<string, string> Items { get; } = new(StringComparer.Ordinal);

		public int Loads { get; private set; }

		public int Saves { get; private set; }

		public bool FailLoads { get; set; }

		public Task SaveAsync(string key, string value)
		{
			Saves++;
			Items[key] = value;
			return Task.CompletedTask;
		}

		public Task<string?> LoadAsync(string key)
		{
			Loads++;
			return FailLoads
				? throw new JSException("Storage is blocked.")
				: Task.FromResult(Items.GetValueOrDefault(key));
		}

		public Task RemoveAsync(string key)
		{
			Items.Remove(key);
			return Task.CompletedTask;
		}
	}
}
