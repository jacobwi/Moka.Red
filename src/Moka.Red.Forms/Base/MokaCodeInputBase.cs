using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;
using Moka.Red.Forms.Common;

namespace Moka.Red.Forms.Base;

/// <summary>
///     Base class for a code typed as digits, such as a one-time code or a PIN: one text field that holds the whole
///     code, drawn over a row of boxes, one per digit.
/// </summary>
/// <remarks>
///     <para>
///         One field rather than one per digit, as web.dev's guidance for one-time code forms, GOV.UK's security code
///         pattern and the WCAG 2.2 understanding of success criterion 3.3.8 point to: a pasted or autofilled code lands
///         whole, the browser and password managers see one field with one <c>autocomplete</c> token, and a screen reader
///         hears one labelled field. The digits are the field's own text, in the monospace font with a letter spacing
///         that steps from box to box, so what is typed shows at once with no round trip, on a static page too.
///     </para>
///     <para>
///         The value keeps only the digits 0 to 9, up to <see cref="Length" />; full width digits count as the digits
///         they show, and spaces, hyphens and anything else are dropped, so a code pasted as "123 456" arrives as "123456". Once the
///         field is interactive a script does the same as the text arrives and marks the box at the caret.
///     </para>
/// </remarks>
public abstract class MokaCodeInputBase : MokaVisualInputBase<string?>
{
	private const string CodeModule = "./_content/Moka.Red.Forms/moka-code.js";
	private const char FullWidthZero = '０';
	private const char FullWidthNine = '９';

	private bool _bound;
	private string? _generatedId;
	private bool _readingSms;

	/// <summary>Compares values so that an empty code and no code are the same.</summary>
	protected MokaCodeInputBase() : base(EmptyMeansEmpty.Instance)
	{
	}

	/// <summary>Label text displayed above the boxes. It names the field; without it the field has a name of its own.</summary>
	[Parameter]
	public string? Label { get; set; }

	/// <summary>Helper text displayed below the boxes.</summary>
	[Parameter]
	public string? HelperText { get; set; }

	/// <summary>Error text displayed below the boxes. Wins over a validation message.</summary>
	[Parameter]
	public string? ErrorText { get; set; }

	/// <summary>
	///     Shows a required marker after the label and sets <c>aria-required</c> on the field. The model's validation
	///     attributes still decide what is valid.
	/// </summary>
	[Parameter]
	public bool Required { get; set; }

	/// <summary>The number of digits, one box each. Below 1 counts as 1.</summary>
	[Parameter]
	public virtual int Length { get; set; } = 6;

	/// <summary>Shows a dot for each digit and makes the field a password field.</summary>
	[Parameter]
	public virtual bool Masked { get; set; }

	/// <summary>Raised when a change the person makes fills every box, with the code.</summary>
	[Parameter]
	public EventCallback<string> OnComplete { get; set; }

	/// <summary>The number of boxes: <see cref="Length" />, at least 1.</summary>
	protected int BoxCount => Math.Max(Length, 1);

	/// <summary>
	///     The field's <c>autocomplete</c> token: <c>one-time-code</c> lets the browser offer a code it received, and
	///     <c>off</c> keeps a secret out of the browser's memory.
	/// </summary>
	protected abstract string AutocompleteToken { get; }

	/// <summary>The field's accessible name when there is no <see cref="Label" /> and no <c>aria-label</c>.</summary>
	protected abstract string DefaultName { get; }

	/// <summary>
	///     Whether the field asks the browser for a code from a text message through the WebOTP API. False unless a
	///     component turns it on.
	/// </summary>
	protected virtual bool ReadsSmsCode => false;

	/// <summary>The field. The derived markup puts <c>@ref="Field"</c> on its <c>input</c>.</summary>
	protected ElementReference Field { get; set; }

	/// <summary>
	///     The field's id: <see cref="MokaInputBase{TValue}.Id" /> when set, otherwise one generated for this instance.
	///     The label's <c>for</c> and the message id follow it.
	/// </summary>
	protected string InputId => string.IsNullOrEmpty(Id) ? _generatedId ??= $"{RootClass}-{Guid.NewGuid():N}" : Id;

	/// <summary>Whether a label is rendered.</summary>
	protected bool HasLabel => !string.IsNullOrEmpty(Label);

	/// <summary>True when <see cref="ErrorText" /> is set or the <c>EditContext</c> reports a message.</summary>
	protected bool HasError => !string.IsNullOrEmpty(ErrorText) || HasValidationError;

	/// <summary>The text below the boxes: the error while there is one, otherwise the helper text.</summary>
	protected string? MessageText => HasError
		? !string.IsNullOrEmpty(ErrorText) ? ErrorText : ValidationErrorText
		: HelperText;

	/// <summary>Whether the message element is rendered.</summary>
	protected bool HasMessage => !string.IsNullOrEmpty(MessageText);

	/// <summary>Id of the message element.</summary>
	protected string MessageId => $"{InputId}-message";

