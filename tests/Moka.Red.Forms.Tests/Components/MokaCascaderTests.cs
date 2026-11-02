using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Forms.Cascader;

namespace Moka.Red.Forms.Tests.Components;

/// <summary>
///     The cascader: drilling into columns of options, choosing a path, and walking the same tree
///     with the keyboard.
/// </summary>
public class MokaCascaderTests : BunitContext
{
	// Earth > Europe > Czechia, and Earth > Asia > Japan
	private static readonly IReadOnlyList<MokaCascaderOption> Options =
	[
		new MokaCascaderOption
		{
			Value = "earth", Label = "Earth",
			Children =
			[
				new MokaCascaderOption
				{
					Value = "europe", Label = "Europe",
					Children =
					[
						new MokaCascaderOption { Value = "czechia", Label = "Czechia" },
						new MokaCascaderOption { Value = "germany", Label = "Germany" }
					]
				},
				new MokaCascaderOption { Value = "asia", Label = "Asia", Children = [new MokaCascaderOption { Value = "japan", Label = "Japan" }] }
			]
		}
	];

	public MokaCascaderTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public void ShowsThePlaceholder_UntilAPathIsChosen()
	{
		IRenderedComponent<MokaCascader> cut = Render<MokaCascader>(p => p
			.Add(x => x.Options, Options)
			.Add(x => x.Placeholder, "Pick a place"));

		Assert.Contains("Pick a place", cut.Find(".moka-cascader-path").TextContent, StringComparison.Ordinal);
	}

	[Fact]
	public async Task Opening_ShowsTheRootColumn_ThenDrillingOpensTheNext()
	{
		IRenderedComponent<MokaCascader> cut = Render<MokaCascader>(p => p
			.Add(x => x.Options, Options));

		await cut.Find(".moka-cascader-trigger").ClickAsync(new MouseEventArgs());
		Assert.Single(cut.FindAll(".moka-cascader-column"));
		Assert.Contains("Earth", cut.Find(".moka-cascader-panel").TextContent, StringComparison.Ordinal);

		await cut.FindAll(".moka-cascader-option").Single(o => o.TextContent.Contains("Earth", StringComparison.Ordinal)).ClickAsync(new MouseEventArgs());
		Assert.Equal(2, cut.FindAll(".moka-cascader-column").Count);
		Assert.Contains("Europe", cut.Find(".moka-cascader-panel").TextContent, StringComparison.Ordinal);
	}

	[Fact]
	public async Task ChoosingALeaf_SelectsTheWholePath_AndCloses()
	{
		List<IReadOnlyList<string>>? changes = null;
		IRenderedComponent<MokaCascader> cut = Render<MokaCascader>(p => p
			.Add(x => x.Options, Options)
			.Add(x => x.ValueChanged, v => (changes ??= []).Add(v)));

		await cut.Find(".moka-cascader-trigger").ClickAsync(new MouseEventArgs());
		await cut.FindAll(".moka-cascader-option").Single(o => o.TextContent.Contains("Earth", StringComparison.Ordinal)).ClickAsync(new MouseEventArgs());
		await cut.FindAll(".moka-cascader-option").Single(o => o.TextContent.Contains("Europe", StringComparison.Ordinal)).ClickAsync(new MouseEventArgs());
		await cut.FindAll(".moka-cascader-option").Single(o => o.TextContent.Contains("Czechia", StringComparison.Ordinal)).ClickAsync(new MouseEventArgs());

		Assert.Empty(cut.FindAll(".moka-cascader-panel"));
		Assert.Equal(["earth", "europe", "czechia"], changes!.Single());
		Assert.Contains("Earth", cut.Find(".moka-cascader-path").TextContent, StringComparison.Ordinal);
		Assert.Contains("Czechia", cut.Find(".moka-cascader-path").TextContent, StringComparison.Ordinal);
	}

	[Fact]
	public async Task WithChangeOnSelect_AParentChoosesItsOwnPath()
	{
		List<IReadOnlyList<string>>? changes = null;
		IRenderedComponent<MokaCascader> cut = Render<MokaCascader>(p => p
			.Add(x => x.Options, Options)
			.Add(x => x.ChangeOnSelect, true)
			.Add(x => x.ValueChanged, v => (changes ??= []).Add(v)));

		await cut.Find(".moka-cascader-trigger").ClickAsync(new MouseEventArgs());
		await cut.FindAll(".moka-cascader-option").Single(o => o.TextContent.Contains("Earth", StringComparison.Ordinal)).ClickAsync(new MouseEventArgs());

		Assert.Equal(["earth"], changes!.Single());
	}

