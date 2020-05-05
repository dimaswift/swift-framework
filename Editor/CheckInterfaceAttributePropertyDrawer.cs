using NUnit.Framework;
using SwiftFramework.EditorUtils;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SwiftFramework.Core.Editor
{
    [CustomPropertyDrawer(typeof(CheckInterfaceAttribute), true)]
    internal class CheckInterfaceAttributePropertyDrawer : BaseInterfaceComponentFieldPropertyDrawer
    {
        protected override GameObject GetGameObject(SerializedProperty property)
        {
            if (property.objectReferenceValue == null)
            {
                return null;
            }
            return property.objectReferenceValue as GameObject;
        }

        protected override bool IsValidType(SerializedProperty property)
        {
            if (property.type == "PPtr<$GameObject>")
            {
                return true;
            }

            return false;
        }

        protected override string InvalidTypeMessage => "Invalid type. Only GameObject supported!";

        protected override IEnumerable<Type> GetInterfaces(SerializedProperty property)
        {
            CheckInterfaceAttribute hasInterfaceAttribute = attribute as CheckInterfaceAttribute;
            return hasInterfaceAttribute.interfaces;
        }
    }

    [CustomPropertyDrawer(typeof(InterfaceComponentField), true)]
    public class InterfaceComponentFieldDrawer : BaseInterfaceComponentFieldPropertyDrawer
    {
        protected override GameObject GetGameObject(SerializedProperty property)
        {
            return GetGameObjectProperty(property).objectReferenceValue as GameObject;
        }

        protected override bool IsValidType(SerializedProperty property)
        {
            return true;
        }

        protected override IEnumerable<Type> GetInterfaces(SerializedProperty property)
        {
            InterfaceComponentField filed = Util.GetTargetObjectOfProperty(property) as InterfaceComponentField;
            yield return filed.InterfaceType;
        }

        protected override SerializedProperty GetGameObjectProperty(SerializedProperty property)
        {
            return property.FindPropertyRelative("target");
        }
    }

    public abstract class BaseInterfaceComponentFieldPropertyDrawer : PropertyDrawer
    {
        public static event Action<GameObject, Type> OnTryToConvertToWrapper = (g, t) => { };

        protected bool triedToAutoConvert;
        protected bool triedToAutoFind = false;
        protected const float lineHeight = 16;
        protected const float validMarkWidth = 150;
        protected const float margin = 3;


        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (IsValidType(property) == false)
            {
                return base.GetPropertyHeight(property, label) + lineHeight + margin + margin;
            }
            GameObject go = GetGameObject(property);
            return IsValid(go, property) ? base.GetPropertyHeight(property, label) : base.GetPropertyHeight(property, label) + ((lineHeight + margin) * GetInterfaces(property).CountFast()) + margin;
        }

        protected abstract IEnumerable<Type> GetInterfaces(SerializedProperty property);

        private bool IsValid(GameObject go, SerializedProperty property)
        {
            if (go == null)
            {
                return false;
            }

            foreach (var @interface in GetInterfaces(property))
            {
                if (go.GetComponent(@interface) == null)
                {
                    return false;
                }
            }

            return true;
        }

        protected bool FindInAssets(SerializedProperty property, SerializedProperty gameObjectProperty)
        {
            foreach (GameObject other in Util.GetAssets<GameObject>())
            {
                if (IsValid(other, property))
                {
                    gameObjectProperty.objectReferenceValue = other;
                    gameObjectProperty.serializedObject.ApplyModifiedProperties();
                    return true;
                }
            }
            return false;
        }

        protected bool FindInScene(SerializedProperty property, SerializedProperty gameObjectProperty)
        {
            if (UnityEditor.Experimental.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage() != null)
            {
                foreach (Transform other in UnityEditor.Experimental.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage().prefabContentsRoot.GetComponentsInChildren<Transform>(true))
                {
                    if (IsValid(other.gameObject, property))
                    {
                        gameObjectProperty.objectReferenceValue = other.gameObject;
                        gameObjectProperty.serializedObject.ApplyModifiedProperties();
                        return true;
                    }
                }
                return false;
            }
            foreach (GameObject other in UnityEngine.Object.FindObjectsOfType<GameObject>())
            {
                if (IsValid(other, property))
                {
                    gameObjectProperty.objectReferenceValue = other;
                    gameObjectProperty.serializedObject.ApplyModifiedProperties();
                    return true;
                }
            }
            return false;
        }

        protected bool FindInChildren(SerializedProperty property, SerializedProperty gameObjectProperty)
        {
            MonoBehaviour root = property.serializedObject.targetObject as MonoBehaviour;

            if (!root)
            {
                return false;
            }
            foreach (Transform other in root.GetComponentInChildren<Transform>(true))
            {
                if (IsValid(other.gameObject, property))
                {
                    gameObjectProperty.objectReferenceValue = other.gameObject;
                    gameObjectProperty.serializedObject.ApplyModifiedProperties();
                    return true;
                }
            }
            return false;
        }

        protected abstract GameObject GetGameObject(SerializedProperty property);

        protected abstract bool IsValidType(SerializedProperty property);

        protected virtual string InvalidTypeMessage => "Invalid type!";

        protected virtual InterfaceSearch GetAutoSearch() => InterfaceSearch.None;

        protected virtual SerializedProperty GetGameObjectProperty(SerializedProperty property)
        {
            return property;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (IsValidType(property) == false)
            {
                GUI.enabled = false;
                position.height = lineHeight;
                EditorGUI.PropertyField(new Rect(position.x, position.y, position.width - margin, lineHeight), property);
                GUI.enabled = true;
                position.y += lineHeight + margin;
                EditorGUI.HelpBox(position, InvalidTypeMessage, MessageType.Error);

                return;
            }

            InterfaceSearch search = GetAutoSearch();

            SerializedProperty gameObjectProperty = GetGameObjectProperty(property);

            if (triedToAutoFind == false && gameObjectProperty.objectReferenceValue == null)
            {
                if (search.HasFlag(InterfaceSearch.Scene))
                {
                    if (FindInScene(property, gameObjectProperty))
                    {
                        triedToAutoFind = true;
                    }
                }

                if (triedToAutoFind == false && search.HasFlag(InterfaceSearch.Assets))
                {
                    FindInAssets(property, gameObjectProperty);
                }

                triedToAutoFind = true;
            }

            GameObject go = GetGameObject(property);

            Rect originalRect = position;
            originalRect.height = lineHeight;

            position.width -= validMarkWidth;
            position.height = lineHeight;

            bool isValid = IsValid(go, property);

            if (go == null)
            {
                EditorGUI.PropertyField(new Rect(position.x, position.y, position.width - margin, position.height), gameObjectProperty, label);
                Rect buttonRect = originalRect;

                buttonRect.width = validMarkWidth / 3;
                buttonRect.x += originalRect.width - validMarkWidth;
                if (GUI.Button(buttonRect, "Scene"))
                {
                    FindInScene(property, gameObjectProperty);
                }
                buttonRect.x += validMarkWidth / 3;
                if (GUI.Button(buttonRect, "Assets"))
                {
                    FindInAssets(property, gameObjectProperty);
                }
                buttonRect.x += validMarkWidth / 3;
                if (GUI.Button(buttonRect, "Child"))
                {
                    FindInChildren(property, gameObjectProperty);
                }
            }
            else
            {
                EditorGUI.PropertyField(new Rect(position.x, position.y, isValid ? position.width - margin : originalRect.width, position.height), gameObjectProperty, label);
            }

            position.y += lineHeight;

            if (isValid == false)
            {
                var interfaces = GetInterfaces(property);

                if (triedToAutoConvert == false && go != null)
                {
                    triedToAutoConvert = true;
                    OnTryToConvertToWrapper(go, interfaces.FirstOrDefaultFast());
                }
                position.y += margin;
                foreach (Type @interface in interfaces)
                {
                    Color color = GUI.color;
                    bool hasInterface = go != null && go.GetComponent(@interface);
                    if (go != null)
                    {
                        GUI.color = hasInterface ? Color.green : Color.red;
                    }
                    position.width = originalRect.width;
                    EditorGUI.HelpBox(position, @interface.Name, hasInterface ? MessageType.Info : MessageType.Error);
                    position.y += lineHeight + margin;
                    GUI.color = color;
                }
            }
            else
            {
                Color c = GUI.color;
                GUI.color = Color.green;
                GUI.enabled = false;
                GUI.Button(new Rect(originalRect.x + position.width, originalRect.y, validMarkWidth, lineHeight), "Valid");
                GUI.enabled = true;
                GUI.color = c;
            }
        }
    }
}