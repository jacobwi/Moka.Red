using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Moka.Red.Primitives.SegmentedControl;

namespace Moka.Red.Primitives.Tests.Components;

// Controlled: after raising ValueChanged the control shows what the parent passes back. The radios are native, so the
// browser checks the one picked before .NET hears of it, and Blazor's tree still holds the old one checked: rendering
// the old choice again would send nothing and leave the refused radio checked in the browser. So the control renders
// the browser's radio checked in a batch of its own first, which gives the next render a change to send, while the
// segments' look stays on the value the whole time.
public sealed class MokaSegmentedControlControlledTests : BunitContext
{
	private static readonly bool[] First = [true, false, false];
	private static readonly bool[] Second = [false, true, false];
	private static readonly bool[] Third = [false, false, true];

	[Fact]
	public async Task ARefusedClick_SendsTheBrowsersRadio_ThenChecksTheOldOneAgain()
	{
		string? asked = null;
		IRenderedComponent<MokaSegmentedControl> cut = RenderControl(p => p
			.Add(x => x.Value, "a")
			.Add(x => x.ValueChanged, value => asked = value));
		List<(bool[] Checked, bool[] Drawn)> renders = [];
		cut.OnMarkupUpdated += (_, _) => renders.Add(Read(cut.Markup));

		await cut.FindAll("input[type=radio]")[1].ChangeAsync(new ChangeEventArgs { Value = "b" });

		Assert.Equal("b", asked);
		Assert.Equal(First, Checked(cut));
		Assert.Equal(First, Drawn(cut));
		Assert.Contains(renders, r => r.Checked.SequenceEqual(Second));
		Assert.Equal(First, renders[^1].Checked);
		Assert.All(renders, r => Assert.Equal(First, r.Drawn));
	}

	[Fact]
	public async Task AnAcceptedClick_ChecksTheNewRadio()
	{
		IRenderedComponent<SegmentHost> host = Render<SegmentHost>(p => p.Add(x => x.Accept, true));

		await host.FindAll("input[type=radio]")[2].ChangeAsync(new ChangeEventArgs { Value = "c" });

		Assert.Equal("c", host.Instance.Value);
		Assert.Equal(Third, Checked(host));
		Assert.Equal(Third, Drawn(host));
	}

	[Fact]
	public async Task ARenderingParentThatRefuses_KeepsTheOldChoice()
	{
		IRenderedComponent<SegmentHost> host = Render<SegmentHost>(p => p.Add(x => x.Accept, false));

		await host.FindAll("input[type=radio]")[2].ChangeAsync(new ChangeEventArgs { Value = "c" });

		Assert.Equal(1, host.Instance.Asked);
		Assert.Equal("a", host.Instance.Value);
		Assert.Equal(First, Checked(host));
		Assert.Equal(First, Drawn(host));
	}

	// Nothing to raise is a refusal too: a controlled control without ValueChanged doesn't change.
	[Fact]
	public async Task WithoutValueChanged_TheChoiceStays()
	{
		IRenderedComponent<MokaSegmentedControl> cut = RenderControl(p => p.Add(x => x.Value, "a"));

		await cut.FindAll("input[type=radio]")[1].ChangeAsync(new ChangeEventArgs { Value = "b" });

		Assert.Equal(First, Checked(cut));
	}

	// Every parameter set copies the value in, even one the uncontrolled control would skip as unchanged.
	[Fact]
	public async Task TheParentsValue_Wins_EvenWhenItDidNotChange()
	{
		IRenderedComponent<MokaSegmentedControl> cut = RenderControl(p => p
			.Add(x => x.Value, "a")
			.Add(x => x.ValueChanged, _ => { }));

		await cut.FindAll("input[type=radio]")[1].ChangeAsync(new ChangeEventArgs { Value = "b" });
		cut.Render(p => p.Add(x => x.Value, "a"));
		Assert.Equal(First, Checked(cut));

		cut.Render(p => p.Add(x => x.Value, "c"));
		Assert.Equal(Third, Checked(cut));
		Assert.Equal(Third, Drawn(cut));
	}

