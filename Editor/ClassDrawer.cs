using System;
using System.Collections.Generic;
using System.Linq;
using Swift.EditorUtils;
using UnityEditor;
using UnityEngine;

namespace Swift.Core.Editor
{
    public class ClassDrawer
    {
        public event Action OnSelectionChanged = () => { };

        public Type SelectedType { get; private set; }

        private string[] modules;
        private string[] modulesNames;

        protected readonly Action<string> selectHandler;
        protected readonly Func<string> currentHandler;
        protected readonly Func<Type, bool> filter;
        protected readonly string label;

        protected ClassDrawer(string label, Func<Type, bool> filter, Action<string> selectHandler,
            Func<string> currentHandler)
        {
            this.label = label;
            this.selectHandler = selectHandler;
            this.currentHandler = currentHandler;
            this.filter = filter;
            Rebuild();
        }

        public virtual float TotalPropertyHeight => EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        public void Rebuild()
        {
            IEnumerable<Type> moduleTypes = Util.GetAllTypes(filter).ToArray();
            modules = new string[moduleTypes.Count() + 1];
            modulesNames = new string[modules.Length];
            modules[0] = null;
            modulesNames[0] = "None";
            int i = 1;
            foreach (Type type in moduleTypes)
            {
                modules[i] = type.AssemblyQualifiedName;
                modulesNames[i] = type.FullName.Replace(".", "/");
                i++;
            }

            SelectedType = Type.GetType(currentHandler());
        }

                
        
        public virtual void Draw(Rect position)
        {
            position.height = EditorGUIUtility.singleLineHeight;
            
            var current = currentHandler();

            var currentIndex = Array.FindIndex(modules, m => m == current);

            if (currentIndex == -1)
            {
                currentIndex = 0;
            }

            var newIndex = EditorGUI.Popup(position, label, currentIndex, modulesNames);

            if (newIndex != currentIndex)
            {
                selectHandler(modules[newIndex]);
                SelectedType = Type.GetType(currentHandler());
                OnSelectionChanged();
            }
        }
    }
}