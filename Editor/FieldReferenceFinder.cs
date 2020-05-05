using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Reflection;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System.Collections;
using System.Linq;
using SwiftFramework.Core;

namespace SwiftFramework.EditorUtils
{
    internal class FieldData<T> where T : class
    {
        public T value;
        public Object asset;
        public SerializedObject serializedObject;
        public FieldInfo field;
        public SerializedProperty property;
    }

    internal static class FieldReferenceFinder
    {
        public const int MaxRecursionDepth = 25;

        private static readonly List<System.Type> attributeFilters = new List<System.Type>();

        public static List<FieldData<T>> FindAllFields<T>(params System.Type[] attributes) where T : class
        {
            List<FieldData<T>> list = new List<FieldData<T>>();

            IEnumerable<Object> allAssets = Util.GetAllAssets();

            attributeFilters.Clear();

            attributeFilters.AddRange(attributes);

            int total = allAssets.Count();

            if(total == 0)
            {
                return list;
            }

            int current = 0;

            foreach (Object asset in allAssets)
            {
                try
                {
                    if (asset is ScriptableObject)
                    {
                        CheckScriptableObject(asset, list);
                    }
                    else if (asset is GameObject)
                    {
                        CheckPrefab(asset, list);
                    }
                    else if (asset is SceneAsset)
                    {
                        CheckScene(asset, list);
                    }

                    current++;

                    EditorUtility.DisplayProgressBar($"Collecting serialized {typeof(T).Name}...", $"Asset checked: {current}/{total}", (float)current / total);

                }
                catch (System.Exception e)
                {
                    Debug.LogException(e);
                    EditorUtility.ClearProgressBar();
                }
            }

            EditorUtility.ClearProgressBar();

            return list;
        }


        private static void CheckScene<T>(Object asset, List<FieldData<T>> list) where T : class
        {
            Scene scene = EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(asset), OpenSceneMode.Additive);

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                CheckPrefab(root, list);
                foreach (Transform child in root.GetComponentInChildren<Transform>(true))
                {
                    CheckPrefab(child.gameObject, list);
                }
            }
            if(EditorSceneManager.GetActiveScene() != scene)
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void CheckPrefab<T>(Object asset, List<FieldData<T>> list) where T : class
        {
            GameObject prefab = asset as GameObject;

            if (prefab == null)
            {
                return;
            }

            foreach (var component in prefab.GetComponents<Component>())
            {
                if (component == null)
                {
                    continue;
                }

                System.Type type = component.GetType();

                IEnumerable<FieldInfo> fields = type.GetRuntimeFields();

                foreach (FieldInfo runTimeField in fields)
                {
                    var field = FieldInfo.GetFieldFromHandle(runTimeField.FieldHandle);
                    if (field.GetCustomAttribute(typeof(SerializeField)) != null || field.IsPublic)
                    {
                        CheckFieldRecursively(asset, field, runTimeField.GetChildValueType(), field.GetValue(component), list, 0);
                    }
                }
            }
        }

        private static void CheckScriptableObject<T>(Object asset, List<FieldData<T>> list) where T : class
        {
            System.Type type = asset.GetType();

            FieldInfo[] fields = type.GetFields();

            foreach (FieldInfo field in fields)
            {
                CheckFieldRecursively(asset, field, field.GetChildValueType(), field.GetValue(asset), list, 0);
            }
        }

        private static void TryAddFieldValue<T>(Object asset, SerializedProperty property, SerializedObject serializedObject, FieldInfo fieldInfo, object value, List<FieldData<T>> list) where T : class
        {
            if (value == null)
            {
                return;
            }

            if (value is T == false)
            {
                return;
            }

            T target = value as T;

            list.Add(new FieldData<T>()
            {
                asset = asset,
                field = fieldInfo,
                serializedObject = serializedObject,
                value = target,
                property = property
            });

        }

        private static bool HasAllAttributes(FieldInfo field)
        {
            bool hasAllAttributes = true;

            foreach (System.Type attr in attributeFilters)
            {
                if (field.GetCustomAttribute(attr) == null)
                {
                    hasAllAttributes = false;
                    break;
                }
            }

            return hasAllAttributes;
        }

        private static bool IsValid (System.Type type)
        {
            if (type.IsValueType || type.IsPrimitive || type.IsEnum || type == typeof(string))
            {
                return false;
            }

            if (type.IsArray && IsValid(type.GetElementType()) == false)
            {
                return false;
            }

            return true;
        }

        private static void CheckFieldRecursively<T>(Object targetAsset, FieldInfo fieldToCheck, System.Type type, object value, List<FieldData<T>> list, int depth) where T : class
        {
            if (IsValid(type) == false)
            {
                return;
            }
            if (depth > MaxRecursionDepth)
            {
                //Debug.LogWarning($"Max recursion depth reached on asset: {targetAsset.name} while checking type {type.Name}");
                return;
            }
            if (value == null)
            {
                return;
            }
            SerializedObject serializedObject = new SerializedObject(targetAsset);
          
            IEnumerable<FieldInfo> runTimeFields = type.GetRuntimeFields();

            IEnumerable<FieldInfo> fields = type.GetFields();
            foreach (var field in fields)
            {
                if (type == field.FieldType)
                {
                    continue;
                }

                if (HasAllAttributes(field))
                {
                    object fieldValue = null;
                    try
                    {
                        fieldValue = field.GetValue(value);
                    }
                    catch
                    {
                        continue;
                    }
                    TryAddFieldValue(targetAsset, serializedObject.FindProperty(field.Name), serializedObject, field, fieldValue, list);
                    CheckFieldRecursively(targetAsset, field, field.FieldType, fieldValue, list, ++depth);
                }
            }
            foreach (var runTimeField in runTimeFields)
            {
                if (type == runTimeField.FieldType)
                {
                    continue;
                }

                var field = FieldInfo.GetFieldFromHandle(runTimeField.FieldHandle);

                if (HasAllAttributes(field))
                {
                    object fieldValue = null;
                    try
                    {
                        fieldValue = field.GetValue(value);
                    }
                    catch
                    {
                        continue;
                    }
                    TryAddFieldValue(targetAsset, serializedObject.FindProperty(field.Name), serializedObject, field, fieldValue, list);
                    CheckFieldRecursively(targetAsset, field, runTimeField.FieldType, fieldValue, list, ++depth);
                }
            }
            


            IEnumerable array = value as IEnumerable;
        
            if (array == null)
            {
                TryAddFieldValue(targetAsset, serializedObject.FindProperty(fieldToCheck.Name), serializedObject, fieldToCheck, value, list);
            }
            else
            {
                int i = 0;
                foreach (object element in array)
                {
                    if (element == null)
                    {
                        continue;
                    }
                    if(element != null && (element is string || element.GetType().IsPrimitive))
                    {
                        break;
                    }

                    SerializedProperty prop = serializedObject.FindProperty(fieldToCheck.Name);

                    if(prop != null)
                    {
                        TryAddFieldValue(targetAsset, prop.GetArrayElementAtIndex(i++), serializedObject, fieldToCheck, element, list);
                    }

                    CheckFieldRecursively(targetAsset, fieldToCheck, element.GetType(), element, list, ++depth);
                }
            }
        }

    }
}
