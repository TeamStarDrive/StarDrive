using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NAudio.Wave;
using Ship_Game.Audio;
using Ship_Game;
using Ship_Game.Audio.NAudio;

namespace UnitTests.Data
{
    [TestClass]
    public class TestAudioConfig : StarDriveTest
    {
        static bool IsSupportedFileExtension(string fileName)
        {
            return fileName.EndsWith(".m4a")
                || fileName.EndsWith(".aac")
                || fileName.EndsWith(".mp4")
                || fileName.EndsWith(".mp3")
                || fileName.EndsWith(".wav");
        }

        [TestMethod]
        public void CanParseMultipleSoundCategories()
        {
            AudioConfig config = new();
            AssertEqual(config.Categories.Length, 4);
            foreach (AudioCategory category in config.Categories)
            {
                AssertTrue(category.Name.NotEmpty(), "Category name cannot be empty");
                AssertGreaterThan(category.Volume, 0.01f, "Expected default volume to be set");
                AssertGreaterThan(category.SoundEffects.Length, 1, "Expected more than one SoundEffects");
                foreach (SoundEffect effect in category.SoundEffects)
                {
                    AssertTrue(effect.Id.NotEmpty(), "Effect Id cannot be empty");
                    AssertGreaterThan(effect.Volume, 0.01f, $"Expected effect={effect.Id} volume to be set");
                    if (effect.Sound.NotEmpty())
                    {
                        AssertTrue(IsSupportedFileExtension(effect.Sound), $"Effect={effect.Id} unsupported sound={effect.Sound}");
                    }
                    else if (effect.Sounds is { Length: > 0 })
                    {
                        foreach (string sound in effect.Sounds)
                            AssertTrue(IsSupportedFileExtension(sound), $"Effect={effect.Id} unsupported sound={sound}");
                    }
                    else
                    {
                        throw new AssertFailedException($"Expected effect={effect.Id} to have Sound or Sounds properties");
                    }
                }
            }
        }

        static FileInfo GetAudioPath(string soundPath)
        {
            string relPath = "Audio/" + soundPath;
            FileInfo fullPath = ResourceManager.GetModOrVanillaFile(relPath);
            if (fullPath is not { Exists: true })
                throw new FileNotFoundException($"Sound file does not exist: {relPath}");
            return fullPath;
        }

        [TestMethod]
        public void EnsureAllAudioFilesExist()
        {
            AudioConfig config = new();
            AssertEqual(config.Categories.Length, 4);
            foreach (AudioCategory category in config.Categories)
            {
                foreach (SoundEffect effect in category.SoundEffects)
                {
                    if (effect.Sound.NotEmpty())
                        GetAudioPath(effect.Sound);
                    else if (effect.Sounds is { Length: > 0 })
                        foreach (string sound in effect.Sounds)
                            GetAudioPath(sound);
                }
            }
        }

        [TestMethod]
        public void CanCacheAudioData()
        {
            FileInfo fullPath = GetAudioPath("Weapons/sd_ui_notification_research_01.m4a");
            WaveFormat format = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
            CachedSoundEffect cached = new(format, fullPath.FullName);
            AssertEqual(cached.AudioData.Length, 292864);
        }
    }
}
