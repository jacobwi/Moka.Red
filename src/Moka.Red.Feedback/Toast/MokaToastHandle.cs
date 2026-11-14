namespace Moka.Red.Feedback.Toast;

/// <summary>
///     The toast a <see cref="IMokaToastService.Show" /> call made, to change or end later: report progress, put the
///     result in place of "Saving", or take it away when the work it describes is gone. Safe to use from any thread, and
///     harmless once the toast has gone.
/// </summary>
public sealed class MokaToastHandle
{
	private readonly IMokaToastService _service;

	/// <summary>A handle on the toast <paramref name="id" /> of <paramref name="service" />.</summary>
	/// <param name="service">The service that shows the toast.</param>
	/// <param name="id">The toast's id.</param>
	public MokaToastHandle(IMokaToastService service, Guid id)
	{
		ArgumentNullException.ThrowIfNull(service);
		_service = service;
		Id = id;
	}

	/// <summary>The toast's id.</summary>
	public Guid Id { get; }

	/// <summary>Whether the toast is still showing or waiting its turn.</summary>
	public bool IsShowing
	{
		get
		{
			foreach (MokaToastMessage toast in _service.Toasts)
			{
				if (toast.Id == Id)
				{
					return true;
				}
			}

			return false;
		}
	}

	/// <summary>Changes the toast. See <see cref="IMokaToastService.Update" />.</summary>
	/// <param name="change">Changes the options, which hold the toast as it is.</param>
	/// <returns>Whether the toast was still there to change.</returns>
	public bool Update(Action<MokaToastOptions> change) => _service.Update(Id, change);

	/// <summary>Takes the toast away, if it's still there.</summary>
	public void Dismiss() => _service.Dismiss(Id);
}
