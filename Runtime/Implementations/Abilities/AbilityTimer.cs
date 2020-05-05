using SwiftFramework.Core;
using System;
using UnityEngine;

namespace SwiftFramework.Core.Supervisors
{
    public class AbilityTimer : MonoBehaviour
    {
        [SerializeField] private GenericImage icon = null;
        [SerializeField] private GenericButton abilityButton = null;
        [SerializeField] private GenericText timer = null;

        private AbilityState state;
        private Action onClick;

        private void Awake()
        {
            if (abilityButton.HasValue)
            {
                abilityButton.Value.AddListener(OnAbilityClick);
            }
        }

        public void Init(AbilityState state, Action onClick)
        {
            timer.Value.Text = null;
            this.onClick = onClick;
            state.ability.Load(ability => 
            {
                this.state = state;
                if (icon.HasValue)
                {
                    icon.Value.SetSprite(ability.icon);
                }
                UpdateTimer();
            });
        }

        private void OnAbilityClick()
        {
            onClick?.Invoke();
        }

        public void Clear()
        {
            timer.Value.Text = null;
            state = null;
        }

        private void OnEnable()
        {
            UpdateTimer();
            App.Core.Clock.Now.OnValueChanged += Now_OnValueChanged;
        }

        private void OnDisable()
        {
            App.Core.Clock.Now.OnValueChanged -= Now_OnValueChanged;
        }

        private void Now_OnValueChanged(long now)
        {
            UpdateTimer();
        }

        private void UpdateTimer()
        {
            if (state != null)
            {
                long now = App.Core.Clock.Now.Value;
                long cooldownSecondsLeft = state.refreshTime - now;
                long activeSecondsLeft = state.endTime - now;

                if (activeSecondsLeft >= 0)
                {
                    timer.Value.Color = Color.green;
                    timer.Value.Text = activeSecondsLeft.ToTimerString();
                }
                else if (cooldownSecondsLeft >= 0)
                {
                    timer.Value.Color = Color.red;
                    timer.Value.Text = cooldownSecondsLeft.ToTimerString();
                }
                else if (timer.HasValue)
                {
                    timer.Value.Text = null;
                }
            }
        }
    }
}

