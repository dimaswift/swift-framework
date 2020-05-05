using UnityEngine;
using SwiftFramework.Core;
using UnityEngine.Networking;
using System.IO;

namespace SwiftFramework.Core
{
    internal class LocalizationConfig : ModuleConfig
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

//#if UNITY_EDITOR

//        [ContextMenu("SwiftFramework/Localization/Download To Resources")]
//        public static void Download()
//        {
//            var config = EditorUtils.Util.GetAsset<LocalizationConfig>();

//            UnityWebRequest request = UnityWebRequest.Get(config.publishedGoogleSheetUrl);

//            var operation = request.SendWebRequest();

//            operation.completed += response =>
//            {
//                if (string.IsNullOrEmpty(request.downloadHandler.text) == false)
//                {
//                    string path = Application.dataPath + "/Resources/localization.csv";
//                    File.WriteAllText(path, request.downloadHandler.text);
//                    UnityEditor.AssetDatabase.Refresh();
//                    Debug.Log($"<color=green>Localization downloaded to {path}</color>");
//                }
//            };
//        }
//#endif
    }
}
