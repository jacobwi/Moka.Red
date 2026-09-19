using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Primitives.SplitButton;

namespace Moka.Red.Primitives.Tests.Components;

// The dropdown half had no keyboard route: the toggle closed the menu as soon as it lost focus, so
// focus could never reach the items. It also had no name and did not say it opens a menu.
public class MokaSplitButtonTests : BunitContext
{
	private const string Module = "./_content/Moka.Red.Primitives/SplitButton/MokaSplitButton.razor.js";

	private readonly BunitJSModuleInterop _module;
	private int _itemClicks;
	private int _itemKeys;

	public MokaSplitButtonTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_module = JSInterop.SetupModule(Module);
	}

	[Fact]
	public async Task TheToggle_IsANamedMenuButton_AndTheMenuIsLabelledByIt()
	{
		IRenderedComponent<MokaSplitButton> cut = RenderSplitButton();
		IElement toggle = cut.Find(".moka-split-btn__toggle");

		Assert.Equal("More options", toggle.GetAttribute("aria-label"));
		Assert.Equal("menu", toggle.GetAttribute("aria-haspopup"));
		Assert.Equal("false", toggle.GetAttribute("aria-expanded"));

		await toggle.ClickAsync(new MouseEventArgs());

		toggle = cut.Find(".moka-split-btn__toggle");
		IElement menu = cut.Find("[role=menu]");
		Assert.Equal("true", toggle.GetAttribute("aria-expanded"));
		Assert.Equal(menu.Id, toggle.GetAttribute("aria-controls"));
		Assert.Equal(toggle.Id, menu.GetAttribute("aria-labelledby"));
	}

	[Fact]
	public async Task ChoosingAnItem_RunsIt_AndClosesTheMenu()
	{
		IRenderedComponent<MokaSplitButton> cut = RenderSplitButton();
		await cut.Find(".moka-split-btn__toggle").ClickAsync(new MouseEventArgs());

		await cut.Find("[role=menuitem]").ClickAsync(new MouseEventArgs());

		Assert.Equal(1, _itemClicks);
		Assert.Empty(cut.FindAll("[role=menu]"));
	}

	[Fact]
	public void TheMenuScript_IsBoundOnlyWhenThereIsAMenu()
	{
		IRenderedComponent<MokaSplitButton> cut = RenderSplitButton();
		JSRuntimeInvocation bind = _module.VerifyInvoke("bindSplitButton");
		Assert.IsType<ElementReference>(bind.Arguments[0]);

		Render<MokaSplitButton>(p => p.AddChildContent("Save"));
		_module.VerifyInvoke("bindSplitButton", 1);
	}

	// Escape inside the open menu closed a MokaDialog around the button as well.
	[Fact]
	public async Task KeysInTheOpenMenu_DoNotReachAContainer()
	{
		int keys = 0;
		IRenderedComponent<KeyCounter> host = Render<KeyCounter>(p => p
			.Add(x => x.OnKey, () => keys++)
			.Add(x => x.ChildContent, (RenderFragment)(b =>
			{
				b.OpenComponent<MokaSplitButton>(0);
				b.AddAttribute(1, nameof(MokaSplitButton.ChildContent), (RenderFragment)(c => c.AddContent(0, "Save")));
				b.AddAttribute(2, nameof(MokaSplitButton.DropdownContent), (RenderFragment)Items);
				b.CloseComponent();
			})));

		await host.Find(".moka-split-btn__toggle").ClickAsync(new MouseEventArgs());
		await host.Find("[role=menuitem]").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

		Assert.Equal(1, _itemKeys);
		Assert.Equal(0, keys);
	}

	private IRenderedComponent<MokaSplitButton> RenderSplitButton() =>
		Render<MokaSplitButton>(p => p
			.AddChildContent("Save")
			.Add(x => x.DropdownContent, (RenderFragment)Items));

	private void Items(RenderTreeBuilder b)
	{
		b.OpenElement(0, "div");
		b.AddAttribute(1, "role", "menuitem");
		b.AddAttribute(2, "tabindex", "-1");
		b.AddAttribute(3, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, () => _itemClicks++));
		b.AddAttribute(4, "onkeydown", EventCallback.Factory.Create<KeyboardEventArgs>(this, () => _itemKeys++));
		b.AddContent(5, "Save as");
		b.CloseElement();
	}

	/// <summary>Counts the keys that reach it, as a MokaDialog's handler would see them.</summary>
	public sealed class KeyCounter : ComponentBase
	{
		[Parameter] public RenderFragment? ChildContent { get; set; }

		[Parameter] public Action? OnKey { get; set; }

		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
			ArgumentNullException.ThrowIfNull(builder);
			builder.OpenElement(0, "div");
			builder.AddAttribute(1, "onkeydown", EventCallback.Factory.Create<KeyboardEventArgs>(this, () => OnKey?.Invoke()));
			builder.AddContent(2, ChildContent);
			builder.CloseElement();
		}
	}
}
