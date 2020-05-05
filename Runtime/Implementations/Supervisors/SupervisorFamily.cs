using System;
using UnityEngine;

namespace SwiftFramework.Core.Supervisors
{
    [PrewarmAsset]
    [CreateAssetMenu(menuName = "SwiftFramework/Supervisors/SupervisorFamily")]
    public class SupervisorFamily : ScriptableObject
    {
        
    }


    [Serializable]
    [LinkFolder(Folders.Configs + "/Supervisors")]
    public class SupervisorFamilyLink : LinkTo<SupervisorFamily>
    {

    }

}
