using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using System;
using System.Collections.ObjectModel;

namespace Ship_Game
{
	public class AudioManager : GameComponent
	{
		private static AudioManager audioManager;

		private AudioEngine audioEngine;

		private SoundBank soundBank;

		private WaveBank waveBank;

		static AudioManager()
		{
		}

		private AudioManager(Microsoft.Xna.Framework.Game game, string settingsFile, string waveBankFile, string soundBankFile) : base(game)
		{
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

		protected override void Dispose(bool disposing)
		{
			try
			{
				if (disposing)
				{
					if (this.soundBank != null)
					{
						this.soundBank.Dispose();
						this.soundBank = null;
					}
					if (this.waveBank != null)
					{
						this.waveBank.Dispose();
						this.waveBank = null;
					}
					if (this.audioEngine != null)
					{
						this.audioEngine.Dispose();
						this.audioEngine = null;
					}
				}
			}
			finally
			{
				base.Dispose(disposing);
			}
		}

		public static AudioEngine getAudioEngine()
		{
			if (AudioManager.audioManager == null || AudioManager.audioManager.audioEngine == null || AudioManager.audioManager.soundBank == null || AudioManager.audioManager.waveBank == null)
			{
				return null;
			}
			return AudioManager.audioManager.audioEngine;
		}

		public static Cue GetCue(string cueName)
		{
			if (AudioManager.audioManager == null || AudioManager.audioManager.audioEngine == null || AudioManager.audioManager.soundBank == null || AudioManager.audioManager.waveBank == null)
			{
				return null;
			}
			return AudioManager.audioManager.soundBank.GetCue(cueName);
		}

		public static void Initialize(Microsoft.Xna.Framework.Game game, string settingsFile, string waveBankFile, string soundBankFile)
		{
			AudioManager.audioManager = new AudioManager(game, settingsFile, waveBankFile, soundBankFile);
			if (game != null)
			{
				game.Components.Add(AudioManager.audioManager);
			}
		}

		public static void PlayCue(string cueName)
		{
			if (AudioManager.audioManager != null && AudioManager.audioManager.audioEngine != null && AudioManager.audioManager.soundBank != null && AudioManager.audioManager.waveBank != null)
			{
				AudioManager.audioManager.soundBank.PlayCue(cueName);
			}
		}

		public override void Update(GameTime gameTime)
		{
			if (this.audioEngine != null)
			{
				this.audioEngine.Update();
			}
			base.Update(gameTime);
		}
	}
}