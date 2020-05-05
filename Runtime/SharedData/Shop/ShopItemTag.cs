using SwiftFramework.Core;
using System;
using UnityEngine;

namespace SwiftFramework.Core.SharedData.Shop
{
    [PrewarmAsset]
    public abstract class ShopItemTag : ScriptableObject
    {
        
    } 

    [Serializable]
    public class ShopItemTagLink : LinkTo<ShopItemTag>
    {

    }
}
