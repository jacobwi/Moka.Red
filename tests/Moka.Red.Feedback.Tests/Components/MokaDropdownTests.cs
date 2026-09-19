using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Feedback.Dialog;
using Moka.Red.Feedback.Dropdown;

namespace Moka.Red.Feedback.Tests.Components;

// The arrow keys, the focus moves and the trigger's ARIA state live in MokaDropdown.razor.js. These
// tests cover what the components render for it and for screen readers, and the .NET side of
// closing: before an item's action, not for a disabled item, and not for the dialog around it.
public class MokaDropdownTests : BunitContext
{
	private const string DropdownModule = "./_content/Moka.Red.Feedback/Dropdown/MokaDropdown.razor.js";

	private static readonly RenderFragment TriggerButton = builder =>
	{
		builder.OpenElement(0, "button");
		builder.AddAttribute(1, "type", "button");
		builder.AddContent(2, "Actions");
		builder.CloseElement();
	};

	public MokaDropdownTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public void TheMenu_ExposesItsItemsToTheKeyboardScript()
	{
		IRenderedComponent<MokaDropdown> cut = RenderDropdown(open: true);

		IElement menu = cut.Find("[role=menu]");
		Assert.False(string.IsNullOrEmpty(menu.Id));
		Assert.Equal("-1", menu.GetAttribute("tabindex"));
		Assert.Equal("true", menu.GetAttribute("data-close-on-select"));

		IReadOnlyList<IElement> items = cut.FindAll("[role=menuitem]");
		Assert.Equal("-1", items[0].GetAttribute("tabindex"));
		Assert.False(items[1].HasAttribute("tabindex"));
		Assert.Equal("true", items[1].GetAttribute("aria-disabled"));
		Assert.NotNull(cut.Find(".moka-dropdown-divider[role=separator]"));
	}

	[Fact]
	public void TheDropdown_BindsItsKeyboardHandlingOnce()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(DropdownModule);
		module.SetupVoid("bindDropdown", _ => true).SetVoidResult();

		IRenderedComponent<MokaDropdown> cut = RenderDropdown();

