using System.Globalization;

namespace Moka.Red.Forms.FileUpload;

/// <summary>
///     Every word a <see cref="MokaFileUpload" /> writes. Set the ones that should say something about your own app, or
///     all of them to translate it. Anything holding a name or a count is a function, so the words can go around it and
///     plurals can follow it. The few words the browser fills in while a file is dragged are strings with <c>{0}</c> where
///     the browser puts the count or the type, since the drag never reaches .NET.
/// </summary>
public sealed record MokaFileUploadText
{
	/// <summary>The zone's words before the Browse link.</summary>
	public string DragHere { get; init; } = "Drag files here or";

	/// <summary>The Browse link of the standard zone, and the Browse button of the field look.</summary>
	public string Browse { get; init; } = "Browse";

	/// <summary>The link inside a sentence, in the card list and quiet looks.</summary>
	public string BrowseInline { get; init; } = "browse";

	/// <summary>The one link of the compact standard zone.</summary>
	public string ChooseFiles { get; init; } = "Choose file(s)";

	/// <summary>The quiet line's words before its link.</summary>
	public string DropHere { get; init; } = "Drop files here or";

	/// <summary>The field look with nothing chosen yet.</summary>
	public string NoFileChosen { get; init; } = "No file chosen";

	/// <summary>The words of the tint drag look while a file is over the zone.</summary>
	public string DropToAdd { get; init; } = "Drop to add it";

	/// <summary>The words of the lift drag look while a file is over the zone.</summary>
	public string DropToUpload { get; init; } = "Drop to upload";

	/// <summary>The button that picks a new file in place of the one there, in the picture and slot looks.</summary>
	public string Replace { get; init; } = "Replace";

	/// <summary>The button that takes the picture away, in the picture look.</summary>
	public string RemoveText { get; init; } = "Remove";

	/// <summary>The empty picture frame.</summary>
	public string AddPicture { get; init; } = "Add picture";

	/// <summary>The tile at the end of the gallery.</summary>
	public string Add { get; init; } = "Add";

	/// <summary>The tag on the first picture of the gallery.</summary>
	public string Cover { get; init; } = "Cover";

	/// <summary>The empty slot.</summary>
	public string NoFileYet { get; init; } = "No file yet";

	/// <summary>The button of the empty slot.</summary>
	public string ChooseFile { get; init; } = "Choose file";

	/// <summary>An upload the app reports done.</summary>
	public string Done { get; init; } = "Done";

	/// <summary>An upload waiting its turn, after the file's size.</summary>
	public string Waiting { get; init; } = "waiting";

	/// <summary>A failed upload whose status has no message.</summary>
	public string Failed { get; init; } = "Failed";

	/// <summary>The text button on a failed upload, where there is room for one.</summary>
	public string RetryText { get; init; } = "Retry";

	/// <summary>The button under the chips list that removes every file.</summary>
	public string ClearAll { get; init; } = "Clear all";

	/// <summary>What goes between two facts about a file, such as its size and its type.</summary>
	public string Separator { get; init; } = ", ";

	/// <summary>The last word of a list of types, as in "PDF, PNG or JPG".</summary>
	public string Or { get; init; } = "or";

	/// <summary>What <c>image/*</c> in <c>Accept</c> reads as in a list of types.</summary>
	public string Images { get; init; } = "images";

	/// <summary>The terminal look's first line, after the prompt.</summary>
	public string TerminalPrompt { get; init; } = "drop files to upload";

	/// <summary>The terminal look's second line, from the accepted types (null for any) and the size limit.</summary>
	public Func<string?, string, string> TerminalHint { get; init; } = (types, size) =>
		types is null ? $"or press Enter to browse. max {size}" : $"or press Enter to browse. {types}, max {size}";

	/// <summary>A terminal line for a file the app reports done.</summary>
	public string TerminalDone { get; init; } = "done";

	/// <summary>A terminal line for a file waiting its turn.</summary>
	public string TerminalWaiting { get; init; } = "waiting";

	/// <summary>A terminal line for a failed upload whose status has no message.</summary>
	public string TerminalFailed { get; init; } = "failed";

	/// <summary>A terminal line for a file of a type the upload doesn't take.</summary>
	public string TerminalWrongType { get; init; } = "wrong type";

	/// <summary>A terminal line for a file over the size limit.</summary>
	public string TerminalTooBig { get; init; } = "too big";

	/// <summary>The terminal look's last line, from how many files are in and how many the last pick or drop turned away.</summary>
	public Func<int, int, string> TerminalSummary { get; init; } = (added, refused) => refused == 0
		? string.Create(CultureInfo.CurrentCulture, $"{added} added")
		: string.Create(CultureInfo.CurrentCulture, $"{added} added, {refused} refused");

