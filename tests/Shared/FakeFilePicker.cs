using System.Text;
using Microsoft.AspNetCore.Components.Forms;
using Moka.Red.Core.Files;

namespace Moka.Red.Tests.Shared;

/// <summary>
///     Stands in for an app's own file picker: it records what each component asked for and hands back
///     <see cref="Files" />, fails with <see cref="Failure" />, or stays open until <see cref="Close" />. Linked into the
///     test projects of the components that open a file dialog.
/// </summary>
internal sealed class FakeFilePicker : IMokaFilePicker
{
	private TaskCompletionSource<IReadOnlyList<IBrowserFile>>? _open;

	public List<MokaFilePickRequest> Requests { get; } = [];

	public IReadOnlyList<IBrowserFile> Files { get; set; } = [];

	public Exception? Failure { get; set; }

	public bool StaysOpen { get; set; }

	public CancellationToken LastToken { get; private set; }

	public Task<IReadOnlyList<IBrowserFile>> PickAsync(MokaFilePickRequest request, CancellationToken cancellationToken)
	{
		Requests.Add(request);
		LastToken = cancellationToken;
		if (Failure is not null)
		{
			return Task.FromException<IReadOnlyList<IBrowserFile>>(Failure);
		}

		if (StaysOpen)
		{
			_open = new TaskCompletionSource<IReadOnlyList<IBrowserFile>>(TaskCreationOptions.RunContinuationsAsynchronously);
			return _open.Task;
		}

		return Task.FromResult(Files);
	}

	/// <summary>Closes a picker that <see cref="StaysOpen" /> with these files, or none for a cancel.</summary>
	public void Close(params IBrowserFile[] files) => _open!.SetResult(files);
}

/// <summary>A file an app's picker chose, whose stream seeks as a file on disk does.</summary>
internal sealed class PickedFile(string name, string content = "x", string contentType = "application/octet-stream") : IBrowserFile
{
	private readonly byte[] _bytes = Encoding.UTF8.GetBytes(content);

	public string Name => name;

	public DateTimeOffset LastModified => DateTimeOffset.UnixEpoch;

	public long Size => SizeOverride ?? _bytes.Length;

	public long? SizeOverride { get; init; }

	public string ContentType => contentType;

	public int Opened { get; private set; }

	public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default)
	{
		Opened++;
		return new MemoryStream(_bytes, writable: false);
	}
}
