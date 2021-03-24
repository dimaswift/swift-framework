using SwiftFramework.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SwiftFramework.Sound
{
    [DefaultModule]
    [Configurable(typeof(SoundManagerConfig))]
    [DisallowCustomModuleBehaviours]
    internal class SoundManager : StatefulBehaviourModule<SoundManagerState>, ISoundManager
    {
        private SoundManagerConfig Config => GetModuleConfig<SoundManagerConfig>();

        private readonly List<AudioClip> clipsPlayedCurrentFrame = new List<AudioClip>(10);

        private bool clipHistoryChanged;

        private readonly Dictionary<int, SoundChannel> channels = new Dictionary<int, SoundChannel>();

        private class SoundChannel
        {
            private readonly List<AudioSource> players = new List<AudioSource>();

            private readonly Dictionary<AudioClip, AudioSource> activeLoops = new Dictionary<AudioClip, AudioSource>();

            private readonly AudioSource oneShotPlayer;

            private readonly SoundType type;

            private readonly GameObject gameObject;

            private readonly int maxActivePlayerCount;

            private bool isMuted;

            public SoundChannel(GameObject gameObject, SoundType type, int startPlayersAmount, int maxActivePlayerCount)
            {
                this.maxActivePlayerCount = maxActivePlayerCount;
                this.gameObject = gameObject;
                this.type = type;
                for (int i = 0; i < startPlayersAmount; i++)
                {
                    players.Add(gameObject.AddComponent<AudioSource>());
                }

                oneShotPlayer = gameObject.AddComponent<AudioSource>();
                oneShotPlayer.loop = false;
                oneShotPlayer.spatialize = false;
                oneShotPlayer.rolloffMode = AudioRolloffMode.Linear;
            }

            public void StopLoop(AudioClipLink clipLink)
            {
                clipLink.Load(clip =>
                {
                    if (IsValid(clip) == false)
                    {
                        return;
                    }

                    if (activeLoops.ContainsKey(clip) == false)
                    {
                        return;
                    }

                    activeLoops[clip].Stop();

                    activeLoops.Remove(clip);
                });
            }

            private bool IsValid(AudioClip clip)
            {
                if (clip == null)
                {
                    Debug.LogError($"Trying to play null audio clip!");
                    return false;
                }

                return true;
            }

            private AudioSource GetFreePlayer()
            {
                foreach (AudioSource player in players)
                {
                    if (player.isPlaying == false)
                    {
                        return player;
                    }
                }

                if (players.Count >= maxActivePlayerCount)
                {
                    Debug.LogWarning($"Can't get free audio player! Players count: {players.Count}!");
                    return null;
                }

                AudioSource newPlayer = gameObject.AddComponent<AudioSource>();
                newPlayer.mute = IsMuted();
                players.Add(newPlayer);
                return newPlayer;
            }

            public bool IsMuted()
            {
                return isMuted;
            }

            public void PlayLoop(AudioClipLink clipLink, float volumeScale)
            {
                clipLink.Load(clip =>
                {
                    if (activeLoops.ContainsKey(clip))
                    {
                        Debug.LogWarning($"Clip {clip} already playing!");
                        return;
                    }

                    AudioSource freePlayer = GetFreePlayer();
                    if (freePlayer == null)
                    {
                        Debug.LogWarning($"Can't get free audio player! Players count: {players.Count}!");
                        return;
                    }

                    freePlayer.clip = clip;
                    freePlayer.volume = volumeScale;
                    freePlayer.loop = true;
                    freePlayer.Play();
                    activeLoops.Add(clip, freePlayer);
                });
            }

            public void SetMuted(bool isMuted)
            {
                this.isMuted = isMuted;
                foreach (AudioSource player in players)
                {
                    player.mute = isMuted;
                }

                oneShotPlayer.mute = isMuted;
            }

            internal void PlayOneShot(AudioClip clip, float volumeScale)
            {
                oneShotPlayer.pitch = 1;
                oneShotPlayer.PlayOneShot(clip, volumeScale);
            }
            
            internal void SetPitch(float pitch)
            {
                oneShotPlayer.pitch = pitch;
            }
        }

        protected override IPromise GetInitPromise()
        {
            foreach (object type in Enum.GetValues(typeof(SoundType)))
            {
                if (channels.ContainsKey((int)type))
                    continue;
                channels.Add((int)type,
                    new SoundChannel(gameObject, (SoundType)type, Config.startPlayersAmount,
                        Config.maxActivePlayerCount));
            }

            foreach (SoundManagerState.SoundTypeState state in State.statesByType)
            {
                SetMuted(state.muted, state.type);
            }

            return Promise.Resolved();
        }

        protected override SoundManagerState GetDefaultState()
        {
            return new SoundManagerState()
            {
                statesByType = new List<SoundManagerState.SoundTypeState>()
                {
                    new SoundManagerState.SoundTypeState() {muted = false, type = SoundType.Music},
                    new SoundManagerState.SoundTypeState() {muted = false, type = SoundType.SFX}
                }
            };
        }

        public void PlayLoop(AudioClipLink clip, SoundType type, float volumeScale = 1)
        {
            GetChannel(type).PlayLoop(clip, volumeScale);
        }

        public void PlayOnce(AudioClipLink clipLink, SoundType type, float volumeScale = 1)
        {
            clipLink.Load(clip =>
            {
                if (clip == null)
                {
                    return;
                }

                if (WasPlayedThisFrame(clip))
                {
                    return;
                }

                GetChannel(type).PlayOneShot(clip, volumeScale);
                AddToHistory(clip);
            });
        }
        
        public void PlayOncePitched(AudioClipLink clipLink, float pitch, float volumeScale = 1)
        {
            clipLink.Load(clip =>
            {
                if (clip == null)
                {
                    return;
                }

                if (WasPlayedThisFrame(clip))
                {
                    return;
                }

                var channel = GetChannel(SoundType.SFX);
              
                channel.PlayOneShot(clip, volumeScale);
                channel.SetPitch(pitch);
                AddToHistory(clip);
            });
        }

        public void Vibrate()
        {
            if (State.enableVibration == false)
            {
                return;
            }
            #if UNITY_ANDROID
                Handheld.Vibrate();
            #endif
        }

        public bool IsVibrationEnabled()
        {
            return State.enableVibration;
        }

        public void SetVibrationEnabled(bool enabled)
        {
            State.enableVibration = enabled;
        }

        private bool WasPlayedThisFrame(AudioClip clip)
        {
            for (int i = 0; i < clipsPlayedCurrentFrame.Count; i++)
            {
                if (clipsPlayedCurrentFrame[i] == clip)
                {
                    return true;
                }
            }

            return false;
        }

        private void AddToHistory(AudioClip clip)
        {
            clipHistoryChanged = true;
            for (int i = 0; i < clipsPlayedCurrentFrame.Count; i++)
            {
                if (clipsPlayedCurrentFrame[i] == null)
                {
                    clipsPlayedCurrentFrame[i] = clip;
                    return;
                }
            }

            clipsPlayedCurrentFrame.Add(clip);
        }

        private void LateUpdate()
        {
            if (clipHistoryChanged)
            {
                for (int i = 0; i < clipsPlayedCurrentFrame.Count; i++)
                {
                    clipsPlayedCurrentFrame[i] = null;
                }

                clipHistoryChanged = false;
            }
        }

        public void SetMutedAll(bool muted)
        {
            SetMuted(muted, SoundType.Music);
            SetMuted(muted, SoundType.SFX);
        }

        private SoundChannel GetChannel(SoundType type)
        {
            return channels[(int)type];
        }

        public void SetMuted(bool muted, SoundType type)
        {
            GetChannel(type).SetMuted(muted);
            foreach (SoundManagerState.SoundTypeState state in State.statesByType)
            {
                if (state.type == type)
                {
                    state.muted = muted;
                    break;
                }
            }
        }

        public void StopLoop(AudioClipLink clip, SoundType type)
        {
            GetChannel(type).StopLoop(clip);
        }

        public bool IsMuted(SoundType type)
        {
            return GetChannel(type).IsMuted();
        }

        public bool IsMutedAll()
        {
            return IsMuted(SoundType.SFX) && IsMuted(SoundType.Music);
        }
    }
}