	// A click never changes the control itself, so while the parent's answer is on its way the old choice stays,
	// through a parent render that passes the old value too; the answer then decides.
	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task TheOldChoiceStays_WhileTheParentDecides(bool accept)
	{
		TaskCompletionSource<bool> answer = new();
		IRenderedComponent<SegmentHost> host = Render<SegmentHost>(p => p.Add(x => x.Answer, answer.Task));

		Task change = host.FindAll("input[type=radio]")[1].ChangeAsync(new ChangeEventArgs { Value = "b" });
		host.WaitForAssertion(() => Assert.Equal(1, host.Instance.Rendered));
		host.WaitForAssertion(() => Assert.Equal(First, Checked(host)));
		Assert.Equal(First, Drawn(host));

		await host.InvokeAsync(() => answer.SetResult(accept));
		await change;

		Assert.Equal(accept ? Second : First, Checked(host));
		Assert.Equal(accept ? Second : First, Drawn(host));
	}

	[Fact]
	public async Task Uncontrolled_KeepsTheClick_WhenTheParentRefuses()
	{
		IRenderedComponent<SegmentHost> host = Render<SegmentHost>(p => p
			.Add(x => x.Accept, false)
			.Add(x => x.Controlled, false));

		await host.FindAll("input[type=radio]")[1].ChangeAsync(new ChangeEventArgs { Value = "b" });

		Assert.Equal(Second, Checked(host));
		Assert.Equal(Second, Drawn(host));
	}

	private static bool[] Checked<T>(IRenderedComponent<T> cut) where T : IComponent =>
		[.. cut.FindAll("input[type=radio]").Select(r => r.HasAttribute("checked"))];

	private static bool[] Drawn<T>(IRenderedComponent<T> cut) where T : IComponent =>
		[.. cut.FindAll(".moka-segment").Select(s => s.ClassList.Contains("moka-segment--active"))];

	private static (bool[] Checked, bool[] Drawn) Read(string markup)
	{
		IDocument page = new HtmlParser().ParseDocument(markup);
		return (
			[.. page.QuerySelectorAll("input[type=radio]").Select(r => r.HasAttribute("checked"))],
			[.. page.QuerySelectorAll(".moka-segment").Select(s => s.ClassList.Contains("moka-segment--active"))]);
	}

	private IRenderedComponent<MokaSegmentedControl> RenderControl(
		Action<ComponentParameterCollectionBuilder<MokaSegmentedControl>> parameters) =>
		Render<MokaSegmentedControl>(p =>
		{
			p.Add(x => x.Controlled, true);
			parameters(p);
			p.AddChildContent<MokaSegment>(s => s.Add(x => x.Value, "a").Add(x => x.Text, "A"))
				.AddChildContent<MokaSegment>(s => s.Add(x => x.Value, "b").Add(x => x.Text, "B"))
				.AddChildContent<MokaSegment>(s => s.Add(x => x.Value, "c").Add(x => x.Text, "C"));
		});

	private sealed class SegmentHost : ComponentBase
	{
		private static readonly string[] Values = ["a", "b", "c"];

		[Parameter]
		public bool Accept { get; set; }

		[Parameter]
		public bool Controlled { get; set; } = true;

		[Parameter]
		public Task<bool>? Answer { get; set; }

		public string Value { get; private set; } = "a";

		public int Asked { get; private set; }

		// Renders done while the answer is pending, each passing the old value.
		public int Rendered { get; private set; }

		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
			builder.OpenComponent<MokaSegmentedControl>(0);
			builder.AddComponentParameter(1, nameof(MokaSegmentedControl.Controlled), Controlled);
			builder.AddComponentParameter(2, nameof(MokaSegmentedControl.Value), Value);
			builder.AddComponentParameter(3, nameof(MokaSegmentedControl.ValueChanged),
				EventCallback.Factory.Create<string>(this, Changed));
			builder.AddComponentParameter(4, nameof(MokaSegmentedControl.ChildContent), (RenderFragment)(segments =>
			{
				foreach (string value in Values)
				{
					segments.OpenComponent<MokaSegment>(5);
					segments.AddComponentParameter(6, nameof(MokaSegment.Value), value);
					segments.AddComponentParameter(7, nameof(MokaSegment.Text), value.ToUpperInvariant());
					segments.CloseComponent();
				}
			}));
			builder.CloseComponent();
		}

		private async Task Changed(string value)
		{
			Asked++;
			bool accept = Accept;
			if (Answer is not null)
			{
				await Task.Yield();
				StateHasChanged();
				Rendered++;
				accept = await Answer;
			}

			if (accept)
			{
				Value = value;
			}
		}
	}
}
