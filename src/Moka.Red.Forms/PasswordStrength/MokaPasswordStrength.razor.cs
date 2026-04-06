using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Forms.PasswordStrength;

/// <summary>
///     A password strength indicator that displays a colored bar and optional label
///     based on password content analysis. Evaluates length, character variety,
///     and configurable requirements to produce a 0–4 strength score.
/// </summary>
public partial class MokaPasswordStrength : MokaComponentBase
{
	/// <summary>The password string to evaluate.</summary>
	[Parameter]
	public string? Password { get; set; }

	/// <summary>Whether to show the strength label text (e.g., "Weak", "Strong"). Defaults to true.</summary>
	[Parameter]
	public bool ShowLabel { get; set; } = true;

	/// <summary>Whether to show a checklist of individual requirements. Defaults to false.</summary>
	[Parameter]
	public bool ShowRequirements { get; set; }

	/// <summary>Minimum required password length. Defaults to 8.</summary>
	[Parameter]
	public int MinLength { get; set; } = 8;

	/// <summary>Whether an uppercase letter is required. Defaults to true.</summary>
	[Parameter]
	public bool RequireUppercase { get; set; } = true;

	/// <summary>Whether a lowercase letter is required. Defaults to true.</summary>
	[Parameter]
	public bool RequireLowercase { get; set; } = true;

	/// <summary>Whether a digit is required. Defaults to true.</summary>
	[Parameter]
	public bool RequireDigit { get; set; } = true;

	/// <summary>Whether a special character is required. Defaults to false.</summary>
	[Parameter]
	public bool RequireSpecial { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-password-strength";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-password-strength--{StrengthClass}")
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle(Style)
		.Build();

	private int Strength => CalculateStrength();

	private string StrengthClass => Strength switch
	{
		0 => "none",
		1 => "weak",
		2 => "fair",
		3 => "good",
		4 => "strong",
		_ => "none"
	};

	private string StrengthLabel => Strength switch
	{
		0 => "",
		1 => "Weak",
		2 => "Fair",
		3 => "Good",
		4 => "Strong",
		_ => ""
	};

	private bool HasMinLength => Password is not null && Password.Length >= MinLength;
	private bool HasUppercase => Password is not null && Password.Any(char.IsUpper);
	private bool HasLowercase => Password is not null && Password.Any(char.IsLower);
	private bool HasDigit => Password is not null && Password.Any(char.IsDigit);
	private bool HasSpecial => Password is not null && Password.Any(c => !char.IsLetterOrDigit(c));

	private int CalculateStrength()
	{
		if (string.IsNullOrEmpty(Password))
		{
			return 0;
		}

		int score = 0;
		int totalChecks = 0;
		int passedChecks = 0;

		// Length is always checked
		totalChecks++;
		if (HasMinLength)
		{
			passedChecks++;
		}

		if (RequireUppercase)
		{
			totalChecks++;
			if (HasUppercase)
			{
				passedChecks++;
			}
		}

		if (RequireLowercase)
		{
			totalChecks++;
			if (HasLowercase)
			{
				passedChecks++;
			}
		}

		if (RequireDigit)
		{
			totalChecks++;
			if (HasDigit)
			{
				passedChecks++;
			}
		}

		if (RequireSpecial)
		{
			totalChecks++;
			if (HasSpecial)
			{
				passedChecks++;
			}
		}

		double ratio = totalChecks > 0 ? (double)passedChecks / totalChecks : 1.0;

		if (string.IsNullOrEmpty(Password))
		{
			return 0;
		}

		if (ratio <= 0.25)
		{
			score = 1;
		}
		else if (ratio <= 0.5)
		{
			score = 2;
		}
		else if (ratio < 1.0)
		{
			score = 3;
		}
		else
		{
			score = 4;
		}

		return Math.Max(score, 1);
	}
}
