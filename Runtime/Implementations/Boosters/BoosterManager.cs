using System;
using System.Collections.Generic;
using UnityEngine;

namespace SwiftFramework.Core.Boosters
{
    public class BoosterManager : IBoosterManager
    {
        public BoostersState State { get; }
        public BoosterManagerConfig Config { get; }

        public event Action<Booster> OnBoosterExpired = b => { };
        public event Action<Booster> OnBoosterDeactivated = b => { };
        public event Action<Booster> OnBoosterActivated = b => { };
        public event Action OnInventoryChanged = () => { };
        public event Action OnMultipliersUpdated = () => { };
        public event Action OnBoosterAddedToInventory = () => { };

        private readonly IClock clock;

        private readonly Dictionary<string, object> boostersCache = new Dictionary<string, object>();

        public BoosterManager(BoostersState state, BoosterManagerConfig config, IClock clock)
        {
            this.clock = clock;
            Config = config;
            State = state;
            CheckExpiredBoosters();
            UpdateMultipliers();
        }

        public void CheckExpiredBoosters()
        {
            long now = clock.Now.Value;

            for (int i = State.boosters.Count - 1; i >= 0; i--)
            {
                Booster booster = State.boosters[i];
                BoosterConfig boosterConfig = TryGetConfig(booster.link);

                if (boosterConfig == null)
                {
                    State.boosters.RemoveAt(i);
                    continue;
                }
                if (boosterConfig.durationSeconds > 0 && now >= booster.expirationTime)
                {
                    State.boosters.RemoveAt(i);
                    OnBoosterExpired(booster);
                    UpdateMultipliers();
                    continue;
                }
            }
        }

        private void UpdateMultipliers()
        {
            OnMultipliersUpdated();
        }

        private bool CanActivate(BoosterConfigLink configLink)
        {
            BoosterConfig config = TryGetConfig(configLink);
            if (config.maxActiveBoostersAmount <= 0)
            {
                return true;
            }
            return GetActiveBoostersAmount(configLink) < config.maxActiveBoostersAmount;
        }

        public bool IsExpired(BoosterConfigLink configLink)
        {
            BoosterConfig config = TryGetConfig(configLink);
            return config.durationSeconds > 0 && GetTotalSecondsLeft(configLink) <= 0;
        }

        public bool IsExpired(BoosterConfig config)
        {
            return config.durationSeconds > 0 && GetTotalSecondsLeft(config) <= 0;
        }

        public bool IsActive(BoosterConfigLink configLink)
        {
            foreach (Booster booster in State.boosters)
            {
                if (booster.link == configLink)
                {
                    return true;
                }
            }
            return false;
        }

        private long GetSecondsLeft(Booster booster)
        {
            if (clock.Now.Value >= booster.expirationTime)
            {
                return 0;
            }
            return booster.expirationTime - clock.Now.Value;
        }

        public long GetTotalSecondsLeft(BoosterConfigLink configLink)
        {
            return GetTotalSecondsLeft(TryGetConfig(configLink));
        }

        public long GetTotalSecondsLeft(BoosterConfig config)
        {
            Booster lastBooster = GetLastBooster(config);
            return lastBooster != null ? GetSecondsLeft(lastBooster) : 0;
        }

        private Booster GetLastBooster(BoosterConfig config)
        {
            long lastExpireTime = 0;
            Booster lastBooster = null;
            foreach (Booster booster in State.boosters)
            {
                BoosterConfig otherConfig = TryGetConfig(booster.link);
                if (otherConfig.mergingTag == config.mergingTag)
                {
                    if (booster.expirationTime > lastExpireTime)
                    {
                        lastExpireTime = booster.expirationTime;
                        lastBooster = booster;
                    }
                }
            }
            return lastBooster;
        }

        private Booster GetLastBooster(BoosterConfigLink configLink)
        {
            BoosterConfig config = configLink.Value as BoosterConfig;
            return GetLastBooster(config);
        }

        public long GetExpirationTimeStamp(BoosterConfigLink configLink)
        {
            Booster lastBooster = GetLastBooster(configLink);
            return lastBooster != null ? lastBooster.expirationTime : 0;
        }

