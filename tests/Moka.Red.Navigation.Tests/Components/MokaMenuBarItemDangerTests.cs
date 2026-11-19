using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Navigation.MenuBar;

namespace Moka.Red.Navigation.Tests.Components;

public class MokaMenuBarItemDangerTests : BunitContext
{
	public MokaMenuBarItemDangerTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public void AnItem_KeepsItsClass_ByDefault() =>
		Assert.Equal("moka-menubar-item", Render<MokaMenuBarItem>(p => p.Add(x => x.Text, "Exit")).Find("button").GetAttribute("class"));

	[Fact]
	public void Danger_AddsOneClass_BeforeTheAppsOwn() =>
		Assert.Equal("moka-menubar-item moka-menubar-item--danger app-row", Render<MokaMenuBarItem>(p => p
			.Add(x => x.Text, "Delete file")
			.Add(x => x.Danger, true)
			.Add(x => x.Class, "app-row")).Find("button").GetAttribute("class"));

	[Fact]
	public void ADangerItem_IsStillAPlainMenuItem()
	{
		IElement item = Render<MokaMenuBarItem>(p => p
			.Add(x => x.Text, "Delete file")
			.Add(x => x.Danger, true)).Find("button");

		Assert.Equal("menuitem", item.GetAttribute("role"));
		Assert.Equal("-1", item.GetAttribute("tabindex"));
		Assert.False(item.HasAttribute("style"));
	}

	[Fact]
	public async Task AHighlightedDangerItem_KeepsBothClasses()
	{
		IRenderedComponent<MokaMenuBar> bar = Render<MokaMenuBar>(p => p.AddChildContent<MokaMenuBarMenu>(menu => menu
			.Add(x => x.Label, "File")
			.Add(x => x.ChildContent, (RenderFragment)(builder =>
			{
				builder.OpenComponent<MokaMenuBarItem>(0);
				builder.AddComponentParameter(1, nameof(MokaMenuBarItem.Text), "Save");
				builder.CloseComponent();
				builder.OpenComponent<MokaMenuBarItem>(2);
				builder.AddComponentParameter(3, nameof(MokaMenuBarItem.Text), "Delete file");
				builder.AddComponentParameter(4, nameof(MokaMenuBarItem.Danger), true);
				builder.CloseComponent();
			}))));

		await bar.Find(".moka-menubar").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowUp" });

		IElement last = bar.FindAll(".moka-menubar-item")[^1];
		Assert.Contains("moka-menubar-item--highlighted", last.ClassList);
		Assert.Contains("moka-menubar-item--danger", last.ClassList);
		Assert.DoesNotContain("moka-menubar-item--danger", bar.FindAll(".moka-menubar-item")[0].ClassList);
	}
}
