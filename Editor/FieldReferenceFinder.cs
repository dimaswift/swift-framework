using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Swift.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Swift.EditorUtils
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
        private const int MAX_RECURSION_DEPTH = 25;

        private static readonly List<Type> attributeFilters = new List<Type>();

        public static List<FieldData<T>> FindAllFields<T>(params Type[] attributes) where T : class
        {
            List<FieldData<T>> list = new List<FieldData<T>>();

            Object[] allAssets = Util.GetAllAssets().ToArray();

            attributeFilters.Clear();

            attributeFilters.AddRange(attributes);

            int total = allAssets.Count();

            if (total == 0)
            {
                return list;
            }

            int current = 0;

            foreach (Object asset in allAssets)
            {
                try
                {
                    switch (asset)
                    {
                        case ScriptableObject _:
                            CheckScriptableObject(asset, list);
                            break;
                        case GameObject _:
                            CheckPrefab(asset, list);
                            break;
                        case SceneAsset _:
                            CheckScene(asset, list);
                            break;
                    }

                    current++;

                    EditorUtility.DisplayProgressBar($"Collecting serialized {typeof(T).Name}...",
                        $"Asset checked: {current}/{total}", (float) current / total);
                }
                catch (Exception e)
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

            if (SceneManager.GetActiveScene() != scene)
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

            foreach (Component component in prefab.GetComponents<Component>())
            {
                if (component == null)
                {
                    continue;
                }

                Type type = component.GetType();

                IEnumerable<FieldInfo> fields = type.GetRuntimeFields();

                foreach (FieldInfo runTimeField in fields)
                {
                    FieldInfo field = FieldInfo.GetFieldFromHandle(runTimeField.FieldHandle);
                    if (field.GetCustomAttribute(typeof(SerializeField)) != null || field.IsPublic)
                    {
                        CheckFieldRecursively(asset, field, runTimeField.GetChildValueType(), field.GetValue(component),
                            list, 0);
                    }
                }
            }
        }

        private static void CheckScriptableObject<T>(Object asset, List<FieldData<T>> list) where T : class
        {
            Type type = asset.GetType();

            FieldInfo[] fields = type.GetFields();

            foreach (FieldInfo field in fields)
            {
                CheckFieldRecursively(asset, field, field.GetChildValueType(), field.GetValue(asset), list, 0);
            }
        }

        private static void TryAddFieldValue<T>(Object asset, SerializedProperty property,
            SerializedObject serializedObject, FieldInfo fieldInfo, object value, List<FieldData<T>> list)
            where T : class
        {
            if (value == null)
            {
                return;
            }

            if (value is T == false)
            {
                return;
            }

            T target = (T) value;

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

            foreach (Type attr in attributeFilters)
            {
                if (field.GetCustomAttribute(attr) == null)
                {
                    hasAllAttributes = false;
                    break;
                }
            }

            return hasAllAttributes;
        }

        private static bool IsValid(Type type)
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

        private static void CheckFieldRecursively<T>(Object targetAsset, FieldInfo fieldToCheck, Type type,
            object value, List<FieldData<T>> list, int depth) where T : class
        {
            if (IsValid(type) == false)
            {
                return;
            }

            if (depth > MAX_RECURSION_DEPTH)
            {
                return;
            }

            if (value == null)
            {
                return;
            }

            SerializedObject serializedObject = new SerializedObject(targetAsset);

            IEnumerable<FieldInfo> runTimeFields = type.GetRuntimeFields();

            IEnumerable<FieldInfo> fields = type.GetFields();
            foreach (FieldInfo field in fields)
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

                    TryAddFieldValue(targetAsset, serializedObject.FindProperty(field.Name), serializedObject, field,
                        fieldValue, list);
                    CheckFieldRecursively(targetAsset, field, field.FieldType, fieldValue, list, ++depth);
                }
            }

            foreach (FieldInfo runTimeField in runTimeFields)
            {
                if (type == runTimeField.FieldType)
                {
                    continue;
                }

                FieldInfo field = FieldInfo.GetFieldFromHandle(runTimeField.FieldHandle);

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

                    TryAddFieldValue(targetAsset, serializedObject.FindProperty(field.Name), serializedObject, field,
                        fieldValue, list);
                    CheckFieldRecursively(targetAsset, field, runTimeField.FieldType, fieldValue, list, ++depth);
                }
            }


            if (!(value is IEnumerable array))
            {
                TryAddFieldValue(targetAsset, serializedObject.FindProperty(fieldToCheck.Name), serializedObject,
                    fieldToCheck, value, list);
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

                    if ((element is string || element.GetType().IsPrimitive))
                    {
                        break;
                    }

                    SerializedProperty prop = serializedObject.FindProperty(fieldToCheck.Name);

                    if (prop != null)
                    {
                        TryAddFieldValue(targetAsset, prop.GetArrayElementAtIndex(i++), serializedObject, fieldToCheck,
                            element, list);
                    }

                    CheckFieldRecursively(targetAsset, fieldToCheck, element.GetType(), element, list, ++depth);
                }
            }
        }
    }
}