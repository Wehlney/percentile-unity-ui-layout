using System;
using System.Collections.Generic;
using Extensions.Layout.Attribute;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Extensions.Layout
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class PercentHorizontalLayoutAnchors : LayoutGroup
    {
        // =====================================================================
        // Child Rule
        // =====================================================================
        [Serializable]
        public sealed class ChildRule
        {
            [SerializeField] private string _name;
            public string Name => _name;

#if UNITY_EDITOR
            public void SetName(string value) => _name = value;
#endif

            public RectTransform child;

            [Header("Sizing")]
            public bool overrideSize = false;

            [Range(0f, 200f)]
            public float sizePercent = 0f;

            [Header("Extra Padding (percent of parent INNER width)")]
            [Range(0f, 200f)] public float extraPaddingLeftPercent = 0f;
            [Range(0f, 200f)] public float extraPaddingRightPercent = 0f;

            [Header("Cross-axis Inset (percent of parent INNER height)")]
            [Range(0f, 200f)] public float insetTopPercent = 0f;
            [Range(0f, 200f)] public float insetBottomPercent = 0f;

            [Header("Multipliers (optional)")]
            [Range(0f, 5f)] public float sizeMultiplier = 1f;
            [Range(0f, 5f)] public float paddingMultiplier = 1f;
            [Range(0f, 5f)] public float insetMultiplier = 1f;

            public bool IsActiveAndValid => child != null && child.gameObject.activeInHierarchy;
        }

        // =====================================================================
        // Parent Settings
        // =====================================================================
        [Header("Parent Padding & Spacing (percent)")]
        [Range(0f, 200f)] public float paddingLeftPercent = 0f;
        [Range(0f, 200f)] public float paddingRightPercent = 0f;
        [Range(0f, 200f)] public float paddingTopPercent = 0f;
        [Range(0f, 200f)] public float paddingBottomPercent = 0f;

        [Range(0f, 200f)] public float spacingPercent = 0f;

        [Header("Child Defaults")]
        [Range(0f, 200f)] public float defaultChildSizePercent = 0f;

        [Header("Layout Order")]
        public bool useCustomSortation = false;

        public bool autoCollectChildren = true;
        public bool includeInactiveInList = false;

        [NamedList]
        [SerializeField] private List<ChildRule> children = new();
        public IReadOnlyList<ChildRule> Children => children;

        private readonly DrivenRectTransformTracker _tracker = new();

        // =====================================================================
        // Unity Lifecycle
        // =====================================================================
        protected override void OnEnable()
        {
            base.OnEnable();
            if (autoCollectChildren) SyncChildren();
            MarkDirty();
        }

        protected override void OnDisable()
        {
            _tracker.Clear();
            base.OnDisable();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (autoCollectChildren) SyncChildren();
            MarkDirty();
        }
#endif

        protected override void OnTransformChildrenChanged()
        {
            base.OnTransformChildrenChanged();

            if (autoCollectChildren)
                SyncChildren();

            MarkDirty();
            ForceRebuildNow();
        }

        // =====================================================================
        // Child Sync
        // =====================================================================
        [ContextMenu("Sync Children")]
        public void SyncChildren()
        {
            var direct = CollectDirectChildren(transform, includeInactiveInList);
            var directSet = new HashSet<RectTransform>(direct);
            var existing = IndexRulesByChild(children);

            if (!useCustomSortation)
            {
                children.Clear();

                for (int i = 0; i < direct.Count; i++)
                {
                    var rt = direct[i];
                    var rule = existing.TryGetValue(rt, out var r)
                        ? r
                        : new ChildRule { child = rt };

#if UNITY_EDITOR
                    rule.SetName(rt.name);
#endif
                    children.Add(rule);
                }

                return;
            }

            // Custom sortation:
            // Keep current list order.
            // Remove missing children.
            // Append newly discovered direct children at the end.

            children.RemoveAll(rule => rule == null || rule.child == null || !directSet.Contains(rule.child));

            for (int i = 0; i < children.Count; i++)
            {
                var rule = children[i];
#if UNITY_EDITOR
                if (rule?.child != null)
                    rule.SetName(rule.child.name);
#endif
            }

            var present = new HashSet<RectTransform>();
            for (int i = 0; i < children.Count; i++)
            {
                var rule = children[i];
                if (rule?.child != null)
                    present.Add(rule.child);
            }

            for (int i = 0; i < direct.Count; i++)
            {
                var rt = direct[i];
                if (present.Contains(rt))
                    continue;

                var rule = existing.TryGetValue(rt, out var r)
                    ? r
                    : new ChildRule { child = rt };

#if UNITY_EDITOR
                rule.SetName(rt.name);
#endif
                children.Add(rule);
            }
        }

        private static List<RectTransform> CollectDirectChildren(Transform parent, bool includeInactive)
        {
            var list = new List<RectTransform>(parent.childCount);

            for (int i = 0; i < parent.childCount; i++)
            {
                if (parent.GetChild(i) is not RectTransform rt)
                    continue;

                if (!includeInactive && !rt.gameObject.activeSelf)
                    continue;

                list.Add(rt);
            }

            return list;
        }

        private static Dictionary<RectTransform, ChildRule> IndexRulesByChild(List<ChildRule> rules)
        {
            var map = new Dictionary<RectTransform, ChildRule>();

            for (int i = 0; i < rules.Count; i++)
            {
                var r = rules[i];
                if (r?.child != null)
                    map[r.child] = r;
            }

            return map;
        }

        // =====================================================================
        // LayoutGroup Contract
        // =====================================================================
        public override void CalculateLayoutInputHorizontal() => base.CalculateLayoutInputHorizontal();
        public override void CalculateLayoutInputVertical() { }

        public override void SetLayoutHorizontal() => ApplyLayout();
        public override void SetLayoutVertical() => ApplyLayout();

        // =====================================================================
        // Layout Execution
        // =====================================================================
        private void ApplyLayout()
        {
            _tracker.Clear();

            int activeCount = CountActiveChildren();
            if (activeCount <= 0)
                return;

            var inner = BuildInnerNormalizedRect();

            float innerW = InnerWidth(inner);
            float innerH = InnerHeight(inner);

            float spacingNorm = PercentOf(innerW, spacingPercent);
            float totalSpacingNorm = TotalInterItemSpacing(activeCount, spacingNorm);

            float availableInnerW = RemainingAfter(innerW, totalSpacingNorm);

            float cursorLeft = inner.left;
            int remaining = activeCount;

            for (int i = 0; i < children.Count; i++)
            {
                var rule = children[i];
                if (rule == null || !rule.IsActiveAndValid)
                    continue;

                var child = rule.child;
                Drive(child);

                float sizePct = EffectiveSizePercent(rule, defaultChildSizePercent);

                float widthNorm = ScaledByPercent(availableInnerW, sizePct, rule.sizeMultiplier);

                float extraLeftNorm = ScaledByPercent(innerW, rule.extraPaddingLeftPercent, rule.paddingMultiplier);
                float extraRightNorm = ScaledByPercent(innerW, rule.extraPaddingRightPercent, rule.paddingMultiplier);

                float insetTopNorm = ScaledByPercent(innerH, rule.insetTopPercent, rule.insetMultiplier);
                float insetBottomNorm = ScaledByPercent(innerH, rule.insetBottomPercent, rule.insetMultiplier);

                cursorLeft = Add(cursorLeft, extraLeftNorm);

                float childLeft = cursorLeft;
                float childRight = childLeft + widthNorm;

                SetChildAnchors(child, inner, insetTopNorm, insetBottomNorm, childLeft, childRight);
                ForceZ(child, 0f);

                cursorLeft = Add(childRight, extraRightNorm);

                remaining--;
                if (remaining > 0)
                    cursorLeft = Add(cursorLeft, spacingNorm);
            }
        }

        // =====================================================================
        // Normalized Rect Model
        // =====================================================================
        private readonly struct NormalizedRect
        {
            public readonly float left, right, top, bottom;

            public NormalizedRect(float left, float right, float top, float bottom)
            {
                this.left = left;
                this.right = right;
                this.top = top;
                this.bottom = bottom;
            }
        }

        private NormalizedRect BuildInnerNormalizedRect()
        {
            float padL = Clamp01(Percent01(paddingLeftPercent));
            float padR = Clamp01(Percent01(paddingRightPercent));
            float padT = Clamp01(Percent01(paddingTopPercent));
            float padB = Clamp01(Percent01(paddingBottomPercent));

            float left = padL;
            float right = Clamp01(1f - padR);
            float top = Clamp01(1f - padT);
            float bottom = padB;

            right = Max(right, left);
            top = Max(top, bottom);

            return new NormalizedRect(left, right, top, bottom);
        }

        private static float InnerWidth(in NormalizedRect r) => RemainingAfter(r.right, r.left);
        private static float InnerHeight(in NormalizedRect r) => RemainingAfter(r.top, r.bottom);

        private static void SetChildAnchors(
            RectTransform child,
            in NormalizedRect inner,
            float insetTopNorm,
            float insetBottomNorm,
            float left,
            float right)
        {
            float yMin = Clamp01(inner.bottom + insetBottomNorm);
            float yMax = Clamp01(inner.top - insetTopNorm);

            yMax = Max(yMax, yMin);
            right = Max(right, left);

            child.anchorMin = new Vector2(left, yMin);
            child.anchorMax = new Vector2(right, yMax);

            child.pivot = new Vector2(0.5f, 0.5f);

            child.offsetMin = Vector2.zero;
            child.offsetMax = Vector2.zero;
            child.anchoredPosition = Vector2.zero;
            child.sizeDelta = Vector2.zero;
        }

        // =====================================================================
        // Z Handling
        // =====================================================================
        private void ForceZ(RectTransform child, float z)
        {
            var ap3 = child.anchoredPosition3D;
            ap3.z = z;
            child.anchoredPosition3D = ap3;

            var lp = child.localPosition;
            lp.z = z;
            child.localPosition = lp;
        }

        // =====================================================================
        // Math Helpers
        // =====================================================================
        private static float EffectiveSizePercent(ChildRule rule, float defaultPercent)
            => rule.overrideSize ? rule.sizePercent : defaultPercent;

        private static float Percent01(float percent) => percent / 100f;

        private static float PercentOf(float baseValue, float percent)
            => baseValue * (percent / 100f);

        private static float ScaledByPercent(float baseValue, float percent, float multiplier)
            => baseValue * (percent / 100f) * NonNegative(multiplier);

        private static float TotalInterItemSpacing(int activeCount, float spacing)
            => activeCount > 1 ? spacing * (activeCount - 1) : 0f;

        private static float RemainingAfter(float total, float subtract)
            => NonNegative(total - subtract);

        private static float Add(float a, float b) => a + b;

        private static float NonNegative(float v) => Mathf.Max(0f, v);
        private static float Clamp01(float v) => Mathf.Clamp01(v);
        private static float Max(float a, float b) => Mathf.Max(a, b);

        // =====================================================================
        // Utility
        // =====================================================================
        private int CountActiveChildren()
        {
            int count = 0;

            for (int i = 0; i < children.Count; i++)
            {
                var r = children[i];
                if (r != null && r.IsActiveAndValid)
                    count++;
            }

            return count;
        }

        private void Drive(RectTransform child)
        {
            _tracker.Add(
                this,
                child,
                DrivenTransformProperties.Anchors |
                DrivenTransformProperties.Pivot |
                DrivenTransformProperties.AnchoredPositionX |
                DrivenTransformProperties.AnchoredPositionY |
                DrivenTransformProperties.AnchoredPositionZ |
                DrivenTransformProperties.SizeDeltaX |
                DrivenTransformProperties.SizeDeltaY
            );
        }

        private void MarkDirty()
        {
            if (!isActiveAndEnabled) return;
            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
        }

        private void ForceRebuildNow()
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        }
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(PercentHorizontalLayoutAnchors.ChildRule))]
    internal sealed class PercentHorizontalLayoutAnchorsChildRuleDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            => EditorGUI.GetPropertyHeight(property, label, includeChildren: true);

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var overrideProp = property.FindPropertyRelative("overrideSize");

            property.isExpanded = EditorGUI.Foldout(
                new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight),
                property.isExpanded,
                label,
                toggleOnLabelClick: true);

            if (!property.isExpanded)
            {
                EditorGUI.EndProperty();
                return;
            }

            EditorGUI.indentLevel++;
            float y = position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            var it = property.Copy();
            var end = it.GetEndProperty();

            bool enterChildren = true;
            while (it.NextVisible(enterChildren) && !SerializedProperty.EqualContents(it, end))
            {
                enterChildren = false;

                float h = EditorGUI.GetPropertyHeight(it, includeChildren: true);
                var r = new Rect(position.x, y, position.width, h);

                if (it.name == "sizePercent")
                {
                    bool enabled = overrideProp.boolValue;
                    using (new EditorGUI.DisabledScope(!enabled))
                        EditorGUI.PropertyField(r, it, includeChildren: true);
                }
                else
                {
                    EditorGUI.PropertyField(r, it, includeChildren: true);
                }

                y += h + EditorGUIUtility.standardVerticalSpacing;
            }

            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }
    }
#endif
}