using System;
using UnityEditor;

namespace SwiftFramework.Editor
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