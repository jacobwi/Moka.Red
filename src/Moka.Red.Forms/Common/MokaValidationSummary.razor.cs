using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Forms.Common;

/// <summary>
///     Every validation message the cascaded <see cref="EditContext" /> holds, listed above a form. Renders
///     nothing while the form has none, so an empty box never sits above the fields, and the list is a
///     <c>role="alert"</c>, so it is read out when it fills.
///     It lists messages rather than linking to fields: a control's id is generated per field wrapper, and a
///     message carries no way back to it.
/// </summary>
public partial class MokaValidationSummary : MokaVisualComponentBase
{
	private readonly string _titleId = $"moka-validation-summary-{Guid.NewGuid():N}";

	private IReadOnlyList<string> _messages = [];
	private EditContext? _subscribed;

	/// <summary>The form this summarises. Supplied by <c>EditForm</c>; without one the summary renders nothing.</summary>
	[CascadingParameter]
	public EditContext? EditContext { get; set; }

	/// <summary>
	///     Heading above the list. Defaults to "Please fix the following"; null or empty renders the messages
	///     with no heading.
	/// </summary>
	[Parameter]
	public string? Title { get; set; } = "Please fix the following";

	/// <inheritdoc />
	protected override string RootClass => "moka-validation-summary";

	private string TitleId => Id is null ? _titleId : $"{Id}-title";

	private bool HasTitle => !string.IsNullOrEmpty(Title);

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass(Class)
		.Build();

	/// <summary>The messages change with the form's validation state, outside the parameter flow.</summary>
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override ValueTask DisposeAsyncCore()
	{
		Unsubscribe();
		return base.DisposeAsyncCore();
	}

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		// A form can hand out a new EditContext (EditForm does on a new Model), and the old one would go on
		// raising events into a summary that no longer shows its messages.
		if (!ReferenceEquals(_subscribed, EditContext))
		{
			Unsubscribe();
			_subscribed = EditContext;

			if (_subscribed is not null)
			{
				_subscribed.OnValidationStateChanged += HandleValidationStateChanged;
			}
		}

		ReadMessages();
	}

	private void HandleValidationStateChanged(object? sender, ValidationStateChangedEventArgs e)
	{
		ReadMessages();
		StateHasChanged();
	}

	// Read when the state changes rather than on every render: the messages are walked to build the list.
	private void ReadMessages() =>
		_messages = EditContext is null ? [] : EditContext.GetValidationMessages().ToList();

	private void Unsubscribe()
	{
		if (_subscribed is not null)
		{
			_subscribed.OnValidationStateChanged -= HandleValidationStateChanged;
			_subscribed = null;
		}
	}
}