        public long GetActiveBoostersAmount(BoosterConfigLink configLink)
        {
            long count = 0;
            foreach (Booster booster in State.boosters)
            {
                if (booster.link == configLink)
                {
                    if (booster.link.Value.durationSeconds > 0 && booster.expirationTime > clock.Now.Value)
                    {
                        long timeLeft = booster.expirationTime - clock.Now.Value;
                        if(timeLeft > 0)
                        {
                            count += Mathf.CeilToInt((timeLeft - 1) / booster.link.Value.durationSeconds) + 1;
                        }
                    }
                    else
                    {
                        count++;
                    }
                }
            }
            return count;
        }

        public BoosterConfig TryGetConfig(BoosterConfigLink link)
        {
            if (link.HasValue && link.Value != null)
            {
                return link.Value;
            }
            return null;
        }

        public long GetActiveBoostersAmount(BoosterTargetLink target)
        {
            long count = 0;
            foreach (Booster booster in State.boosters)
            {
                if (TryGetConfig(booster.link).target == target)
                {
                    count++;
                }
            }
            return count;
        }

        public void ForceExpire(BoosterTargetLink target, long count)
        {
            for (int i = State.boosters.Count - 1; i >= 0; i--)
            {
                Booster booster = State.boosters[i];
                if (TryGetConfig(booster.link).target == target && count > 0)
                {
                    State.boosters.RemoveAt(i);
                    OnBoosterExpired(booster);
                    UpdateMultipliers();
                    count--;
                }
            }
        }

        private bool FindActiveBoosterWithTag(string mergingTag, out Booster result)
        {
            foreach (Booster booster in State.boosters)
            {
                if (TryGetConfig(booster.link).mergingTag == mergingTag)
                {
                    result = booster;
                    return true;
                }
            }
            result = null;
            return false;
        }

        public void ActivateBooster(BoosterConfigLink configLink, int amount = 1, Link context = null)
        {
            BoosterConfig config = TryGetConfig(configLink);

            while (amount > 0)
            {
                amount--;
                if (TryGetBoosterContainer(configLink, out BoosterContainer container))
                {
                    container.amount--;

                    long startTime = clock.Now.Value;

                    if (FindActiveBoosterWithTag(config.mergingTag, out Booster activeBooster))
                    {
                        startTime = activeBooster.expirationTime;
                    }

                    ActivateBooster(configLink, startTime, context);

                }
                else
                {
                    ActivateBooster(configLink, clock.Now.Value, context);
                }
            }

            OnInventoryChanged();
        }

        private void ActivateBooster(BoosterConfigLink configLink, long startTime, Link context)
        {
            BoosterConfig config = TryGetConfig(configLink);

            if (config == null)
            {
                return;
            }

            if (config.durationSeconds > 0)
            {
                Booster existingBooster = State.boosters.Find(b => b.link.Value.mergingTag == configLink.Value.mergingTag);
                if (existingBooster != null)
                {
                    existingBooster.expirationTime = startTime + config.durationSeconds;
                    OnBoosterActivated(existingBooster);
                    return;
                }
            }

            State.Sort();
            Booster newBooster = new Booster(configLink, startTime + config.durationSeconds, context);
            State.Add(newBooster);
            UpdateMultipliers();
            OnBoosterActivated(newBooster);
        }

        private bool TryGetBoosterContainer(BoosterConfigLink booster, out BoosterContainer boosterContainer)
        {
            foreach (BoosterContainer container in State.boostersInventory)
            {
                if (container.configLink == booster)
                {
                    boosterContainer = container;
                    return true;
                }
            }
            boosterContainer = null;
            return false;
        }

        public void AddBoosterToInventory(BoosterConfigLink configLink, int amount, Link context = null)
        {
            if (TryGetBoosterContainer(configLink, out BoosterContainer container))
            {
                container.amount += amount;
            }
            else
            {
                State.boostersInventory.Add(new BoosterContainer() { amount = amount, configLink = configLink });
            }

            if (TryGetConfig(configLink).activateInstantly)
            {
                ActivateBooster(configLink, amount, context);
            }
            else
            {
                OnBoosterAddedToInventory();
            }
            OnInventoryChanged();
        }

        public IEnumerable<(BoosterConfigLink link, int amount)> GetBoostersInInventory()
        {
            foreach (BoosterContainer container in State.boostersInventory)
            {
                if (container.configLink.HasValue && container.amount > 0)
                {
                    yield return (container.configLink, container.amount);
                }
            }
        }

        public IEnumerable<Booster> GetActiveBoosters(BoosterType type, BoosterTargetLink target)
        {
            return GetFilteredActiveBoosters(type, target);
        }

