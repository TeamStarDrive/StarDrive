using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using NAudio.Wave;
using Cue = Microsoft.Xna.Framework.Audio.Cue;

namespace Ship_Game
{
	public sealed class AudioManager : GameComponent
	{
		private static AudioManager audioManager;

		private AudioEngine audioEngine;

		private SoundBank soundBank;

		private WaveBank waveBank;

        private bool disposed;

        //Added by McShooterz: store sounds instances
        private List<SoundEffectInstance> SoundEffectInstances;

		static AudioManager()
		{
		}
        

	static public bool limitOK
	{
        get { return AudioManager.audioManager.SoundEffectInstances.Count <7 ; }
		
	}
	
		private AudioManager(Microsoft.Xna.Framework.Game game, string settingsFile, string waveBankFile, string soundBankFile) : base(game)
		{
            this.SoundEffectInstances = new List<SoundEffectInstance>();
			try
			{
				this.audioEngine = new AudioEngine(settingsFile);
				this.waveBank = new WaveBank(this.audioEngine, waveBankFile, 0, 16);
				this.soundBank = new SoundBank(this.audioEngine, soundBankFile);
			}
			catch (NoAudioHardwareException)
			{
				this.audioEngine = null;
				this.waveBank = null;
				this.soundBank = null;
			}
			catch (InvalidOperationException)
			{
				this.audioEngine = null;
				this.waveBank = null;
				this.soundBank = null;
			}
			while (!this.waveBank.IsPrepared)
			{
				this.audioEngine.Update();
			}
		}

        ~AudioManager() { Dispose(false); }

		protected override void Dispose(bool disposing)
		{
		    if (disposed) return;
		    if (disposing)
		    {
		        soundBank?.Dispose();
		        waveBank?.Dispose();
		        audioEngine?.Dispose();
		    }
		    soundBank   = null;
		    waveBank    = null;
		    audioEngine = null;
		    disposed    = true;
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

        public static void PlayCue(string cueName, AudioListener listener, AudioEmitter emitter)
        {
            Cue cue = GetCue(cueName);
            cue.Apply3D(listener, emitter);
            cue.Play();
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
            sei.Volume = GlobalStats.Config.EffectsVolume * VolumeMod;
            sei.Play();
        }

        //Added by McShooterz: Play 3d sound effect
        public static void PlaySoundEffect(SoundEffect se, AudioListener al, AudioEmitter ae, float VolumeMod)
        {
            if (AudioManager.audioManager.SoundEffectInstances.Count > 6)
                return;
            SoundEffectInstance sei = se.CreateInstance();
            AudioManager.audioManager.SoundEffectInstances.Add(sei);
            sei.Apply3D(al, ae);
            sei.Volume = GlobalStats.Config.EffectsVolume * VolumeMod;
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