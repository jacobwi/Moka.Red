using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Moka.Red.Tests.Shared;

// A parent that holds an input's value the way an app does: it passes Value, listens to ValueChanged and keeps what
// it decides, the asked value unless Keep or Answer says otherwise. Renders TInput with the parameters in With.
internal sealed class ValueParent<TInput, TValue> : ComponentBase
	where TInput : IComponent
{
	private bool _busy;
	private bool _seeded;

	[Parameter]
	public TValue? Value { get; set; }

	[Parameter]
	public Func<TValue?, TValue?>? Keep { get; set; }

	[Parameter]
	public Func<TValue?, Task<TValue?>>? Answer { get; set; }

	[Parameter]
	public bool PassValue { get; set; } = true;

	[Parameter]
	public bool Listen { get; set; } = true;

	[Parameter]
	public bool Bind { get; set; }

	// The input's value parameter, with its callback and expression named after it.
	[Parameter]
	public string ValueName { get; set; } = "Value";

	[Parameter]
	public string? BusyParameter { get; set; }

	[Parameter]
	public IReadOnlyDictionary<string, object?>? With { get; set; }

	public TValue? Held { get; private set; }

	public List<TValue?> Asked { get; } = [];

	protected override void OnParametersSet()
	{
		if (!_seeded)
		{
			_seeded = true;
			Held = Value;
		}
	}

	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		builder.OpenComponent<TInput>(0);
		if (PassValue)
		{
			builder.AddComponentParameter(1, ValueName, Held);
		}

		if (Listen)
		{
			builder.AddComponentParameter(2, ValueName + "Changed", EventCallback.Factory.Create<TValue>(this, TakeAsync));
		}

		if (Bind)
		{
			builder.AddComponentParameter(3, ValueName + "Expression", (Expression<Func<TValue>>)(() => Held!));
		}

		if (BusyParameter is not null)
		{
			builder.AddComponentParameter(4, BusyParameter, _busy);
		}

		foreach (KeyValuePair<string, object?> parameter in With ?? new Dictionary<string, object?>())
		{
			builder.AddComponentParameter(5, parameter.Key, parameter.Value);
		}

		builder.CloseComponent();
	}

	private async Task TakeAsync(TValue? asked)
	{
		Asked.Add(asked);
		if (Answer is null)
		{
			Held = Keep is null ? asked : Keep(asked);
			return;
		}

		_busy = true;
		TValue? kept = await Answer(asked);
		_busy = false;
		Held = kept;
	}
}
