using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace Moka.Red.Tests.Shared;

/// <summary>
///     Checks a floating panel against Core's moka-popup.js, which shows it in the top layer so no ancestor clips it and
///     no stacking context covers it: the panel is a manual popover, and the component asks the module to open it
///     against its anchor after the render that adds it and to close it after the one that removes it. Linked into the
///     test projects that need it.
/// </summary>
internal static class TopLayerPopup
{
	public const string ModulePath = "./_content/Moka.Red.Core/moka-popup.js";

	/// <summary>Plans moka-popup.js on the context, so its calls can be read back.</summary>
	public static BunitJSModuleInterop Setup(BunitJSInterop jsInterop)
	{
		BunitJSModuleInterop module = jsInterop.SetupModule(ModulePath);
		module.SetupVoid("open", _ => true).SetVoidResult();
		module.SetupVoid("close", _ => true).SetVoidResult();
		return module;
	}

	/// <summary>
	///     Asserts that <paramref name="panel" /> is a manual popover and that the last open call named it, the anchor with
	///     the given element reference id, and the placement. Returns the options the call passed.
	/// </summary>
	public static IReadOnlyDictionary<string, object?> AssertOpened(BunitJSModuleInterop module, IElement panel,
		string? anchorId, string placement)
	{
		Assert.Equal("manual", panel.GetAttribute("popover"));
		Assert.True(panel.HasAttribute("data-moka-popup"), "The panel lacks data-moka-popup, so moka.css leaves the browser's popover box on it.");

		JSRuntimeInvocation open = module.Invocations["open"][^1];
		string? panelId = Assert.IsType<ElementReference>(open.Arguments[1]).Id;
		Assert.False(string.IsNullOrEmpty(panelId));

		// bUnit writes the reference id only on the render that creates the element (gotcha 15), so a panel that has
		// rendered again since can only be told apart from the anchor.
		string? renderedPanelId = panel.GetAttribute("blazor:elementReference");
		if (!string.IsNullOrEmpty(renderedPanelId))
		{
			Assert.Equal(renderedPanelId, panelId);
		}

		Assert.False(string.IsNullOrEmpty(anchorId));
		Assert.Equal(anchorId, Assert.IsType<ElementReference>(open.Arguments[2]).Id);
		Assert.NotEqual(anchorId, panelId);

		IReadOnlyDictionary<string, object?> options = Assert.IsAssignableFrom<IReadOnlyDictionary<string, object?>>(open.Arguments[4]);
		Assert.Equal(placement, options["placement"]);
		return options;
	}

	/// <summary>
	///     Asserts that <paramref name="panel" /> is a manual popover that the last open call lifted without an anchor or
	///     options, for a panel that places itself, with <paramref name="backdrop" /> shown under it, or no backdrop when
	///     that is null.
	/// </summary>
	public static void AssertLifted(BunitJSModuleInterop module, IElement panel, IElement? backdrop)
	{
		JSRuntimeInvocation open = module.Invocations["open"][^1];
		string? panelId = AssertPopover(panel, open.Arguments[1]);
		Assert.Null(open.Arguments[2]);
		Assert.Null(open.Arguments[4]);

		if (backdrop is null)
		{
			Assert.Null(open.Arguments[3]);
			return;
		}

		Assert.NotEqual(panelId, AssertPopover(backdrop, open.Arguments[3]));
	}

	// The element is a manual popover, and the reference passed for it is its own while bUnit still shows which that is.
	private static string? AssertPopover(IElement element, object? reference)
	{
		Assert.Equal("manual", element.GetAttribute("popover"));
		Assert.True(element.HasAttribute("data-moka-popup"), "The element lacks data-moka-popup, so moka.css leaves the browser's popover box on it.");

		string? id = Assert.IsType<ElementReference>(reference).Id;
		Assert.False(string.IsNullOrEmpty(id));

		string? renderedId = element.GetAttribute("blazor:elementReference");
		if (!string.IsNullOrEmpty(renderedId))
		{
			Assert.Equal(renderedId, id);
		}

		return id;
	}

	/// <summary>The key the last open call used, which close has to repeat.</summary>
	public static string OpenedKey(BunitJSModuleInterop module) =>
		Assert.IsType<string>(module.Invocations["open"][^1].Arguments[0]);

	/// <summary>Asserts that the last close call named the key the last open call used.</summary>
	public static void AssertClosed(BunitJSModuleInterop module)
	{
		string key = OpenedKey(module);
		Assert.Contains(module.Invocations["close"], call => key.Equals(call.Arguments[0] as string, StringComparison.Ordinal));
	}

	/// <summary>The number of calls made to a function of the module.</summary>
	public static int Count(BunitJSModuleInterop module, string identifier) => module.Invocations[identifier].Count;
}
