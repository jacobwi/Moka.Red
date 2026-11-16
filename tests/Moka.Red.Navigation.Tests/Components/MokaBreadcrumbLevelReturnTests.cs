using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Moka.Red.Navigation.Breadcrumb;

namespace Moka.Red.Navigation.Tests.Components;

// A level inside an @if that the page renders again is a new component, which registers after the levels already there
// while its markup sits between them. The trail creates its levels again so they count in markup order.
public class MokaBreadcrumbLevelReturnTests : BunitContext
{
	public MokaBreadcrumbLevelReturnTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public async Task ALevelThatComesBack_TakesItsPlace_AndTheCurrentPageStaysLast()
	{
		IRenderedComponent<Host> host = Render<Host>();

		await host.InvokeAsync(() => host.Instance.Set(showNetwork: false));
		await host.InvokeAsync(() => host.Instance.Set(showNetwork: true));

		Assert.Equal(["Home", "Network", "Devices"], host.FindAll(".moka-breadcrumb-item__label").Select(e => e.TextContent.Trim()));
		Assert.Equal("Devices", Assert.Single(host.FindAll("[aria-current]")).TextContent.Trim());
		Assert.NotEmpty(host.FindAll("a[href='/Network']"));
	}

	private sealed class Host : ComponentBase
	{
		private bool _showNetwork = true;

		public void Set(bool showNetwork)
		{
			_showNetwork = showNetwork;
			StateHasChanged();
		}

		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
			builder.OpenComponent<MokaBreadcrumb>(0);
			builder.AddComponentParameter(1, nameof(MokaBreadcrumb.ChildContent), (RenderFragment)Levels);
			builder.CloseComponent();
		}

		private void Levels(RenderTreeBuilder builder)
		{
			Level(builder, 0, "Home");
			if (_showNetwork)
			{
				Level(builder, 10, "Network");
			}

			Level(builder, 20, "Devices");
		}

		private static void Level(RenderTreeBuilder builder, int sequence, string text)
		{
			builder.OpenComponent<MokaBreadcrumbItem>(sequence);
			builder.AddComponentParameter(sequence + 1, nameof(MokaBreadcrumbItem.Text), text);
			builder.AddComponentParameter(sequence + 2, nameof(MokaBreadcrumbItem.Href), "/" + text);
			builder.CloseComponent();
		}
	}
}
