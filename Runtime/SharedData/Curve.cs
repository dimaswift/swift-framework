using System;
using UnityEngine;

namespace SwiftFramework.Core.SharedData
{
    [Serializable]
    [CreateAssetMenu(menuName = "SwiftFramework/Utils/Shared Curve")]
    [PrewarmAsset]
    public class Curve : ScriptableObject
    {
        [SerializeField] private AnimationCurve curve = AnimationCurve.Linear(0, 0, 1, 1);

        public float Evaluate(float time)
        {
            return curve.Evaluate(time);
        }
    }

    [Serializable]
    [FlatHierarchy]
    [LinkFolder(Folders.Configs + "/Curves")]
    public class CurveLink : LinkToScriptable<Curve>
    {
        public float Evaluate(float time)
        {
            return Value.Evaluate(time);
        }
    }
}
