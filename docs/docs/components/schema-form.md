---
title: Schema Form
description: A settings form drawn from a JSON Schema, for settings an app only learns about at run time, with checks, defaults, secrets and a save flow.
order: 144
---

# Schema Form

`MokaSchemaForm` draws a [settings form](/components/settings-form) from a JSON Schema. It's for settings an app doesn't know when it's built: a plugin's options, a connector's sign-in, a backup job's target. Each property becomes a field, checked once it's left and again before Save. Defaults fill in what isn't set, settings marked advanced wait under "Show advanced", and write-only properties are secrets the form never holds.

`MokaFormSchema.Parse` reads the schema. It reads a subset of JSON Schema draft 2020-12, refuses what it can't honour with a clear problem, and never throws on bad input.

## Setup

```razor
@using Moka.Red.Forms.SchemaForm
```

## A form

```razor
<MokaSchemaForm Schema="_schema" @bind-Value="_settings" SetSecrets="_setSecrets"
                Problems="_problems" Refusal="@_refusal" OnSave="SaveAsync"
                Icon="MokaIcons.Content.Server"/>

@code {
    private const string MailServer = """
        {
            "type": "object",
            "title": "Mail server",
            "description": "Where reports are sent from.",
            "required": ["host", "from"],
            "properties": {
                "host": { "type": "string", "title": "Host", "format": "hostname", "examples": ["mail.example.com"] },
                "port": { "type": "integer", "title": "Port", "minimum": 1, "maximum": 65535, "default": 587 },
                "from": { "type": "string", "title": "From address", "format": "email" },
                "security": { "type": "string", "title": "Security", "default": "starttls",
                              "oneOf": [ { "const": "none", "title": "None" },
                                         { "const": "starttls", "title": "STARTTLS" },
                                         { "const": "tls", "title": "TLS from the start" } ] },
                "password": { "type": "string", "title": "Password", "writeOnly": true },
                "timeout": { "type": "number", "title": "Timeout in seconds", "exclusiveMinimum": 0, "x-advanced": true }
            }
        }
        """;

    private readonly MokaFormSchema _schema = MokaFormSchema.Parse(MailServer).Schema!;
    private JsonObject _settings = new() { ["host"] = "mail.example.com" };
    private HashSet<string> _setSecrets = ["password"];
    private IReadOnlyDictionary<string, string>? _problems;
    private string? _refusal;

    private async Task SaveAsync(MokaSchemaFormSave save)
    {
        SaveReply reply = await Api.SaveMailServerAsync(save.Values, save.Secrets);
        _problems = reply.Problems;
        _refusal = reply.Message;
    }
}
```

Read the schema once and keep it. A schema read again from the same JSON counts as the same form, so what's typed stays.

## Parameters

