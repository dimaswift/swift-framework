﻿using UnityEngine;

namespace Swift.Core
{
    public class LocalizationConfig : ModuleConfig
    {
        public SystemLanguage defaultLanguage = SystemLanguage.English;
        public Language[] availableLanguages = { };
        public bool onlyEnglish = true;
    }
}