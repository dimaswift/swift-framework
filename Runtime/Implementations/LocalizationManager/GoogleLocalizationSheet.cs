using System;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace SwiftFramework.Core
{
    [CreateAssetMenu(menuName = "SwiftFramework/Localization/Google Localization Sheet")]
    public class GoogleLocalizationSheet : LocalizationSheet
    {
        [SerializeField] private int downloadTimeout = 3;
        [SerializeField] private bool downloadSheetOnLoad = false;
        [SerializeField] private string publishedGoogleSheetUrl = null;
        [SerializeField] private SheetExtension sheetExtension = SheetExtension.TSV;
        
        [SerializeField][HideInInspector] private string[] downloadedSheet = {};

        private string CachedSheetPath => name + "_sheet." + sheetExtension.ToString().ToLower();

        public override char Separator => sheetExtension == SheetExtension.CSV ? ',' : '\t';
        
#if UNITY_EDITOR

        [ContextMenu("Download")]
        private void DownloadInternal()
        {
            UnityWebRequest request = UnityWebRequest.Get(publishedGoogleSheetUrl);

            request.timeout = 10;

            UnityWebRequestAsyncOperation operation = request.SendWebRequest();

            operation.completed += response => 
            {
                if (request.isHttpError || request.isNetworkError)
                {
                    Debug.LogError($"GoogleLocalizationSheet: {request.error}");
                }
                else
                {
                    if (string.IsNullOrEmpty(request.downloadHandler.text))
                    {
                        Debug.LogError("GoogleLocalizationSheet: Empty response");
                    }
                    else
                    {
                        downloadedSheet = request.downloadHandler.text.Split('\n');
                        UnityEditor.EditorUtility.SetDirty(this);
                        Debug.Log($"<color=green>GoogleLocalizationSheet: Downloaded {downloadedSheet.Length} rows.</color>");
                    }
                }
            };
        }

#endif

        private string[] GetCachedSheet()
        {
            if (File.Exists(CachedSheetPath))
            {
                return File.ReadAllLines(CachedSheetPath);
            }
            return downloadedSheet;
        }
        
        public override IPromise<string[]> LoadSheet()
        {
            Promise<string[]> promise = Promise<string[]>.Create();
            
            if (downloadSheetOnLoad == false)
            {
                Download();
                promise.Resolve(GetCachedSheet());
                return promise;
            }
            
            return Download();
        }

        private IPromise<string[]> Download()
        {
            Promise<string[]> promise = Promise<string[]>.Create();
            App.Core.Net.Get(publishedGoogleSheetUrl, downloadTimeout).Then(body =>
                {
                    if (string.IsNullOrEmpty(body) == false)
                    {
                        try
                        {
                            if (File.Exists(CachedSheetPath))
                            {
                                File.Delete(CachedSheetPath);
                            }
                            File.WriteAllText(CachedSheetPath, body);
                        }
                        catch (Exception exception)
                        {
                            Debug.LogError($"Cannot write localization file: {exception.Message}");
                        }

                        promise.Resolve(body.Split('\n'));
                    }
                    else
                    {
                        promise.Resolve(GetCachedSheet());
                    }
                })
                .Catch(e =>
                {
                    promise.Resolve(GetCachedSheet());
                });

            return promise;
        }
    }
    
    public enum SheetExtension
    {
        TSV, CSV
    } 
    
    [System.Serializable]
    public class Language
    {
        public SystemLanguage language;
        public Sprite icon;
    }
}