using System;

namespace SwiftFramework.Core.Prestige
{
    public abstract class PrestigeController<T, S> : IPrestigeController where T : PrestigeState
    {
        public IStatefulEvent<bool> IsAvailable => isAvailable;

        public event Action OnPrestigeChanged = () => { };
        public event Action OnBecameAvailableForTheFirstTime = () => { };

        private readonly StatefulEvent<bool> isAvailable = new StatefulEvent<bool>();

        protected readonly T prestigeState;
        protected readonly S state;
        private readonly PrestigeSettings config;

        protected void UpdateAvailability()
        {
            isAvailable.SetValue(IsAvaialble());
        }

        public virtual bool IsAvaialble() => true;

        public PrestigeController(T prestigeState, S state, PrestigeSettings config)
        {
            this.state = state;
            this.config = config;
            this.prestigeState = prestigeState;
        }

        public IPromise<bool> UpgradePrestige()
        {
            Promise<bool> promise = Promise<bool>.Create();

            PrestigeSettings.PresigeLevel level = config.levels[prestigeState.level + 1];

            level.price.Pay().Done(paid =>
            {
                if (paid)
                {
                    prestigeState.level++;

                    ProcessUpgrade().Done(() =>
                    {
                        OnPrestigeChanged();

                        UpdateAvailability();

                        promise.Resolve(true);
                    });
                }
                else
                {
                    promise.Resolve(false);
                }

            });

            return promise;
        }

        protected abstract IPromise ProcessUpgrade();

        public (float multiplier, PriceLink price, int level) GetNextPrestige()
        {
            int levelIndex = prestigeState.level + 1;
            if (levelIndex >= config.levels.Length)
            {
                levelIndex = config.levels.Length - 1;
            }
            PrestigeSettings.PresigeLevel level = config.levels[levelIndex];
            return (level.multiplier, level.price, levelIndex);
        }

        public (float multiplier, PriceLink price, int level) GetCurrentPrestige()
        {
            if (config.levels.Length == 0)
            {
                return (1, null, 0);
            }
            PrestigeSettings.PresigeLevel level = config.levels[prestigeState.level];
            return (level.multiplier, level.price, prestigeState.level);
        }

        public bool ShouldBeShown()
        {
            return prestigeState.notifiedAboutAvailablePresitge || IsAvailable.Value;
        }

        public int GetMaxLevel()
        {
            return config.levels.Length;
        }
    }
}
