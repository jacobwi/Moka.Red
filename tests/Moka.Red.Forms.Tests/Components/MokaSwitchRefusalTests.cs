using System.Linq.Expressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Moka.Red.Forms.Switch;

namespace Moka.Red.Forms.Tests.Components;

// A switch bound one way, Value and ValueChanged with no ValueExpression, whose parent never takes the change: a
// setting the system refuses to save. Blazor hands a child its parameters again only when one of them changed, and
// every parameter here is a bool, a string or a callback, so nothing used to put the switch back.
public sealed class MokaSwitchRefusalTests : BunitContext
{
	private const string Probe = "probe";

	public MokaSwitchRefusalTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public void ASwitchWhoseParentKeepsItsValue_GoesBackToIt()
	{
		IRenderedComponent<Host> host = Render<Host>();

		host.Find($"#{Probe}").Change(true);

		Assert.Equal(1, host.Instance.Asked);
		Assert.False(host.Instance.Saved);
		Assert.False(Checked(host));
	}

	[Fact]
	public async Task TheSameSwitch_GoesBack_WhenTheParentRendersWhileItDecides()
	{
		IRenderedComponent<Host> host = Render<Host>(parameters => parameters.Add(item => item.Busies, true));

		await host.Find($"#{Probe}").ChangeAsync(new ChangeEventArgs { Value = true });
		Assert.True(Checked(host));

		await host.InvokeAsync(() => host.Instance.Decide());

		Assert.False(Checked(host));
	}

	[Fact]
	public void TheSameSwitch_GoesBack_WithAValueExpression()
	{
		IRenderedComponent<Host> host = Render<Host>(parameters => parameters.Add(item => item.WithExpression, true));

		host.Find($"#{Probe}").Change(true);

		Assert.False(Checked(host));
	}

	[Fact]
	public void TheFrameworkCheckbox_GoesBack()
	{
		IRenderedComponent<Host> host = Render<Host>(parameters => parameters.Add(item => item.Framework, true));

		host.Find($"#{Probe}").Change(true);

		Assert.False(Checked(host));
	}

	private static bool Checked(IRenderedComponent<Host> host) => host.Find($"#{Probe}").HasAttribute("checked");

	private sealed class Host : ComponentBase
	{
		private readonly TaskCompletionSource _decided = new();
		private bool _busy;

		[Parameter]
		public bool Busies { get; set; }

		[Parameter]
		public bool Framework { get; set; }

		[Parameter]
		public bool WithExpression { get; set; }

		public bool Saved { get; private set; }

		public int Asked { get; private set; }

		public void Decide() => _decided.TrySetResult();

		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
			if (Framework)
			{
				builder.OpenComponent<InputCheckbox>(0);
				builder.AddComponentParameter(1, nameof(InputCheckbox.Value), Saved);
				builder.AddComponentParameter(2, nameof(InputCheckbox.ValueChanged), EventCallback.Factory.Create<bool>(this, RefuseAsync));
				builder.AddComponentParameter(3, nameof(InputCheckbox.ValueExpression), (Expression<Func<bool>>)(() => Saved));
				builder.AddComponentParameter(4, nameof(InputCheckbox.AdditionalAttributes), new Dictionary<string, object> { ["id"] = Probe });
				builder.CloseComponent();
				return;
			}

			builder.OpenComponent<MokaSwitch>(10);
			builder.AddComponentParameter(11, nameof(MokaSwitch.Id), Probe);
			builder.AddComponentParameter(12, nameof(MokaSwitch.Value), Saved);
			builder.AddComponentParameter(13, nameof(MokaSwitch.ValueChanged), EventCallback.Factory.Create<bool>(this, RefuseAsync));
			builder.AddComponentParameter(14, nameof(MokaSwitch.Disabled), _busy);
			if (WithExpression)
			{
				builder.AddComponentParameter(15, nameof(MokaSwitch.ValueExpression), (Expression<Func<bool>>)(() => Saved));
			}

			builder.CloseComponent();
		}

		private async Task RefuseAsync(bool asked)
		{
			Asked++;
			if (!Busies)
			{
				return;
			}

			_busy = true;
			StateHasChanged();
			await _decided.Task;
			_busy = false;
		}
	}
}
