---
title: Credit Card Input
description: Card number, expiry, CVV and name fields with type detection and a live card preview.
order: 117
---

# Credit Card Input

`MokaCreditCardInput` collects a payment card in four fields: number, expiry, CVV and cardholder name. It formats the number as the user types, detects the card type from the first digits, and draws a card that updates with every keystroke. The fields carry the browser's `cc-*` autocomplete tokens, so saved cards can fill them.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `CardNumber` | `string` | `""` | Card number, formatted with spaces as the user types. Two-way bindable |
| `CardNumberChanged` | `EventCallback<string>` | -- | Fires on each change with the formatted number, such as `"4242 4242 4242 4242"` |
| `ExpiryDate` | `string` | `""` | Expiry as `MM/YY`. The slash is added after the month. Two-way bindable |
| `ExpiryDateChanged` | `EventCallback<string>` | -- | Fires on each change of the expiry |
| `Cvv` | `string` | `""` | Security code: 3 digits, or 4 for American Express. Two-way bindable |
| `CvvChanged` | `EventCallback<string>` | -- | Fires on each change of the CVV |
| `CardholderName` | `string` | `""` | Name on the card. Two-way bindable |
| `CardholderNameChanged` | `EventCallback<string>` | -- | Fires on each change of the name |
| `Label` | `string?` | `"Card Details"` | Text above the component. `null` hides it |
| `ShowCardPreview` | `bool` | `true` | Shows the card picture above the fields |
| `CardStyle` | `MokaCardStyle` | `Auto` | Look of the card picture. `Auto` follows the detected card type |
| `CustomCardBackground` | `string?` | -- | Any CSS `background` value for the card picture. Used only with `CardStyle="MokaCardStyle.Custom"` |
| `CustomCardTextColor` | `string?` | -- | Text color of the card picture with `Custom`. Unset uses white |
| `OnCardTypeDetected` | `EventCallback<string>` | -- | Fires when typing changes the detected type, with `"visa"`, `"mastercard"`, `"amex"`, `"discover"` or `"unknown"` |
| `Disabled` | `bool` | `false` | Disables all four fields |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

`MokaCardStyle` has `Auto`, `Dark`, `Light`, `Neon`, `Gold`, `Platinum`, `Rose`, `Ocean`, `Sunset`, `Minimal` and `Custom`.

### Card Types

| Type | Number starts with | Number format | CVV | Auto style |
|------|--------------------|---------------|-----|------------|
| `visa` | 4 | Groups of 4, 16 digits | 3 digits | Blue |
| `mastercard` | 51 to 55, or 2221 to 2720 | Groups of 4, 16 digits | 3 digits | Orange |
| `amex` | 34 or 37 | 4-6-5, 15 digits | 4 digits | Green |
| `discover` | 6011, 644 to 649, or 65 | Groups of 4, 16 digits | 3 digits | Amber |
| `unknown` | Anything else | Groups of 4, 16 digits | 3 digits | Indigo |

## Basic

```blazor-preview
<div style="width:100%;max-width:360px">
    <MokaCreditCardInput @bind-CardNumber="_number"
                         @bind-ExpiryDate="_expiry"
                         @bind-Cvv="_cvv"
                         @bind-CardholderName="_name" />
</div>

@code {
    private string _number = "";
    private string _expiry = "";
    private string _cvv = "";
    private string _name = "";
}
```

## Card Type Detection

Type a test number: `4242 4242 4242 4242` (Visa), `5555 5555 5555 4444` (Mastercard), `3782 822463 10005` (American Express) or `6011 1111 1111 1117` (Discover). The card color, the badge in the number field and the CVV length follow the type.

```blazor-preview
<div style="width:100%;max-width:360px;display:flex;flex-direction:column;gap:var(--moka-spacing-sm)">
    <MokaCreditCardInput @bind-CardNumber="_number" OnCardTypeDetected="@(type => _type = type)" />
    <div style="display:flex;align-items:center;gap:var(--moka-spacing-xs);font-size:var(--moka-font-size-sm)">
        Detected type: <MokaTag Text="@_type" />
    </div>
</div>

@code {
    private string _number = "";
    private string _type = "unknown";
}
```

## Card Styles

A fixed `CardStyle` keeps one look whatever the card type.

```blazor-preview
<div style="width:100%;max-width:360px;display:flex;flex-direction:column;gap:var(--moka-spacing-md)">
    <MokaSegmentedControl @bind-Value="_style" FullWidth>
        <MokaSegment Value="Dark" Text="Dark" />
        <MokaSegment Value="Neon" Text="Neon" />
        <MokaSegment Value="Gold" Text="Gold" />
        <MokaSegment Value="Ocean" Text="Ocean" />
        <MokaSegment Value="Minimal" Text="Minimal" />
    </MokaSegmentedControl>
    <MokaCreditCardInput CardStyle="@CardLook" Label="@null"
                         CardNumber="4242 4242 4242 4242"
                         ExpiryDate="09/29"
                         CardholderName="Ada Lovelace" />
</div>

@code {
    private string? _style = "Dark";

    private MokaCardStyle CardLook => Enum.Parse<MokaCardStyle>(_style ?? "Dark");
}
```

## Custom Style

`Custom` takes any CSS background, including gradients and images.

```blazor-preview
<div style="width:100%;max-width:360px">
    <MokaCreditCardInput CardStyle="MokaCardStyle.Custom"
                         CustomCardBackground="linear-gradient(135deg, #0c0c10 0%, #3a1214 100%)"
                         CustomCardTextColor="#ff6b68"
                         Label="Company card" />
</div>
```

## Fields Only

```blazor-preview
<div style="width:100%;max-width:360px">
    <MokaCreditCardInput ShowCardPreview="false" Label="Payment card" />
</div>
```

## Behaviour

- The number, expiry and CVV fields keep digits only. The number stops at 16 digits (15 for American Express), the CVV at 3 (4 for American Express) and the expiry at 4.
- `CardNumberChanged` reports the number with its spaces. Remove them before you pass the number to a payment API.
- The card picture shows the digits typed so far with `*` for the rest, the name in capitals and the expiry. It never shows the CVV.
- There is no validation: no Luhn checksum, no check that the expiry is in the future, and no `EditForm` integration. Validate on submit, or let your payment provider do it.
- A field keeps what the user typed when the parent re-renders, and takes a value from the parent only when the parent passes a different one. A number from the parent also sets the card type, without raising `OnCardTypeDetected`.
- The values are plain strings in your component. With Blazor Server, every keystroke reaches your server over the circuit.
- `Id`, `Class`, `Style` and unmatched attributes go on the outer element. `Margin` and `Padding` go there too, around the label, the card and the fields, and `Rounded` goes on each of the four fields. Up to 0.1.12 the component ignored all three.

Up to 0.1.12 a parent render put the parent's value back over what the user had typed into an unbound field, and a number passed in by the parent showed no card type until the user typed.

## Accessibility

- The four fields sit in a `role="group"` that `Label` names ("Card Details" by default), so a screen reader announces the group as focus enters it. With `Label="@null"`, pass `aria-label` to name the group.
- Each field has a name of its own: "Card number", "Expiry date (MM/YY)", "Security code (CVV)" and "Cardholder name". The placeholders are hints only.
- The number, expiry and CVV fields use `inputmode="numeric"`, so phones show a number pad.
- The `cc-number`, `cc-exp`, `cc-csc` and `cc-name` autocomplete tokens let the browser offer saved cards.
