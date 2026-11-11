---
title: "Label colors"
description: "Named hues for tags, folders and libraries that people color themselves"
order: 5
---

# Label colors

Label colors are for things the person using your app colors: tags, folders, libraries, projects. They're kept apart from the colors that mean something (`Error`, `Warning`, `Success`, `Info`), so a red label never reads as a failure.

A label is a `MokaLabelColor` value, such as `MokaLabelColor.Teal`. Store that name, never a color. The theme decides every shade, so a stored choice works in light and dark, on every preset, and in every tone.

```razor
<MokaTag Text="Design" Label="MokaLabelColor.Teal"/>
<MokaChip Text="Research" Label="MokaLabelColor.Purple"/>
<MokaTreeItem Text="Photos" Icon="MokaIcons.File.Folder" Label="MokaLabelColor.Blue"/>
<MokaListItem Text="Reading list" Label="MokaLabelColor.Indigo"/>
<MokaColorSwatch Labels="MokaLabelColors.Standard" @bind-SelectedLabel="_color"/>
```

## The hues

Sixteen hues around the wheel, then slate as the neutral. The enum runs in this order, which is the order to show them in a picker.

| Label | Key | `Standard` | `Distinct` |
|---|---|---|---|
| `Red` | `red` | yes | yes |
| `Rose` | `rose` | | |
| `Orange` | `orange` | yes | yes |
| `Amber` | `amber` | yes | |
| `Yellow` | `yellow` | yes | yes |
| `Lime` | `lime` | yes | |
| `Green` | `green` | yes | yes |
| `Emerald` | `emerald` | | |
| `Teal` | `teal` | yes | yes |
| `Cyan` | `cyan` | yes | |
| `Sky` | `sky` | | |
| `Blue` | `blue` | yes | yes |
| `Indigo` | `indigo` | yes | |
| `Violet` | `violet` | | |
| `Purple` | `purple` | yes | yes |
| `Pink` | `pink` | yes | yes |
| `Slate` | `slate` | yes | yes |

`MokaLabelColors.All` is every hue. `MokaLabelColors.Standard` is twelve and slate, enough to tell many tags apart with each still distinct. `MokaLabelColors.Distinct` is eight and slate, so no two look alike, even for people who see color differently. Offer whichever fits the app; all of them stay valid in storage.

## Tokens

Every theme writes three tokens per hue, 51 in all:

| Token | Use | Promise |
|---|---|---|
| `--moka-label-{key}` | The solid: dots, icons, swatches, borders | 3:1 against cards, panels and the page |
| `--moka-label-{key}-bg` | The soft tint behind a tag. A `color-mix` of the hue into `--moka-color-surface`, so it follows the preset's surfaces | |
| `--moka-label-{key}-ink` | Text on that tint | 4.5:1 on the tint, and on a plain card |

On a dark page the solids are the hues as designed. On a light page the brightest of them (orange, amber, yellow, lime, green, emerald, teal, cyan, sky) are darkened at the same hue until a dot shows 3:1, and the inks are darkened until they read. `LabelContrastTests` checks every hue, tone and preset.

`MokaTheme.Dark` and `MokaTheme.Light` write the tokens too, and `moka.css` holds the same values as fallbacks, so labels work on the original design and without a provider.

## The helper

`MokaLabelColors` turns a label into its CSS and its name, for your own markup:

| Member | Returns |
|---|---|
| `All`, `Standard`, `Distinct` | `IReadOnlyList<MokaLabelColor>`, in picker order with slate last |
| `Key(color)` | `"teal"`, the part of the token names after `--moka-label-` |
| `Solid(color)` | `"var(--moka-label-teal)"` |
| `Background(color)` | `"var(--moka-label-teal-bg)"` |
| `Ink(color)` | `"var(--moka-label-teal-ink)"` |
| `Name(color)` | `"Teal"`, in English |
| `Name(color, names)` | The app's own name from `names`, or English when it gives none |
| `TryParse(text, out color)` | Reads a key (`teal`), a name in any case (`Teal`) or a `Solid` reference. Never a number |

```razor
<span class="folder-dot" style="background: @MokaLabelColors.Solid(folder.Color)"></span>
```

Store the key or the enum's name, and read either back with `TryParse`:

