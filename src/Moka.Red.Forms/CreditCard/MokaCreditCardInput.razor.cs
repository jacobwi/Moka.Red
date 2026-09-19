using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Forms.CreditCard;

/// <summary>
///     A formatted credit card input with card type detection, expiry, CVV, and visual card preview.
/// </summary>
public partial class MokaCreditCardInput : MokaVisualComponentBase
{
	private readonly string _labelId = $"moka-creditcard-{Guid.NewGuid():N}-label";
	private string _cardholderName = "";
	private string _cardNumber = "";
	private string _cardType = "unknown";
	private string _cvv = "";

	// What a box renders for one pass after its handler dropped characters (see RenderRawOnceAsync).
	// Null the rest of the time.
	private string? _cvvEcho;
	private string _expiryDate = "";
	private string? _expiryEcho;
	private string? _lastCardholderName;
	private string? _lastCardNumber;
	private string? _lastCvv;
	private string? _lastExpiryDate;
	private string? _numberEcho;

	/// <summary>Card number, auto-formatted with spaces (e.g., 4242 4242 4242 4242).</summary>
	[Parameter]
	public string CardNumber { get; set; } = "";

	/// <summary>Callback when the card number changes.</summary>
	[Parameter]
	public EventCallback<string> CardNumberChanged { get; set; }

	/// <summary>Expiry date in MM/YY format.</summary>
	[Parameter]
	public string ExpiryDate { get; set; } = "";

	/// <summary>Callback when the expiry date changes.</summary>
	[Parameter]
	public EventCallback<string> ExpiryDateChanged { get; set; }

	/// <summary>Card verification value (3-4 digits).</summary>
	[Parameter]
	public string Cvv { get; set; } = "";

	/// <summary>Callback when the CVV changes.</summary>
	[Parameter]
	public EventCallback<string> CvvChanged { get; set; }

	/// <summary>Name of the cardholder.</summary>
	[Parameter]
	public string CardholderName { get; set; } = "";

	/// <summary>Callback when the cardholder name changes.</summary>
	[Parameter]
	public EventCallback<string> CardholderNameChanged { get; set; }

	/// <summary>
	///     Label text displayed above the component. It names the group of fields; each field has its
	///     own name ("Card number", "Expiry date (MM/YY)", "Security code (CVV)", "Cardholder name").
	/// </summary>
	[Parameter]
	public string? Label { get; set; } = "Card Details";

	/// <summary>Whether to show a visual card representation.</summary>
	[Parameter]
	public bool ShowCardPreview { get; set; } = true;

	/// <summary>
	///     Visual style of the card preview. Default is Auto (changes gradient based on detected card type).
	///     Set to a specific variant for a fixed style, or Custom to use <see cref="CustomCardBackground" />.
	/// </summary>
	[Parameter]
	public MokaCardStyle CardStyle { get; set; } = MokaCardStyle.Auto;

	/// <summary>
	///     Custom CSS background value for the card preview (e.g., "linear-gradient(135deg, #667eea, #764ba2)" or "#1a1a2e" or
	///     "url(...)").
	///     Only used when <see cref="CardStyle" /> is set to <see cref="MokaCardStyle.Custom" />.
	/// </summary>
	[Parameter]
	public string? CustomCardBackground { get; set; }

	/// <summary>
	///     Custom text color for the card preview. Default is white.
	///     Only used when <see cref="CardStyle" /> is set to <see cref="MokaCardStyle.Custom" />.
	/// </summary>
	[Parameter]
	public string? CustomCardTextColor { get; set; }

	/// <summary>Fires with the detected card type: "visa", "mastercard", "amex", "discover", or "unknown".</summary>
	[Parameter]
	public EventCallback<string> OnCardTypeDetected { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-creditcard";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass(Class)
		.Build();

	// The group is the outermost element, so it takes the margin and the padding. Each box draws its
	// own border, so each takes the radius.

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin)
		.AddStyle("padding", ResolvedPadding)
		.AddStyle(Style)
		.Build();

	private string? InputStyle => new StyleBuilder()
		.AddStyle("border-radius", ResolvedRounding)
		.Build();

	private string CardPreviewClass => new CssBuilder("moka-creditcard-preview")
		.AddClass($"moka-creditcard-preview--{ResolvedCardStyle}")
		.Build();

	private string? CardPreviewStyle => CardStyle == MokaCardStyle.Custom
		? new StyleBuilder()
			.AddStyle("background", CustomCardBackground)
			.AddStyle("color", CustomCardTextColor ?? "#ffffff")
			.Build()
		: null;

