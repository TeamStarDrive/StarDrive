using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using System;
using Cue = Microsoft.Xna.Framework.Audio.Cue;

namespace Ship_Game
{
	public sealed class AudioManager : GameComponent
	{
		private static AudioManager audioManager;
		private AudioEngine audioEngine;
		private SoundBank soundBank;
		private WaveBank waveBank;

        //Added by McShooterz: store sounds instances
        private readonly Array<SoundEffectInstance> SoundEffectInstances;

	    public static bool LimitOk => audioManager.SoundEffectInstances.Count < 7;

	    private AudioManager(Game game, string settingsFile, string waveBankFile, string soundBankFile) : base(game)
		{
            SoundEffectInstances = new Array<SoundEffectInstance>();
			try
			{
				audioEngine = new AudioEngine(settingsFile);
				waveBank = new WaveBank(audioEngine, waveBankFile, 0, 16);
				soundBank = new SoundBank(audioEngine, soundBankFile);

                while (!waveBank.IsPrepared)
                {
                    audioEngine.Update();
                }
            }
			catch (NoAudioHardwareException)  {}
			catch (InvalidOperationException) {}
            finally
			{
                //audioEngine = null;
                //waveBank    = null;
                //soundBank   = null;
            }
		}

        // Dispose called by GameComponent Dispose() or finalizer
		protected override void Dispose(bool disposing)
		{
            soundBank?.Dispose(ref soundBank);
            waveBank?.Dispose(ref waveBank);
            audioEngine?.Dispose(ref audioEngine);
            base.Dispose(disposing);
		}

	    public static AudioManager Manager
	    {
	        get
	        {
	            if (audioManager?.audioEngine == null
                    || audioManager.soundBank == null
                    || audioManager.waveBank == null)
                    return null;
                return audioManager;
	        }
	    }
        public static AudioEngine AudioEngine => Manager?.audioEngine;

        public static Cue GetCue(string cueName)
		{
			return Manager?.soundBank.GetCue(cueName);
		}

		public static void Initialize(Game game, string settingsFile, string waveBankFile, string soundBankFile)
		{
			audioManager = new AudioManager(game, settingsFile, waveBankFile, soundBankFile);
		    game?.Components.Add(audioManager);
		}

		public static void PlayCue(string cueName)
		{
			if (audioManager?.audioEngine != null 
                && audioManager.soundBank != null 
                && audioManager.waveBank != null 
                && audioManager.SoundEffectInstances.Count < 7 )
			{
                audioManager.soundBank.PlayCue(cueName);
			}
		}
        public static void PlayCue(string cueName, Vector3 listener, Vector3 emitter)
        {
            if (audioManager?.audioEngine != null 
                && audioManager.soundBank != null 
                && audioManager.waveBank != null)
            {
                
                audioManager.soundBank.PlayCue(cueName);
            }
        }

        public static Cue PlayCue(string cueName, AudioListener listener, AudioEmitter emitter)
        {
            Cue cue = GetCue(cueName);
            cue.Apply3D(listener, emitter);
            cue.Play();
            return cue;
        }

		public override void Update(GameTime gameTime)
		{
            DisposeSoundEffectInstances();
		    audioEngine?.Update();
		    base.Update(gameTime);
		}

        //Added by McShooterz: Play a sound
        public static void PlaySoundEffect(SoundEffect se, float VolumeMod)
        {
            if (AudioManager.audioManager.SoundEffectInstances.Count > 6)
                return;
            SoundEffectInstance sei = se.CreateInstance();
            AudioManager.audioManager.SoundEffectInstances.Add(sei);
            sei.Volume = GlobalStats.EffectsVolume * VolumeMod;
            sei.Play();
        }

        //Added by McShooterz: Play 3d sound effect
        public static void PlaySoundEffect(SoundEffect se, AudioListener al, AudioEmitter ae, float volumeMod)
        {
            if (audioManager.SoundEffectInstances.Count > 6)
                return;
            SoundEffectInstance sei = se.CreateInstance();
            audioManager.SoundEffectInstances.Add(sei);
            sei.Apply3D(al, ae);
            sei.Volume = GlobalStats.EffectsVolume * volumeMod;
            sei.Play();
        }

        //Added by McShooterz: Dispose and remove sounds when finished
        private void DisposeSoundEffectInstances()
        {
            for (int i = 0; i < SoundEffectInstances.Count; i++)
            {
                if (SoundEffectInstances[i].State == SoundState.Stopped && !SoundEffectInstances[i].IsDisposed)
                {
                    SoundEffectInstances[i].Dispose();
                }
            }
            SoundEffectInstances.RemoveAll(x => x.IsDisposed);
        }
	}
}