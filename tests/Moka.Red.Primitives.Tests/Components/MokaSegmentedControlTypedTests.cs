using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Moka.Red.Primitives.SegmentedControl;

namespace Moka.Red.Primitives.Tests.Components;

// The segments' values can be any type: they match by EqualityComparer<TValue>.Default, and each radio's own change
// handler carries its segment's value, so nothing goes through a string.
public sealed class MokaSegmentedControlTypedTests : BunitContext
{
	private static readonly Period[] Periods = [Period.Day, Period.Week, Period.Month];

	private enum Period
	{
		Day,
		Week,
		Month
	}

	[Fact]
	public async Task EnumValues_CheckTheMatchingSegment_AndReportTheEnum()
	{
		Period? chosen = null;
		IRenderedComponent<MokaSegmentedControl<Period>> cut = Render<MokaSegmentedControl<Period>>(p => p
			.Add(x => x.Value, Period.Week)
			.Add(x => x.ValueChanged, value => chosen = value)
			.AddChildContent<MokaSegment<Period>>(s => s.Add(x => x.Value, Period.Day).Add(x => x.Text, "Day"))
			.AddChildContent<MokaSegment<Period>>(s => s.Add(x => x.Value, Period.Week).Add(x => x.Text, "Week"))
			.AddChildContent<MokaSegment<Period>>(s => s.Add(x => x.Value, Period.Month).Add(x => x.Text, "Month")));

		Assert.Equal([false, true, false], Checked(cut));

		await cut.FindAll("input[type=radio]")[2].ChangeAsync(new ChangeEventArgs { Value = "on" });

		Assert.Equal(Period.Month, chosen);
		Assert.Equal([false, false, true], Checked(cut));
		Assert.Equal([false, false, true], Drawn(cut));
	}

	[Fact]
	public async Task IntValues_CheckTheMatchingSegment_AndReportTheNumber()
	{
		int chosen = 0;
		IRenderedComponent<MokaSegmentedControl<int>> cut = Render<MokaSegmentedControl<int>>(p => p
			.Add(x => x.Value, 25)
			.Add(x => x.ValueChanged, value => chosen = value)
			.AddChildContent<MokaSegment<int>>(s => s.Add(x => x.Value, 10).Add(x => x.Text, "10"))
			.AddChildContent<MokaSegment<int>>(s => s.Add(x => x.Value, 25).Add(x => x.Text, "25"))
			.AddChildContent<MokaSegment<int>>(s => s.Add(x => x.Value, 50).Add(x => x.Text, "50")));

		Assert.Equal([false, true, false], Checked(cut));

		await cut.FindAll("input[type=radio]")[2].ChangeAsync(new ChangeEventArgs { Value = "on" });

		Assert.Equal(50, chosen);
		Assert.Equal([false, false, true], Checked(cut));
	}

	// @bind-Value compiles to Value plus a ValueChanged that writes the field back, with the parent as the receiver.
	[Fact]
	public async Task BindValue_RoundTrips_BothWays()
	{
		IRenderedComponent<BoundHost<Period>> host = Render<BoundHost<Period>>(p => p
			.Add(x => x.Start, Period.Week)
			.Add(x => x.Options, Periods));

		await host.FindAll("input[type=radio]")[2].ChangeAsync(new ChangeEventArgs { Value = "on" });

		Assert.Equal(Period.Month, host.Instance.Value);
		Assert.Equal([false, false, true], Checked(host));

		host.Render(p => p.Add(x => x.Start, Period.Day));

		Assert.Equal([true, false, false], Checked(host));
		Assert.Equal([true, false, false], Drawn(host));
	}

	[Fact]
	public void ARecord_MatchesASegment_ByItsContents()
	{
		IRenderedComponent<MokaSegmentedControl<Span>> cut = Render<MokaSegmentedControl<Span>>(p => p
			.Add(x => x.Value, new Span(7))
			.AddChildContent<MokaSegment<Span>>(s => s.Add(x => x.Value, new Span(1)).Add(x => x.Text, "A day"))
			.AddChildContent<MokaSegment<Span>>(s => s.Add(x => x.Value, new Span(7)).Add(x => x.Text, "A week"))
			.AddChildContent<MokaSegment<Span>>(s => s.Add(x => x.Value, new Span(30)).Add(x => x.Text, "A month")));

		Assert.Equal([false, true, false], Checked(cut));
	}

