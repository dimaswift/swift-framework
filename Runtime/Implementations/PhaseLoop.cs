using System;
using System.Collections.Generic;
using Swift.Core.SharedData;
using UnityEditor;
using UnityEngine;

namespace Swift.Core
{
    public class PhaseLoop<S>
    {
        public bool Activated { get; private set; }
        public int CurrentIteration { get; private set; }
        public IStatefulEvent<S> State => state;

        public bool Finished => timer >= targetTime;
        public float TimeNormalized => targetTime == 0 ? 0 : timer / targetTime;
        public float Time => timer;

        private readonly List<Phase<S>> phases = new List<Phase<S>>();
        private readonly StatefulEvent<S> state = new StatefulEvent<S>();
        private float timer = 0;
        private float targetTime;
        private int currentPhaseIndex = 0;
        private Func<bool> autoActivate;

        public void Activate()
        {
            if (Activated)
            {
                return;
            }

            Activated = true;
            SetPhase(0);
        }

        public void Deactivate()
        {
            Activated = false;
        }

        private void SetPhase(int index)
        {
            if (index < 0)
            {
                return;
            }
            currentPhaseIndex = index;
            timer = 0;
            targetTime = phases[currentPhaseIndex].time();
            state.SetValue(phases[currentPhaseIndex].state);
            phases[currentPhaseIndex].onStart?.Invoke();
            CurrentIteration = 0;
        }

        public void ForceState(S newState)
        {
            SetPhase(phases.FindIndex(p => p.state.Equals(newState)));
        }

        public void Update(float delta)
        {
            if (Activated == false)
            {
                return;
            }

            Phase<S> current = phases[currentPhaseIndex];

            if (current.startCondition != null && current.startCondition() == false)
            {
                return;
            }

            timer += current.speed * delta;


            if (timer >= targetTime)
            {

                timer = 0;
                CurrentIteration++;
                current.onComplete?.Invoke();

                if (CurrentIteration < current.iterations || (current.completeCondition != null && current.completeCondition() == false))
                {
                    return;
                }


                MoveToNextPhase();
            }
        }

        private void MoveToNextPhase()
        {
            currentPhaseIndex++;

            CurrentIteration = 0;

            if (currentPhaseIndex >= phases.Count)
            {
                StartFromFirstPhase();
            }
            else
            {
                Phase<S> current = phases[currentPhaseIndex];
                timer = 0;
                targetTime = phases[currentPhaseIndex].time();
                state.SetValue(current.state);
                current.onStart?.Invoke();
            }
        }

        private void StartFromFirstPhase()
        {
            currentPhaseIndex = 0;
            Activated = false;
            if (autoActivate())
            {
                Activate();
            }
            else
            {
                timer = 0;
                targetTime = phases[currentPhaseIndex].time();
                state.SetValue(phases[currentPhaseIndex].state);
                phases[currentPhaseIndex].onStart?.Invoke();
            }
        }

        public void UpdatePhases(IEnumerable<Phase<S>> newPhases)
        {
            int i = 0;
            foreach (var newPhase in newPhases)
            {
                if (i < phases.Count)
                {
                    phases[i].speed = newPhase.speed;
                    phases[i].time = newPhase.time;
                }
                i++;
            }
        }

        public void SetLoop(IEnumerable<Phase<S>> phases, Func<bool> autoActivate)
        {
            this.phases.Clear();
            this.phases.AddRange(phases);
            if (this.phases.Count == 0)
            {
                return;
            }
            this.autoActivate = autoActivate;
            if (autoActivate())
            {
                Activate();
            }
        }
    }
}
