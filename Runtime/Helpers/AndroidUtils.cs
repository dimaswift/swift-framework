using System;
using UnityEngine;

namespace SwiftFramework.Utils
{
    public static class AndroidUtils
    {
        public static bool IsAppInstalled(string bundleID)
        {
            
#if UNITY_EDITOR
            return true;
#endif
            
#if UNITY_ANDROID
            AndroidJavaClass up = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject ca = up.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaObject packageManager = ca.Call<AndroidJavaObject>("getPackageManager");
            AndroidJavaObject launchIntent = null;
            try
            {
                launchIntent = packageManager.Call<AndroidJavaObject>("getLaunchIntentForPackage", bundleID);
            }
            catch (Exception ex)
            {
                Debug.Log("exception" + ex.Message);
            }

            if (launchIntent == null)
            {
                Debug.Log(bundleID + " not installed");
                return false;
            }

            return true;
#else
            return false;
#endif
        }
    }
}