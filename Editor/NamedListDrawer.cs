using Wehlney.PercentileUILayout;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Wehlney.PercentileUILayout.Editor
{
    [CustomPropertyDrawer(typeof(NamedListAttribute))]
    public sealed class NamedListDrawer : PropertyDrawer
    {
        private ReorderableList _list;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isArray || property.propertyType == SerializedPropertyType.String)
                return EditorGUI.GetPropertyHeight(property, label, true);

            EnsureList(property, label);
            return _list.GetHeight();
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!property.isArray || property.propertyType == SerializedPropertyType.String)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            if (!HasNameField(property))
            {
                EditorGUI.HelpBox(position,
                    "[NamedList] requires element to contain serialized field '_name'.",
                    MessageType.Error);
                return;
            }

            EnsureList(property, label);
            _list.DoList(position);
        }

        private void EnsureList(SerializedProperty arrayProp, GUIContent label)
        {
            if (_list != null && _list.serializedProperty == arrayProp)
                return;

            _list = new ReorderableList(arrayProp.serializedObject, arrayProp, true, true, true, true);

            _list.drawHeaderCallback = r => EditorGUI.LabelField(r, label);

            _list.elementHeightCallback = i =>
            {
                var el = arrayProp.GetArrayElementAtIndex(i);
                return EditorGUI.GetPropertyHeight(el, true) + 4f;
            };

            _list.drawElementCallback = (r, i, active, focused) =>
            {
                var el = arrayProp.GetArrayElementAtIndex(i);
                var nameProp = el.FindPropertyRelative("_name");

                string title = (!string.IsNullOrWhiteSpace(nameProp?.stringValue))
                    ? nameProp.stringValue
                    : $"Element {i}";

                r.y += 2f;
                r.height = EditorGUI.GetPropertyHeight(el, true);
                EditorGUI.PropertyField(r, el, new GUIContent(title), true);
            };
        }

        private static bool HasNameField(SerializedProperty arrayProp)
        {
            if (arrayProp.arraySize == 0) return true;
            var el = arrayProp.GetArrayElementAtIndex(0);
            var nameProp = el.FindPropertyRelative("_name");
            return nameProp != null && nameProp.propertyType == SerializedPropertyType.String;
        }
    }
}
