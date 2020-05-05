using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Numerics;
using SwiftFramework.Core;

namespace SwiftFramework.Core.Editor
{
    [CustomPropertyDrawer(typeof(BigNumber))]
    public class BigNumberCustomInstepctor : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var prop = property.FindPropertyRelative("stringValue");
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
