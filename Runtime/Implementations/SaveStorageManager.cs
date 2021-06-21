using Swift.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Swift.Core
{
    [DefaultModule]
    public class SaveStorageManager : Module, ISaveStorage
    {
        public SaveStorageManager(){}
        
        public long SaveTimestamp => container.timestamp;

        private const string NOT_LINKED = "not_linked";

        private const string DEFAULT_SAVE_ID = "default_save";

        private static string EditorSavePath => Application.dataPath + "/save.json";

        private Dictionary<string, Dictionary<string, object>> linkedItems = new Dictionary<string, Dictionary<string, object>>();

        private Dictionary<string, object> notLinkedItems;

        private string saveId;

        private SaveItemsContainer container = new SaveItemsContainer();

        public long GetSaveTimestamp(string rawSave)
        {
            try
            {
                SaveItemsContainer c = Json.Deserialize<SaveItemsContainer>(Json.Decompress(rawSave));
                return c.timestamp;
            }
            catch
            {
                return 0;
            }
          
        }

        public event Action OnBeforeSaveOnPause = () => { };

        public event Action OnBeforeSave = () => { };
        public event Action OnAfterLoad = () => { };
        public event Action OnRawSaveLoaded = () => { };

        
#if UNITY_EDITOR
        [UnityEditor.MenuItem("SwiftFramework/Delete Save")]
        private static void DeleteSave()
        {
            if (File.Exists(EditorSavePath))
            {
                UnityEditor.AssetDatabase.DeleteAsset("Assets/save.json");
                UnityEditor.AssetDatabase.Refresh();
            }
        }
#endif
        
        protected override IPromise GetInitPromise()
        {
            saveId = DEFAULT_SAVE_ID;

            App.Boot.OnFocused += OnFocusChanged;
            
            App.Boot.OnPaused += BootOnOnPaused;

            LoadFromPlayerPrefs();

            return base.GetInitPromise();
        }

        private void BootOnOnPaused()
        {
            OnBeforeSave();
            WriteSave();
        }

        private void LoadFromPlayerPrefs()
        {
            string json = PlayerPrefs.GetString(saveId, null);
#if UNITY_EDITOR
            json = File.Exists(EditorSavePath) ? File.ReadAllText(EditorSavePath) : null;
            if (json != null)
            {
                Debug.Log($"Original save file size: <b>{Encoding.ASCII.GetByteCount(json).ToFileSize() }</b>, compressed file size: <color=green><b>{Encoding.ASCII.GetByteCount(Json.Compress(json)).ToFileSize()}</b></color>");
            }
#endif

            if (Application.isEditor == false)
            {
                try
                {
                    json = Json.Decompress(json);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
            
            if (string.IsNullOrEmpty(json) == false)
            {
                Parse(json);
            }
            else
            {
                if (linkedItems.ContainsKey(NOT_LINKED) == false)
                {
                    linkedItems.Add(NOT_LINKED, new Dictionary<string, object>());
                }
            
                notLinkedItems = linkedItems[NOT_LINKED];
            }
        }

        private void Parse(string json)
        {
            container = Json.Deserialize<SaveItemsContainer>(json);

            linkedItems = new Dictionary<string, Dictionary<string, object>>();

            foreach (var linked in container.items)
            {
                if (linked == null || string.IsNullOrEmpty(linked.link) || linked.item == null)
                {
                    continue;
                }
                if (linkedItems.TryGetValue(linked.link, out Dictionary<string, object> dict) == false)
                {
                    dict = new Dictionary<string, object>();
                    linkedItems.Add(linked.link, dict);
                }
                string itemKey = linked.item.GetType().FullName;
                if (dict.ContainsKey(itemKey) == false)
                {
                    dict.Add(itemKey, linked.item);
                }
            }
            
            if (linkedItems.ContainsKey(NOT_LINKED) == false)
            {
                linkedItems.Add(NOT_LINKED, new Dictionary<string, object>());
            }
            
            notLinkedItems = linkedItems[NOT_LINKED];
            
            OnAfterLoad();
        }

        private void OnFocusChanged(bool focus)
        {
            if (focus == false)
            {
                OnBeforeSaveOnPause();
                WriteSaveToDisk();
            }
        }

        public void WriteSaveToDisk()
        {
            OnBeforeSave();
            WriteSave();
        }
        
        private void WriteSave()
        {
            container.id = saveId;
            container.timestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
            container.version++;
            container.items = new List<LinkedItem>();

            foreach (var linked in linkedItems)
            {
                foreach (var item in linked.Value)
                {
                    container.items.Add(new LinkedItem() { link = linked.Key, item = item.Value });
                }
            }

            string save = GetRawCompressedSave();
            PlayerPrefs.SetString(saveId, save);
            PlayerPrefs.Save();

#if UNITY_EDITOR
            File.WriteAllText(EditorSavePath, Json.Serialize(container, Newtonsoft.Json.Formatting.Indented));
#endif
        }

        public string GetRawCompressedSave()
        {
            container.timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return Json.Compress(Json.Serialize(container));
        }
        
        public T Load<T>()
        {
            return Load<T>(notLinkedItems);
        }

        public void LoadRawSave(string rawCompressedSave)
        {
            Parse(Json.Decompress(rawCompressedSave));
            OnRawSaveLoaded();
        }

        public void Save<T>(T data)
        {
            Save(data, notLinkedItems);
        }

        private T Load<T>(Dictionary<string, object> dict)
        {
            string type = typeof(T).FullName;
            if (dict.TryGetValue(type, out object value))
            {
                return (T)value;
            }
            return default;
        }

        private void Save<T>(T data, Dictionary<string, object> dict)
        {
            string type = typeof(T).FullName;
            if (dict.ContainsKey(type) == false)
            {
                dict.Add(type, data);
            }
            dict[type] = data;
        }

        public bool Exists<T>()
        {
            return Exists(typeof(T));
        }

        public bool Exists(Type type)
        {
            return notLinkedItems.ContainsKey(type.FullName);
        }

        public void SetSaveId(string id)
        {
            if (id != saveId)
            {
                saveId = id;
                LoadFromPlayerPrefs();
            }
        }

        public void RegisterState<T>(Func<T> state)
        {
            OnBeforeSave += () => 
            {
                Save(state());
            };
        }

        public void RegisterState<T>(Func<T> state, ILink link)
        {
            OnBeforeSave += () =>
            {
                Save(state(), link);
            };
        }

        public void RegisterState<T>(out T existingState, ILink link, Func<T> state) where T : new()
        {
            existingState = LoadOrCreateNew<T>(link);
            OnBeforeSave += () =>
            {
                Save(state(), link);
            };
        }
        
        public void RegisterState<T>(out T existingState, Func<T> state) where T : new()
        {
            existingState = LoadOrCreateNew<T>();
            OnBeforeSave += () =>
            {
                Save(state());
            };
        }
        
        public void RegisterState<T>(out T existingState, Func<T> state, Func<T> defaultState) where T : new()
        {
            existingState = LoadOrCreateNew(defaultState);
            OnBeforeSave += () =>
            {
                Save(state());
            };
        }
        
        public void RegisterState<T>(out T existingState, ILink link, Func<T> state,  Func<T> defaultState) where T : new()
        {
            existingState = LoadOrCreateNew(link, defaultState);
            OnBeforeSave += () =>
            {
                Save(state(), link);
            };
        }

        
        public void Save<T>(T data, ILink link)
        {
            string path = link.GetPath();
            if (linkedItems.ContainsKey(path) == false)
            {
                linkedItems.Add(path, new Dictionary<string, object>());
            }
            if (linkedItems.TryGetValue(path, out Dictionary<string, object> dict))
            {
                Save(data, dict);
            }
        }

        public T Load<T>(ILink link)
        {
            if (linkedItems.TryGetValue(link.GetPath(), out Dictionary<string, object> dict))
            {
                return Load<T>(dict);
            }
            return default;
        }

        public T LoadOrCreateNew<T>(ILink link) where T : new()
        {
            T value = Load<T>(link);
            if(value != null)
            {
                return value;
            }
            return new T();
        }

        public T LoadOrCreateNew<T>() where T : new()
        {
            T value = Load<T>();

            if (value != null)
            {
                return value;
            }

            return new T();
        }

        public T LoadOrCreateNew<T>(Func<T> defaultValue)
        {
            T value = Load<T>();

            if (value != null)
            {
                return value;
            }

            return defaultValue();
        }

        public T LoadOrCreateNew<T>(ILink link, Func<T> defaultValue)
        {
            T value = Load<T>(link);
            if (value != null)
            {
                return value;
            }
            return defaultValue();
        }

        public bool Exists<T>(ILink link)
        {
            return Exists(typeof(T), link);
        }

        public bool Exists(Type type, ILink link)
        {
            if (linkedItems.TryGetValue(link.GetPath(), out Dictionary<string, object> dict))
            {
                return dict.ContainsKey(type.FullName);
            }
            return false;
        }

        public void Delete<T>()
        {
            Delete(typeof(T));
        }

        public void Delete(Type type)
        {
            if (Exists(type))
            {
                notLinkedItems.Remove(type.FullName);
            }
        }

        public void Delete<T>(ILink link)
        {
            Delete(typeof(T), link);
        }

        public void DeleteAll(ILink link)
        {
            if (linkedItems.TryGetValue(link.GetPath(), out Dictionary<string, object> dict))
            {
                linkedItems.Remove(link.GetPath());
            }
        }

        public void Delete(Type type, ILink link)
        {
            if (linkedItems.TryGetValue(link.GetPath(), out Dictionary<string, object> dict))
            {
                if (dict.ContainsKey(type.FullName))
                {
                    dict.Remove(type.FullName);
                }
            }
        }

        public T OverwriteScriptable<T>(T source) where T : ScriptableObject
        {
            if (Exists<T>())
            {
                T obj = Load<T>();
                var json = JsonUtility.ToJson(obj);
                JsonUtility.FromJsonOverwrite(json, source);
            }
            return source;
        }

        public void DeleteAll()
        {
#if UNITY_EDITOR
            File.Delete(EditorSavePath);
#endif
            container = new SaveItemsContainer();
            notLinkedItems.Clear();
            linkedItems.Clear();
            OnBeforeSave = () => { };
            OnAfterLoad = () => { };
            WriteSave();
        }


        [Serializable]
        private class SaveItemsContainer
        {
            public int version;
            public long timestamp;
            public string id;
            public List<LinkedItem> items = new List<LinkedItem>();
        }

        [Serializable]
        private class LinkedItem
        {
            public string link;
            public object item;
        }
    }
}
