namespace Moka.Red.Forms.SchemaForm;

/// <summary>
///     Every word a <see cref="MokaSchemaForm" /> writes, and the problems <see cref="MokaFormSchema.Check" /> reports. A
///     message that names a setting gets its title; one about a limit also gets the limit, written in the current culture.
/// </summary>
public sealed record MokaSchemaFormText
{
	/// <summary>The words a form uses unless it is given others.</summary>
	public static MokaSchemaFormText Default { get; } = new();

	/// <summary>The form's title when neither the form nor the schema names one.</summary>
	public string Title { get; init; } = "Settings";

	/// <summary>The heading of the group that holds the settings.</summary>
	public string GroupTitle { get; init; } = "General";

	/// <summary>The heading of the group that holds the settings marked <c>x-advanced</c>.</summary>
	public string AdvancedGroupTitle { get; init; } = "More settings";

	/// <summary>The tag beside the advanced group's heading.</summary>
	public string AdvancedTag { get; init; } = "Advanced";

	/// <summary>What the form says when the schema has no settings, or there is no schema yet.</summary>
	public string Empty { get; init; } = "There's nothing to set.";

	/// <summary>The choice that leaves a setting that isn't required unset.</summary>
	public string NotSet { get; init; } = "Not set";

	/// <summary>What a required choice shows before one is picked.</summary>
	public string Choose { get; init; } = "Choose one";

	/// <summary>What a write-only setting that holds a value shows in place of it.</summary>
	public string SecretSet { get; init; } = "Set";

	/// <summary>What a write-only setting shows once Clear is pressed.</summary>
	public string SecretCleared { get; init; } = "Cleared when you save";

	/// <summary>The help under a write-only setting without a <c>description</c>.</summary>
	public string SecretHint { get; init; } = "It isn't shown again once saved.";

	/// <summary>The button that opens a write-only setting for a new value. The setting's title follows for screen readers.</summary>
	public string Change { get; init; } = "Change";

	/// <summary>The button that clears a write-only setting that isn't required. The setting's title follows for screen readers.</summary>
	public string Clear { get; init; } = "Clear";

	/// <summary>The button that drops a change to a write-only setting and keeps what's saved.</summary>
	public string KeepSaved { get; init; } = "Keep the saved one";

	/// <summary>The tag beside the title while there are edits nobody saved.</summary>
	public string Unsaved { get; init; } = "Unsaved";

	/// <summary>The footer's sentence while there are edits nobody saved.</summary>
	public string Dirty { get; init; } = "Changes not saved yet";

	/// <summary>The button that puts the edits back.</summary>
	public string Reset { get; init; } = "Put back";

	/// <summary>The save button.</summary>
	public string Save { get; init; } = "Save";

	/// <summary>The save button while a save runs.</summary>
	public string Saving { get; init; } = "Saving";

	/// <summary>The toggle's text while the advanced settings are hidden.</summary>
	public string ShowAdvanced { get; init; } = "Show advanced";

	/// <summary>The toggle's text while the advanced settings show.</summary>
	public string HideAdvanced { get; init; } = "Hide advanced";

	/// <summary>Under the fields when Save found problems, with how many settings have one.</summary>
	public Func<int, string> CheckMarked { get; init; } = count => count == 1
		? "Check the setting marked above."
		: $"Check the {count} settings marked above.";

	/// <summary>A required setting that isn't set.</summary>
	public Func<string, string> Needed { get; init; } = title => $"{title} is needed.";

	/// <summary>A text setting that holds something else.</summary>
	public Func<string, string> NotText { get; init; } = title => $"{title} takes text.";

	/// <summary>A number setting that holds something else.</summary>
	public Func<string, string> NotNumber { get; init; } = title => $"{title} takes a number.";

	/// <summary>A whole-number setting that holds a fraction or something else.</summary>
	public Func<string, string> NotWholeNumber { get; init; } = title => $"{title} takes a whole number.";

	/// <summary>A switch setting that holds something other than true or false.</summary>
	public Func<string, string> NotToggle { get; init; } = title => $"{title} is on or off.";

	/// <summary>A choice setting that holds a value that isn't one of its choices.</summary>
	public Func<string, string> NotChoice { get; init; } = title => $"{title} takes one of its choices.";

	/// <summary>Text shorter than <c>minLength</c>.</summary>
	public Func<string, string, string> TooShort { get; init; } = (title, limit) => $"{title} needs at least {limit} characters.";