        public IEnumerable<Booster> GetActiveBoosters(BoosterType type, BoosterOperation operation, BoosterTargetLink target)
        {
            return GetFilteredActiveBoosters(type, target, operation);
        }

        private IEnumerable<Booster> GetFilteredActiveBoosters(BoosterType type, BoosterTargetLink target = null, BoosterOperation? operation = null)
        {
            boostersCache.Clear();
            foreach (Booster booster in State.boosters)
            {
                BoosterConfig config = TryGetConfig(booster.link);
                if (IsExpired(config) == false && config.type == type)
                {
                    if (target != null && target != config.target)
                    {
                        continue;
                    }

                    if (operation.HasValue && operation.Value != config.operation)
                    {
                        continue;
                    }

                    if (boostersCache.ContainsKey(config.mergingTag) == false)
                    {
                        yield return booster;
                    }
                }
            }
        }

        public IEnumerable<Booster> GetActiveBoostersAllOfType(BoosterType type)
        {
            return GetFilteredActiveBoosters(type, null);
        }

        public long GetTotalMultiplier(BoosterTargetLink target)
        {
            return GetTotalMultiplier(target, null);
        }

        public BoosterConfigLink GenerateBooster(BoosterTemplateLink templateLink)
        {
            BoosterTemplate template = templateLink.GetAs<BoosterTemplate>();

            BoosterConfigLink randomLink = Link.Generate<BoosterConfigLink>();

            BoosterConfig generatedConfig = ScriptableObject.CreateInstance<BoosterConfig>();
            generatedConfig.hideFlags = HideFlags.DontSave;

            generatedConfig.type = template.type;
            generatedConfig.cooldownSeconds = template.possibleCooldownSeconds.Random();
            generatedConfig.durationSeconds = template.possibleDurations.Random();
            generatedConfig.multiplier = template.possibleMultipliers.Random();
            generatedConfig.target = template.possibleTargets.Random();
            generatedConfig.icon = template.possibleIcons.Random();

            App.Core.Storage.Save(generatedConfig, randomLink);

            return randomLink;
        }

        public bool TryDeactivateBooster(BoosterConfigLink configLink)
        {
            for (int i = State.boosters.Count - 1; i >= 0; i--)
            {
                Booster booster = State.boosters[i];
                if (booster.link == configLink)
                {
                    State.boosters.RemoveAt(i);
                    OnBoosterDeactivated(booster);
                    UpdateMultipliers();
                    return true;
                }
            }
            return false;
        }

        public void ForceExpire(BoosterConfigLink config, Link context = null)
        {
            for (int i = State.boosters.Count - 1; i >= 0; i--)
            {
                Booster booster = State.boosters[i];
                if (booster.link == config && booster.context == context)
                {
                    State.boosters.RemoveAt(i);
                    OnBoosterExpired(booster);
                    UpdateMultipliers();
                    break;
                }
            }
        }

        public long GetTotalMultiplier(BoosterTargetLink target, Link context)
        {
            boostersCache.Clear();

            long total = 1;

            foreach (Booster booster in State.boosters)
            {
                if (booster.context != null && booster.context != context)
                {
                    continue;
                }

                BoosterConfig config = TryGetConfig(booster.link);

                if (IsExpired(config) == false && config.target == target)
                {
                    if (boostersCache.ContainsKey(config.mergingTag) == false)
                    {
                        if (config.operation == BoosterOperation.Addition)
                        {
                            total += config.multiplier;
                        }
                        else if (config.operation == BoosterOperation.Multiplication)
                        {
                            total *= config.multiplier;
                        }
                    }
                }
            }

            return total;
        }

        public long GetTotalSecondsLeft(BoosterTargetLink target)
        {
            boostersCache.Clear();
            long now = clock.Now.Value;
            long secondsLeft = 0;

            foreach (Booster booster in State.boosters)
            {
                BoosterConfig config = TryGetConfig(booster.link);

                if (IsExpired(config) == false && config.target == target)
                {
                    if (boostersCache.ContainsKey(config.mergingTag) == false)
                    {
                        secondsLeft += booster.expirationTime - now;
                    }
                }
            }

            return secondsLeft;
        }

        public int GetBoosterAmountInInventory(BoosterConfigLink link)
        {
            foreach (BoosterContainer container in State.boostersInventory)
            {
                if (container.configLink == link && container.amount > 0)
                {
                    return container.amount;
                }
            }
            return 0;
        }
    }
}