using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SwiftFramework.Core.Editor
{
    [CustomPropertyDrawer(typeof(SceneLink))]
    public class SceneLinkPropertyDrawer : PropertyDrawer
    {
        private SceneLinkDrawer drawer;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if(drawer == null)
            {
                drawer = new SceneLinkDrawer(typeof(SceneAsset), fieldInfo);
            }
            drawer.Draw(position, property, label);
        }
    }


    internal class SceneLinkDrawer : BaseLinkDrawer
    {
        public SceneLinkDrawer(System.Type type, FieldInfo fieldInfo) : base(type, fieldInfo)
        {
        }

        protected override void Reload()
        {
            assets.Clear();

            assets.AddRange(AddrHelper.GetScenes());
        }
    }
}