	/// <summary>The field's <c>aria-describedby</c>: what the app passed, then the message while it is rendered.</summary>
	protected string? DescribedBy => MokaAttributes.DescribedBy(AdditionalAttributes, HasMessage ? MessageId : null);

	/// <summary>The app's attributes for the field, without the <c>aria-describedby</c> that <see cref="DescribedBy" /> carries.</summary>
	protected IReadOnlyDictionary<string, object>? InputAttributes => MokaAttributes.WithoutDescribedBy(AdditionalAttributes);

	/// <summary>
	///     The field's <c>aria-label</c>: <see cref="DefaultName" /> when nothing else names it, otherwise left out so
	///     the label or the app's own <c>aria-label</c> or <c>aria-labelledby</c> does.
	/// </summary>
	protected string? AccessibleName => HasLabel || NamedByApp ? null : DefaultName;

	/// <summary><c>aria-invalid</c> for the field, as the string ARIA expects.</summary>
	protected string AriaInvalid => HasError ? "true" : "false";

	/// <summary><c>aria-required</c> for the field: <c>"true"</c> when required, otherwise left out.</summary>
	protected string? AriaRequired => Required ? "true" : null;

	/// <summary>The field's type: a password field while <see cref="Masked" />.</summary>
	protected string FieldType => Masked ? "password" : "text";

	/// <summary>What the field shows: the value's digits, up to <see cref="Length" />.</summary>
	protected string CodeText => Digits(CurrentValueAsString, BoxCount);

	/// <summary>The style of the row of boxes, which carries the number of boxes for the field's letter spacing.</summary>
	protected string BoxesStyle => FormattableString.Invariant($"--_code-length: {BoxCount}");

	/// <summary>The inline style for every box: the radius from <c>Rounded</c> or <c>RoundedValue</c>.</summary>
	protected string? BoxStyle => new StyleBuilder()
		.AddStyle("border-radius", ResolvedRounding)
		.Build();

	private bool NamedByApp => AdditionalAttributes is { } attributes && attributes.Keys.Any(key =>
		string.Equals(key, "aria-label", StringComparison.OrdinalIgnoreCase)
		|| string.Equals(key, "aria-labelledby", StringComparison.OrdinalIgnoreCase));

	/// <summary>
	///     Takes text from the field, bound with <c>@bind:get="CodeText"</c> and <c>@bind:set</c> on the input event, and
	///     raises <see cref="OnComplete" /> when the change fills every box.
	/// </summary>
	/// <param name="text">The field's text.</param>
	protected async Task HandleTypedAsync(string? text)
	{
		string before = CurrentValue ?? "";
		CurrentValueAsString = text;
		string code = CurrentValue ?? "";
		if (OnComplete.HasDelegate && code.Length == BoxCount && !string.Equals(code, before, StringComparison.Ordinal))
		{
			await OnComplete.InvokeAsync(code);
		}
	}

	/// <inheritdoc />
	protected override bool TryParseValueFromString(string? value, out string? result,
		[NotNullWhen(false)] out string? validationErrorMessage)
	{
		result = Digits(value, BoxCount);
		validationErrorMessage = null;
		return true;
	}

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			_bound = true;
			await SafeModuleInvokeVoidAsync(CodeModule, "bindCode", Field);
		}

		if (ReadsSmsCode != _readingSms)
		{
			_readingSms = ReadsSmsCode;
			await SafeModuleInvokeVoidAsync(CodeModule, "readSmsCode", Field, _readingSms);
		}
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		if (_bound)
		{
			await SafeModuleInvokeVoidAsync(CodeModule, "unbindCode", Field);
		}

		await base.DisposeAsyncCore();
	}

	// Full width digits, which a Japanese or Chinese keyboard types, count as the digits they show. Mapped here, since
	// compatibility normalization isn't available to .NET in the browser.
	internal static string Digits(string? text, int length)
	{
		if (string.IsNullOrEmpty(text))
		{
			return "";
		}

		if (text.Length <= length && text.All(char.IsAsciiDigit))
		{
			return text;
		}

		StringBuilder digits = new(length);
		foreach (char c in text)
		{
			if (DigitOf(c) is not { } digit)
			{
				continue;
			}

			digits.Append(digit);
			if (digits.Length == length)
			{
				break;
			}
		}

		return digits.ToString();
	}

	private static char? DigitOf(char c) => c switch
	{
		>= '0' and <= '9' => c,
		>= FullWidthZero and <= FullWidthNine => (char)('0' + (c - FullWidthZero)),
		_ => null
	};

	// A cleared field gives "" and a parent may pass null. Both mean no code.
	private sealed class EmptyMeansEmpty : IEqualityComparer<string?>
	{
		public static readonly EmptyMeansEmpty Instance = new();

		public bool Equals(string? x, string? y) => string.Equals(x ?? "", y ?? "", StringComparison.Ordinal);

		public int GetHashCode(string? obj) => (obj ?? "").GetHashCode(StringComparison.Ordinal);
	}
}
