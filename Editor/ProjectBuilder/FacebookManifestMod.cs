namespace SwiftFramework.EditorUtils
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Xml;
    using UnityEditor;
    using UnityEngine;

    internal class FacebookManifestMod
    {
        public const string AppLinkActivityName = "com.facebook.unity.FBUnityAppLinkActivity";
        public const string DeepLinkingActivityName = "com.facebook.unity.FBUnityDeepLinkingActivity";
        public const string UnityLoginActivityName = "com.facebook.unity.FBUnityLoginActivity";
        public const string UnityDialogsActivityName = "com.facebook.unity.FBUnityDialogsActivity";
        public const string UnityGameRequestActivityName = "com.facebook.unity.FBUnityGameRequestActivity";
        public const string UnityGameGroupCreateActivityName = "com.facebook.unity.FBUnityCreateGameGroupActivity";
        public const string UnityGameGroupJoinActivityName = "com.facebook.unity.FBUnityJoinGameGroupActivity";
        public const string ApplicationIdMetaDataName = "com.facebook.sdk.ApplicationId";
        public const string AutoLogAppEventsEnabled = "com.facebook.sdk.AutoLogAppEventsEnabled";
        public const string AdvertiserIDCollectionEnabled = "com.facebook.sdk.AdvertiserIDCollectionEnabled";
        public const string FacebookContentProviderName = "com.facebook.FacebookContentProvider";
        public const string FacebookContentProviderAuthFormat = "com.facebook.app.FacebookContentProvider{0}";
        public const string FacebookActivityName = "com.facebook.FacebookActivity";
        public const string AndroidManifestPath = "Plugins/Android/AndroidManifest.xml";
        public const string FacebookDefaultAndroidManifestPath = "FacebookSDK/SDK/Editor/android/DefaultAndroidManifest.xml";

        public static void GenerateManifest(string appId)
        {
#if ENABLE_FACEBOOK
            string str = Path.Combine(Application.dataPath, "Plugins/Android/AndroidManifest.xml");
            Directory.CreateDirectory(Path.GetDirectoryName(str));
            if (!File.Exists(str))
                FacebookManifestMod.CreateDefaultAndroidManifest(str);
            FacebookManifestMod.UpdateManifest(str, appId);
#endif
        }

#if ENABLE_FACEBOOK
        public static bool CheckManifest()
        {
            bool flag = true;
            string str = Path.Combine(Application.dataPath, "Plugins/Android/AndroidManifest.xml");
            if (!File.Exists(str))
            {
                Debug.LogError((object)"An android manifest must be generated for the Facebook SDK to work.  Go to Facebook->Edit Settings and press \"Regenerate Android Manifest\"");
                return false;
            }
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.Load(str);
            if (xmlDocument == null)
            {
                Debug.LogError((object)("Couldn't load " + str));
                return false;
            }
            XmlNode childNode = FacebookManifestMod.FindChildNode(FacebookManifestMod.FindChildNode((XmlNode)xmlDocument, "manifest"), "application");
            if (childNode == null)
            {
                Debug.LogError((object)("Error parsing " + str));
                return false;
            }
            XmlElement element1;
            if (!FacebookManifestMod.TryFindElementWithAndroidName(childNode, "com.facebook.unity.FBUnityLoginActivity", out element1, "activity"))
            {
                Debug.LogError((object)string.Format("{0} is missing from your android manifest.  Go to Facebook->Edit Settings and press \"Regenerate Android Manifest\"", (object)"com.facebook.unity.FBUnityLoginActivity"));
                flag = false;
            }
            string attrNameValue = "com.facebook.unity.FBUnityPlayerActivity";
            XmlElement element2;
            if (FacebookManifestMod.TryFindElementWithAndroidName(childNode, attrNameValue, out element2, "activity"))
                Debug.LogWarning((object)string.Format("{0} is deprecated and no longer needed for the Facebook SDK.  Feel free to use your own main activity or use the default \"com.unity3d.player.UnityPlayerNativeActivity\"", (object)attrNameValue));
            return flag;
        }

        public static void UpdateManifest(string fullPath, string appId)
        {
            if (string.IsNullOrEmpty(appId))
            {
                Debug.LogError((object)"You didn't specify a Facebook app ID.  Please add one using the Facebook menu in the main Unity editor.");
            }
            else
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(fullPath);
                if (doc == null)
                {
                    Debug.LogError((object)("Couldn't load " + fullPath));
                }
                else
                {
                    XmlNode childNode = FacebookManifestMod.FindChildNode(FacebookManifestMod.FindChildNode((XmlNode)doc, "manifest"), "application");
                    if (childNode == null)
                    {
                        Debug.LogError((object)("Error parsing " + fullPath));
                    }
                    else
                    {
                        string namespaceOfPrefix = childNode.GetNamespaceOfPrefix("android");
                        XmlElement unityOverlayElement1 = FacebookManifestMod.CreateUnityOverlayElement(doc, namespaceOfPrefix, "com.facebook.unity.FBUnityLoginActivity");
                        FacebookManifestMod.SetOrReplaceXmlElement(childNode, unityOverlayElement1);
                        XmlElement unityOverlayElement2 = FacebookManifestMod.CreateUnityOverlayElement(doc, namespaceOfPrefix, "com.facebook.unity.FBUnityDialogsActivity");
                        FacebookManifestMod.SetOrReplaceXmlElement(childNode, unityOverlayElement2);
                        FacebookManifestMod.AddAppLinkingActivity(doc, childNode, namespaceOfPrefix, Facebook.Unity.Settings.FacebookSettings.AppLinkSchemes[Facebook.Unity.Settings.FacebookSettings.SelectedAppIndex].Schemes);
                        FacebookManifestMod.AddSimpleActivity(doc, childNode, namespaceOfPrefix, "com.facebook.unity.FBUnityDeepLinkingActivity", true);
                        FacebookManifestMod.AddSimpleActivity(doc, childNode, namespaceOfPrefix, "com.facebook.unity.FBUnityGameRequestActivity", false);
                        FacebookManifestMod.AddSimpleActivity(doc, childNode, namespaceOfPrefix, "com.facebook.unity.FBUnityCreateGameGroupActivity", false);
                        FacebookManifestMod.AddSimpleActivity(doc, childNode, namespaceOfPrefix, "com.facebook.unity.FBUnityJoinGameGroupActivity", false);
                        XmlElement element1 = doc.CreateElement("meta-data");
                        element1.SetAttribute("name", namespaceOfPrefix, "com.facebook.sdk.ApplicationId");
                        element1.SetAttribute("value", namespaceOfPrefix, "fb" + appId);
                        FacebookManifestMod.SetOrReplaceXmlElement(childNode, element1);
                        string lower1 = Facebook.Unity.Settings.FacebookSettings.AutoLogAppEventsEnabled.ToString().ToLower();
                        XmlElement element2 = doc.CreateElement("meta-data");
                        element2.SetAttribute("name", namespaceOfPrefix, "com.facebook.sdk.AutoLogAppEventsEnabled");
                        element2.SetAttribute("value", namespaceOfPrefix, lower1);
                        FacebookManifestMod.SetOrReplaceXmlElement(childNode, element2);
                        string lower2 = Facebook.Unity.Settings.FacebookSettings.AdvertiserIDCollectionEnabled.ToString().ToLower();
                        XmlElement element3 = doc.CreateElement("meta-data");
                        element3.SetAttribute("name", namespaceOfPrefix, "com.facebook.sdk.AdvertiserIDCollectionEnabled");
                        element3.SetAttribute("value", namespaceOfPrefix, lower2);
                        FacebookManifestMod.SetOrReplaceXmlElement(childNode, element3);
                        XmlElement contentProviderElement = FacebookManifestMod.CreateContentProviderElement(doc, namespaceOfPrefix, appId);
                        FacebookManifestMod.SetOrReplaceXmlElement(childNode, contentProviderElement);
                        XmlElement element4;
                        if (FacebookManifestMod.TryFindElementWithAndroidName(childNode, "com.facebook.FacebookActivity", out element4, "activity"))
                            childNode.RemoveChild((XmlNode)element4);
                        XmlWriterSettings settings = new XmlWriterSettings()
                        {
                            Indent = true,
                            IndentChars = "  ",
                            NewLineChars = "\r\n",
                            NewLineHandling = NewLineHandling.Replace
                        };
                        using (XmlWriter w = XmlWriter.Create(fullPath, settings))
                            doc.Save(w);
                    }
                }
            }
        }

        private static XmlNode FindChildNode(XmlNode parent, string name)
        {
            for (XmlNode xmlNode = parent.FirstChild; xmlNode != null; xmlNode = xmlNode.NextSibling)
            {
                if (xmlNode.Name.Equals(name))
                    return xmlNode;
            }
            return (XmlNode)null;
        }

        private static void SetOrReplaceXmlElement(XmlNode parent, XmlElement newElement)
        {
            string attribute = newElement.GetAttribute("name");
            string name = newElement.Name;
            XmlElement element;
            if (FacebookManifestMod.TryFindElementWithAndroidName(parent, attribute, out element, name))
                parent.ReplaceChild((XmlNode)newElement, (XmlNode)element);
            else
                parent.AppendChild((XmlNode)newElement);
        }

        private static bool TryFindElementWithAndroidName(
            XmlNode parent,
            string attrNameValue,
            out XmlElement element,
            string elementType = "activity")
        {
            string namespaceOfPrefix = parent.GetNamespaceOfPrefix("android");
            for (XmlNode xmlNode = parent.FirstChild; xmlNode != null; xmlNode = xmlNode.NextSibling)
            {
                XmlElement xmlElement = xmlNode as XmlElement;
                if (xmlElement != null && xmlElement.Name == elementType && xmlElement.GetAttribute("name", namespaceOfPrefix) == attrNameValue)
                {
                    element = xmlElement;
                    return true;
                }
            }
            element = (XmlElement)null;
            return false;
        }

        private static void AddSimpleActivity(
            XmlDocument doc,
            XmlNode xmlNode,
            string ns,
            string className,
            bool export = false)
        {
            XmlElement activityElement = FacebookManifestMod.CreateActivityElement(doc, ns, className, export);
            FacebookManifestMod.SetOrReplaceXmlElement(xmlNode, activityElement);
        }

        private static XmlElement CreateUnityOverlayElement(
            XmlDocument doc,
            string ns,
            string activityName)
        {
            XmlElement activityElement = FacebookManifestMod.CreateActivityElement(doc, ns, activityName, false);
            activityElement.SetAttribute("configChanges", ns, "fontScale|keyboard|keyboardHidden|locale|mnc|mcc|navigation|orientation|screenLayout|screenSize|smallestScreenSize|uiMode|touchscreen");
            activityElement.SetAttribute("theme", ns, "@android:style/Theme.Translucent.NoTitleBar.Fullscreen");
            return activityElement;
        }

        private static XmlElement CreateContentProviderElement(
            XmlDocument doc,
            string ns,
            string appId)
        {
            XmlElement element = doc.CreateElement("provider");
            element.SetAttribute("name", ns, "com.facebook.FacebookContentProvider");
            string str = string.Format((IFormatProvider)CultureInfo.InvariantCulture, "com.facebook.app.FacebookContentProvider{0}", (object)appId);
            element.SetAttribute("authorities", ns, str);
            element.SetAttribute("exported", ns, "true");
            return element;
        }

        private static XmlElement CreateActivityElement(
            XmlDocument doc,
            string ns,
            string activityName,
            bool exported = false)
        {
            XmlElement element = doc.CreateElement("activity");
            element.SetAttribute("name", ns, activityName);
            if (exported)
                element.SetAttribute(nameof(exported), ns, "true");
            return element;
        }

        private static void AddAppLinkingActivity(
            XmlDocument doc,
            XmlNode xmlNode,
            string ns,
            List<string> schemes)
        {
            XmlElement activityElement = FacebookManifestMod.CreateActivityElement(doc, ns, "com.facebook.unity.FBUnityAppLinkActivity", true);
            foreach (string scheme in schemes)
            {
                XmlElement element1 = doc.CreateElement("intent-filter");
                XmlElement element2 = doc.CreateElement("action");
                element2.SetAttribute("name", ns, "android.intent.action.VIEW");
                element1.AppendChild((XmlNode)element2);
                XmlElement element3 = doc.CreateElement("category");
                element3.SetAttribute("name", ns, "android.intent.category.DEFAULT");
                element1.AppendChild((XmlNode)element3);
                XmlElement element4 = doc.CreateElement("data");
                element4.SetAttribute("scheme", ns, scheme);
                element1.AppendChild((XmlNode)element4);
                activityElement.AppendChild((XmlNode)element1);
            }
            FacebookManifestMod.SetOrReplaceXmlElement(xmlNode, activityElement);
        }

        private static void CreateDefaultAndroidManifest(string outputFile)
        {
            string str = Path.Combine(EditorApplication.applicationContentsPath, "PlaybackEngines/androidplayer/AndroidManifest.xml");
            if (!File.Exists(str))
                str = Path.Combine(EditorApplication.applicationContentsPath, "PlaybackEngines/AndroidPlayer/Apk/AndroidManifest.xml");
            if (File.Exists(str))
            {
                File.Copy(str, outputFile);
            }
            else
            {
                Stream manifestResourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Facebook.Unity.Editor.android.DefaultAndroidManifest.xml");
                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.Load(manifestResourceStream);
                Debug.LogWarning((object)string.Format("No existing android manifest found at '{0}'. Creating a default manifest file", (object)outputFile));
                xmlDocument.Save(outputFile);
            }
        }
#endif
    }
}