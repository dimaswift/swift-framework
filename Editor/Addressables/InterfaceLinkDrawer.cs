using UnityEditor;
using UnityEngine;
using System.Reflection;
using SwiftFramework.Core;
using System.Collections.Generic;
using SwiftFramework.EditorUtils;

namespace SwiftFramework.Core.Editor
{
    internal class InterfaceLinkDrawer : BaseLinkDrawer
    {
        public InterfaceLinkDrawer(System.Type interfaceType, FieldInfo fieldInfo = null) : base(interfaceType, fieldInfo)
        {
        }

        protected override bool CanCreate => true;

        protected override IPromise<string> OnCreate()
        {
            Promise<string> promise = Promise<string>.Create();

            List<System.Type> typesToChoose = new List<System.Type>(Util.GetAllTypes(t => 
            t.IsGenericType == false && 
            t.IsAbstract == false && 
            t != type &&
            type.IsAssignableFrom(t) &&
            typeof(Object).IsAssignableFrom(t)));

            TypeSelectorWindow.Open(typesToChoose, $"Choose {type.Name} implementation").Done(selectedType =>
            {
                promise.Resolve(CreateAsset(selectedType, fieldInfo.GetChildValueType()));
            });

            return promise; 
        }


        protected override void Reload()
        {
            assets.Clear();

#if USE_ADDRESSABLES
            if (fieldInfo.GetChildValueType().BaseType.Name.Contains("LinkToScriptable"))
            {
                assets.AddRange(AddrHelper.GetScriptableObjectsWithInterface(type));
            }
            else
            {
                assets.AddRange(AddrHelper.GetPrefabsWithComponent(type));
            }

#else
            if (fieldInfo.GetChildValueType().BaseType.Name.Contains("LinkToScriptable"))
            {
                assets.AddRange(AssetHelper.GetScriptableObjectsWithInterface(type));
            }
            else
            {
                assets.AddRange(AssetHelper.GetPrefabsWithComponent(type));
            }
#endif

            if (fieldInfo != null)
            {
                LinkFilterAttribute interfaceFilter = fieldInfo.GetCustomAttribute<LinkFilterAttribute>();

                if (interfaceFilter != null)
                {
                    assets.RemoveAll(t =>
                    {
                        GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(t.AssetPath);
                        return go.GetComponent(interfaceFilter.interfaceType) == null;
                    });
                }

                LinkTypeFilterAttribute typeFilter = fieldInfo.GetCustomAttribute<LinkTypeFilterAttribute>();

                if (typeFilter != null)
                {
                    assets.RemoveAll(t =>
                    {
                        GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(t.AssetPath);
                        return go.GetComponent(typeFilter.type) != null;
                    });
                }
            }

        }
    }

}
