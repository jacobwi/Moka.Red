using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Forms.ToggleGroup;

namespace Moka.Red.Forms.Tests.Components;

// Controlled: after raising its change callback, the group shows what the parent passes back, so a refused change
// reverts. Without it the group keeps the user's pick (OneWayBindingTests pins that).
public class MokaToggleGroupControlledTests : BunitContext
{
	private static readonly string[] OnlyA = ["a"];

	[Fact]
	public async Task Single_ARefusedPick_Reverts()
	{
		IRenderedComponent<Host> host = Render<Host>(p => p.Add(x => x.Accept, _ => false));

		await host.FindAll("button")[1].ClickAsync(new MouseEventArgs());

		Assert.Equal(["true", "false", "false"], Pressed(host));
		Assert.Equal(["b"], host.Instance.Asked);
	}

	[Fact]
	public async Task Single_AnAcceptedPick_Shows()
	{
		IRenderedComponent<Host> host = Render<Host>(p => p.Add(x => x.Accept, _ => true));

		await host.FindAll("button")[1].ClickAsync(new MouseEventArgs());

		Assert.Equal(["false", "true", "false"], Pressed(host));
	}

	[Fact]
	public async Task Single_ShowsWhatTheParentChoseInstead()
	{
		IRenderedComponent<Host> host = Render<Host>(p => p.Add(x => x.Replace, _ => "c"));

		await host.FindAll("button")[1].ClickAsync(new MouseEventArgs());

		Assert.Equal(["false", "false", "true"], Pressed(host));
	}

	[Fact]
	public async Task Single_ARefusedUnpick_Reverts()
	{
		IRenderedComponent<Host> host = Render<Host>(p => p.Add(x => x.Accept, _ => false));

		await host.FindAll("button")[0].ClickAsync(new MouseEventArgs());

		Assert.Equal(["true", "false", "false"], Pressed(host));
		Assert.Equal([null], host.Instance.Asked);
	}

	// The parent's handler awaits before it decides, as a save to a server does.
	[Fact]
	public async Task Single_AParentThatDecidesLater_IsFollowed()
	{
		TaskCompletionSource<bool> answer = new(TaskCreationOptions.RunContinuationsAsynchronously);
		IRenderedComponent<Host> host = Render<Host>(p => p.Add(x => x.Decide, answer.Task));

		Task click = host.FindAll("button")[1].ClickAsync(new MouseEventArgs());
		Assert.Equal(["true", "false", "false"], Pressed(host));

		answer.SetResult(true);
		await click;

		host.WaitForAssertion(() => Assert.Equal(["false", "true", "false"], Pressed(host)));
	}

	[Fact]
	public async Task Multiple_ARefusedChange_Reverts()
	{
		IRenderedComponent<Host> host = Render<Host>(p => p
			.Add(x => x.Multiple, true)
			.Add(x => x.Accept, _ => false));

		await host.FindAll("button")[2].ClickAsync(new MouseEventArgs());

		Assert.Equal(["true", "false", "false"], Pressed(host));
		Assert.Equal(["a,c"], host.Instance.Asked);
	}

	[Fact]
	public async Task Multiple_AnAcceptedChange_Shows()
	{
		IRenderedComponent<Host> host = Render<Host>(p => p
			.Add(x => x.Multiple, true)
			.Add(x => x.Accept, _ => true));

		await host.FindAll("button")[2].ClickAsync(new MouseEventArgs());

		Assert.Equal(["true", "false", "true"], Pressed(host));
	}

	// A one-way value with no callback: nothing the user clicks can stick.
	[Fact]
	public async Task WithoutACallback_NothingChanges()
	{
		IRenderedComponent<MokaToggleGroup> cut = Render<MokaToggleGroup>(p => p
			.Add(x => x.Controlled, true)
			.Add(x => x.Value, "a")
			.AddChildContent<MokaToggleGroupItem>(item => item.Add(x => x.Value, "a"))
			.AddChildContent<MokaToggleGroupItem>(item => item.Add(x => x.Value, "b")));

		await cut.FindAll("button")[1].ClickAsync(new MouseEventArgs());

		Assert.Equal(["true", "false"], cut.FindAll("button").Select(b => b.GetAttribute("aria-pressed")));
	}

	[Fact]
	public async Task Uncontrolled_StillKeepsTheUsersPick()
	{
		IRenderedComponent<Host> host = Render<Host>(p => p
			.Add(x => x.Controlled, false)
			.Add(x => x.Accept, _ => false));

		await host.FindAll("button")[1].ClickAsync(new MouseEventArgs());

		Assert.Equal(["false", "true", "false"], Pressed(host));
	}

	private static IEnumerable<string?> Pressed(IRenderedComponent<Host> host) =>
		host.FindAll("button").Select(b => b.GetAttribute("aria-pressed"));

	private sealed class Host : ComponentBase
	{
		private string? _value = "a";
		private IReadOnlyList<string> _values = OnlyA;

		[Parameter]
		public bool Controlled { get; set; } = true;

		[Parameter]
		public bool Multiple { get; set; }

		[Parameter]
		public Func<string?, bool>? Accept { get; set; }

		[Parameter]
		public Func<string?, string?>? Replace { get; set; }

		[Parameter]
		public Task<bool>? Decide { get; set; }

		public List<string?> Asked { get; } = [];

		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
			builder.OpenComponent<MokaToggleGroup>(0);
			builder.AddComponentParameter(1, nameof(MokaToggleGroup.Controlled), Controlled);
			builder.AddComponentParameter(2, nameof(MokaToggleGroup.Multiple), Multiple);
			builder.AddComponentParameter(3, nameof(MokaToggleGroup.Value), _value);
			builder.AddComponentParameter(4, nameof(MokaToggleGroup.ValueChanged),
				EventCallback.Factory.Create<string?>(this, ValueChangedAsync));
			builder.AddComponentParameter(5, nameof(MokaToggleGroup.Values), _values);
			builder.AddComponentParameter(6, nameof(MokaToggleGroup.ValuesChanged),
				EventCallback.Factory.Create<IReadOnlyList<string>?>(this, ValuesChanged));
			builder.AddComponentParameter(7, nameof(MokaToggleGroup.ChildContent), (RenderFragment)Items);
			builder.CloseComponent();
		}

		private static void Items(RenderTreeBuilder builder)
		{
			foreach (string value in new[] { "a", "b", "c" })
			{
				builder.OpenComponent<MokaToggleGroupItem>(0);
				builder.AddComponentParameter(1, nameof(MokaToggleGroupItem.Value), value);
				builder.AddComponentParameter(2, nameof(MokaToggleGroupItem.Text), value.ToUpperInvariant());
				builder.CloseComponent();
			}
		}

		private async Task ValueChangedAsync(string? value)
		{
			Asked.Add(value);
			if (Decide is not null)
			{
				if (await Decide)
				{
					_value = value;
				}

				return;
			}

			if (Replace is not null)
			{
				_value = Replace(value);
			}
			else if (Accept?.Invoke(value) == true)
			{
				_value = value;
			}
		}

		private void ValuesChanged(IReadOnlyList<string>? values)
		{
			string joined = string.Join(',', values ?? []);
			Asked.Add(joined);
			if (Accept?.Invoke(joined) == true)
			{
				_values = values ?? [];
			}
		}
	}
}
