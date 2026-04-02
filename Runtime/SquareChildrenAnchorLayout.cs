using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Wehlney.PercentileUILayout;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Wehlney.PercentileUILayout.Layout
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class SquareChildrenAnchorLayout : LayoutGroup
    {
        public enum HorizontalAlignment
        {
            Left,
            Middle,
            Center
        }

        [Serializable]
        public sealed class ChildRule
        {
            [SerializeField] private string _name;
            public string Name => _name;

#if UNITY_EDITOR
            public void SetName(string value) => _name = value;
#endif

            public RectTransform child;

            [Header("Scale")]
            [Range(0f, 5f)] public float scaleMultiplier = 1f;
        }

        [Header("Base Sizing")]
        [SerializeField, Min(0f)] private float heightPadding = 0f;
        [SerializeField, Range(0f, 5f)] private float baseSizeMultiplier = 1f;
        [SerializeField] private float minSize = 1f;
        [SerializeField] private float maxSize = 10000f;

        [Header("Alignment")]
        [SerializeField] private HorizontalAlignment alignment = HorizontalAlignment.Middle;

        [Header("Offsets")]
        [SerializeField] private float horizontalOffset = 0f;
        [SerializeField] private float verticalOffset = 0f;

        [Header("Child Management")]
        [SerializeField] private bool autoCollectChildren = true;
        [SerializeField] private bool includeInactiveInList = false;

        [NamedList]
        [SerializeField] private List<ChildRule> children = new();
        public IReadOnlyList<ChildRule> Children => children;

        private readonly DrivenRectTransformTracker _tracker = new();
        private readonly HashSet<RectTransform> _drivenChildren = new();

        protected override void OnEnable()
        {
            base.OnEnable();
            RefreshHierarchyState();
        }

        protected override void OnDisable()
        {
            ClearDrivenState(resetReleasedChildren: false);
            base.OnDisable();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

            if (!isActiveAndEnabled)
                return;

            RefreshHierarchyState(deferImmediateRebuild: true);
        }
#endif

        protected override void OnTransformChildrenChanged()
        {
            base.OnTransformChildrenChanged();

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                RefreshHierarchyState(deferImmediateRebuild: true);
                return;
            }
#endif

            RefreshHierarchyState();
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            MarkDirty();
        }

        protected override void OnDidApplyAnimationProperties()
        {
            base.OnDidApplyAnimationProperties();
            MarkDirty();
        }

        [ContextMenu("Sync Children")]
        public void SyncChildren()
        {
            var directChildren = CollectDirectChildren(transform, includeInactiveInList);
            var existing = IndexRulesByChild(children);

            var newRules = new List<ChildRule>(directChildren.Count);

            for (int i = 0; i < directChildren.Count; i++)
            {
                var rt = directChildren[i];
                var rule = existing.TryGetValue(rt, out var oldRule)
                    ? oldRule
                    : new ChildRule { child = rt };

#if UNITY_EDITOR
                rule.SetName(rt.name);
#endif

                newRules.Add(rule);
            }

            children = newRules;
        }

        [ContextMenu("Rebuild Layout Now")]
        public void RebuildLayoutNow()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                RefreshHierarchyState(deferImmediateRebuild: true);
                return;
            }
