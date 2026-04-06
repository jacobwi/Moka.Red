namespace Moka.Red.Feedback.Dialog;

/// <summary>
///     Cascaded to components rendered inside a dialog.
///     Use this to close the dialog and optionally return a result.
/// </summary>
/// <example>
///     <code>
/// [CascadingParameter]
/// public MokaDialogContext? Dialog { get; set; }
/// 
/// private void Save()
/// {
///     Dialog?.Close(myResult);
/// }
/// 
/// private void Cancel()
/// {
///     Dialog?.Cancel();
/// }
/// </code>
/// </example>
public sealed class MokaDialogContext
{
	private readonly IMokaDialogService _service;

	internal MokaDialogContext(IMokaDialogService service)
	{
		_service = service;
	}

	/// <summary>Closes the dialog and returns a result to the caller.</summary>
	/// <param name="result">The result object. The caller of ShowComponentAsync receives this.</param>
	public void Close(object? result = null) => _service.CloseWithResult(result);

	/// <summary>Cancels the dialog (returns null to the caller).</summary>
	public void Cancel() => _service.CloseWithResult(null);
}
