using UnityEngine;

namespace SwiftFramework.Core
{
    public enum Extention
    {
        TSV, CSV
    }
    public class LocalizationConfig : ModuleConfig
    {
        public string publishedGoogleSheetUrl = null;
        public SystemLanguage fallbackLanguage = SystemLanguage.English;
        public Extention extention = Extention.TSV;
        public Language[] availableLanguages = { };

        [System.Serializable]
        public class Language
        {
            public SystemLanguage language;
            public Sprite icon;
        }
    }
}
