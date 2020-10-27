using UnityEngine;
using SwiftFramework.Core;
using System;
using System.IO;
using System.Collections.Generic;

namespace SwiftFramework.Core
{
    [DefaultModule]
    [Configurable(typeof(LocalizationConfig))]
    public class LocalizationManager : Module, ILocalizationManager
    {
        public LocalizationManager(ModuleConfigLink configLink)
        {
            SetConfig(configLink);
        }
        
        public LocalizationManager()
        {

        }
        
        public SystemLanguage CurrentLanguage { get; private set; } = SystemLanguage.English;

        private Promise downloadPromise = null;
        
        private readonly List<LocalizationSheet> sheets = new List<LocalizationSheet>();

        private readonly HashSet<string> warnings = new HashSet<string>();
        
        private readonly Dictionary<SystemLanguage, Dictionary<string, string>> dict 
            = new Dictionary<SystemLanguage, Dictionary<string, string>>();

        private LocalizationConfig config;
        
        protected override IPromise GetInitPromise()
        {
            downloadPromise = Promise.Create();
            config = GetModuleConfig<LocalizationConfig>();
            SetLanguage();
            sheets.AddRange(AssetCache.GetAssets<LocalizationSheet>());
            LoadNextSheet(0);
            return downloadPromise;
        }

        private void LoadNextSheet(int current)
        {
            if (current >= sheets.Count)
            {
                downloadPromise.Resolve();
                return;
            }

            LocalizationSheet sheet = sheets[current];
            
            sheet.LoadSheet(rows =>  ParseSheet(rows, sheet.Separator)).Then(rows =>
            {
                try
                {
                    ParseSheet(rows, sheet.Separator);
                }
                catch (Exception e)
                {
                    Debug.LogError($"LocalizationManager: Error while parsing {sheet.name}: {e.Message}");
                }
                LoadNextSheet(++current);
                
            }).Catch(e =>
            {
                Debug.LogError($"LocalizationManager: Error while loading {sheet.name}: {e.Message}");
                LoadNextSheet(++current);
            });
        }
        
        public void SetLanguage(SystemLanguage language)
        {
            CurrentLanguage = language;
            OnLanguageChanged();
            App.Storage.Save(new SelectedLanguage() { language = language });
        }

        public event Action OnLanguageChanged = () => { };

        public string GetText(string key)
        {
            string result = GetText(key, CurrentLanguage, out bool success);

            if (success == false)
            {
                Warn($"Using default language for key <b>{key}</b>!");
                result = GetText(key, config.defaultLanguage, out success);
            }

            return result;
        }

        private void Warn(string message)
        {
            if (warnings.Contains(message))
            {
                return;
            }

            LogWarning(message);
            warnings.Add(message);
        }

        private string GetText(string key, SystemLanguage lang, out bool success)
        {
            if (string.IsNullOrEmpty(key))
            {
                success = false;
                return "#empty_localization_key";
            }

            key = key.Trim();
            if (dict.TryGetValue(lang, out Dictionary<string, string> keys) == false)
            {
                if(warnings.Contains(CurrentLanguage.ToString()) == false)
                {
                    Warn($"{CurrentLanguage} is not localized!");
                }
              
                success = false;
                return key;
            }

            if (keys.TryGetValue(key, out string result) == false)
            {
                Warn($"Localization key <b>{key}</b> for <b>{lang}</b> is not localized!");
                success = false;
                return key;
            }

            if (string.IsNullOrEmpty(result))
            {
                success = false;
            }

            success = true;

            return result;
        }

        public string GetText(string key, params object[] args)
        {
            try
            {
                return string.Format(GetText(key), args);
            }
            catch (Exception)
            {
                LogError($"Invalid format for localization key {key}!");
                return GetText(key);
            }
        }
        
        private void SetLanguage()
        {
            if (App.Storage.Exists<SelectedLanguage>())
            {
                CurrentLanguage = App.Storage.Load<SelectedLanguage>().language;
            }
            else
            {
                SetSystemLanguage();
            }
        }

        private void SetSystemLanguage()
        {
            if (Application.isEditor || config.onlyEnglish)
            {
                SetLanguage(SystemLanguage.English);
            }
            else
            {
                SetLanguage(Application.systemLanguage);
            }
        }
        
        private void ParseSheet(string[] rows, char separator)
        {
            if (rows.Length == 0)
            {
                LogError("Invalid google sheet!");
                return;
            }

            string[] languages = rows[0].Split(separator);

            for (int i = 0; i < languages.Length; i++)
            {
                languages[i] = languages[i].Replace((char)160, (char)32);
            }

            for (int i = 1; i < rows.Length; i++)
            {
                string[] columns = rows[i].Split(separator);
                string key = columns[0].Trim();
                for (int j = 1; j < columns.Length; j++)
                {
                    string lang = languages[j];

                    if (Enum.TryParse(lang, out SystemLanguage parsedLang) == false)
                    {
                        LogError($"Cannot parse language {lang}");
                    }
                    else
                    {
                        if (dict.ContainsKey(parsedLang) == false)
                        {
                            dict.Add(parsedLang, new Dictionary<string, string>());
                        }

                        Dictionary<string, string> keys = dict[parsedLang];

                        if(keys.ContainsKey(key) == false && string.IsNullOrEmpty(columns[j]) == false)
                        {
                            keys.Add(key, columns[j]);
                        }
                    }
                }
            }
        }

        public IEnumerable<SystemLanguage> GetAvailableLanguages()
        {
            foreach (Language language in config.availableLanguages)
            {
                yield return language.language;
            }
        }

        public Sprite GetLanguageIcon(SystemLanguage language)
        {
            foreach (Language lang in config.availableLanguages)
            {
                if (language == lang.language)
                {
                    return lang.icon;
                }
            }
            return null;
        }
        
        [Serializable]
        private struct SelectedLanguage
        {
            public SystemLanguage language;
        }

    }
}