	private string ResolvedCardStyle => CardStyle switch
	{
		MokaCardStyle.Auto => _cardType,
		MokaCardStyle.Dark => "dark",
		MokaCardStyle.Light => "light",
		MokaCardStyle.Neon => "neon",
		MokaCardStyle.Gold => "gold",
		MokaCardStyle.Platinum => "platinum",
		MokaCardStyle.Rose => "rose",
		MokaCardStyle.Ocean => "ocean",
		MokaCardStyle.Sunset => "sunset",
		MokaCardStyle.Minimal => "minimal",
		MokaCardStyle.Custom => "custom",
		_ => _cardType
	};

	private string MaskedNumber
	{
		get
		{
			string digits = StripNonDigits(_cardNumber);
			if (digits.Length == 0)
			{
				return "**** **** **** ****";
			}

			// Pad remaining with asterisks
			string padded = digits.PadRight(MaxDigits(_cardType), '*');
			return FormatCardNumber(padded, _cardType);
		}
	}

	private string NumberInputValue => _numberEcho ?? _cardNumber;

	private string ExpiryInputValue => _expiryEcho ?? _expiryDate;

	private string CvvInputValue => _cvvEcho ?? _cvv;

	private string? GroupLabelledBy => Label is null ? null : _labelId;

	private string CardTypeDisplay => _cardType switch
	{
		"visa" => "VISA",
		"mastercard" => "MC",
		"amex" => "AMEX",
		"discover" => "DISC",
		_ => ""
	};

	private int CvvMaxLength => _cardType == "amex" ? 4 : 3;

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		// Each field only takes the parent's value when the parent passes a new one. Copying them on
		// every parent render wiped what the user had typed into unbound fields.
		if (!string.Equals(_lastCardNumber, CardNumber, StringComparison.Ordinal))
		{
			_lastCardNumber = CardNumber;
			_cardNumber = CardNumber ?? "";

			// The type only came from typing before, so a number set by the parent showed no brand.
			_cardType = DetectCardType(StripNonDigits(_cardNumber));
		}

		if (!string.Equals(_lastExpiryDate, ExpiryDate, StringComparison.Ordinal))
		{
			_lastExpiryDate = ExpiryDate;
			_expiryDate = ExpiryDate ?? "";
		}

		if (!string.Equals(_lastCvv, Cvv, StringComparison.Ordinal))
		{
			_lastCvv = Cvv;
			_cvv = Cvv ?? "";
		}

