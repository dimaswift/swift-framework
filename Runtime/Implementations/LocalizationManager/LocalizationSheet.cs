using System;
using UnityEngine;

namespace Swift.Core
{
    [PrewarmAsset]
    public abstract class LocalizationSheet : ScriptableObject
    {
        public virtual char Separator => '\t';
        public abstract IPromise<string[]> LoadSheet(Action<string[]> onLazeLoad);
    }
}