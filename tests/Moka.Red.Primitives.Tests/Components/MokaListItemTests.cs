using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Moka.Red.Primitives.List;

namespace Moka.Red.Primitives.Tests.Components;

// Enter and Space are handled by Core's moka-keys.js in the browser, so these tests cover the
// wiring: which rows take focus, and when the list binds its key listener.
public class MokaListItemTests : BunitContext
{
	private const string KeysModule = "./_content/Moka.Red.Core/moka-keys.js";

	[Fact]
	public void ClickableItem_IsATabStop()
	{
		SetupListModule();

		IRenderedComponent<MokaList> cut = Render<MokaList>(p => p
			.AddChildContent<MokaListItem>(item => item
				.Add(x => x.Text, "Inbox")
				.Add(x => x.OnClick, _ => { })));

		IElement row = cut.Find(".moka-list-item");
		Assert.Equal("0", row.GetAttribute("tabindex"));
		Assert.True(row.ClassList.Contains("moka-list-item--interactive"));
	}

	[Fact]
	public void ContextMenuOnlyItem_IsATabStop()
	{
		SetupListModule();

		IRenderedComponent<MokaList> cut = Render<MokaList>(p => p
			.AddChildContent<MokaListItem>(item => item
				.Add(x => x.Text, "Inbox")
				.Add(x => x.OnContextMenu, _ => { })));

		Assert.Equal("0", cut.Find(".moka-list-item").GetAttribute("tabindex"));
	}

	[Fact]
	public void StaticList_TakesNoFocusAndLoadsNoScript()
	{
		IRenderedComponent<MokaList> cut = Render<MokaList>(p => p
			.AddChildContent<MokaListItem>(item => item
				.Add(x => x.Text, "Inbox")));

		IElement row = cut.Find(".moka-list-item");
		Assert.False(row.HasAttribute("tabindex"));
		Assert.False(row.ClassList.Contains("moka-list-item--interactive"));
		Assert.Empty(JSInterop.Invocations);
	}

	[Fact]
	public void ClickableItems_BindKeysOnTheListOnce()
	{
		BunitJSModuleInterop module = SetupListModule();

		IRenderedComponent<MokaList> cut = Render<MokaList>(p => p
			.AddChildContent<MokaListItem>(item => item
				.Add(x => x.Text, "Inbox")
				.Add(x => x.OnClick, _ => { }))
			.AddChildContent<MokaListItem>(item => item
				.Add(x => x.Text, "Drafts")
				.Add(x => x.OnClick, _ => { })));

		JSRuntimeInvocation bind = module.VerifyInvoke("bindActivation");
		ElementReference list = Assert.IsType<ElementReference>(bind.Arguments[0]);
		Assert.Equal(list.Id, cut.Find(".moka-list").GetAttribute("blazor:elementReference"));
		Assert.Equal(".moka-list-item--interactive", bind.Arguments[1]);
	}

	[Fact]
	public void Click_InvokesOnClick()
	{
		// The key listener activates a row with click(), so this is the path Enter and Space take.
		SetupListModule();
		bool clicked = false;

		IRenderedComponent<MokaList> cut = Render<MokaList>(p => p
			.AddChildContent<MokaListItem>(item => item
				.Add(x => x.Text, "Inbox")
				.Add(x => x.OnClick, _ => { clicked = true; })));

		cut.Find(".moka-list-item").Click();

		Assert.True(clicked);
	}

	[Fact]
	public void DisabledItem_IsSkippedAndInert()
	{
		SetupListModule();
		bool clicked = false;

		IRenderedComponent<MokaList> cut = Render<MokaList>(p => p
			.AddChildContent<MokaListItem>(item => item
				.Add(x => x.Text, "Inbox")
				.Add(x => x.Disabled, true)
				.Add(x => x.OnClick, _ => { clicked = true; })));
		IElement row = cut.Find(".moka-list-item");

		row.Click();

		Assert.False(clicked);
		Assert.False(row.HasAttribute("tabindex"));
		Assert.Equal("true", row.GetAttribute("aria-disabled"));
	}

	[Fact]
	public void LinkItem_KeepsItsNativeFocus()
	{
		IRenderedComponent<MokaList> cut = Render<MokaList>(p => p
			.AddChildContent<MokaListItem>(item => item
				.Add(x => x.Text, "Docs")
				.Add(x => x.Href, "/docs")
				.Add(x => x.OnClick, _ => { })));

		IElement link = cut.Find("a.moka-list-item");
		Assert.False(link.HasAttribute("tabindex"));
		Assert.False(link.ClassList.Contains("moka-list-item--interactive"));
		Assert.Empty(JSInterop.Invocations);
	}

	// role="listitem" on the <a> itself would replace the link role, so a wrapper carries it.
	[Fact]
	public void LinkItem_KeepsItsLinkRole()
	{
		IRenderedComponent<MokaList> cut = Render<MokaList>(p => p
			.AddChildContent<MokaListItem>(item => item
				.Add(x => x.Text, "Docs")
				.Add(x => x.Href, "/docs")));

		IElement link = cut.Find("a.moka-list-item");
		Assert.False(link.HasAttribute("role"));
		Assert.Equal("listitem", link.ParentElement?.GetAttribute("role"));
	}

	[Fact]
	public void DisabledLinkItem_StillSaysItIsALink()
	{
		IRenderedComponent<MokaList> cut = Render<MokaList>(p => p
			.AddChildContent<MokaListItem>(item => item
				.Add(x => x.Text, "Docs")
				.Add(x => x.Href, "/docs")
				.Add(x => x.Disabled, true)));

		IElement link = cut.Find("a.moka-list-item");
		Assert.False(link.HasAttribute("href"));
		Assert.Equal("link", link.GetAttribute("role"));
		Assert.Equal("true", link.GetAttribute("aria-disabled"));
	}

	[Fact]
	public void ItemOutsideAList_StaysOutOfTheTabOrder()
	{
		// Nothing would handle Enter without a MokaList, so the row does not take focus.
		IRenderedComponent<MokaListItem> cut = Render<MokaListItem>(p => p
			.Add(x => x.Text, "Inbox")
			.Add(x => x.OnClick, _ => { }));

		Assert.False(cut.Find(".moka-list-item").HasAttribute("tabindex"));
		Assert.Empty(JSInterop.Invocations);
	}

	private BunitJSModuleInterop SetupListModule()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(KeysModule);
		module.SetupVoid("bindActivation", _ => true).SetVoidResult();
		return module;
	}
}
