﻿using System;
using UnityEngine;

namespace Swift.Core
{
    [ModuleGroup(ModuleGroups.Core)]
    [BuiltInModule]
    public interface ISaveStorage : IModule
    {
        long GetSaveTimestamp(string rawSave);
        event Action OnBeforeSave;
        event Action OnAfterLoad;
        event Action OnRawSaveLoaded;
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
        void DeleteAll(ILink link);
        void Delete(Type type, ILink link);
        string GetRawCompressedSave();
        void LoadRawSave(string rawCompressedSave);
        void RegisterState<T>(Func<T> state);
        void RegisterState<T>(out T existingState, Func<T> state) where T : new();
        void RegisterState<T>(out T existingState, Func<T> state, Func<T> defaultState) where T : new();
        void RegisterState<T>(out T existingState, ILink link, Func<T> state, Func<T> defaultState) where T : new();
        void RegisterState<T>(out T existingState, ILink link, Func<T> state) where T : new();
        void RegisterState<T>(Func<T> state, ILink link);
        void DeleteAll();
        long SaveTimestamp { get; }
        void WriteSaveToDisk();
    }
}