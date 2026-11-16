using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Layout.Accordion;

namespace Moka.Red.Layout.Tests.Components;

// The accordion keeps its items only to close the others when one opens, so where an item stands in its list doesn't
// matter. An item inside an @if that comes back still takes part.
public class MokaAccordionItemReturnTests : BunitContext
{
	[Fact]
	public async Task AnItemThatComesBack_StillClosesTheOthers_AndIsClosedByThem()
	{
		IRenderedComponent<Host> host = Render<Host>();
		await host.InvokeAsync(() => host.Instance.Set(showBilling: false));
		await host.InvokeAsync(() => host.Instance.Set(showBilling: true));

		await Header(host, 0).ClickAsync(new MouseEventArgs());
		await Header(host, 1).ClickAsync(new MouseEventArgs());
		Assert.Equal(["false", "true", "false"], Expanded(host));

		await Header(host, 2).ClickAsync(new MouseEventArgs());
		Assert.Equal(["false", "false", "true"], Expanded(host));
	}

	private static AngleSharp.Dom.IElement Header(IRenderedComponent<Host> host, int index) =>
		host.FindAll(".moka-accordion-item-header")[index];

	private static string?[] Expanded(IRenderedComponent<Host> host) =>
		[.. host.FindAll(".moka-accordion-item-header").Select(header => header.GetAttribute("aria-expanded"))];

	private sealed class Host : ComponentBase
	{
		private bool _showBilling = true;

		public void Set(bool showBilling)
		{
			_showBilling = showBilling;
			StateHasChanged();
		}

		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
			builder.OpenComponent<MokaAccordion>(0);
			builder.AddComponentParameter(1, nameof(MokaAccordion.ChildContent), (RenderFragment)Items);
			builder.CloseComponent();
		}

		private void Items(RenderTreeBuilder builder)
		{
			Item(builder, 0, "Account");
			if (_showBilling)
			{
				Item(builder, 10, "Billing");
			}

			Item(builder, 20, "Review");
		}

		private static void Item(RenderTreeBuilder builder, int sequence, string title)
		{
			builder.OpenComponent<MokaAccordionItem>(sequence);
			builder.AddComponentParameter(sequence + 1, nameof(MokaAccordionItem.Title), title);
			builder.AddComponentParameter(sequence + 2, nameof(MokaAccordionItem.ChildContent), (RenderFragment)(b => b.AddContent(0, title)));
			builder.CloseComponent();
		}
	}
}
