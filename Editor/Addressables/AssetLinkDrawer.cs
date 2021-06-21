using System;
using UnityEditor;
using UnityEngine;
using System.Reflection;
using System.Collections.Generic;
using Swift.EditorUtils;
using Object = UnityEngine.Object;

namespace Swift.Core.Editor
{
    internal class AssetLinkDrawer : BaseLinkDrawer
    {
        private readonly bool allowCreation;
        
        public AssetLinkDrawer(System.Type type, FieldInfo fieldInfo = null, bool forceFlatHierarchy = false, bool allowCreation = true) 
            : base(type, fieldInfo, forceFlatHierarchy)
        {
            this.allowCreation = allowCreation;
        }

        protected override bool CanCreate
        {
            get
            {
                if (allowCreation == false)
                {
                    return false;
                }
                return type.IsAbstract == false
                       && type.IsGenericType == false
                       && (typeof(ScriptableObject).IsAssignableFrom(type) ||
                           typeof(MonoBehaviour).IsAssignableFrom(type));
            }
        }

        protected override IPromise<string> OnCreate()
        {
            Promise<string> result = Promise<string>.Create();

            Type linkType = fieldInfo.GetChildValueType();
            
            if (fieldInfo != null)
            {
                LinkFilterAttribute filter = fieldInfo.GetCustomAttribute<LinkFilterAttribute>();
                if (filter != null)
                {
                    if (filter.interfaceType.IsInterface)
                    {
                        TypeSelectorWindow.Open(filter.interfaceType, "Choose implementation").Done(selectedType =>
                        {
                            result.Resolve(CreateAsset(selectedType, linkType, fieldInfo));
                        });
                    }
                    else
                    {
                        result.Resolve(CreateAsset(filter.interfaceType, linkType, fieldInfo));
                    }
                }
                else
                {
                    result.Resolve(CreateAsset(type, linkType, fieldInfo));
                }
            }
            else
            {
                result.Resolve(CreateAsset(type, linkType, fieldInfo));
            }

            return result;
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

                        if (type == typeof(MonoBehaviour))
                        {
                            return ((GameObject) asset).GetComponent(interfaceFilter.interfaceType) == null;
                        }
                        
                        return interfaceFilter.interfaceType.IsInstanceOfType(asset) == false;
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
