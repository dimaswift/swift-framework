using UnityEditor;

namespace Swift.Core.Editor
{
    public static class LinkEditorExtensions
    {
        public static T ToLink<T>(this SerializedProperty serializedProperty) where T : Link, new()
        {
            return Link.Create<T>(serializedProperty.FindPropertyRelative(Link.PathPropertyName).stringValue);
        }

        public static bool HasLinkValue<T>(this SerializedProperty serializedProperty) where T : Link, new()
        {
            return serializedProperty.ToLink<T>().HasValue;
        }

        public static void SaveLink(this SerializedProperty serializedProperty, string path)
        {
            serializedProperty.FindPropertyRelative(Link.PathPropertyName).stringValue = path;
            serializedProperty.serializedObject.ApplyModifiedProperties();
        }
    }
}



