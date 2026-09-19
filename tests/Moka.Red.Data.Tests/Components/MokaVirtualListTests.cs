using System.Globalization;
using System.Reflection;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
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