		if (!string.Equals(_lastCardholderName, CardholderName, StringComparison.Ordinal))
		{
			_lastCardholderName = CardholderName;
			_cardholderName = CardholderName ?? "";
		}
	}

	private static string StripNonDigits(string? input)
	{
		if (string.IsNullOrEmpty(input))
		{
			return "";
		}

		char[] chars = new char[input.Length];
		int index = 0;
		foreach (char c in input)
		{
			if (char.IsDigit(c))
			{
				chars[index++] = c;
			}
		}

		return new string(chars, 0, index);
	}

	private static int MaxDigits(string cardType) => cardType == "amex" ? 15 : 16;

	private static string FormatCardNumber(string digits, string cardType)
	{
		if (cardType == "amex")
		{
			// Amex: 4-6-5 pattern
			var parts = new List<string>();
			if (digits.Length > 0)
			{
				parts.Add(digits[..Math.Min(4, digits.Length)]);
			}

			if (digits.Length > 4)
			{
				parts.Add(digits[4..Math.Min(10, digits.Length)]);
			}

			if (digits.Length > 10)
			{
				parts.Add(digits[10..Math.Min(15, digits.Length)]);
			}

			return string.Join(" ", parts);
		}

		// Default: groups of 4
		var result = new List<string>();
		for (int i = 0; i < digits.Length; i += 4)
		{
			result.Add(digits[i..Math.Min(i + 4, digits.Length)]);
		}

		return string.Join(" ", result);
	}

	private static string DetectCardType(string digits)
	{
		if (digits.Length == 0)
		{
			return "unknown";
		}

		// Visa: starts with 4
		if (digits[0] == '4')
		{
			return "visa";
		}

		// Amex: starts with 34 or 37
		if (digits.Length >= 2)
		{
			string first2 = digits[..2];
			if (first2 == "34" || first2 == "37")
			{
				return "amex";
			}
		}

		// Mastercard: starts with 51-55 or 2221-2720
		if (digits.Length >= 2)
		{
			if (int.TryParse(digits[..2], out int mc2) && mc2 >= 51 && mc2 <= 55)
			{
				return "mastercard";
			}
		}

		if (digits.Length >= 4)
		{
			if (int.TryParse(digits[..4], out int mc4) && mc4 >= 2221 && mc4 <= 2720)
			{
				return "mastercard";
			}
		}

		// Discover: starts with 6011, 644-649, or 65
		if (digits.Length >= 4)
		{
			if (digits[..4] == "6011")
			{
				return "discover";
			}
		}

		if (digits.Length >= 3)
		{
			if (int.TryParse(digits[..3], out int d3) && d3 >= 644 && d3 <= 649)
			{
				return "discover";
			}
		}

		if (digits.Length >= 2)
		{
			if (digits[..2] == "65")
			{
				return "discover";
			}
		}

		return "unknown";
	}

	private async Task HandleCardNumberInput(ChangeEventArgs e)
	{
		string raw = e.Value?.ToString() ?? "";
		_numberEcho = null;
		string digits = StripNonDigits(raw);

		// The brand comes from the first digits, so it is known before the length is capped. Capping
		// with the brand of the number being replaced cut a pasted Visa to 15 digits after an Amex.
		string cardType = DetectCardType(digits);
		if (digits.Length > MaxDigits(cardType))
		{
			digits = digits[..MaxDigits(cardType)];
		}

		string formatted = FormatCardNumber(digits, cardType);
		if (formatted != raw && formatted == _cardNumber)
		{
			_numberEcho = raw;
			if (!await RenderRawOnceAsync(() => _numberEcho == raw))
			{
				return;
			}

			_numberEcho = null;
		}

		bool typeChanged = cardType != _cardType;
		_cardType = cardType;
		_cardNumber = formatted;

		if (typeChanged)
		{
			if (OnCardTypeDetected.HasDelegate)
			{
				await OnCardTypeDetected.InvokeAsync(cardType);
			}

			// An Amex code has four digits, every other brand three.
			if (_cvv.Length > CvvMaxLength)
			{
				_cvv = _cvv[..CvvMaxLength];
				if (CvvChanged.HasDelegate)
				{
					await CvvChanged.InvokeAsync(_cvv);
				}
			}
		}

		if (CardNumberChanged.HasDelegate)
		{
			await CardNumberChanged.InvokeAsync(formatted);
		}
	}

	private async Task HandleExpiryInput(ChangeEventArgs e)
	{
		string raw = e.Value?.ToString() ?? "";
		_expiryEcho = null;
		string digits = StripNonDigits(raw);
		if (digits.Length > 4)
		{
			digits = digits[..4];
		}

		string formatted = digits.Length > 2
			? $"{digits[..2]}/{digits[2..]}"
			: digits;

		// A slash typed after the month stays. Dropping it would make typing "12/34" by hand lose the
		// slash until the year starts.
		if (digits.Length == 2 && raw.Contains('/', StringComparison.Ordinal))
		{
			formatted += "/";
		}

		if (formatted != raw && formatted == _expiryDate)
		{
			_expiryEcho = raw;
			if (!await RenderRawOnceAsync(() => _expiryEcho == raw))
			{
				return;
			}

			_expiryEcho = null;
		}

		_expiryDate = formatted;
		if (ExpiryDateChanged.HasDelegate)
		{
			await ExpiryDateChanged.InvokeAsync(formatted);
		}
	}

	private async Task HandleCvvInput(ChangeEventArgs e)
	{
		string raw = e.Value?.ToString() ?? "";
		_cvvEcho = null;
		string digits = StripNonDigits(raw);
		if (digits.Length > CvvMaxLength)
		{
			digits = digits[..CvvMaxLength];
		}

		if (digits != raw && digits == _cvv)
		{
			_cvvEcho = raw;
			if (!await RenderRawOnceAsync(() => _cvvEcho == raw))
			{
				return;
			}

			_cvvEcho = null;
		}

		_cvv = digits;
		if (CvvChanged.HasDelegate)
		{
			await CvvChanged.InvokeAsync(digits);
		}
	}

	// When the kept text equals what the box last rendered, Blazor sends no change and the browser
	// keeps showing the characters the handler dropped, such as a letter in the card number
	// (gotcha #24). One render with the raw text gives the next render a value to replace. False
	// when a later input to the same box got there first.
	private async Task<bool> RenderRawOnceAsync(Func<bool> stillLatest)
	{
		StateHasChanged();
		await Task.Yield();
		return stillLatest();
	}

	private async Task HandleNameInput(ChangeEventArgs e)
	{
		string value = e.Value?.ToString() ?? "";
		_cardholderName = value;
		if (CardholderNameChanged.HasDelegate)
		{
			await CardholderNameChanged.InvokeAsync(value);
		}
	}
}
