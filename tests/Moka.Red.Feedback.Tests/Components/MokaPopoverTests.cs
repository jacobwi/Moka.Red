using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Feedback.Popover;

namespace Moka.Red.Feedback.Tests.Components;

public class MokaPopoverTests : BunitContext
{
	public MokaPopoverTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	// The offsets were written in the current culture, so in Swedish a negative one came out with the
	// minus sign U+2212, which CSS does not read, and the popup ignored it.
	[Fact]
	public void NegativeOffsets_AreWrittenForCss_InAnyCulture()
	{
		CultureInfo previous = CultureInfo.CurrentCulture;
		CultureInfo.CurrentCulture = new CultureInfo("sv-SE");
		try
		{
			IRenderedComponent<MokaPopover> cut = Render<MokaPopover>(p => p
				.Add(x => x.Open, true)
				.Add(x => x.OffsetX, -6)
				.Add(x => x.OffsetY, -2));

			string style = cut.Find(".moka-popover-popup").GetAttribute("style") ?? "";
			Assert.Contains("--moka-popover-offset-x: -6px", style, StringComparison.Ordinal);
			Assert.Contains("--moka-popover-offset-y: -2px", style, StringComparison.Ordinal);
		}
		finally
		{
			CultureInfo.CurrentCulture = previous;
		}
	}

	// The popover wrote the user's open into its own Open parameter, and every parameter set copied
	// Open back into its state, so any parent re-render closed it again (gotcha #9).
	[Fact]
	public async Task AnOpenedPopover_StaysOpen_WhenTheParentPassesTheOldValueAgain()
	{
		IRenderedComponent<MokaPopover> cut = Render<MokaPopover>(p => p
			.Add(x => x.Open, false)
			.Add(x => x.PopoverContent, "Details")
			.AddChildContent("<button>Info</button>"));

		await cut.Find(".moka-popover-trigger").ClickAsync(new MouseEventArgs());
		Assert.Single(cut.FindAll("[role=dialog]"));

		cut.Render(p => p.Add(x => x.Open, false));
		Assert.Single(cut.FindAll("[role=dialog]"));

		cut.Render(p => p.Add(x => x.Open, true));
		cut.Render(p => p.Add(x => x.Open, false));
		Assert.Empty(cut.FindAll("[role=dialog]"));
	}

	// A MokaDialog around the popover closed on the same Escape as the popover.
	[Fact]
	public async Task Escape_ClosesThePopover_WithoutReachingItsContainer()
	{
		IRenderedComponent<EscapeHost> host = Render<EscapeHost>(p => p.Add(x => x.ChildContent, (RenderFragment)(b =>
		{
			b.OpenComponent<MokaPopover>(0);
			b.AddAttribute(1, nameof(MokaPopover.PopoverContent), (RenderFragment)(c => c.AddContent(0, "Details")));
			b.AddAttribute(2, nameof(MokaPopover.ChildContent), (RenderFragment)(c => c.AddMarkupContent(0, "<button>Info</button>")));
			b.CloseComponent();
		})));

		await host.Find(".moka-popover-trigger").ClickAsync(new MouseEventArgs());
		await host.Find("[role=dialog]").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

		Assert.Empty(host.FindAll("[role=dialog]"));
		Assert.Equal(0, host.Instance.Escapes);
	}

	// The popup was a dialog with no name.
	[Fact]
	public async Task ThePopup_IsNamedByItsTrigger_UnlessGivenALabel()
	{
		IRenderedComponent<MokaPopover> cut = Render<MokaPopover>(p => p
			.Add(x => x.PopoverContent, "Details")
			.AddChildContent("<button>Info</button>"));
		await cut.Find(".moka-popover-trigger").ClickAsync(new MouseEventArgs());

		IElement trigger = cut.Find(".moka-popover-trigger");
		Assert.Equal(trigger.Id, cut.Find("[role=dialog]").GetAttribute("aria-labelledby"));

		cut.Render(p => p.Add(x => x.AriaLabel, "Shipping details"));

		IElement popup = cut.Find("[role=dialog]");
		Assert.Equal("Shipping details", popup.GetAttribute("aria-label"));
		Assert.False(popup.HasAttribute("aria-labelledby"));
	}

	// The trigger never said it opens a popup or whether it is open.
	[Fact]
	public async Task TheTriggersState_IsWrittenOnceAndOnEveryOpenAndClose()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule("./_content/Moka.Red.Feedback/Popover/MokaPopover.razor.js");
		module.Mode = JSRuntimeMode.Loose;
		IRenderedComponent<MokaPopover> cut = Render<MokaPopover>(p => p
			.Add(x => x.PopoverContent, "Details")
			.AddChildContent("<button>Info</button>"));

		await cut.Find(".moka-popover-trigger").ClickAsync(new MouseEventArgs());
		await cut.Find(".moka-popover-trigger").ClickAsync(new MouseEventArgs());

		IReadOnlyList<JSRuntimeInvocation> syncs = module.VerifyInvoke("syncTrigger", 3);
		Assert.Equal([false, true, false], syncs.Select(i => (bool)i.Arguments[1]!));
	}

	// Id went nowhere, and the root's class was a fixed moka-popover-wrapper rather than RootClass.
	// The root is always in the page, around the trigger, so Id goes there with Class and Style. The
	// popup exists only while open and keeps its own id.
	[Fact]
	public void Id_AndClass_GoOnTheRoot()
	{
		IRenderedComponent<MokaPopover> cut = Render<MokaPopover>(p => p
			.Add(x => x.Id, "shipping")
			.Add(x => x.Class, "promo")
			.Add(x => x.Open, true)
			.Add(x => x.PopoverContent, "Details")
			.AddChildContent("<button>Info</button>"));

		IElement root = cut.Nodes.OfType<IElement>().First();
		Assert.Equal("shipping", root.Id);
		Assert.Equal("moka-popover promo", root.ClassName);
		Assert.Single(cut.FindAll("#shipping"));
		Assert.False(string.IsNullOrEmpty(cut.Find("[role=dialog]").Id));
	}

	[Fact]
	public void WithoutId_TheRootHasNone()
	{
		IRenderedComponent<MokaPopover> cut = Render<MokaPopover>(p => p.AddChildContent("<button>Info</button>"));

		Assert.False(cut.Find(".moka-popover").HasAttribute("id"));
	}

	/// <summary>Counts the Escapes that reach it, as a MokaDialog's handler would see them.</summary>
	public sealed class EscapeHost : ComponentBase
	{
		[Parameter] public RenderFragment? ChildContent { get; set; }

		public int Escapes { get; private set; }

		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
			ArgumentNullException.ThrowIfNull(builder);
			builder.OpenElement(0, "div");
			builder.AddAttribute(1, "onkeydown", EventCallback.Factory.Create<KeyboardEventArgs>(this, e =>
			{
				if (e.Key == "Escape")
				{
					Escapes++;
				}
			}));
			builder.AddContent(2, ChildContent);
			builder.CloseElement();
		}
	}
}
