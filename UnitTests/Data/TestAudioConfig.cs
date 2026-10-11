using System;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NAudio.Wave;
using Ship_Game.Audio;
using Ship_Game;
using Ship_Game.Audio.NAudio;
using Ship_Game.GameScreens.Scene;

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
        public void PerEffectMaxConcurrentOverridesCategoryLimit()
        {
            AudioConfig config = new();
            AudioCategory weapons = config.GetCategory("Weapons");

            SoundEffect capped = config.GetSoundEffect("sd_weapon_rocket_flight_01");
            AssertGreaterThan(capped.MaxConcurrent, 0, "Expected a per-effect cap on the flight cue");
            AssertGreaterThan(weapons.MaxConcurrentSoundsPerEffect, capped.MaxConcurrent,
                "This test only means something while the per-effect cap is the lower of the two");

            capped.NumActiveInstances = capped.MaxConcurrent - 1;
            AssertTrue(weapons.CanPlayEffect(capped), "Expected to play below the effect cap");
            capped.NumActiveInstances = capped.MaxConcurrent;
            AssertFalse(weapons.CanPlayEffect(capped), "Expected the effect cap to block, not the category cap");
            capped.NumActiveInstances = 0;

            SoundEffect uncapped = config.GetSoundEffect("sd_weapon_flakcannon_01");
            AssertEqual(0, uncapped.MaxConcurrent);
            uncapped.NumActiveInstances = weapons.MaxConcurrentSoundsPerEffect - 1;
            AssertTrue(weapons.CanPlayEffect(uncapped), "Expected the category cap to still apply");
            uncapped.NumActiveInstances = weapons.MaxConcurrentSoundsPerEffect;
            AssertFalse(weapons.CanPlayEffect(uncapped), "Expected the category cap to block");
            uncapped.NumActiveInstances = 0;
        }

        [TestMethod]
        public void WeaponExplosionSoundsAreCappedSoTheyCannotCrowdOutFireSounds()
        {
            AudioConfig config = new();
            foreach ((string id, string categoryName) in new[] { ("Explo1", "Explosions"),
                         ("sd_weapon_rocket_explode_01", "Weapons"), ("sd_weapon_flakcannon_explode_01", "Weapons") })
            {
                AudioCategory category = config.GetCategory(categoryName);
                SoundEffect effect = config.GetSoundEffect(id);
                AssertEqual(8, effect.MaxConcurrent, $"{id} is capped per effect");

                effect.NumActiveInstances = 7;
                AssertTrue(category.CanPlayEffect(effect), $"{id}: below the cap it still plays");
                effect.NumActiveInstances = 8;
                AssertFalse(category.CanPlayEffect(effect), $"{id}: at the cap it is turned away");
                effect.NumActiveInstances = 0;
            }
        }

        [TestMethod]
        public void EveryWeaponSoundIsDefined()
        {
            AudioConfig config = new();
            var cue = new Regex(@"<(FireCueName|DieCue|InFlightCue|ToggleSoundName)>\s*([^<]*?)\s*</\1>");
            int checkedCues = 0;
            foreach (FileInfo file in new DirectoryInfo("Content/Weapons").GetFiles("*.xml", SearchOption.AllDirectories))
            {
                string xml = Regex.Replace(File.ReadAllText(file.FullName), "<!--.*?-->", "", RegexOptions.Singleline);
                foreach (Match m in cue.Matches(xml))
                {
                    string id = m.Groups[2].Value;
                    if (id.IsEmpty())
                        continue;
                    ++checkedCues;
                    AssertTrue(config.GetSoundEffect(id) != null, $"{file.Name} <{m.Groups[1].Value}> names an undefined sound: {id}");
                }
            }
            AssertGreaterThan(checkedCues, 100, "setup: the weapon files were not found");
        }

        [TestMethod]
        public void TroopTakeOffAndLandingSoundsAreTurnedDownAndDoNotStack()
        {
            AudioConfig config = new();
            AudioCategory ground = config.GetCategory("Ground");
            foreach (string id in new[] { "sd_troop_takeoff", "sd_troop_land" })
            {
                SoundEffect effect = config.GetSoundEffect(id);
                AssertEqual(0.001f, 0.4f, effect.Volume, $"{id} plays with no distance falloff, so it is turned down");

                effect.NumActiveInstances = 1;
                AssertTrue(ground.CanPlayEffect(effect), $"{id}: a second quick launch still plays");
                effect.NumActiveInstances = 2;
                AssertFalse(ground.CanPlayEffect(effect), $"{id}: quick launches stop stacking at two");
                effect.NumActiveInstances = 0;
            }
        }

        [TestMethod]
        public void EmitterVolumeScaleMultipliesTheEffectiveVolume()
        {
            AudioConfig config = new();
            AudioCategory warp = config.GetCategory("Warp");
            config.SetListenerPos(SDGraphics.Vector3.Zero);
            var full  = new AudioEmitter(maxDistance: 1000f) { Position = new SDGraphics.Vector3(500, 0, 0) };
            var quiet = new AudioEmitter(maxDistance: 1000f, volumeScale: 0.65f) { Position = new SDGraphics.Vector3(500, 0, 0) };

            AssertEqual(0.0001f, 0.4f, full.GetEffectiveVolume(warp, 0.8f), "half the distance gives half the volume");
            AssertEqual(0.0001f, 0.26f, quiet.GetEffectiveVolume(warp, 0.8f), "the scale applies on top of the falloff");
        }

        [TestMethod]
        public void MainMenuShipsPlayTheirWarpSoundsAtHalfVolume()
        {
            var spawn = new ObjectSpawnInfo { Empire = (EmpireData)ResourceManager.MajorRaces[0], AI = new SceneShipAI() };
            var menuShip = new SceneObj(null, spawn);
            AssertEqual(0.0001f, 0.5f, menuShip.SoundEmitter.VolumeScale);
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
