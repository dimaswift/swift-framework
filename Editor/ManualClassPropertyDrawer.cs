using System;
using UnityEditor;
using UnityEngine;

namespace Swift.Core.Editor
{
    public class ManualClassPropertyDrawer : ClassPropertyDrawer
    {
        private bool showInfo;
        
        public ManualClassPropertyDrawer(string label, Func<Type, bool> filter, SerializedProperty property, Action onSelectionChanged = null) :
            base(label, filter, property, onSelectionChanged) 
        {
            
        }

        public override float TotalPropertyHeight
        {
            get
            {
                if (showInfo)
                {
                    return base.TotalPropertyHeight * 3;
                }

                return base.TotalPropertyHeight;
            }
        }

        private static readonly string[] info = { "", "", "Version=0.0.0.0", "Culture=neutral", "PublicKeyToken=null" };
        
        public override void Draw(Rect position)
        {
            base.Draw(position);

            if (string.IsNullOrEmpty(property.stringValue) == false)
            {
                try
                {
                    string[] values = property.stringValue.Split(',');
                    info[0] = values[0];
                    info[1] = values[1];
                }
                catch
                {
                    info[0] = "";
                    info[1] = "";
                }
            }
            else
            {
                info[0] = "";
                info[1] = "";
            }

            float y = position.y;

            showInfo = EditorGUI.Foldout(new Rect(position.x, y, position.width, position.height),
                showInfo, "", true);

            if (showInfo)
            {
                y += base.TotalPropertyHeight;
                EditorGUI.indentLevel++;
                info[0] = EditorGUI.TextField(
                    new Rect(position.x, y, position.width, position.height),
                    "Full Name",
                    info[0].Trim());

                y += base.TotalPropertyHeight;
                info[1] = EditorGUI.TextField(
                    new Rect(position.x, y, position.width, position.height),
                    "Assembly",
                    info[1].Trim());
                
                if (GUI.changed)
                { 
                    property.stringValue = string.Join(", ", info);
                    property.serializedObject.ApplyModifiedProperties();
                }
                EditorGUI.indentLevel--;
            }
        }
    }
}