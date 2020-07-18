using UnityEngine;

namespace SwiftFramework.Core
{
    [PrewarmAsset]
    public abstract class LocalizationSheet : ScriptableObject
    {
        public virtual char Separator => '\t';
        public abstract IPromise<string[]> LoadSheet();
    }
}