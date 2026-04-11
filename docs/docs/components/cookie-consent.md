---
title: Cookie Consent
description: GDPR-compliant cookie consent banner with accept, reject, and customize actions.
order: 61
---

# Cookie Consent

`MokaCookieConsent` renders a dismissible banner for obtaining user consent for cookies and tracking. It supports configurable button labels, positioning, and callbacks for accept, reject, and customize actions.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Message` | `string` | `"We use cookies to improve your experience."` | The consent message displayed to the user |
| `AcceptText` | `string` | `"Accept"` | Label for the accept button |
| `RejectText` | `string?` | -- | Label for the reject button (hidden if `null`) |
| `CustomizeText` | `string?` | -- | Label for the customize/preferences button (hidden if `null`) |
| `Position` | `MokaCookieConsentPosition` | `Bottom` | Where the banner appears: `Top` or `Bottom` |
| `OnAccept` | `EventCallback` | -- | Callback when the user accepts |
| `OnReject` | `EventCallback` | -- | Callback when the user rejects |
| `OnCustomize` | `EventCallback` | -- | Callback when the user clicks customize |
| `Visible` | `bool` | `true` | Whether the banner is visible. Supports two-way binding via `@bind-Visible`. |
| `VisibleChanged` | `EventCallback<bool>` | -- | Callback when visibility changes |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

## Basic Bottom Banner

```blazor-preview
<MokaCookieConsent OnAccept="() => { }" />
```

## Top Position

```blazor-preview
<MokaCookieConsent Position="MokaCookieConsentPosition.Top"
                   Message="This site uses cookies for analytics and personalization."
                   OnAccept="() => { }" />
```

## With Customize Button

Add a customize option to let users manage their cookie preferences.

```blazor-preview
<MokaCookieConsent Message="We use cookies for analytics and ads. Manage your preferences below."
                   AcceptText="Accept All"
                   RejectText="Reject All"
                   CustomizeText="Cookie Settings"
                   OnAccept="() => { }"
                   OnReject="() => { }"
                   OnCustomize="() => { }" />
```

## Controlled Visibility

Bind the `Visible` property to control when the banner appears.

```blazor-preview
<MokaButton OnClick="() => showConsent = true" Variant="MokaVariant.Outlined">Show Banner</MokaButton>

<MokaCookieConsent @bind-Visible="showConsent"
                   Message="Your privacy matters. Choose your cookie preferences."
                   AcceptText="Got it"
                   OnAccept="() => { }" />

@code {
    bool showConsent;
}
```