	// A value type built apart from the control's value is the same choice when it compares equal, so picking it again
	// raises nothing, as picking the chosen segment never does.
	[Fact]
	public async Task AValueTypeEqualByValue_IsTheChosenSegment_AndPickingItRaisesNothing()
	{
		int asked = 0;
		IRenderedComponent<MokaSegmentedControl<Window>> cut = Render<MokaSegmentedControl<Window>>(p => p
			.Add(x => x.Value, new Window(24))
			.Add(x => x.ValueChanged, _ => asked++)
			.AddChildContent<MokaSegment<Window>>(s => s.Add(x => x.Value, new Window(6)).Add(x => x.Text, "6 h"))
			.AddChildContent<MokaSegment<Window>>(s => s.Add(x => x.Value, new Window(24)).Add(x => x.Text, "24 h")));

		Assert.Equal([false, true], Checked(cut));

		await cut.FindAll("input[type=radio]")[1].ChangeAsync(new ChangeEventArgs { Value = "on" });

		Assert.Equal(0, asked);
		Assert.Equal([false, true], Checked(cut));
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task Controlled_WithAnEnum_ShowsWhatTheParentDecides(bool accept)
	{
		IRenderedComponent<BoundHost<Period>> host = Render<BoundHost<Period>>(p => p
			.Add(x => x.Start, Period.Day)
			.Add(x => x.Options, Periods)
			.Add(x => x.Controlled, true)
			.Add(x => x.Accept, _ => accept));

		await host.FindAll("input[type=radio]")[1].ChangeAsync(new ChangeEventArgs { Value = "on" });

		Assert.Equal(1, host.Instance.Asked);
		Assert.Equal(accept ? Period.Week : Period.Day, host.Instance.Value);
		bool[] expected = accept ? [false, true, false] : [true, false, false];
		Assert.Equal(expected, Checked(host));
		Assert.Equal(expected, Drawn(host));
	}

	[Fact]
	public async Task ANullableEnum_StartsWithNothingChosen()
	{
		IRenderedComponent<BoundHost<Period?>> host = Render<BoundHost<Period?>>(p => p
			.Add(x => x.Options, [Period.Day, Period.Week, Period.Month]));

		Assert.Equal([false, false, false], Checked(host));

		await host.FindAll("input[type=radio]")[1].ChangeAsync(new ChangeEventArgs { Value = "on" });

		Assert.Equal(Period.Week, host.Instance.Value);
		Assert.Equal([false, true, false], Checked(host));
	}

	// The browser posts "on" for a radio without a value, and the control never reads it: the value never has to be
	// written as text or read back from it.
	[Fact]
	public async Task TheRadio_CarriesNoValueAttribute_ForATypedValue()
	{
		IRenderedComponent<BoundHost<Period>> host = Render<BoundHost<Period>>(p => p
			.Add(x => x.Start, Period.Day)
			.Add(x => x.Options, Periods));

		Assert.All(host.FindAll("input[type=radio]"), radio => Assert.False(radio.HasAttribute("value")));

		await host.FindAll("input[type=radio]")[2].ChangeAsync(new ChangeEventArgs { Value = "on" });

		Assert.Equal(Period.Month, host.Instance.Value);
	}

	// A segment infers its type from its own value first, so Period.Week inside a control of Period? is a segment no
	// control reaches. It says so rather than drawing a radio that does nothing.
	[Fact]
	public void ASegmentOfAnotherType_ThanItsControl_Throws()
	{
		InvalidOperationException error = Assert.Throws<InvalidOperationException>(() =>
			Render<MokaSegmentedControl<Period?>>(p => p
				.AddChildContent<MokaSegment<Period>>(s => s.Add(x => x.Value, Period.Week).Add(x => x.Text, "Week"))));

		Assert.Contains("MokaSegment<Period> must be inside a MokaSegmentedControl<Period>", error.Message, StringComparison.Ordinal);
		Assert.Contains("TValue=", error.Message, StringComparison.Ordinal);
	}

	private static bool[] Checked<T>(IRenderedComponent<T> cut) where T : IComponent =>
		[.. cut.FindAll("input[type=radio]").Select(r => r.HasAttribute("checked"))];

	private static bool[] Drawn<T>(IRenderedComponent<T> cut) where T : IComponent =>
		[.. cut.FindAll(".moka-segment").Select(s => s.ClassList.Contains("moka-segment--active"))];

	private sealed record Span(int Days);

	private readonly record struct Window(int Hours);

	// What a parent with @bind-Value="Value" compiles to, plus the refusal a Controlled parent can make.
	private sealed class BoundHost<TValue> : ComponentBase
	{
		[Parameter]
		public TValue? Start { get; set; }

		[Parameter]
		public IReadOnlyList<TValue> Options { get; set; } = [];

		[Parameter]
		public bool Controlled { get; set; }

		[Parameter]
		public Func<TValue, bool> Accept { get; set; } = static _ => true;

		public TValue? Value { get; private set; }

		public int Asked { get; private set; }

		protected override void OnParametersSet() => Value = Start;

		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
			builder.OpenComponent<MokaSegmentedControl<TValue>>(0);
			builder.AddComponentParameter(1, nameof(MokaSegmentedControl<TValue>.Controlled), Controlled);
			builder.AddComponentParameter(2, nameof(MokaSegmentedControl<TValue>.Value), Value);
			builder.AddComponentParameter(3, nameof(MokaSegmentedControl<TValue>.ValueChanged),
				EventCallback.Factory.Create<TValue>(this, Changed));
			builder.AddComponentParameter(4, nameof(MokaSegmentedControl<TValue>.ChildContent), (RenderFragment)(segments =>
			{
				foreach (TValue option in Options)
				{
					segments.OpenComponent<MokaSegment<TValue>>(5);
					segments.AddComponentParameter(6, nameof(MokaSegment<TValue>.Value), option);
					segments.AddComponentParameter(7, nameof(MokaSegment<TValue>.Text), option?.ToString());
					segments.CloseComponent();
				}
			}));
			builder.CloseComponent();
		}

		private void Changed(TValue value)
		{
			Asked++;
			if (Accept(value))
			{
				Value = value;
			}
		}
	}
}