#endif

            RefreshHierarchyState();
        }

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();
        }

        public override void CalculateLayoutInputVertical()
        {
        }

        public override void SetLayoutHorizontal()
        {
            ApplyLayout();
        }

        public override void SetLayoutVertical()
        {
            ApplyLayout();
        }

        private void RefreshHierarchyState(bool deferImmediateRebuild = false)
        {
            ClearDrivenState(resetReleasedChildren: true);

            if (autoCollectChildren)
                SyncChildren();
            else
                PurgeInvalidRules();

            MarkDirty();

#if UNITY_EDITOR
            if (!Application.isPlaying && deferImmediateRebuild)
            {
                EditorApplication.delayCall -= DeferredRebuild;
                EditorApplication.delayCall += DeferredRebuild;
                return;
            }
#endif

            ForceRebuildNow();
        }

        private void ApplyLayout()
        {
            _tracker.Clear();

            PurgeInvalidRules();

            float parentWidth = rectTransform.rect.width;
            float parentHeight = rectTransform.rect.height;

            float baseSize = Mathf.Clamp(
                Mathf.Max(0f, parentHeight - heightPadding) * Mathf.Max(0f, baseSizeMultiplier),
                minSize,
                maxSize);

            var currentDriven = new HashSet<RectTransform>();

            for (int i = 0; i < children.Count; i++)
            {
                var rule = children[i];
                if (rule == null || rule.child == null)
                    continue;

                var child = rule.child;

                if (!IsDirectValidLayoutChild(child))
                    continue;

                currentDriven.Add(child);
                Drive(child);

                float childSize = Mathf.Clamp(
                    baseSize * Mathf.Max(0f, rule.scaleMultiplier),
                    minSize,
                    maxSize);

                float anchorX = GetAnchorX(parentWidth, childSize);
                float anchorY = 0.5f;

                SetChildAnchorsAndSize(
                    child,
                    anchorX,
                    anchorY,
                    childSize,
                    horizontalOffset,
                    verticalOffset);

                ForceZ(child, 0f);
            }

            foreach (var oldChild in _drivenChildren)
            {
                if (oldChild == null)
                    continue;

                if (!currentDriven.Contains(oldChild))
                    ResetDrivenProperties(oldChild);
            }

            _drivenChildren.Clear();
            foreach (var child in currentDriven)
                _drivenChildren.Add(child);
        }

        private void PurgeInvalidRules()
        {
            for (int i = children.Count - 1; i >= 0; i--)
            {
                var rule = children[i];

                if (rule == null || rule.child == null)
                {
                    children.RemoveAt(i);
                    continue;
                }

                if (!IsDirectChild(rule.child))
                {
                    children.RemoveAt(i);
                    continue;
                }

                if (!includeInactiveInList && !rule.child.gameObject.activeSelf)
                {
                    children.RemoveAt(i);
                }
            }
        }

        private bool IsDirectValidLayoutChild(RectTransform child)
        {
            if (child == null)
                return false;

            if (!IsDirectChild(child))
                return false;

            if (!child.gameObject.activeInHierarchy)
                return false;

            if (!includeInactiveInList && !child.gameObject.activeSelf)
                return false;

            return true;
        }

        private bool IsDirectChild(RectTransform child)
        {
            return child != null && child.parent == transform;
        }

        private void ClearDrivenState(bool resetReleasedChildren)
        {
            _tracker.Clear();

            if (resetReleasedChildren)
            {
                foreach (var child in _drivenChildren)
                {
                    if (child == null)
                        continue;

                    if (!IsDirectChild(child))
                        ResetDrivenProperties(child);
                }
            }

            _drivenChildren.Clear();
        }

#if UNITY_EDITOR
        private void DeferredRebuild()
        {
            if (this == null || rectTransform == null)
                return;

            if (!isActiveAndEnabled)
                return;

            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        }
#endif

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
                var rule = rules[i];
                if (rule?.child != null)
                    map[rule.child] = rule;
            }

            return map;
        }

        private float GetAnchorX(float parentWidth, float childSize)
        {
            return alignment switch
            {
                HorizontalAlignment.Left => parentWidth > 0f
                    ? (childSize * 0.5f) / parentWidth
                    : 0f,

                HorizontalAlignment.Middle => 0.5f,
                HorizontalAlignment.Center => 0.5f,
                _ => 0.5f
            };
        }

        private static void SetChildAnchorsAndSize(
            RectTransform child,
            float anchorX,
            float anchorY,
            float size,
            float offsetX,
            float offsetY)
        {
            child.anchorMin = new Vector2(anchorX, anchorY);
            child.anchorMax = new Vector2(anchorX, anchorY);
            child.pivot = new Vector2(0.5f, 0.5f);

            child.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size);
            child.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size);

            child.anchoredPosition = new Vector2(offsetX, offsetY);
        }

        private static void ForceZ(RectTransform child, float z)
        {
            var ap3 = child.anchoredPosition3D;
            ap3.z = z;
            child.anchoredPosition3D = ap3;

            var lp = child.localPosition;
            lp.z = z;
            child.localPosition = lp;
        }

        private void Drive(RectTransform child)
        {
            if (!IsDirectChild(child))
                return;

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

        private static void ResetDrivenProperties(RectTransform child)
        {
            if (child == null)
                return;

            child.anchorMin = new Vector2(0.5f, 0.5f);
            child.anchorMax = new Vector2(0.5f, 0.5f);
            child.pivot = new Vector2(0.5f, 0.5f);
            child.anchoredPosition = Vector2.zero;

            var ap3 = child.anchoredPosition3D;
            ap3.z = 0f;
            child.anchoredPosition3D = ap3;
        }

        private void MarkDirty()
        {
            if (!isActiveAndEnabled)
                return;

            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
        }

        private void ForceRebuildNow()
        {
            if (!isActiveAndEnabled)
                return;

            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        }
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(SquareChildrenAnchorLayout.ChildRule))]
    internal sealed class SquareChildrenAnchorLayoutChildRuleDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            => EditorGUI.GetPropertyHeight(property, label, includeChildren: true);

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            property.isExpanded = EditorGUI.Foldout(
                new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight),
                property.isExpanded,
                label,
                true);

            if (!property.isExpanded)
            {
                EditorGUI.EndProperty();
                return;
            }

            EditorGUI.indentLevel++;
            float y = position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            var iterator = property.Copy();
            var end = iterator.GetEndProperty();

            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, end))
            {
                enterChildren = false;

                float h = EditorGUI.GetPropertyHeight(iterator, includeChildren: true);
                var r = new Rect(position.x, y, position.width, h);

                EditorGUI.PropertyField(r, iterator, includeChildren: true);
                y += h + EditorGUIUtility.standardVerticalSpacing;
            }

            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }
    }
#endif
}