	/// <summary>Text longer than <c>maxLength</c>.</summary>
	public Func<string, string, string> TooLong { get; init; } = (title, limit) => $"{title} takes at most {limit} characters.";

	/// <summary>A number below <c>minimum</c>.</summary>
	public Func<string, string, string> TooSmall { get; init; } = (title, limit) => $"{title} is {limit} or more.";

	/// <summary>A number at or below <c>exclusiveMinimum</c>.</summary>
	public Func<string, string, string> TooSmallExclusive { get; init; } = (title, limit) => $"{title} is more than {limit}.";

	/// <summary>A number above <c>maximum</c>.</summary>
	public Func<string, string, string> TooLarge { get; init; } = (title, limit) => $"{title} is {limit} or less.";

	/// <summary>A number at or above <c>exclusiveMaximum</c>.</summary>
	public Func<string, string, string> TooLargeExclusive { get; init; } = (title, limit) => $"{title} is less than {limit}.";

	/// <summary>A number that isn't a multiple of <c>multipleOf</c>.</summary>
	public Func<string, string, string> NotMultiple { get; init; } = (title, step) => $"{title} goes in steps of {step}.";

	/// <summary>Text that doesn't match <c>pattern</c>.</summary>
	public Func<string, string> NoMatch { get; init; } = title => $"{title} isn't in the form it takes.";

	/// <summary>Text that isn't an email address (<c>"format": "email"</c>).</summary>
	public Func<string, string> NotEmail { get; init; } = title => $"{title} is an email address, like name@example.com.";

	/// <summary>Text that isn't an absolute address (<c>"format": "uri"</c>).</summary>
	public Func<string, string> NotUri { get; init; } = title => $"{title} is a full address, like https://example.com.";

	/// <summary>Text that isn't a host name (<c>"format": "hostname"</c>).</summary>
	public Func<string, string> NotHostName { get; init; } = title => $"{title} is a host name, like mail.example.com.";

	/// <summary>Text that isn't an IPv4 address (<c>"format": "ipv4"</c>).</summary>
	public Func<string, string> NotIpv4 { get; init; } = title => $"{title} is an IPv4 address, like 192.0.2.10.";

	/// <summary>Text that isn't an IPv6 address (<c>"format": "ipv6"</c>).</summary>
	public Func<string, string> NotIpv6 { get; init; } = title => $"{title} is an IPv6 address, like 2001:db8::1.";

	/// <summary>Text that isn't a UUID (<c>"format": "uuid"</c>).</summary>
	public Func<string, string> NotUuid { get; init; } = title => $"{title} is a UUID, like 123e4567-e89b-12d3-a456-426614174000.";

	/// <summary>
	///     Text that fits none of the formats an <c>anyOf</c> lists. Gets the setting's title and the formats' names, such as
	///     <see cref="HostNameFormatName" />, in the schema's order.
	/// </summary>
	public Func<string, IReadOnlyList<string>, string> NotAnyFormat { get; init; } = (title, formats) =>
		$"{title} is {JoinWithOr(formats)}.";

	/// <summary>The <c>email</c> format's name in <see cref="NotAnyFormat" />.</summary>
	public string EmailFormatName { get; init; } = "an email address";

	/// <summary>The <c>uri</c> format's name in <see cref="NotAnyFormat" />.</summary>
	public string FullAddressFormatName { get; init; } = "a full address";

	/// <summary>The <c>hostname</c> format's name in <see cref="NotAnyFormat" />.</summary>
	public string HostNameFormatName { get; init; } = "a host name";

	/// <summary>The <c>ipv4</c> format's name in <see cref="NotAnyFormat" />.</summary>
	public string Ipv4FormatName { get; init; } = "an IPv4 address";

	/// <summary>The <c>ipv6</c> format's name in <see cref="NotAnyFormat" />.</summary>
	public string Ipv6FormatName { get; init; } = "an IPv6 address";

	/// <summary>The <c>uuid</c> format's name in <see cref="NotAnyFormat" />.</summary>
	public string UuidFormatName { get; init; } = "a UUID";

	/// <summary>A property the schema doesn't name, when it sets <c>"additionalProperties": false</c>. Gets the property's name.</summary>
	public Func<string, string> NotAllowed { get; init; } = name => $"There's no setting called {name}.";

	private static string JoinWithOr(IReadOnlyList<string> names) => names.Count switch
	{
		0 => string.Empty,
		1 => names[0],
		_ => $"{string.Join(", ", names.Take(names.Count - 1))} or {names[^1]}"
	};
}
