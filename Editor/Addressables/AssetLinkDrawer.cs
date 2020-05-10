using UnityEditor;
using UnityEngine;
using System.Reflection;
using System.Collections.Generic;
using SwiftFramework.EditorUtils;

namespace SwiftFramework.Core.Editor
{
    internal class AssetLinkDrawer : BaseLinkDrawer
    {
        public AssetLinkDrawer(System.Type type, FieldInfo fieldInfo = null, bool forceFlatHierarchy = false) : base(type, fieldInfo, forceFlatHierarchy)
        {

        }

        protected override bool CanCreate => type.IsAbstract == false
            && type.IsGenericType == false
            && (typeof(ScriptableObject).IsAssignableFrom(type) || typeof(MonoBehaviour).IsAssignableFrom(type));

        protected override IPromise<string> OnCreate()
        {
            return Promise<string>.Resolved(CreateAsset(type, fieldInfo.GetChildValueType()));
        }

        private class Sorter : IComparer<string>
        {
            public int Compare(string x, string y)
            {
                return CompareName(x, y);
            }
        }

        protected override void Reload()
        {
            assets.Clear();


#if USE_ADDRESSABLES
            assets.AddRange(AddrHelper.GetAssets(type));
#else
            assets.AddRange(ResourcesAssetHelper.GetAssets(type));
#endif

            if (fieldInfo != null)
            {
                LinkFilterAttribute interfaceFilter = fieldInfo.GetCustomAttribute<LinkFilterAttribute>();

                if (interfaceFilter != null)
                {
                    assets.RemoveAll(t =>
                    {
                        Object asset = AssetDatabase.LoadAssetAtPath(t.AssetPath, typeof(Object));
                        return interfaceFilter.interfaceType.IsAssignableFrom(asset.GetType()) == false;
                    });
                }

                LinkTypeFilterAttribute typeFilter = fieldInfo.GetCustomAttribute<LinkTypeFilterAttribute>();

                if (typeFilter != null)
                {
                    assets.RemoveAll(t =>
                    {
                        Object asset = AssetDatabase.LoadAssetAtPath(t.AssetPath, typeof(Object));
                        return asset.GetType() != typeFilter.type;
                    });
                }
            }


        }

    }

}
