using System;
using UnityEditor;

namespace SwiftFramework.Core.Editor
{
    public class ClassPropertyDrawer : ClassDrawer
    {
        protected readonly SerializedProperty property;
        
        public ClassPropertyDrawer(string label, Func<Type, bool> filter, SerializedProperty property, Action onSelectionChanged = null) :
            base(label, filter, s =>
                {
                    property.stringValue = s;
                    property.serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(property.serializedObject.targetObject);
                    onSelectionChanged?.Invoke();
                },
                () => property.stringValue)
        {
            this.property = property;
        }
    }
}