using System;
using UnityEngine;

namespace SwiftFramework.Core
{
    [ModuleGroup(ModuleGroups.Core)]
    [BuiltInModule]
    public interface ISaveStorage : IModule
    {
        event Action OnBeforeSave;
        event Action OnAfterLoad;
        void SetSaveId(string id);
        void Save<T>(T data);
        void Save<T>(T data, ILink link);
        T Load<T>();
        T Load<T>(ILink link);
        T LoadOrCreateNew<T>() where T : new();
        T OverwriteScriptable<T>(T obj) where T : ScriptableObject;
        T LoadOrCreateNew<T>(Func<T> defaultValue);
        T LoadOrCreateNew<T>(ILink link) where T : new();
        T LoadOrCreateNew<T>(ILink link, Func<T> defaultValue);
        bool Exists<T>();
        bool Exists(Type type);
        bool Exists<T>(ILink link);
        bool Exists(Type type, ILink link);
        void Delete<T>();
        void Delete(Type type);
        void Delete<T>(ILink link);
        void Delete(Type type, ILink link);
        string GetSaveJson();
        void OverrideSaveJson(string saveJson);
        void RegisterState<T>(Func<T> state);
        void RegisterState<T>(Func<T> state, ILink link);
        void DeleteAll();
    }
}
