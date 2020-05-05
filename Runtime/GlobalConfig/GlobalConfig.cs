using UnityEngine;

namespace SwiftFramework.Core
{
    [PrewarmAsset]
    [CreateAssetMenu(fileName = "GlobalConfig", menuName = "SwiftFramework/Config/GlobalConfig")]
    public class GlobalConfig : LinkedScriptableObject
    {
        public string storeUrl;
        public string bundleId;
    }
}
