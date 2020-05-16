using System;
using UnityEditor;


namespace SwiftFramework.Core.Editor
{
    public class ClassPropertyDrawer : ClassDrawer
    {
        public ClassPropertyDrawer(string label, Func<Type, bool> filter, SerializedProperty property) : 
            base(label, filter, s =>
            { 
                property.stringValue = s; 
                property.serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(property.serializedObject.targetObject);
            }, 
            () => property.stringValue)
        {

        }
    }
}
