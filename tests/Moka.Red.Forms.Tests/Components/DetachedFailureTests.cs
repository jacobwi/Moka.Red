using Bunit;
using Microsoft.AspNetCore.Components;
using Moka.Red.Forms.AutoComplete;
using Moka.Red.Forms.ColorPicker;
using Moka.Red.Forms.SearchInput;
using Moka.Red.Forms.TextField;
using TestContext = Xunit.TestContext;

namespace Moka.Red.Forms.Tests.Components;

// These run a consumer's code from a timer or a synchronous path, in a task nothing awaited, so what it
// threw vanished: no error boundary, no log, just a control that stopped working.
public class DetachedFailureTests : BunitContext
{
	private static readonly TimeSpan Wait = TimeSpan.FromSeconds(5);

	public DetachedFailureTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public async Task AutoComplete_ASearchThatFails_ReachesTheRenderer()
	{
		IRenderedComponent<MokaAutoComplete<string>> cut = Render<MokaAutoComplete<string>>(p => p
			.Add(x => x.Debounce, 1)
			.Add(x => x.SearchFunc, Fail<IEnumerable<string>>));

		await cut.Find("input[role=combobox]").InputAsync(new ChangeEventArgs { Value = "Ber" });

		await AssertReachedTheRenderer();
	}

	[Fact]
	public async Task SearchInput_AnOnSearchThatFails_ReachesTheRenderer()
	{
		IRenderedComponent<MokaSearchInput> cut = Render<MokaSearchInput>(p => p
			.Add(x => x.Debounce, 1)
			.Add(x => x.OnSearch, EventCallback.Factory.Create<string>(this, Fail)));

		await cut.Find("input").InputAsync(new ChangeEventArgs { Value = "Ber" });

		await AssertReachedTheRenderer();
	}

	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	public async Task TextField_AnOnInputThatFails_ReachesTheRenderer(int debounceDelay)
	{
		IRenderedComponent<MokaTextField> cut = Render<MokaTextField>(p => p
			.Add(x => x.DebounceDelay, debounceDelay)
			.Add(x => x.OnInput, EventCallback.Factory.Create<string>(this, Fail)));

		await cut.Find("input").InputAsync(new ChangeEventArgs { Value = "Ber" });

		await AssertReachedTheRenderer();
	}

	[Fact]
	public async Task ColorPicker_DraggingRaisesValueChangedOnce()
	{
		List<string> values = [];
		IRenderedComponent<MokaColorPicker> cut = Render<MokaColorPicker>(p => p
			.Add(x => x.Value, "#336699")
			.Add(x => x.ValueChanged, value => values.Add(value)));

		await cut.InvokeAsync(() => cut.Instance.OnHueSliderChanged(0.5));

		Assert.Single(values);
	}

	private async Task AssertReachedTheRenderer()
	{
		Exception error = await Renderer.UnhandledException.WaitAsync(Wait, TestContext.Current.CancellationToken);
		Assert.Equal("Handler failed", error.Message);
	}

	private static async Task Fail(string _)
	{
		await Task.Yield();
		throw new InvalidOperationException("Handler failed");
	}

	private static async Task<T> Fail<T>(string _)
	{
		await Task.Yield();
		throw new InvalidOperationException("Handler failed");
	}
}
