using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Feedback.SlashMenu;

namespace Moka.Red.Feedback.Tests.Components;

public class MokaSlashMenuTests : BunitContext
{
	private static IReadOnlyList<MokaSlashMenuItem> Items =>
	[
		new() { Title = "Heading", Keywords = "h1 title", Category = "struct" },
		new() { Title = "Bullet list", Keywords = "ul", Category = "struct" },
		new() { Title = "Code block", Keywords = "fence", Category = "code" }
	];

	[Fact]
	public void Closed_RendersNothing()
	{
		IRenderedComponent<MokaSlashMenu> cut = Render<MokaSlashMenu>(p => p
			.Add(x => x.Open, false)
			.Add(x => x.Items, Items));

		Assert.Empty(cut.FindAll(".moka-slash-menu"));
	}

	[Fact]
	public void Open_RendersListboxWithOptions()
	{
		IRenderedComponent<MokaSlashMenu> cut = Render<MokaSlashMenu>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Items, Items));

		Assert.Equal("listbox", cut.Find(".moka-slash-menu-list").GetAttribute("role"));
		Assert.Equal(3, cut.FindAll("[role='option']").Count);
	}

	[Fact]
	public void Query_FiltersByTitle()
	{
		IRenderedComponent<MokaSlashMenu> cut = Render<MokaSlashMenu>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Items, Items)
			.Add(x => x.Query, "code"));

		Assert.Single(cut.FindAll("[role='option']"));
	}

	[Fact]
	public void Query_FiltersByKeyword()
	{
		IRenderedComponent<MokaSlashMenu> cut = Render<MokaSlashMenu>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Items, Items)
			.Add(x => x.Query, "fence"));

		Assert.Single(cut.FindAll("[role='option']"));
	}

	[Fact]
	public void NoMatches_RendersEmpty()
	{
		IRenderedComponent<MokaSlashMenu> cut = Render<MokaSlashMenu>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Items, Items)
			.Add(x => x.Query, "zzzzz"));

		Assert.NotNull(cut.Find(".moka-slash-menu-empty"));
	}

	[Fact]
	public void Header_Default_Renders()
	{
		IRenderedComponent<MokaSlashMenu> cut = Render<MokaSlashMenu>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Items, Items));

		Assert.Equal("Insert", cut.Find(".moka-slash-menu-label").TextContent);
	}

	[Fact]
	public void Header_Empty_NoHeader()
	{
		IRenderedComponent<MokaSlashMenu> cut = Render<MokaSlashMenu>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Items, Items)
			.Add(x => x.Header, ""));

		Assert.Empty(cut.FindAll(".moka-slash-menu-header"));
	}

	[Fact]
	public async Task HandleKeyAsync_ArrowDown_MovesActiveAndConsumes()
	{
		IRenderedComponent<MokaSlashMenu> cut = Render<MokaSlashMenu>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Items, Items));

		var consumed = false;
		await cut.InvokeAsync(async () =>
			consumed = await cut.Instance.HandleKeyAsync(new KeyboardEventArgs { Key = "ArrowDown" }));

		Assert.True(consumed);
		var options = cut.FindAll("[role='option']");
		Assert.Equal("true", options[1].GetAttribute("aria-selected"));
	}

	[Fact]
	public async Task HandleKeyAsync_Enter_SelectsActiveItem()
	{
		MokaSlashMenuItem? selected = null;
		IRenderedComponent<MokaSlashMenu> cut = Render<MokaSlashMenu>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Items, Items)
			.Add(x => x.OnSelect, EventCallback.Factory.Create<MokaSlashMenuItem>(this, i => selected = i)));

		await cut.InvokeAsync(async () =>
			await cut.Instance.HandleKeyAsync(new KeyboardEventArgs { Key = "Enter" }));

		Assert.NotNull(selected);
		Assert.Equal("Heading", selected!.Title);
	}

	[Fact]
	public async Task HandleKeyAsync_Escape_ClosesViaOpenChanged()
	{
		var open = true;
		IRenderedComponent<MokaSlashMenu> cut = Render<MokaSlashMenu>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Items, Items)
			.Add(x => x.OpenChanged, EventCallback.Factory.Create<bool>(this, v => open = v)));

		await cut.InvokeAsync(async () =>
			await cut.Instance.HandleKeyAsync(new KeyboardEventArgs { Key = "Escape" }));

		Assert.False(open);
	}

	[Fact]
	public async Task HandleKeyAsync_WhenClosed_ReturnsFalse()
	{
		IRenderedComponent<MokaSlashMenu> cut = Render<MokaSlashMenu>(p => p
			.Add(x => x.Open, false)
			.Add(x => x.Items, Items));

		var consumed = true;
		await cut.InvokeAsync(async () =>
			consumed = await cut.Instance.HandleKeyAsync(new KeyboardEventArgs { Key = "ArrowDown" }));

		Assert.False(consumed);
	}

	// Focus stays in the host textarea, so it has to point aria-activedescendant at the highlighted
	// option and aria-controls at the list. Neither had an id to point at.
	[Fact]
	public void ListboxAndOptions_HaveIds()
	{
		IRenderedComponent<MokaSlashMenu> cut = Render<MokaSlashMenu>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Items, Items));

		string listboxId = cut.Instance.ListboxId;
		Assert.Equal(listboxId, cut.Find("[role=listbox]").Id);
		Assert.Equal(
			new[] { $"{listboxId}-option-0", $"{listboxId}-option-1", $"{listboxId}-option-2" },
			cut.FindAll("[role=option]").Select(o => o.Id));
	}

	[Fact]
	public async Task ActiveOptionId_NamesTheHighlightedOption()
	{
		IRenderedComponent<MokaSlashMenu> cut = Render<MokaSlashMenu>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Items, Items));
		Assert.Equal("true", cut.Find($"#{cut.Instance.ActiveOptionId}").GetAttribute("aria-selected"));

		await cut.InvokeAsync(() => cut.Instance.HandleKeyAsync(new KeyboardEventArgs { Key = "ArrowUp" }));

		IElement active = cut.Find($"#{cut.Instance.ActiveOptionId}");
		Assert.Equal("true", active.GetAttribute("aria-selected"));
		Assert.Contains("Code block", active.TextContent, StringComparison.Ordinal);
	}

	[Fact]
	public void ActiveOptionId_FollowsANewQuery()
	{
		IRenderedComponent<MokaSlashMenu> cut = Render<MokaSlashMenu>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Items, Items));

		cut.Render(p => p.Add(x => x.Query, "list"));

		IElement active = cut.Find($"#{cut.Instance.ActiveOptionId}");
		Assert.Contains("Bullet list", active.TextContent, StringComparison.Ordinal);
	}

	[Fact]
	public async Task ActiveOptionId_IsNull_WhileClosedOrWithoutMatches()
	{
		IRenderedComponent<MokaSlashMenu> cut = Render<MokaSlashMenu>(p => p
			.Add(x => x.Open, false)
			.Add(x => x.Items, Items));
		Assert.Null(cut.Instance.ActiveOptionId);

		cut.Render(p => p.Add(x => x.Open, true).Add(x => x.Query, "zzzzz"));
		Assert.Null(cut.Instance.ActiveOptionId);

		cut.Render(p => p.Add(x => x.Query, ""));
		Assert.NotNull(cut.Instance.ActiveOptionId);

		await cut.InvokeAsync(() => cut.Instance.HandleKeyAsync(new KeyboardEventArgs { Key = "Escape" }));
		Assert.Null(cut.Instance.ActiveOptionId);
	}

	[Fact]
	public void ListboxId_StaysTheSame_AcrossRendersAndReopening()
	{
		IRenderedComponent<MokaSlashMenu> cut = Render<MokaSlashMenu>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Items, Items));
		string listboxId = cut.Instance.ListboxId;

		cut.Render(p => p.Add(x => x.Open, false));
		cut.Render(p => p.Add(x => x.Open, true).Add(x => x.Query, "code"));

		Assert.Equal(listboxId, cut.Instance.ListboxId);
		Assert.Equal(listboxId, cut.Find("[role=listbox]").Id);
	}

	// Two menus on one page must not share ids, or aria-activedescendant could point into the wrong one.
	[Fact]
	public void TwoMenus_HaveDifferentIds()
	{
		IRenderedComponent<MokaSlashMenu> first = Render<MokaSlashMenu>(p => p.Add(x => x.Items, Items));
		IRenderedComponent<MokaSlashMenu> second = Render<MokaSlashMenu>(p => p.Add(x => x.Items, Items));

		Assert.NotEqual(first.Instance.ListboxId, second.Instance.ListboxId);
	}
}
