﻿using System;
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
            AssertEqual(8, config.Categories.Length);
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

        /// <summary>
        /// This is an interesting unit test approach for the main release build.
        /// It ensures that all audio files referenced in AudioConfig actually exist before installer is packaged.
        /// </summary>
        [TestMethod]
        public void EnsureAllAudioFilesExist()
        {
            AudioConfig config = new();
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
            FileInfo fullPath = GetAudioPath("UI/sd_ui_notification_research_01.m4a");
            WaveFormat format = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
            CachedSoundEffect cached = new(format, fullPath.FullName);
            AssertEqual(292864, cached.NumSamples);

            // test that we can read the cached data
            // create an inconveniently sized buffer to guarantee multiple cross-chunk reads
            float[] buffer1 = new float[(int)(format.SampleRate * format.Channels * 0.66f)];
            ISampleProvider reader1 = cached.CreateReader();
            int totalSamples1 = 0;
            for (int n; (n = reader1.Read(buffer1, 0, buffer1.Length)) > 0; totalSamples1 += n) {}
            AssertEqual(292864, totalSamples1);

            // read again, but this time with a much bigger buffer
            float[] buffer2 = new float[(int)(format.SampleRate * format.Channels * 2.66f)];
            ISampleProvider reader2 = cached.CreateReader();
            int totalSamples2 = 0;
            for (int n; (n = reader2.Read(buffer2, 0, buffer2.Length)) > 0; totalSamples2 += n) {}
            AssertEqual(292864, totalSamples2);
        }
    }
}
