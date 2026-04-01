# Percentile UI Layout

UPM package providing percent-based `LayoutGroup` implementations for Unity UI.

## What you get

### Layout components

These `LayoutGroup` components position children by **percentage of the parent rect**.

- `Wehlney.PercentileUILayout.Layout.PercentHorizontalLayoutAnchors`
- `Wehlney.PercentileUILayout.Layout.PercentVerticalLayoutAnchors`

Project integration note:
- If you previously used the types under `Extensions.Layout.*` in an existing project, you can keep your scene/prefab bindings by keeping thin wrapper scripts in `Assets/` that inherit from the package types.

### Editor support

- `[NamedList]` attribute (for list fields)
- `Wehlney.PercentileUILayout.Editor.NamedListDrawer` (Unity Editor only)

`[NamedList]` renders list elements using their serialized `_name` field (useful with the `ChildRule` lists inside the layout components).

And editor support:

- `[NamedList]` attribute (for list fields)
- `NamedListDrawer` custom inspector drawer (Unity Editor only)

## Install (Git URL)

In Unity Package Manager: `+` -> **Add package from git URL...**

- Standalone repo install:
  - `https://github.com/Wehlney/percentile-unity-ui-layout.git#v0.1.1`

(Replace tag/version as needed.)

## Usage

### 1) Add a layout component

1. Select a parent UI object (must have a `RectTransform`).
2. Add one of:
   - `PercentHorizontalLayoutAnchors`
   - `PercentVerticalLayoutAnchors`

### 2) Configure parent spacing/padding as percentages

Use the inspector to set:
- parent padding percents
- `spacingPercent`
- `defaultChildSizePercent`

The percentages are interpreted relative to the parent **inner** size (after padding).

### 3) Configure per-child rules (optional)

The component maintains a list of `ChildRule` entries.

Recommended workflow:
- Keep `autoCollectChildren` enabled
- Use the context menu **Sync Children** after adding/removing children

Per-child you can:
- assign the `RectTransform`
- override size percent
- add extra padding/inset percents
- apply multipliers

## Scripting / namespaces

If you need to reference the types directly from code:

```csharp
using Wehlney.PercentileUILayout.Layout;

// ...
var layout = GetComponent<PercentHorizontalLayoutAnchors>();
```

## Development tip (local package)

If you want to edit the package while working on a game project, reference it via a local UPM `file:` dependency in the game project's `Packages/manifest.json`, e.g.:

```json
"com.wehlney.percentile-ui-layout": "file:F:/Unity%20Projects/percentile-unity-ui-layout"
```
