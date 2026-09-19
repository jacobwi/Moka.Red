using Bunit;
using Microsoft.AspNetCore.Components;
using Moka.Red.Primitives.Sortable;

namespace Moka.Red.Primitives.Tests.Components;

// Blazor only checks @key values against the previous render, so a repeated key passed the first
// render and threw on the next one.
public class MokaSortableTests : BunitContext
{
	public MokaSortableTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public void EqualItems_WithoutAnItemKey_SurviveARerender()
	{
		List<string> items = ["alpha", "alpha", "beta"];

		IRenderedComponent<MokaSortable<string>> cut = Render<MokaSortable<string>>(p => p
			.Add(x => x.Items, items));
		cut.Render();

		Assert.Equal(["alpha", "alpha", "beta"],
			cut.FindAll(".moka-sortable-item__text").Select(e => e.TextContent));
	}

	[Fact]
	public void ARepeatedItemKey_SurvivesARerender()
	{
		List<string> items = ["one", "two", "three"];

		IRenderedComponent<MokaSortable<string>> cut = Render<MokaSortable<string>>(p => p
			.Add(x => x.Items, items)
			.Add(x => x.ItemKey, item => item.Length));
		cut.Render();

		Assert.Equal(3, cut.FindAll(".moka-sortable-item").Count);
	}

	// Items with a unique key must still move with their DOM and components rather than be rebuilt
	// in place.
	[Fact]
	public void UniqueKeys_StillMoveTheirItems()
	{
		List<string> items = ["a", "b", "c"];

		IRenderedComponent<MokaSortable<string>> cut = Render<MokaSortable<string>>(p => p
			.Add(x => x.Items, items)
			.Add(x => x.ItemTemplate, item => builder =>
			{
				builder.OpenComponent<Probe>(0);
				builder.AddAttribute(1, nameof(Probe.Value), item);
				builder.CloseComponent();
			}));
		Probe c = cut.FindComponents<Probe>()[2].Instance;

		items.Reverse();
		cut.Render();

		Probe first = cut.FindComponents<Probe>()[0].Instance;
		Assert.Same(c, first);
		Assert.Equal("c", first.Value);
	}

	private sealed class Probe : ComponentBase
	{
		[Parameter] public string? Value { get; set; }
	}
}
