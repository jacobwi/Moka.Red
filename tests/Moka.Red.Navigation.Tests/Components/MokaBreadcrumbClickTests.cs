using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Navigation.Breadcrumb;

namespace Moka.Red.Navigation.Tests.Components;

// A level picked through the app's own state rather than an address: from the trail, the menu of folded levels and the
// back link a narrow trail shows. A level had only an address, so such a trail couldn't fold or go back.
public class MokaBreadcrumbClickTests : BunitContext
{
	public MokaBreadcrumbClickTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public async Task ALevelWithOnlyAClickHandler_IsAButtonStyledAsTheLink_AndRaisesIt()
	{
		List<string> picked = [];
		IRenderedComponent<Trail> cut = Render<Trail>(p => p
			.Add(x => x.Levels, ["Home", "Network", "Devices"])
			.Add(x => x.Picked, picked));

		IElement network = cut.FindAll("li.moka-breadcrumb-item")[1].QuerySelector("button")!;
		Assert.Equal("button", network.GetAttribute("type"));
		Assert.Contains("moka-breadcrumb-item__link", network.ClassList);

		await network.ClickAsync(new MouseEventArgs());

		Assert.Equal(["Network"], picked);
	}

	[Fact]
	public void TheCurrentPage_StaysText_EvenWithAClickHandler()
	{
		IRenderedComponent<Trail> cut = Render<Trail>(p => p.Add(x => x.Levels, ["Home", "Network"]).Add(x => x.Picked, []));

		IElement current = cut.FindAll("li.moka-breadcrumb-item")[^1];
		Assert.Null(current.QuerySelector("button, a"));
		Assert.Equal("page", current.QuerySelector("[aria-current]")!.GetAttribute("aria-current"));
	}

	[Fact]
	public async Task ALinkWithAClickHandler_RaisesItToo()
	{
		List<string> picked = [];
		IRenderedComponent<Trail> cut = Render<Trail>(p => p
			.Add(x => x.Levels, ["Home", "Network"])
			.Add(x => x.Linked, true)
			.Add(x => x.Picked, picked));

		await cut.Find("a[href='/Home']").ClickAsync(new MouseEventArgs());

		Assert.Equal(["Home"], picked);
	}

	[Fact]
	public async Task AFoldedLevel_WithOnlyAClickHandler_CanBePickedFromTheMenu()
	{
		List<string> picked = [];
		IRenderedComponent<Trail> cut = Render<Trail>(p => p
			.Add(x => x.Levels, ["Home", "Network", "Devices", "Desktop"])
			.Add(x => x.MaxItems, 2)
			.Add(x => x.Picked, picked));

		await cut.Find(".moka-breadcrumb__fold .moka-popover-trigger").ClickAsync(new MouseEventArgs());
		IElement devices = cut.FindAll(".moka-breadcrumb__fold-item")[1];
		Assert.False(devices.HasAttribute("disabled"));
		await devices.ClickAsync(new MouseEventArgs());

		Assert.Equal(["Devices"], picked);
		Assert.EndsWith("/", Services.GetRequiredService<NavigationManager>().Uri, StringComparison.Ordinal);
	}

	[Fact]
	public async Task TheBackLink_ToAParentWithOnlyAClickHandler_IsAButtonThatRaisesIt()
	{
		List<string> picked = [];
		IRenderedComponent<Trail> cut = Render<Trail>(p => p
			.Add(x => x.Levels, ["Home", "Network", "Devices"])
			.Add(x => x.Picked, picked));

		IElement back = cut.Find("nav > button.moka-breadcrumb__back");
		Assert.Equal("Network", back.TextContent.Trim());
		await back.ClickAsync(new MouseEventArgs());

		Assert.Equal(["Network"], picked);
	}

	private sealed class Trail : ComponentBase
	{
		[Parameter] public IReadOnlyList<string> Levels { get; set; } = [];

		[Parameter] public List<string> Picked { get; set; } = [];

		[Parameter] public bool Linked { get; set; }

		[Parameter] public int? MaxItems { get; set; }

		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
			builder.OpenComponent<MokaBreadcrumb>(0);
			builder.AddComponentParameter(1, nameof(MokaBreadcrumb.MaxItems), MaxItems);
			builder.AddComponentParameter(2, nameof(MokaBreadcrumb.ShowCurrent), true);
			builder.AddComponentParameter(3, nameof(MokaBreadcrumb.ChildContent), (RenderFragment)Items);
			builder.CloseComponent();
		}

		private void Items(RenderTreeBuilder builder)
		{
			foreach (string level in Levels)
			{
				builder.OpenComponent<MokaBreadcrumbItem>(0);
				builder.SetKey(level);
				builder.AddComponentParameter(1, nameof(MokaBreadcrumbItem.Text), level);
				if (Linked)
				{
					builder.AddComponentParameter(2, nameof(MokaBreadcrumbItem.Href), $"/{level}");
				}

				builder.AddComponentParameter(3, nameof(MokaBreadcrumbItem.OnClick),
					EventCallback.Factory.Create<MouseEventArgs>(this, () => Picked.Add(level)));
				builder.CloseComponent();
			}
		}
	}
}
