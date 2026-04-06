---
title: Password Strength
description: Visual strength indicator bar with optional requirement checklist.
order: 51
---

# Password Strength

`MokaPasswordStrength` displays a strength meter bar that evaluates password quality in real time. It can optionally show a label (e.g. "Weak", "Strong") and a checklist of requirements.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Password` | `string` | `""` | The password string to evaluate |
| `ShowLabel` | `bool` | `true` | Show strength label (Weak / Fair / Good / Strong) |
| `ShowRequirements` | `bool` | `false` | Show a checklist of requirement statuses |
| `MinLength` | `int` | `8` | Minimum length requirement |
| `RequireUppercase` | `bool` | `true` | Require at least one uppercase letter |
| `RequireLowercase` | `bool` | `true` | Require at least one lowercase letter |
| `RequireDigit` | `bool` | `true` | Require at least one digit |
| `RequireSpecial` | `bool` | `false` | Require at least one special character |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

## Basic Strength Meter

```blazor-preview
<MokaTextField @bind-Value="_pw1" Label="Password" Placeholder="Type a password..." />
<MokaPasswordStrength Password="@_pw1" />

@code {
    private string _pw1 = "";
}
```

## With Requirements Checklist

```blazor-preview
<MokaTextField @bind-Value="_pw2" Label="Password" Placeholder="Enter password..." />
<MokaPasswordStrength Password="@_pw2" ShowRequirements="true" RequireSpecial="true" />

@code {
    private string _pw2 = "";
}
```

## Custom Minimum Length

```blazor-preview
<MokaTextField @bind-Value="_pw3" Label="Password" Placeholder="At least 12 characters..." />
<MokaPasswordStrength Password="@_pw3" MinLength="12" ShowRequirements="true" />

@code {
    private string _pw3 = "";
}
```
