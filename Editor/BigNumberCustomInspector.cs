using System.Numerics;
using UnityEditor;
using UnityEngine;

namespace Swift.Core.Editor
{
    [CustomPropertyDrawer(typeof(BigNumber))]
    public class BigNumberCustomInspector : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty prop = property.FindPropertyRelative("stringValue");
            if (prop != null)
            {
                EditorGUI.PropertyField(position, prop, label);
                if (BigInteger.TryParse(prop.stringValue, out BigInteger v))
                {
                    prop.stringValue = v.ToString();
                }
            }
        }
    }
}