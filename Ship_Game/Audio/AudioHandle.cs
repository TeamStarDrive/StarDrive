using Microsoft.Xna.Framework.Audio;

namespace Ship_Game.Audio
{
    interface IAudioHandle
    {
        void OnLoaded(IAudioInstance audio);
        void Destroy();
    }

    public class AudioHandle : IAudioHandle
    {
        bool Loading;
        IAudioInstance Audio;

        public AudioHandle() {}
        internal AudioHandle(IAudioInstance audio) => Audio = audio;
        public bool IsPlaying => Loading || Audio?.IsPlaying == true;
        public bool IsPaused  => Audio != null && Audio.IsPaused;
        public bool IsStopped => Audio == null || Audio.IsStopped;
        public void Pause()   => Audio?.Pause();
        public void Resume()  => Audio?.Resume();
        public void Stop()
        {
            if (Audio != null) { Audio.Stop(); Audio = null; }
        }
        public void Destroy()
        {
            if (Audio != null) { Audio.Dispose(); Audio = null; }
        }
        void IAudioHandle.OnLoaded(IAudioInstance audio)
        {
            Audio = audio;
            Loading = false;
        }
        public void PlaySfxAsync(string cueName, AudioEmitter emitter)
        {
            if (GameAudio.CantPlaySfx(cueName))
                return;
            if (!Loading)
            {
                Loading = true;
                GameAudio.PlaySfxAsync(cueName, emitter, this);
            }
        }
    }
}