using UnityEngine;

namespace SwiftFramework.Core
{
    public interface ISoundManager : IModule
    {
        void PlayOnce(AudioClipLink clip, SoundType type, float volumeScale = 1f);
        void PlayLoop(AudioClipLink clip, SoundType type, float volumeScale = 1f);
        void StopLoop(AudioClipLink clip, SoundType type);
        void SetMuted(bool muted, SoundType type);
        void SetMutedAll(bool muted);
        bool IsMuted(SoundType type);
    }

    public enum SoundType
    {
        SFX = 0, Music = 1
    }
}