### MokaSchemaForm

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Schema` | `MokaFormSchema?` | **required** | The form, from `MokaFormSchema.Parse`. Without one the form says there's nothing to set |
| `Value` | `JsonObject?` | - | The settings. A setting it leaves out takes its `default`. Write-only properties in it are never shown and count as set. Properties the schema doesn't name are kept as they are |
| `ValueChanged` | `EventCallback<JsonObject>` | - | Raised with a new copy each time a setting is edited, defaults filled in and write-only settings left out. A parent that passes `Value` and listens owns it, as with the library's inputs: what it passes back shows, so a refused change is undone |
| `SetSecrets` | `IReadOnlyCollection<string>?` | - | The write-only settings that hold a value where they're kept |
| `OnSave` | `EventCallback<MokaSchemaFormSave>` | - | Raised by Save once every field fits. The form shows Saving until the handler has run. The save counts as done when the handler leaves no `Refusal` and no `Problems`. Without a listener, Save takes the edits as saved |
| `Problems` | `IReadOnlyDictionary<string, string>?` | - | Problems from outside, by property name. Each shows at its field until the field is edited; a name the schema doesn't have shows under the fields |
| `Refusal` | `string?` | - | Why the last save was refused, shown under the fields |
| `Title` | `string?` | - | The form's title. Left out, the schema's `title`, else "Settings" |
| `Lead` | `string?` | - | What the settings are for. Left out, the schema's `description` |
| `Icon` | `MokaIconDefinition?` | - | An icon in an accent tile before the title |
| `Status` | `RenderFragment?` | - | Beside the title: what just happened |
| `HeaderActions` | `RenderFragment?` | - | Controls at the end of the header, such as a button that tests a connection |
| `Appearance` | `MokaSettingsAppearance` | `Plain` | `Cards` draws the header and each group as a card |
| `Columns` | `int` | `2` | The most columns of fields a group has. A column is never narrower than 16rem of the group's width |
| `Text` | `MokaSchemaFormText?` | - | Every word the form writes, and the problems it reports |
| `Disabled` | `bool` | `false` | Shows the settings and lets nothing change: no Save, no Change or Clear |
| `Id` | `string?` | - | The form's id. Each field's input gets `{Id}-{n}`, `n` its place in the schema |
| `Class` | `string?` | - | Additional CSS classes on the form |
| `Style` | `string?` | - | Additional inline styles on the form |

`Margin` and `Rounded` land on the form, and `Padding` on its body, as on `MokaSettingsForm`.

### MokaFormSchema

| Member | Description |
|--------|-------------|
| `Parse(string json)` | Reads a schema from JSON. Gives a `MokaSchemaParseResult`: the `Schema`, or every `Problems` sentence that kept it from being one. Never throws on bad input |
| `Parse(JsonObject schema)` | The same for a schema already parsed, such as one .NET's `JsonSchemaExporter` made from a type (`options.GetJsonSchemaAsNode(typeof(T)).AsObject()`). The object isn't changed or kept |
| `Title`, `Description` | The schema's `title` and `description` |
| `Fields` | The settings, in the order `properties` lists them, each a `MokaSchemaField` |
| `Field(name)` | The setting with that property name, or null |
| `AllowsOtherProperties` | False when the schema says `"additionalProperties": false` |
| `WithDefaults(values)` | A copy where each setting that isn't set takes its `default` |
| `Check(values, setSecrets, text)` | What's wrong, by property name, with the form's rules and words, for checking what a server is sent |

`MokaSchemaField` carries what the property said: `Name`, `Title`, `Description`, `Kind` (`Text`, `WholeNumber`, `Number`, `Toggle`, `Choice`), `Required`, `ReadOnly`, `WriteOnly`, `Advanced`, `Multiline`, `Default`, `Placeholder`, `Format`, `Pattern`, `MinLength`, `MaxLength`, `Minimum`, `Maximum`, `ExclusiveMinimum`, `ExclusiveMaximum`, `MultipleOf` and `Choices` (each a `MokaSchemaChoice` with its `Value` and `Label`).

### MokaSchemaFormSave

| Member | Description |
|--------|-------------|
| `Values` | The whole settings object to store: defaults filled in, write-only settings left out, other properties kept. A copy |
| `Changed` | The names of the settings whose values differ from the saved ones |
| `Secrets` | Every write-only setting, with a `MokaSecretChange`: `Edit` is `Unchanged`, `Set` or `Cleared`, and `Value` holds the new value only for `Set` |

## What a schema can say

The root is an object schema: `"type": "object"` (or `["object", "null"]`, as .NET exports a class) with its settings under `properties`. `title` and `description` name the form, `required` lists the settings that have to be set, and `"additionalProperties": false` makes `Check` refuse other properties.

| Property | Becomes |
|----------|---------|
| `"type": "string"` | A text field. `"format": "email"` and `"uri"` set the input's type; `x-multiline` makes it a text area |
| `"type": "integer"` | A number field that takes whole numbers, with arrow keys that step by `multipleOf`, else 1 |
| `"type": "number"` | A number field |
| `"type": "boolean"` | A switch. With no value and no default it shows off and is saved as `false` |
| `enum` | A select of the values. Labelled choices are `oneOf` (or `anyOf`) of `{ "const": value, "title": "Label" }`, the standard way to name them |
| `writeOnly: true` | A secret: see below |
| `readOnly: true` | Shown and not editable. Nothing checks it |
| `x-advanced: true` | Moved to the advanced group, under "Show advanced" |

A type may also be a list of one type and `"null"`, as .NET exports a nullable property, and a `null` in `enum` means the setting may be left unset.

| Keyword | Checks |
|---------|--------|
| `required` | A missing value, `null` and empty text all count as not set |
| `minLength`, `maxLength` | Characters, counted as Unicode code points as JSON Schema does, so an emoji is one |
| `pattern` | A regular expression the text has to match somewhere. It runs without backtracking, so lookarounds and backreferences are refused when the schema is read |
| `format` | `email`, `uri`, `hostname`, `ipv4`, `ipv6` and `uuid` are checked. Any other format is kept as written and not checked, as the format-annotation vocabulary has it |
| `minimum`, `maximum` | Inclusive limits; the number field holds to them as it's typed |
| `exclusiveMinimum`, `exclusiveMaximum` | The limit itself, as 2020-12 writes it. Draft 4's `true` is refused |
| `multipleOf` | Checked in decimal, so 0.3 is a multiple of 0.1 |

`title` labels a field (the property name otherwise), `description` goes under it and is tied to it for screen readers, `default` fills a setting that isn't set, and the first of `examples` is the placeholder. A `default` that doesn't fit its own setting is a problem.

Keywords the form doesn't know are ignored, `$schema`, `$id`, `$comment`, `deprecated` and other `x-` keywords included. Keywords that change what a value may be and that a form can't honour are problems, so no rule is dropped quietly: nested objects, arrays, `$ref`, `allOf`, `not`, `if`, `then`, `else`, `const` on its own and the other object and array keywords.

`x-advanced` and `x-multiline` are this library's extension keywords (`MokaFormSchema.AdvancedKeyword` and `MultilineKeyword`). JSON Schema has none for either, and the `x-` prefix keeps them apart from any it may get.

## Secrets

A `writeOnly` property is a secret, such as a password or an API key. The server never sends it back, so the form never holds it. It only knows whether one is set, from `SetSecrets` (or a value in `Value`, which is never shown).

- **Set**: a read-only field says "Set", with **Change** and **Clear**. A required secret has no Clear.
- **Change** opens a password field (with `autocomplete="new-password"`, so the browser doesn't offer the person's own password) and **Keep the saved one** goes back. Focus moves to the field that takes the button's place.
- **Clear** says "Cleared when you save".
- **Not set**: a password field.

What a save does to each secret comes in `MokaSchemaFormSave.Secrets`, never in `Values`. A Change left empty keeps what's saved. `MokaSecretChange.ToString()` writes only the edit, so a secret can't reach a log through it.

```razor
private async Task SaveAsync(MokaSchemaFormSave save)
{
    foreach ((string name, MokaSecretChange change) in save.Secrets)
    {
        switch (change.Edit)
        {
            case MokaSecretEdit.Set:
                await Vault.StoreAsync(name, change.Value!);
                break;
            case MokaSecretEdit.Cleared:
                await Vault.RemoveAsync(name);
                break;
        }
    }

    await Store.SaveAsync(save.Values);
}
```

## Checks and the save flow

A switch or a choice is checked as it changes. Text and numbers are checked once the field is left, and on every change after that, so a half-typed value isn't called wrong. Each problem shows on the field's own helper line, tied to it with `aria-describedby`.

Save checks every field first. With a problem it saves nothing, says "Check the setting marked above." under the fields and moves focus to the first field with a problem, opening the advanced settings if it's there. A number field holding text it can't read counts too.

When every field fits, `OnSave` gets the settings. While the handler runs Save says Saving. Hand back what the server said: `Problems` by property name show at their fields (an advanced one opens the advanced settings), and `Refusal` shows under the fields. With either, the edits stay unsaved for another try. With neither, what was sent becomes what's saved, the Unsaved tag goes, and secrets that were set or cleared show as such.

Put back returns every field and secret to what's saved and reports the saved settings through `ValueChanged`.

The form keeps what's saved apart from `Value`: what it was first given, what a successful save stored, and any value the parent passes while nothing is edited. To start over with new settings while edits are open, give the form a new `@key`.

## Checking on the server

`Check` runs the same rules with the same words, so a server can check what it's sent before storing it. Call `WithDefaults` first when a setting left out means its default.

```csharp
MokaFormSchema schema = MokaFormSchema.Parse(plugin.SettingsSchema).Schema!;
JsonObject values = schema.WithDefaults(request.Values);
IReadOnlyDictionary<string, string> problems = schema.Check(values, secretsSet);
if (problems.Count > 0)
{
    return new SaveReply(problems);
}
```

## Limits

A schema from a plugin or a connector is untrusted input, so reading one is bounded. Past any limit the schema is refused with a problem.

| Limit | Value |
|-------|-------|
| `MaxJsonLength` | 65,536 characters of JSON |
| `MaxDepth` | 16 levels of nesting |
| `MaxProperties` | 100 settings |
| `MaxChoices` | 250 choices a setting |
| `MaxNameLength` | 128 characters a property name, with no control characters |
| `MaxTextLength` | 4,000 characters a title, description, example, default or choice |
| `MaxPatternLength` | 1,000 characters a pattern |
| `PatternTimeout` | 50 ms a match. Patterns run with `RegexOptions.NonBacktracking`, so a match takes time in proportion to the text; one too large to run safely is refused when read |

A repeated property name is refused too, rather than the last one quietly winning.

## Words

`MokaSchemaFormText` holds every word: the group headings ("General", "More settings"), the secret buttons, the footer's, and the problems, which are functions of the setting's title (and the limit, written in the current culture).

```razor
<MokaSchemaForm Schema="_schema" @bind-Value="_settings"
                Text="@(MokaSchemaFormText.Default with { GroupTitle = "Connection", Needed = title => $"Fill in {title}." })"/>
```

## Accessibility

- Every field is labelled by its `title`, else its property name, and a required one is marked and has `aria-required`.
- A field's `description`, or its problem, is its helper line and is tied to it with `aria-describedby`.
- Change and Clear name their setting for screen readers ("Change Password"), and focus moves to the field that takes a pressed button's place.
- What's wrong after Save is said in a `role="alert"` line under the fields, and focus moves to the first field with a problem.
