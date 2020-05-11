using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SwiftFramework.Utils
{
    [RequireComponent(typeof(Animator))]
    public class ManualAnimator : MonoBehaviour
    {
        private Animator anim;
        private Animator Animator => anim = anim ?? GetComponent<Animator>();

        [SerializeField]  private string[] stateNames = new string[0];
        [SerializeField] [HideInInspector] private int[] stateHashes = new int[0];

        private void OnValidate()
        {
#if UNITY_EDITOR

            UnityEditor.Animations.AnimatorController ac = GetComponent<Animator>().runtimeAnimatorController as UnityEditor.Animations.AnimatorController;
            var states = ac.layers[0].stateMachine.states;
            stateHashes = new int[states.Length];
            stateNames = new string[states.Length];
            for (int i = 0; i < states.Length; i++)
            {
                stateHashes[i] = states[i].state.nameHash;
                stateNames[i] = states[i].state.name;
            }
#endif
        }

        public bool IsAnimating
        {
            get
            {
                return Animator.enabled;
            }
            set
            {
                Animator.enabled = value;
            }
        }

        private float Normalize(float time)
        {
            if (time < 0)
            {
                return 0;
            }
            if (Mathf.Approximately(time, 1))
            {
                return .999f;
            }
            return time;
        }

        public void Evaluate(float timeNormalized, string stateName)
        {
            Animator.Play(stateName, 0, Normalize(timeNormalized));
        }

        public void Evaluate(float timeNormalized, int stateIndex = 0)
        {
            if(Mathf.Approximately(timeNormalized, 1))
            {
                timeNormalized = .99f;
            }
            Animator.Play(stateHashes[stateIndex], 0, Normalize(timeNormalized));
        }
    }
}
