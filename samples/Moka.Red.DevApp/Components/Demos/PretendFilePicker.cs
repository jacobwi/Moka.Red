using System.Text;
using Microsoft.AspNetCore.Components.Forms;
using Moka.Red.Core.Files;

namespace Moka.Red.DevApp.Components.Demos;

/// <summary>What <see cref="PretendFilePicker" /> does when it is asked.</summary>
internal enum PretendAnswer
{
	Files,
	Cancel,
	Fail
}

/// <summary>
///     Stands in for a native picker, such as MAUI's: it waits a moment as a dialog would, then hands back made-up files,
///     or cancels, or fails. The QR scanner gets the picture of a code the page drew.
/// </summary>
internal sealed class PretendFilePicker : IMokaFilePicker
{
	private static readonly TimeSpan OpenFor = TimeSpan.FromMilliseconds(1200);

	public PretendAnswer Answer { get; set; }

	public string? CodeImage { get; set; }

	public List<string> Log { get; } = [];

	public event Action? Changed;

	public async Task<IReadOnlyList<IBrowserFile>> PickAsync(MokaFilePickRequest request, CancellationToken cancellationToken)
	{
		Log.Add($"Asked: {(request.Multiple ? "several files" : "one file")}, accept {request.Accept ?? "anything"}");
		Changed?.Invoke();
		await Task.Delay(OpenFor, cancellationToken);

		switch (Answer)
		{
			case PretendAnswer.Cancel:
				Log.Add("Cancelled");
				Changed?.Invoke();
				return [];
			case PretendAnswer.Fail:
				Log.Add("Failed");
				Changed?.Invoke();
				throw new InvalidOperationException("The pretend picker failed, as a native one can.");
		}

		IReadOnlyList<IBrowserFile> files = request.Accept == "image/*"
			? [new PretendFile("code.svg", "image/svg+xml", CodeImage ?? "")]
			:
			[
				new PretendFile("notes.txt", "text/plain", "Notes from the pretend picker."),
				new PretendFile("report.pdf", "application/pdf", "%PDF-1.7"),
				new PretendFile("archive.zip", "application/zip", new string('x', 2 * 1024 * 1024))
			];
		if (!request.Multiple)
		{
			files = [files[0]];
		}

		Log.Add($"Picked: {string.Join(", ", files.Select(file => file.Name))}");
		Changed?.Invoke();
		return files;
	}

	private sealed class PretendFile(string name, string contentType, string content) : IBrowserFile
	{
		private readonly byte[] _bytes = Encoding.UTF8.GetBytes(content);

		public string Name => name;

		public DateTimeOffset LastModified => DateTimeOffset.Now;

		public long Size => _bytes.Length;

		public string ContentType => contentType;

		public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default) =>
			Size > maxAllowedSize
				? throw new IOException($"{Name} is bigger than {maxAllowedSize} bytes.")
				: new MemoryStream(_bytes, writable: false);
	}
}
