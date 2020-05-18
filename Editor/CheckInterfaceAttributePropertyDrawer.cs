using System;
using System.Collections.Generic;
using System.Linq;
using SwiftFramework.EditorUtils;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

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
            return property.type == "PPtr<$GameObject>";
        }

        protected override string InvalidTypeMessage => "Invalid type. Only GameObject supported!";

        protected override IEnumerable<Type> GetInterfaces(SerializedProperty property)
        {
            CheckInterfaceAttribute hasInterfaceAttribute = attribute as CheckInterfaceAttribute;
            return hasInterfaceAttribute?.interfaces;
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
            yield return filed?.InterfaceType;
        }

        protected override SerializedProperty GetGameObjectProperty(SerializedProperty property)
        {
            return property.FindPropertyRelative("target");
        }
    }

    public abstract class BaseInterfaceComponentFieldPropertyDrawer : PropertyDrawer
    {
        public static event Action<GameObject, Type> OnTryToConvertToWrapper = (g, t) => { };

        private bool triedToAutoConvert;
        private bool triedToAutoFind;
        private const float LINE_HEIGHT = 16;
        private const float VALID_MARK_WIDTH = 150;
        private const float MARGIN = 3;


        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (IsValidType(property) == false)
            {
                return base.GetPropertyHeight(property, label) + LINE_HEIGHT + MARGIN + MARGIN;
            }

            GameObject go = GetGameObject(property);
            return IsValid(go, property)
                ? base.GetPropertyHeight(property, label)
                : base.GetPropertyHeight(property, label) +
                  ((LINE_HEIGHT + MARGIN) * GetInterfaces(property).CountFast()) + MARGIN;
        }

        protected abstract IEnumerable<Type> GetInterfaces(SerializedProperty property);

        private bool IsValid(GameObject go, SerializedProperty property)
        {
            if (go == null)
            {
                return false;
            }

            foreach (Type @interface in GetInterfaces(property))
            {
                if (go.GetComponent(@interface) == null)
                {
                    return false;
                }
            }

            return true;
        }

        private void FindInAssets(SerializedProperty property, SerializedProperty gameObjectProperty)
        {
            foreach (GameObject other in Util.GetAssets<GameObject>())
            {
                if (IsValid(other, property))
                {
                    gameObjectProperty.objectReferenceValue = other;
                    gameObjectProperty.serializedObject.ApplyModifiedProperties();
                    return;
                }
            }
        }

        private bool FindInScene(SerializedProperty property, SerializedProperty gameObjectProperty)
        {
            if (PrefabStageUtility.GetCurrentPrefabStage() != null)
            {
                foreach (Transform other in PrefabStageUtility.GetCurrentPrefabStage().prefabContentsRoot
                    .GetComponentsInChildren<Transform>(true))
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

            foreach (GameObject other in Object.FindObjectsOfType<GameObject>())
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

        private void FindInChildren(SerializedProperty property, SerializedProperty gameObjectProperty)
        {
            MonoBehaviour root = property.serializedObject.targetObject as MonoBehaviour;

            if (root == null)
            {
                return;
            }

            foreach (Transform other in root.GetComponentInChildren<Transform>(true))
            {
                if (IsValid(other.gameObject, property))
                {
                    gameObjectProperty.objectReferenceValue = other.gameObject;
                    gameObjectProperty.serializedObject.ApplyModifiedProperties();
                    return;
                }
            }
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
                position.height = LINE_HEIGHT;
                EditorGUI.PropertyField(new Rect(position.x, position.y, position.width - MARGIN, LINE_HEIGHT),
                    property);
                GUI.enabled = true;
                position.y += LINE_HEIGHT + MARGIN;
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
            originalRect.height = LINE_HEIGHT;

            position.width -= VALID_MARK_WIDTH;
            position.height = LINE_HEIGHT;

            bool isValid = IsValid(go, property);

            if (go == null)
            {
                EditorGUI.PropertyField(new Rect(position.x, position.y, position.width - MARGIN, position.height),
                    gameObjectProperty, label);
                Rect buttonRect = originalRect;

                buttonRect.width = VALID_MARK_WIDTH / 3;
                buttonRect.x += originalRect.width - VALID_MARK_WIDTH;
                if (GUI.Button(buttonRect, "Scene"))
                {
                    FindInScene(property, gameObjectProperty);
                }

                buttonRect.x += VALID_MARK_WIDTH / 3;
                if (GUI.Button(buttonRect, "Assets"))
                {
                    FindInAssets(property, gameObjectProperty);
                }

                buttonRect.x += VALID_MARK_WIDTH / 3;
                if (GUI.Button(buttonRect, "Child"))
                {
                    FindInChildren(property, gameObjectProperty);
                }
            }
            else
            {
                EditorGUI.PropertyField(
                    new Rect(position.x, position.y, isValid ? position.width - MARGIN : originalRect.width,
                        position.height), gameObjectProperty, label);
            }

            position.y += LINE_HEIGHT;

            if (isValid == false)
            {
                var interfaces = GetInterfaces(property).ToArray();

                if (triedToAutoConvert == false && go != null)
                {
                    triedToAutoConvert = true;
                    OnTryToConvertToWrapper(go, interfaces.FirstOrDefaultFast());
                }

                position.y += MARGIN;
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
                    position.y += LINE_HEIGHT + MARGIN;
                    GUI.color = color;
                }
            }
            else
            {
                Color c = GUI.color;
                GUI.color = Color.green;
                GUI.enabled = false;
                GUI.Button(new Rect(originalRect.x + position.width, originalRect.y, VALID_MARK_WIDTH, LINE_HEIGHT),
                    "Valid");
                GUI.enabled = true;
                GUI.color = c;
            }
        }
    }
}