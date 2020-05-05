using UnityEditor;
using UnityEngine;

namespace SwiftFramework.Core.Editor
{


    [CustomPropertyDrawer(typeof(PriceLink), true)]
    [CanEditMultipleObjects]
    public class PriceLinkDrawer : LinkPropertyDrawer<IPrice>
    {
        private InterfaceLinkDrawer drawer;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            position = EditorGUI.PrefixLabel(position, label);

            if (drawer == null)
            {
                drawer = new InterfaceLinkDrawer(typeof(IPrice), fieldInfo);
                drawer.AllowSelectAndPing = false;
            }

            EditorGUI.PropertyField(new Rect(position.x, position.y, position.width * .45f, position.height), property.FindPropertyRelative("amount").FindPropertyRelative("stringValue"), GUIContent.none);

            if (property.hasMultipleDifferentValues)
            {
                EditorGUI.HelpBox(new Rect(position.x + position.width * .45f, position.y, position.width * .55f, position.height), "Different price sources selected", MessageType.Warning);
                return;
            }

            drawer.Draw(new Rect(position.x + position.width * .45f, position.y, position.width * .55f, position.height), property, GUIContent.none);
        }
    }

}
