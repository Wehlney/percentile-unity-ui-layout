# Percentile UI Layout

`com.wehlney.percentile-ui-layout` contains uGUI `LayoutGroup` components for layouts that size and place direct children from parent `RectTransform` percentages.

Components:

- `PercentHorizontalLayoutAnchors`
- `PercentVerticalLayoutAnchors`
- `SquareChildrenAnchorLayout`

The horizontal and vertical layouts use normalized anchors, so the driven children scale with the parent rect.

## Common behavior

- Each component operates on direct children of the GameObject it is attached to.
- Layouts use Unity's `LayoutRebuilder` and update when the parent rect changes.
- Inspector lists use `[NamedList]` so entries show the child object name.

## `PercentHorizontalLayoutAnchors`

Positions children left to right using percent values relative to the parent width.

Use it for HUD rows or responsive UI strips where widths, padding, spacing, and cross-axis insets should scale with the parent.

1. Create a UI parent object (any GameObject with a `RectTransform`).
2. Add `PercentHorizontalLayoutAnchors`.
3. Add UI objects as children of that parent.
4. Configure child size, padding, and inset rules in the Inspector.
5. Use `Sync Children` from the component context menu after manual hierarchy changes if auto-collection is disabled.

## `PercentVerticalLayoutAnchors`

Positions children top to bottom using percent values relative to the parent height.

1. Add `PercentVerticalLayoutAnchors` to a parent `RectTransform`.
2. Add direct children.
3. Configure child size, padding, and inset rules in the Inspector.
4. Use `Sync Children` from the component context menu after manual hierarchy changes if auto-collection is disabled.

## `SquareChildrenAnchorLayout`

Forces each direct child to a square size derived from the parent height.

Sizing:

- Computes `baseSize` from parent height minus optional padding.
- Applies `baseSizeMultiplier` and each child's `scaleMultiplier`.
- Clamps the final size between `minSize` and `maxSize`.

Positioning:

- Anchors each driven child; `Left` alignment uses the child size and parent width.
- Applies optional `horizontalOffset` / `verticalOffset`.
- Drives anchors, pivot, size, and anchored position.

1. Add `SquareChildrenAnchorLayout` to a parent UI object.
2. Add direct children.
3. Configure base sizing, alignment, offsets, and per-child scale multipliers.
4. Use `Sync Children` from the component context menu after manual hierarchy changes if auto-collection is disabled.

## Installation (Git UPM)

Add this dependency to `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.wehlney.percentile-ui-layout": "https://github.com/Wehlney/percentile-unity-ui-layout.git#v0.1.2"
  }
}
```

You can also paste the same URL into Unity's Package Manager with **Add package from git URL**.

## Requirements

- Unity 2020.3+ (as declared in `package.json`)
- uGUI (`UnityEngine.UI`)
