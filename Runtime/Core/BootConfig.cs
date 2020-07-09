using UnityEngine;

namespace SwiftFramework.Core
{
    [CreateAssetMenu(fileName = "BootConfig", menuName = "SwiftFramework/Config/BootConfig")]
    [AddrSingleton(Folders.Configs)]
    public class BootConfig : ScriptableObject
    {
        public int buildNumber;
    }
}