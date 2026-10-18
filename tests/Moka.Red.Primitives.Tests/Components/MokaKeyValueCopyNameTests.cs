using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components.Rendering;
using Moka.Red.Feedback.Tooltip;
using Moka.Red.Primitives.KeyValue;

namespace Moka.Red.Primitives.Tests.Components;

// Every copyable key-value drew a copy button called "Copy", so a list of them gave a screen reader a row of
// identical buttons and no way to tell which value each one would copy.
public class MokaKeyValueCopyNameTests : BunitContext
{
	public MokaKeyValueCopyNameTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public void TheCopyButtonIsNamedAfterItsKey()
	{
		IRenderedComponent<MokaKeyValue> cut = Render<MokaKeyValue>(p => p
			.Add(x => x.Label, "Fingerprint")
			.Add(x => x.Value, "ab:cd")
			.Add(x => x.Copyable, true));

		Assert.Equal("Copy Fingerprint", cut.Find(".moka-kv__copy").GetAttribute("aria-label"));
	}

	// The point of the fix: two of them in a list are told apart.
	[Fact]
	public void TwoKeyValuesGetDifferentNames()
	{
		IRenderedComponent<TwoRows> cut = Render<TwoRows>();

		Assert.Equal(
			["Copy Trusted", "Copy Fingerprint"],
			cut.FindAll(".moka-kv__copy").Select(b => b.GetAttribute("aria-label")));
	}

	// The tooltip says the same thing as the accessible name.
	[Fact]
	public void TheTooltipSaysTheSame()
	{
		IRenderedComponent<MokaKeyValue> cut = Render<MokaKeyValue>(p => p
			.Add(x => x.Label, "Fingerprint")
			.Add(x => x.Value, "ab:cd")
			.Add(x => x.Copyable, true));

		Assert.Equal("Copy Fingerprint", cut.Find(".moka-tooltip-popup").TextContent.Trim());
	}

	// An upper-case label keeps its case: lower-casing would mangle IP, API and SHA-256, and the name has to
	// match the visible text.
	[Fact]
	public void AnUpperCaseLabelKeepsItsCase()
	{
		IRenderedComponent<MokaKeyValue> cut = Render<MokaKeyValue>(p => p
			.Add(x => x.Label, "TRUSTED")
			.Add(x => x.Value, "yes")
			.Add(x => x.Copyable, true));

		Assert.Equal("Copy TRUSTED", cut.Find(".moka-kv__copy").GetAttribute("aria-label"));
	}

	[Fact]
	public void CopyLabelWinsOverTheDefault()
	{
		IRenderedComponent<MokaKeyValue> cut = Render<MokaKeyValue>(p => p
			.Add(x => x.Label, "TRUSTED")
			.Add(x => x.Value, "yes")
			.Add(x => x.Copyable, true)
			.Add(x => x.CopyLabel, "Copy trust state"));

		Assert.Equal("Copy trust state", cut.Find(".moka-kv__copy").GetAttribute("aria-label"));
	}

	// A key-value with no label still has a usable button rather than "Copy ".
	[Fact]
	public void WithoutALabelItIsJustCopy()
	{
		IRenderedComponent<MokaKeyValue> cut = Render<MokaKeyValue>(p => p
			.Add(x => x.Value, "ab:cd")
			.Add(x => x.Copyable, true));

		Assert.Equal("Copy", cut.Find(".moka-kv__copy").GetAttribute("aria-label"));
	}

	[Fact]
	public void TheTooltipSideIsPassedThrough()
	{
		IRenderedComponent<MokaKeyValue> cut = Render<MokaKeyValue>(p => p
			.Add(x => x.Label, "Host")
			.Add(x => x.Value, "localhost")
			.Add(x => x.Copyable, true)
			.Add(x => x.CopyTooltipPosition, MokaTooltipPosition.Top));

		Assert.Contains("moka-tooltip-popup--top", cut.Find(".moka-tooltip-popup").ClassList);
	}

	private sealed class TwoRows : Microsoft.AspNetCore.Components.ComponentBase
	{
		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
			Row(builder, 0, "Trusted", "yes");
			Row(builder, 10, "Fingerprint", "ab:cd");
		}

		private static void Row(RenderTreeBuilder builder, int seq, string label, string value)
		{
			builder.OpenComponent<MokaKeyValue>(seq);
			builder.AddComponentParameter(seq + 1, nameof(MokaKeyValue.Label), label);
			builder.AddComponentParameter(seq + 2, nameof(MokaKeyValue.Value), value);
			builder.AddComponentParameter(seq + 3, nameof(MokaKeyValue.Copyable), true);
			builder.CloseComponent();
		}
	}
}
