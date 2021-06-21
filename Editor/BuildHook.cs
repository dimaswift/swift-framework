using System;
using UnityEditor;

namespace Swift.Editor
{
    public static class BuildHook
    {
        public static event Action OnBeforeBuild = () => { };
        
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            BuildPlayerWindow.RegisterBuildPlayerHandler(BuildPlayerHandler);
        }
        
        private static void BuildPlayerHandler(BuildPlayerOptions options)
        {
            OnBeforeBuild();
            BuildPlayerWindow.DefaultBuildMethods.BuildPlayer(options);
        }
    }
}