```csharp
string stored = MokaLabelColors.Key(MokaLabelColor.Teal);

MokaLabelColor? color = MokaLabelColors.TryParse(stored, out MokaLabelColor read) ? read : null;

string name = MokaLabelColors.Name(MokaLabelColor.Teal, c => Localizer[$"Color{c}"]);
```

## Tone

How strong the label colors are is a style choice, like corners or density. `Vivid` is the default.

| Tone | Look |
|---|---|
| `MokaLabelTone.Vivid` | Clear hues on a soft tint, with ink that reads on it. The most distinct |
| `MokaLabelTone.Muted` | The same hues, calmer, so labels sit quietly beside the accent |
| `MokaLabelTone.Pastel` | Lighter dots and fuller tints. Friendly, a little less distinct |

```csharp
MokaTheme theme = MokaTheme.Red.WithLabelTone(MokaLabelTone.Muted);
```

Every tone keeps the contrast promises above. Like the other style choices, `WithLabelTone` does nothing on `MokaTheme.Dark` and `MokaTheme.Light`, which draw labels vivid. The tone travels with the theme: `MokaThemeSerializer` writes it as `labelTone`, `MokaAppearance.LabelTone` holds a person's pick, and `MokaAppearancePanel` offers it as the Label colors row (`MokaAppearanceRows.LabelTone`, part of `Style`).

## Where they show

| Component | Parameter | What it draws |
|---|---|---|
| [MokaTag](/components/tag) | `Label` | The tint behind the ink, and a dot before the text when there's no icon. Replaces `Color`; `Variant` still shapes it |
| [MokaChip](/components/chip) | `Label` | The same as a tag. A selected chip keeps the label's color with a border in it |
| [MokaListItem](/components/list) | `Label`, `LabelMark` | The icon in the label's color, or a dot before the name |
| [MokaTreeItem](/components/tree) | `Label`, `LabelMark` | The same, for colored folders and libraries |
| [MokaColorSwatch](/components/color-swatch) | `Labels`, `SelectedLabel` | The hues to pick from, named, with the pick coming back as a `MokaLabelColor` |

### Variants on tags and chips

`Soft` (the default) and `Filled` draw the tint behind the ink. `Outlined` draws a border in the solid on no background, with the text in the ink. `Text` draws the ink alone.

```razor
<MokaTag Text="Design" Label="MokaLabelColor.Teal"/>
<MokaTag Text="In review" Label="MokaLabelColor.Amber" Variant="MokaVariant.Outlined"/>
<MokaTag Text="Research" Label="MokaLabelColor.Purple" Variant="MokaVariant.Text"/>
```

### Colored folders and dots

`LabelMark` says how a list or tree item shows its label:

| Mark | What it draws |
|---|---|
| `MokaLabelMark.Icon` (default) | The icon, or whatever `IconContent` draws, in the label's color. An item with no icon gets a dot |
| `MokaLabelMark.TintedIcon` | The same, with a soft fill of the label's color inside an outline icon: the look of a colored folder. Suits closed shapes such as folders, books and tags |
| `MokaLabelMark.Dot` | A small dot before the name. The icon keeps its own color |

`IconColor` wins over the label on the icon.

```razor
<MokaTree>
    <MokaTreeItem Text="Photos" Icon="MokaIcons.File.Folder" Label="MokaLabelColor.Blue" LabelMark="MokaLabelMark.TintedIcon"/>
    <MokaTreeItem Text="Recipes" Icon="MokaIcons.File.Folder" Label="MokaLabelColor.Green" LabelMark="MokaLabelMark.TintedIcon"/>
</MokaTree>

<MokaList>
    <MokaListItem Text="Reading list" Label="MokaLabelColor.Indigo"/>
    <MokaListItem Text="Garden plans" Label="MokaLabelColor.Orange"/>
</MokaList>
```

### A Color item in a menu

```razor
<MokaColorSwatch Labels="MokaLabelColors.Standard" @bind-SelectedLabel="_folder.Color" AllowClear Columns="7"
                 AriaLabel="Folder color"/>
```

Each swatch is named by its hue, "Teal" or your own word through `LabelName`, for screen readers, its tooltip and the text beside the chosen color.
