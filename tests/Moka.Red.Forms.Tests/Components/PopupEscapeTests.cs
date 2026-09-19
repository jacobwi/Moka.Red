using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Forms.DatePicker;
using Moka.Red.Forms.DateRangePicker;
using Moka.Red.Forms.SelectField;
using Moka.Red.Forms.TimePicker;
using Moka.Red.Forms.TreeSelect;

namespace Moka.Red.Forms.Tests.Components;

// A MokaDialog closes on the Escape that bubbles up to it, so a popup inside a dialog used to close
// along with the dialog. Each popup here sits in an EscapeHost whose own handler plays the dialog.
public class PopupEscapeTests : BunitContext
{
	private static readonly string[] Colours = ["Red", "Green"];

	public PopupEscapeTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public async Task AnOpenSelect_KeepsEscapeFromItsContainer()
	{
		IRenderedComponent<EscapeHost> host = RenderHost(b =>
		{
			b.OpenComponent<MokaSelect<string>>(0);
			b.AddAttribute(1, nameof(MokaSelect<string>.Items), Colours);
			b.CloseComponent();
		});

		await host.Find(".moka-select-trigger").ClickAsync(new MouseEventArgs());
		Assert.Single(host.FindAll("[role=listbox]"));

		await host.Find(".moka-select-trigger").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

		Assert.Empty(host.FindAll("[role=listbox]"));
		Assert.Equal(0, host.Instance.Escapes);

		// Closed, the select lets Escape through again, so the dialog can close.
		await host.Find(".moka-select-trigger").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });
		Assert.Equal(1, host.Instance.Escapes);
	}

	[Fact]
	public async Task DatePicker_OpensFromTheKeyboard_AndEscapeClosesIt()
	{
		IRenderedComponent<EscapeHost> host = RenderHost(b =>
		{
			b.OpenComponent<MokaDatePicker>(0);
			b.CloseComponent();
		});

		IElement input = host.Find("input");
		Assert.Equal("false", input.GetAttribute("aria-expanded"));

		await input.KeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown" });
		Assert.Equal("true", host.Find("input").GetAttribute("aria-expanded"));
		Assert.Single(host.FindAll(".moka-datepicker-dropdown"));

		await host.Find(".moka-datepicker-dropdown button").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

		Assert.Empty(host.FindAll(".moka-datepicker-dropdown"));
		Assert.Equal(0, host.Instance.Escapes);
		JSInterop.VerifyFocusAsyncInvoke();
	}

	// Its read-only input was the only trigger, and it answered clicks only.
	[Fact]
	public async Task DateRangePicker_OpensFromTheKeyboard()
	{
		IRenderedComponent<EscapeHost> host = RenderHost(b =>
		{
			b.OpenComponent<MokaDateRangePicker>(0);
			b.CloseComponent();
		});

		await host.Find("input").KeyDownAsync(new KeyboardEventArgs { Key = " " });

		Assert.Equal("true", host.Find("input").GetAttribute("aria-expanded"));

		await host.Find("input").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

		Assert.Equal("false", host.Find("input").GetAttribute("aria-expanded"));
		Assert.Equal(0, host.Instance.Escapes);
	}

	[Fact]
	public async Task TimePicker_EscapeClosesIt_WithoutReachingItsContainer()
	{
		IRenderedComponent<EscapeHost> host = RenderHost(b =>
		{
			b.OpenComponent<MokaTimePicker>(0);
			b.CloseComponent();
		});

		await host.Find("input").ClickAsync(new MouseEventArgs());
		Assert.Single(host.FindAll(".moka-timepicker-dropdown"));

		await host.Find("input").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

		Assert.Empty(host.FindAll(".moka-timepicker-dropdown"));
		Assert.Equal(0, host.Instance.Escapes);
	}

	[Fact]
	public async Task TreeSelect_EscapeClosesIt_WithoutReachingItsContainer()
	{
		IRenderedComponent<EscapeHost> host = RenderHost(b =>
		{
			b.OpenComponent<MokaTreeSelect<string>>(0);
			b.AddAttribute(1, nameof(MokaTreeSelect<string>.Items),
				new List<MokaTreeSelectItem<string>> { new("a", "Alpha") });
			b.CloseComponent();
		});

		IElement trigger = host.Find(".moka-tree-select-trigger");
		await trigger.ClickAsync(new MouseEventArgs());
		Assert.Equal("true", host.Find(".moka-tree-select-trigger").GetAttribute("aria-expanded"));

		await host.Find(".moka-tree-select-trigger").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

		Assert.Equal("false", host.Find(".moka-tree-select-trigger").GetAttribute("aria-expanded"));
		Assert.Equal(0, host.Instance.Escapes);
	}

	private IRenderedComponent<EscapeHost> RenderHost(RenderFragment content) =>
		Render<EscapeHost>(p => p.Add(x => x.ChildContent, content));

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