		ElementReference root = Assert.IsType<ElementReference>(module.VerifyInvoke("bindDropdown").Arguments[0]);
		Assert.Equal(root.Id, cut.Find(".moka-dropdown").GetAttribute("blazor:elementReference"));
		Assert.NotNull(cut.Find(".moka-dropdown__trigger > button"));
	}

	// The menu used to close only after the item's action returned, so an action that opened a
	// dialog left the menu open behind it until the dialog was done.
	[Fact]
	public async Task AnItem_ClosesTheMenuBeforeItsActionRuns()
	{
		var log = new List<string>();
		IRenderedComponent<MokaDropdown> cut = RenderDropdown(
			open: true,
			onOpenChanged: open => log.Add(open ? "opened" : "closed"),
			onEdit: () => log.Add("action"));

		await cut.FindAll("[role=menuitem]")[0].ClickAsync(new MouseEventArgs());

		Assert.Equal(["closed", "action"], log);
		Assert.Empty(cut.FindAll("[role=menu]"));
	}

	[Fact]
	public async Task ADisabledItem_LeavesTheMenuOpen()
	{
		bool? changed = null;
		IRenderedComponent<MokaDropdown> cut = RenderDropdown(open: true, onOpenChanged: open => changed = open);

		await cut.FindAll("[role=menuitem]")[1].ClickAsync(new MouseEventArgs());

		Assert.Null(changed);
		Assert.NotEmpty(cut.FindAll("[role=menu]"));
	}

	// The dialog's Blazor handler also got the Escape that closed the menu, so both closed.
	[Fact]
	public async Task Escape_InsideADialog_ClosesOnlyTheDropdown()
	{
		bool? dialogChanged = null;
		IRenderedComponent<MokaDialog> cut = Render<MokaDialog>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.OpenChanged, open => dialogChanged = open)
			.AddChildContent<MokaDropdown>(dropdown => dropdown
				.Add(x => x.Open, true)
				.Add(x => x.ChildContent, TriggerButton)
				.Add(x => x.Items, Items(null))));

		await cut.FindAll("[role=menuitem]")[0].KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

		Assert.Empty(cut.FindAll("[role=menu]"));
		Assert.Null(dialogChanged);
		Assert.NotEmpty(cut.FindAll(".moka-dialog-wrapper"));
	}

	[Fact]
	public async Task Escape_OnAClosedDropdown_StillReachesTheDialog()
	{
		bool? dialogChanged = null;
		IRenderedComponent<MokaDialog> cut = Render<MokaDialog>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.OpenChanged, open => dialogChanged = open)
			.AddChildContent<MokaDropdown>(dropdown => dropdown
				.Add(x => x.ChildContent, TriggerButton)
				.Add(x => x.Items, Items(null))));

		await cut.Find(".moka-popover-trigger button").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

		Assert.False(dialogChanged);
	}

	// Blazor passes every parameter again when the parent re-renders, so writing the open state
	// into the Open parameter let the parent's one-way value close the menu under the user.
	[Fact]
	public async Task AMenuTheUserOpened_SurvivesAParentRerender()
	{
		IRenderedComponent<MokaDropdown> cut = RenderDropdown();

		await cut.Find(".moka-popover-trigger").ClickAsync(new MouseEventArgs());
		Assert.NotEmpty(cut.FindAll("[role=menu]"));

		cut.Render(p => p.Add(x => x.Open, false));

		Assert.NotEmpty(cut.FindAll("[role=menu]"));
	}

	[Fact]
	public void ANewOpenValue_FromTheParent_StillApplies()
	{
		IRenderedComponent<MokaDropdown> cut = RenderDropdown();

		cut.Render(p => p.Add(x => x.Open, true));

		Assert.NotEmpty(cut.FindAll("[role=menu]"));
	}

	// MokaSplitButton renders MokaDropdownItem without a MokaDropdown around it.
	[Fact]
	public async Task AnItemOutsideADropdown_StillRunsItsAction()
	{
		bool clicked = false;
		IRenderedComponent<MokaDropdownItem> cut = Render<MokaDropdownItem>(p => p
			.Add(x => x.Text, "Export")
			.Add(x => x.OnClick, _ => clicked = true));

		await cut.Find("[role=menuitem]").ClickAsync(new MouseEventArgs());

		Assert.True(clicked);
	}

	// The root is display: contents, so an Id there named an element with no box: it could not be
	// measured, scrolled to or anchored. The popover's wrapper is the dropdown's box in the page.
	[Fact]
	public void Id_GoesOnTheBoxInThePage()
	{
		IRenderedComponent<MokaDropdown> cut = Render<MokaDropdown>(p => p
			.Add(x => x.Id, "actions")
			.Add(x => x.Open, true)
			.Add(x => x.ChildContent, TriggerButton)
			.Add(x => x.Items, Items(null)));

		IElement box = cut.Find("#actions");
		Assert.Contains("moka-popover", box.ClassList);
		Assert.False(cut.Find(".moka-dropdown").HasAttribute("id"));
		Assert.Equal("actions-menu", cut.Find("[role=menu]").Id);
	}

	private IRenderedComponent<MokaDropdown> RenderDropdown(bool open = false, Action<bool>? onOpenChanged = null,
		Action? onEdit = null) =>
		Render<MokaDropdown>(p =>
		{
			p.Add(x => x.ChildContent, TriggerButton)
				.Add(x => x.Items, Items(onEdit));
			if (open)
			{
				p.Add(x => x.Open, true);
			}

			if (onOpenChanged is not null)
			{
				p.Add(x => x.OpenChanged, onOpenChanged);
			}
		});

	// Edit, a divider, then a disabled Delete.
	private RenderFragment Items(Action? onEdit) => builder =>
	{
		AddItem(builder, "Edit", false, onEdit);
		builder.OpenComponent<MokaDropdownItem>(10);
		builder.AddAttribute(11, nameof(MokaDropdownItem.Divider), true);
		builder.CloseComponent();
		AddItem(builder, "Delete", true, null);
	};

	private void AddItem(RenderTreeBuilder builder, string text, bool disabled, Action? onClick)
	{
		builder.OpenComponent<MokaDropdownItem>(0);
		builder.AddAttribute(1, nameof(MokaDropdownItem.Text), text);
		builder.AddAttribute(2, nameof(MokaDropdownItem.Disabled), disabled);
		if (onClick is not null)
		{
			builder.AddAttribute(3, nameof(MokaDropdownItem.OnClick),
				EventCallback.Factory.Create<MouseEventArgs>(this, onClick));
		}

		builder.CloseComponent();
	}
}
