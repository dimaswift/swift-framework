using UnityEngine;
using SwiftFramework.Core;
using System;
using System.IO;
using System.Collections.Generic;

namespace SwiftFramework.Core
{
    internal enum Extention
    {
        TSV, CSV
    }
    [DefaultModule]
    [Configurable(typeof(LocalizationConfig))]
    [DependsOnModules(typeof(INetworkManager))]
    internal class LocalizationManager : Module, ILocalizationManager
    {
        private struct SelectedLanguage
        {
            public SystemLanguage language;
        }

        private Dictionary<string, string> warnings = new Dictionary<string, string>();

        public LocalizationManager(ModuleConfigLink configLink)
        {
            SetConfig(configLink);
        }

        public LocalizationConfig Config => config;

        public SystemLanguage CurrentLanguage { get; private set; } = SystemLanguage.English;

        private readonly Promise downloadPromise = Promise.Create();

        private Dictionary<SystemLanguage, Dictionary<string, string>> dict = new Dictionary<SystemLanguage, Dictionary<string, string>>();

        private const string fileName = "localization";

        private string localSheetPath => $"{Application.persistentDataPath}/{fileName}.csv";

        private LocalizationConfig config;

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

            if(success == false)
            {
                Warn($"Using fallback language for key <b>{key}</b>!");
                result = GetText(key, Config.fallbackLanguage, out success);
            }

            return result;
        }

        private void Warn(string message)
        {
            if (warnings.ContainsKey(message))
            {
                return;
            }

            LogWarning(message);
            warnings.Add(message, "");
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
                if(warnings.ContainsKey(CurrentLanguage.ToString()) == false)
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

        public IPromise DownloadSheet(string url, string filePath)
        {
            return Download(App.Net).Then(body => File.WriteAllText(filePath, body));
        }

        protected override IPromise GetInitPromise()
        {
            GetModuleConfigAsync<LocalizationConfig>().Then(c => 
            {
                config = c;
              
                if (string.IsNullOrEmpty(Config.publishedGoogleSheetUrl))
                {
                    LogWarning("Cannot download localization, invalid url");
                    downloadPromise.Resolve();
                    return;
                }

                ParseLocal();
                SetLanguage();
                downloadPromise.Resolve();
                if (Application.isEditor == false)
                {
                    DownloadSheet(Config.publishedGoogleSheetUrl, localSheetPath).Then(() =>
                    {
                        ParseGoogleSheet(File.ReadAllLines(localSheetPath));
                    });
                }
            },
             e => downloadPromise.Reject(e));

            return downloadPromise;
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
            if (Application.isEditor)
            {
                SetLanguage(Config.fallbackLanguage);
            }
            else
            {
                SetLanguage(Application.systemLanguage);
            }
        }

        private void ParseLocal()
        {
            if (Application.isEditor == false && File.Exists(localSheetPath))
            {
                ParseGoogleSheet(File.ReadAllLines(localSheetPath));
            }
            else
            {
                string[] rows = Resources.Load<TextAsset>(fileName)?.text.Split('\n');
                if(rows == null)
                {
                    LogError($"Cannot parse file from resources! Should be inside Resources/{fileName}.csv");
                    return;
                }
                ParseGoogleSheet(rows);
            }
        }


        private void ParseGoogleSheet(string[] rows)
        {
            char separator;

            switch (Config.extention)
            {
                case Extention.TSV:
                    separator = '\t';
                    break;
                case Extention.CSV:
                    separator = ',';
                    break;
                default:
                    separator = ',';
                    break;
            }

            if(rows.Length == 0)
            {
                LogError("Invalid google sheet!");
                return;
            }

            var langs = rows[0].Split(separator);

            for (int i = 0; i < langs.Length; i++)
            {
                langs[i] = langs[i].Replace((char)160, (char)32);
            }

            for (int i = 1; i < rows.Length; i++)
            {
                var columns = rows[i].Split(separator);
                var key = columns[0].Trim();
                for (int j = 1; j < columns.Length; j++)
                {
                    var lang = langs[j];

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

                        var keys = dict[parsedLang];

                        if(keys.ContainsKey(key) == false && string.IsNullOrEmpty(columns[j]) == false)
                        {
                            keys.Add(key, columns[j]);
                        }
                    }
                }
            }
        }

        public IPromise<string> Download(INetworkManager networkManager)
        {
            Promise<string> promise = Promise<string>.Create();
            networkManager.Get(Config.publishedGoogleSheetUrl).Then(body =>
            {
                promise.Resolve(body);
            })
            .Catch(e => promise.Reject(e));
            return promise;
        }

        public IEnumerable<SystemLanguage> GetAvailableLanguages()
        {
            foreach (LocalizationConfig.Language language in Config.availableLanguages)
            {
                yield return language.language;
            }
        }

        public Sprite GetLanguageIcon(SystemLanguage language)
        {
            foreach (LocalizationConfig.Language lang in Config.availableLanguages)
            {
                if(language == lang.language)
                {
                    return lang.icon;
                }
            }
            return null;
        }
    }
}