	/// <summary>Names the remove button, from the file's name. Shown in its tooltip and read by screen readers.</summary>
	public Func<string, string> Remove { get; init; } = name => $"Remove {name}";

	/// <summary>Names the remove button of a file the app reports uploading, from the file's name.</summary>
	public Func<string, string> Cancel { get; init; } = name => $"Cancel {name}";

	/// <summary>Names the retry button, from the file's name. Shown in its tooltip and read by screen readers.</summary>
	public Func<string, string> Retry { get; init; } = name => $"Retry {name}";

	/// <summary>How much of a file is up, such as "516.1 of 860.2 KB". Done comes first.</summary>
	public Func<string, string, string> Amount { get; init; } = (done, whole) => $"{done} of {whole}";

	/// <summary>How many files and how much they hold together, such as "3 files, 3.3 MB".</summary>
	public Func<int, string, string> Total { get; init; } = (count, size) => count == 1
		? $"1 file, {size}"
		: string.Create(CultureInfo.CurrentCulture, $"{count} files, {size}");

	/// <summary>How full the gallery is, such as "7 of 12".</summary>
	public Func<int, int, string> CountOf { get; init; } = (count, limit) =>
		string.Create(CultureInfo.CurrentCulture, $"{count} of {limit}");

	/// <summary>The card list zone's second line, from the size limit.</summary>
	public Func<string, string> SizeLimit { get; init; } = size => $"Up to {size} each";

	/// <summary>The empty slot's second line, from the accepted types (null for any) and the size limit.</summary>
	public Func<string?, string, string> SlotLimit { get; init; } = (types, size) =>
		types is null ? $"One file, {size} max" : $"One {types}, {size} max";

	/// <summary>While a file is dragged over, a drop that fits. <c>{0}</c> is the count, from <see cref="OneFile" /> or <see cref="ManyFiles" />.</summary>
	public string CheckReady { get; init; } = "{0}, ready to drop";

	/// <summary>The count of one dragged file.</summary>
	public string OneFile { get; init; } = "1 file";

	/// <summary>The count of several dragged files. <c>{0}</c> is the number.</summary>
	public string ManyFiles { get; init; } = "{0} files";

	/// <summary>While a file is dragged over, a type the upload doesn't take. <c>{0}</c> is the type, such as "ZIP".</summary>
	public string CheckWrongType { get; init; } = "{0} files can't be added";

	/// <summary>The line under <see cref="CheckWrongType" />, from the accepted types.</summary>
	public Func<string, string> CheckOnly { get; init; } = types => $"Only {types}";

	/// <summary>While a file is dragged over, an upload that already holds its limit.</summary>
	public string CheckFull { get; init; } = "No room for more files";

	/// <summary>The line under <see cref="CheckFull" />, from the limit.</summary>
	public Func<int, string> CheckLimit { get; init; } = limit => string.Create(CultureInfo.CurrentCulture, $"The limit is {limit}");

	/// <summary>While files are dragged over, more of them than fit.</summary>
	public string CheckTooMany { get; init; } = "Too many files";

	/// <summary>The line under <see cref="CheckTooMany" />, from how many more fit.</summary>
	public Func<int, string> CheckRoom { get; init; } = room => room == 1
		? "Only 1 more fits"
		: string.Create(CultureInfo.CurrentCulture, $"Only {room} more fit");

	/// <summary>The line under <see cref="CheckTooMany" /> for an upload that takes one file.</summary>
	public string CheckOneAtATime { get; init; } = "One file at a time";

	/// <summary>The error for a file of a type the upload doesn't take, from its name.</summary>
	public Func<string, string> NotAccepted { get; init; } = name => $"{name} is not an accepted file type";

	/// <summary>The error for a file over the size limit, from its name and the limit.</summary>
	public Func<string, string, string> TooLarge { get; init; } = (name, size) => $"{name} exceeds maximum size ({size})";

	/// <summary>The error for the files past <c>MaxFiles</c>, from how many and the limit.</summary>
	public Func<int, int, string> OverLimit { get; init; } = (count, limit) => count == 1
		? string.Create(CultureInfo.CurrentCulture, $"1 file was not added: the limit is {limit}")
		: string.Create(CultureInfo.CurrentCulture, $"{count} files were not added: the limit is {limit}");

	/// <summary>What a screen reader hears when the app reports a file uploaded.</summary>
	public Func<string, string> Uploaded { get; init; } = name => $"{name} uploaded";

	/// <summary>What a screen reader hears when the app reports a file failed, from its name and the status message.</summary>
	public Func<string, string?, string> UploadFailed { get; init; } = (name, message) =>
		string.IsNullOrWhiteSpace(message) ? $"{name} failed" : $"{name} failed: {message}";
}
