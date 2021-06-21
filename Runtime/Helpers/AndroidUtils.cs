using System;
using UnityEngine;

namespace Swift.Utils
{
    public static class AndroidUtils
    {
        public static bool IsAppInstalled(string bundleID)
        {
            
#if UNITY_EDITOR
            return true;
#endif
            
#if UNITY_ANDROID
            bool installed = false;
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject curActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaObject packageManager = curActivity.Call<AndroidJavaObject>("getPackageManager");

            AndroidJavaObject launchIntent = null;
            try
            {
                launchIntent = packageManager.Call<AndroidJavaObject>("getLaunchIntentForPackage", bundleID);
                if (launchIntent == null)
                    installed = false;

                else
                    installed = true;
            }

            catch (System.Exception e)
            {
                installed = false;
            }

            return installed;
#else
            return false;
#endif
        }
    }
}