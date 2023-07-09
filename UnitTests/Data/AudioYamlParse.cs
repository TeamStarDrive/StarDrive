using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game.Audio;
using Ship_Game;

namespace UnitTests.Data
{
    [TestClass]
    public class AudioYamlParse : StarDriveTest
    {
        static bool IsSupportedFileExtension(string fileName)
        {
            return fileName.EndsWith(".m4a")
                || fileName.EndsWith(".wav")
                || fileName.EndsWith(".mp3");
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
                    {
                        string relPath = "Audio/" + effect.Sound;
                        FileInfo fullPath = ResourceManager.GetModOrVanillaFile(relPath);
                        if (fullPath is not { Exists: true })
                            throw new FileNotFoundException($"Effect={effect.Id} Sound file does not exist: {relPath}");
                    }
                    else if (effect.Sounds is { Length: > 0 })
                    {
                        foreach (string sound in effect.Sounds)
                        {
                            string relPath = "Audio/" + sound;
                            FileInfo fullPath = ResourceManager.GetModOrVanillaFile(relPath);
                            if (fullPath is not { Exists: true })
                                throw new FileNotFoundException($"Effect={effect.Id} Sound file does not exist: {relPath}");
                        }
                    }
                }
            }
        }
    }
}