	[Fact]
	public async Task AValueFromTheParent_ShowsAsABreadcrumb_AndOpensDrilledToIt()
	{
		IRenderedComponent<MokaCascader> cut = Render<MokaCascader>(p => p
			.Add(x => x.Options, Options)
			.Add(x => x.Value, ["earth", "asia"]));

		Assert.Contains("Asia", cut.Find(".moka-cascader-path").TextContent, StringComparison.Ordinal);

		await cut.Find(".moka-cascader-trigger").ClickAsync(new MouseEventArgs());

		// The root column, Earth's children, and Asia's own children - drilled to where the value
		// ends, so the next choice is one click away - with Asia itself the expanded row
		Assert.Equal(3, cut.FindAll(".moka-cascader-column").Count);
		Assert.Contains("moka-cascader-option--expanded",
			cut.FindAll(".moka-cascader-option").Single(o => o.TextContent.Contains("Asia", StringComparison.Ordinal)).ClassName,
			StringComparison.Ordinal);
	}

	[Fact]
	public async Task Clearable_TakesTheWholePathOff()
	{
		IRenderedComponent<MokaCascader> cut = Render<MokaCascader>(p => p
			.Add(x => x.Options, Options)
			.Add(x => x.Clearable, true)
			.Add(x => x.Value, ["earth"]));

		await cut.Find(".moka-cascader-clear").ClickAsync(new MouseEventArgs());

		Assert.Contains("Select...", cut.Find(".moka-cascader-path").TextContent, StringComparison.Ordinal);
	}

	[Fact]
	public async Task ADisabledOption_CannotBeChosen()
	{
		IRenderedComponent<MokaCascader> cut = Render<MokaCascader>(p => p
			.Add(x => x.Options,
			[
				new MokaCascaderOption { Value = "locked", Label = "Locked", Disabled = true },
				new MokaCascaderOption { Value = "open", Label = "Open" }
			]));

		await cut.Find(".moka-cascader-trigger").ClickAsync(new MouseEventArgs());
		await cut.FindAll(".moka-cascader-option").Single(o => o.TextContent.Contains("Locked", StringComparison.Ordinal)).ClickAsync(new MouseEventArgs());

		Assert.Single(cut.FindAll(".moka-cascader-panel"));
	}

	[Fact]
	public async Task TheKeyboard_WalksTheColumns()
	{
		List<IReadOnlyList<string>>? changes = null;
		IRenderedComponent<MokaCascader> cut = Render<MokaCascader>(p => p
			.Add(x => x.Options, Options)
			.Add(x => x.ValueChanged, v => (changes ??= []).Add(v)));

		IElement trigger = cut.Find(".moka-cascader-trigger");

		// Down to Earth, right into Earth's children, Enter on Europe to open it, down to Germany
		await trigger.KeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown" });
		await trigger.KeyDownAsync(new KeyboardEventArgs { Key = "ArrowRight" });
		await trigger.KeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown" });
		await trigger.KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });
		await trigger.KeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown" });
		await trigger.KeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown" });
		await trigger.KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

		Assert.Equal(["earth", "europe", "germany"], changes!.Single());
	}

	[Fact]
	public async Task Escape_ClosesWithoutChoosing()
	{
		IRenderedComponent<MokaCascader> cut = Render<MokaCascader>(p => p
			.Add(x => x.Options, Options));

		await cut.Find(".moka-cascader-trigger").ClickAsync(new MouseEventArgs());
		await cut.Find(".moka-cascader-trigger").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

		Assert.Empty(cut.FindAll(".moka-cascader-panel"));
	}

	[Fact]
	public void WithoutOptions_ThePanelSaysSo()
	{
		IRenderedComponent<MokaCascader> cut = Render<MokaCascader>();

		cut.Find(".moka-cascader-trigger").Click();

		Assert.Contains("No options", cut.Find(".moka-cascader-empty").TextContent, StringComparison.Ordinal);
	}
}
