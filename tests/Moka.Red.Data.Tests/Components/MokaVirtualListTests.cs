using System.Globalization;
using System.Reflection;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.JSInterop;
using Moka.Red.Data.VirtualList;

namespace Moka.Red.Data.Tests.Components;

// With OnItemClick the list is a listbox with one tab stop. The keys are read in
// MokaVirtualList.razor.js, which calls the list's [JSInvokable] methods by name; these tests call
// them the same way, by name, from the reference the script is handed.
public class MokaVirtualListTests : BunitContext
{
	private const string Module = "./_content/Moka.Red.Data/VirtualList/MokaVirtualList.razor.js";

	private static readonly string[] Files = ["alpha.cs", "beta.cs", "gamma.cs", "delta.cs", "epsilon.cs"];

	private const int RemoteCount = 1000;

	private readonly BunitJSModuleInterop _module;

	public MokaVirtualListTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_module = JSInterop.SetupModule(Module);
	}

	[Fact]
	public void WithOnItemClick_TheListIsAListboxWithOneTabStop()
	{
		IRenderedComponent<MokaVirtualList<string>> cut = RenderList(_ => { });

		IElement list = cut.Find(".moka-virtual-list");
		Assert.Equal("0", list.GetAttribute("tabindex"));
		Assert.Equal("listbox", list.GetAttribute("role"));
		Assert.False(list.HasAttribute("aria-activedescendant"));

		IReadOnlyList<IElement> options = cut.FindAll("[role=option]");
		Assert.Equal(Files.Length, options.Count);
		Assert.All(options, option =>
		{
			Assert.False(option.HasAttribute("tabindex"));
			Assert.Equal("false", option.GetAttribute("aria-selected"));
			Assert.Equal("5", option.GetAttribute("aria-setsize"));
		});
		Assert.Equal("3", options[2].GetAttribute("aria-posinset"));
		Assert.Equal(Files.Length, options.Select(o => o.Id).Distinct().Count());
	}

	[Fact]
	public void WithoutOnItemClick_TheListTakesNoFocus()
	{
		IRenderedComponent<MokaVirtualList<string>> cut = RenderList(null);

		IElement list = cut.Find(".moka-virtual-list");
		Assert.False(list.HasAttribute("tabindex"));
		Assert.False(list.HasAttribute("role"));
		Assert.Empty(cut.FindAll("[role=option]"));
		Assert.Empty(_module.Invocations);
	}

	[Fact]
	public void TheList_BindsItsKeysOnce()
	{
		IRenderedComponent<MokaVirtualList<string>> cut = RenderList(_ => { });

		JSRuntimeInvocation bind = _module.VerifyInvoke("bindList");
		ElementReference list = Assert.IsType<ElementReference>(bind.Arguments[0]);
		Assert.Equal(list.Id, cut.Find(".moka-virtual-list").GetAttribute("blazor:elementReference"));
	}

	[Fact]
	public async Task NavigationKeys_MoveTheActiveItem()
	{
		IRenderedComponent<MokaVirtualList<string>> cut = RenderList(_ => { });

		// The first key starts at the first item in view.
		Assert.Equal(2, await CallFromScriptAsync<int>(cut, "MoveActive", "ArrowDown", 2, 3));
		AssertActive(cut, 2);

		Assert.Equal(3, await CallFromScriptAsync<int>(cut, "MoveActive", "ArrowDown", 0, 3));
		Assert.Equal(4, await CallFromScriptAsync<int>(cut, "MoveActive", "End", 0, 3));
		Assert.Equal(4, await CallFromScriptAsync<int>(cut, "MoveActive", "ArrowDown", 0, 3));
		Assert.Equal(1, await CallFromScriptAsync<int>(cut, "MoveActive", "PageUp", 0, 3));
		Assert.Equal(0, await CallFromScriptAsync<int>(cut, "MoveActive", "Home", 0, 3));
		Assert.Equal(0, await CallFromScriptAsync<int>(cut, "MoveActive", "ArrowUp", 0, 3));
		AssertActive(cut, 0);
	}

	[Fact]
	public async Task EnterAndSpace_ActivateTheActiveItem()
	{
		string? clicked = null;
		IRenderedComponent<MokaVirtualList<string>> cut = RenderList(item => clicked = item);

		await CallFromScriptAsync<int>(cut, "MoveActive", "ArrowDown", 1, 3);
		await CallFromScriptAsync<object>(cut, "ActivateActive");

		Assert.Equal("beta.cs", clicked);
	}

	[Fact]
	public async Task AClickedItem_BecomesTheActiveOne()
	{
		string? clicked = null;
		IRenderedComponent<MokaVirtualList<string>> cut = RenderList(item => clicked = item);

		await cut.FindAll(".moka-virtual-list__item")[3].ClickAsync(new MouseEventArgs());

		Assert.Equal("delta.cs", clicked);
		AssertActive(cut, 3);
	}

	// A comma-decimal culture turned the item height into "36,5px", which CSS drops.
	[Fact]
	public void ItemHeight_IsValidCss_InAnyCulture()
	{
		CultureInfo culture = CultureInfo.CurrentCulture;
		CultureInfo.CurrentCulture = new CultureInfo("de-DE");
		try
		{
			IRenderedComponent<MokaVirtualList<string>> cut = RenderList(null, 36.5f);

			Assert.Contains("height: 36.5px", cut.Find(".moka-virtual-list__item").GetAttribute("style"),
				StringComparison.Ordinal);
		}
		finally
		{
			CultureInfo.CurrentCulture = culture;
		}
	}

	private static void AssertActive(IRenderedComponent<MokaVirtualList<string>> cut, int index)
	{
		IReadOnlyList<IElement> options = cut.FindAll("[role=option]");
		Assert.Equal(options[index].Id, cut.Find(".moka-virtual-list").GetAttribute("aria-activedescendant"));
		for (int i = 0; i < options.Count; i++)
		{
			Assert.Equal(i == index ? "true" : "false", options[i].GetAttribute("aria-selected"));
		}
	}

	// What the script does: take the DotNetObjectReference it was bound with and call a
	// [JSInvokable] method on it by name.
	private async Task<T?> CallFromScriptAsync<T>(IRenderedComponent<MokaVirtualList<string>> cut, string method,
		params object[] args)
	{
		JSRuntimeInvocation bind = _module.VerifyInvoke("bindList");
		object target = bind.Arguments[1]!.GetType().GetProperty("Value")!.GetValue(bind.Arguments[1])!;
		MethodInfo invokable = target.GetType().GetMethods()
			.Single(m => m.Name == method && m.GetCustomAttribute<JSInvokableAttribute>() is not null);

		object? result = null;
		await cut.InvokeAsync(async () =>
		{
			var task = (Task)invokable.Invoke(target, args)!;
			await task;
			result = task.GetType().GetProperty("Result")?.GetValue(task);
		});
		return (T?)result;
	}

	private List<ItemsProviderRequest> Requests { get; } = [];

	private ValueTask<ItemsProviderResult<string>> Provide(ItemsProviderRequest request)
	{
		Requests.Add(request);
		string[] items = [.. Enumerable.Range(request.StartIndex, Math.Min(request.Count, RemoteCount - request.StartIndex))
			.Select(i => $"row-{i}")];
		return ValueTask.FromResult(new ItemsProviderResult<string>(items, RemoteCount));
	}

	private IRenderedComponent<MokaVirtualList<string>> RenderRemote(Action<string>? onItemClick = null) =>
		Render<MokaVirtualList<string>>(p =>
		{
			p.Add(x => x.ItemsProvider, Provide)
				.Add(x => x.ItemHeight, 30f)
				.Add(x => x.ItemTemplate, item => builder => builder.AddContent(0, item));
			if (onItemClick is not null)
			{
				p.Add(x => x.OnItemClick, onItemClick);
			}
		});

	[Fact]
	public void ItemsProvider_RendersWhatItReturns()
	{
		IRenderedComponent<MokaVirtualList<string>> cut = RenderRemote();

		Assert.NotEmpty(Requests);
		Assert.Contains("row-0", cut.Find(".moka-virtual-list__item").TextContent, StringComparison.Ordinal);
	}

	// Only the rendered items are in the page, so the position has to come from the whole list.
	[Fact]
	public void ItemsProvider_OptionsCountTheWholeList()
	{
		IRenderedComponent<MokaVirtualList<string>> cut = RenderRemote(_ => { });

		IElement first = cut.Find("[role=option]");
		Assert.Equal(RemoteCount.ToString(CultureInfo.InvariantCulture), first.GetAttribute("aria-setsize"));
		Assert.Equal("1", first.GetAttribute("aria-posinset"));
	}

	[Fact]
	public async Task ItemsProvider_End_MovesToTheLastItemOfTheWholeList()
	{
		IRenderedComponent<MokaVirtualList<string>> cut = RenderRemote(_ => { });

		int moved = await cut.InvokeAsync(() => cut.Instance.MoveActive("End", 0, 10));

		Assert.Equal(RemoteCount - 1, moved);
	}

	[Fact]
	public async Task ItemsProvider_Enter_RaisesTheActiveItemWhenItsLoaded()
	{
		string? clicked = null;
		IRenderedComponent<MokaVirtualList<string>> cut = RenderRemote(item => clicked = item);

		await cut.InvokeAsync(async () =>
		{
			await cut.Instance.MoveActive("Home", 0, 10);
			await cut.Instance.ActivateActive();
		});

		Assert.Equal("row-0", clicked);
	}

	[Fact]
	public async Task ItemsProvider_Enter_DoesNothingForAnItemNotLoadedYet()
	{
		string? clicked = null;
		IRenderedComponent<MokaVirtualList<string>> cut = RenderRemote(item => clicked = item);

		await cut.InvokeAsync(async () =>
		{
			await cut.Instance.MoveActive("End", 0, 10);
			await cut.Instance.ActivateActive();
		});

		Assert.Null(clicked);
	}

	[Fact]
	public async Task ItemsProvider_ActiveItemStaysInsideAShorterList()
	{
		int count = 100;
		IRenderedComponent<MokaVirtualList<string>> cut = Render<MokaVirtualList<string>>(p => p
			.Add(x => x.ItemsProvider, request => ValueTask.FromResult(new ItemsProviderResult<string>(
				Enumerable.Range(0, Math.Min(request.Count, count)).Select(i => $"row-{i}"), count)))
			.Add(x => x.ItemHeight, 30f)
			.Add(x => x.OnItemClick, (string _) => { })
			.Add(x => x.ItemTemplate, item => builder => builder.AddContent(0, item)));

		await cut.InvokeAsync(() => cut.Instance.MoveActive("End", 0, 10));
		count = 3;
		await cut.InvokeAsync(cut.Instance.RefreshDataAsync);

		int moved = await cut.InvokeAsync(() => cut.Instance.MoveActive("ArrowDown", 0, 10));
		Assert.Equal(2, moved);
	}

	[Fact]
	public async Task RefreshDataAsync_AsksTheProviderAgain()
	{
		IRenderedComponent<MokaVirtualList<string>> cut = RenderRemote();
		int before = Requests.Count;

		await cut.InvokeAsync(cut.Instance.RefreshDataAsync);

		Assert.True(Requests.Count > before);
	}

	[Fact]
	public async Task RefreshDataAsync_IsSafeBeforeARenderAndWithItems()
	{
		await using MokaVirtualList<string> unrendered = new();
		await unrendered.RefreshDataAsync();

		IRenderedComponent<MokaVirtualList<string>> cut = RenderList(null);
		await cut.InvokeAsync(cut.Instance.RefreshDataAsync);
		Assert.Equal(Files.Length, cut.FindAll(".moka-virtual-list__item").Count);
	}

	[Fact]
	public void ItemsAndItemsProvider_CantBothBeSet()
	{
		Assert.Throws<InvalidOperationException>(() => Render<MokaVirtualList<string>>(p => p
			.Add(x => x.Items, Files)
			.Add(x => x.ItemsProvider, Provide)
			.Add(x => x.ItemHeight, 30f)
			.Add(x => x.ItemTemplate, item => builder => builder.AddContent(0, item))));
	}

	[Fact]
	public async Task ItemsProvider_AnAnswerThatComesLater_StillRenders()
	{
		TaskCompletionSource<ItemsProviderResult<string>> answer = new();
		IRenderedComponent<MokaVirtualList<string>> cut = Render<MokaVirtualList<string>>(p => p
			.Add(x => x.ItemsProvider, _ => new ValueTask<ItemsProviderResult<string>>(answer.Task))
			.Add(x => x.ItemHeight, 30f)
			.Add(x => x.ItemTemplate, item => builder => builder.AddContent(0, item)));

		await cut.InvokeAsync(() => answer.SetResult(new ItemsProviderResult<string>(["row-0"], 1)));

		cut.WaitForAssertion(() => Assert.Contains("row-0", cut.Markup, StringComparison.Ordinal));
	}

	[Fact]
	public void ItemsProvider_PassesTheCancellationTokenThrough()
	{
		RenderRemote();

		Assert.All(Requests, request => Assert.True(request.CancellationToken.CanBeCanceled));
	}

	private IRenderedComponent<MokaVirtualList<string>> RenderList(Action<string>? onItemClick, float itemHeight = 36) =>
		Render<MokaVirtualList<string>>(p =>
		{
			p.Add(x => x.Items, Files)
				.Add(x => x.ItemHeight, itemHeight)
				.Add(x => x.ItemTemplate, item => builder => builder.AddContent(0, item));
			if (onItemClick is not null)
			{
				p.Add(x => x.OnItemClick, onItemClick);
			}
		});
}
