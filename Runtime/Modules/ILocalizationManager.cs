using System;
using System.Collections.Generic;
using UnityEngine;

namespace SwiftFramework.Core
{
    [ModuleGroup(ModuleGroups.Core)]
    public interface ILocalizationManager : IModule
    {
        IEnumerable<SystemLanguage> GetAvailableLanguages();
        SystemLanguage CurrentLanguage { get; }
        string GetText(string key);
        string GetText(string key, params object[] args);
        event Action OnLanguageChanged;
        void SetLanguage(SystemLanguage language);
        Sprite GetLanguageIcon(SystemLanguage language);
    }
}
