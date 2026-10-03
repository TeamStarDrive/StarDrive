using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDUtils;
using Ship_Game;
using Ship_Game.Audio;
using Ship_Game.Universe.SolarBodies;

namespace UnitTests.Data
{
    /// <summary>
    /// The PlanetAmbient category shipped with six cues that nothing ever played.
    /// They are now driven from PlanetType.AmbientCues while a colony screen is open.
    /// </summary>
    [TestClass]
    public class TestPlanetAmbience : StarDriveTest
    {
        static PlanetType FirstOfCategory(PlanetCategory category)
        {
            foreach (PlanetType t in ResourceManager.Planets.Types)
                if (t.Category == category)
                    return t;
            return null;
        }

        [TestMethod]
        public void VanillaPlanetTypesCarryTheirTerrainCue()
        {
            PlanetType barren = FirstOfCategory(PlanetCategory.Barren);
            Assert.IsNotNull(barren, "expected a Barren planet type");
            AssertEqual(2, barren.AmbientCues.Length, "Barren carries the gas giant bed as a second cue");
            AssertEqual("sd_planet_barren_01", barren.AmbientCues[0]);
            AssertEqual("sd_planet_gasgiant_01", barren.AmbientCues[1]);

            // Desert shares the barren cue but not the second one
            PlanetType desert = FirstOfCategory(PlanetCategory.Desert);
            Assert.IsNotNull(desert, "expected a Desert planet type");
            AssertEqual(1, desert.AmbientCues.Length);
            AssertEqual("sd_planet_barren_01", desert.AmbientCues[0]);

            PlanetType terran = FirstOfCategory(PlanetCategory.Terran);
            Assert.IsNotNull(terran, "expected a Terran planet type");
            AssertEqual("sd_planet_forest_01", terran.AmbientCues[0]);
        }

        // Spot-checking two types would let the other 34 lose their cue unnoticed:
        // the resolve test only validates cues that are present.
        [TestMethod]
        public void EveryTypeExceptGasGiantsCarriesACue()
        {
            int withCue = 0, silent = 0;
            foreach (PlanetType t in ResourceManager.Planets.Types)
            {
                if (t.AmbientCues.Length > 0) ++withCue;
                else                          ++silent;
            }

            AssertEqual(36, withCue, "expected every non-GasGiant vanilla type to name a cue");
            AssertEqual(7, silent, "expected only the GasGiant types to be silent");
        }

        [TestMethod]
        public void GasGiantsAreDeliberatelySilent()
        {
            foreach (PlanetType t in ResourceManager.Planets.Types)
                if (t.Category == PlanetCategory.GasGiant)
                    AssertEqual(0, t.AmbientCues.Length, $"{t.Name} should have no ambience");
        }

        [TestMethod]
        public void EveryAmbientCueResolvesToAPlanetAmbientEffect()
        {
            AudioConfig config = new();
            foreach (PlanetType t in ResourceManager.Planets.Types)
            {
                foreach (string cue in t.AmbientCues)
                {
                    SoundEffect effect = config.GetSoundEffect(cue);
                    Assert.IsNotNull(effect, $"{t.Name} names cue '{cue}' which no AudioConfig entry defines");
                    AssertEqual("PlanetAmbient", effect.Category.Name,
                        $"{t.Name} cue '{cue}' must live in the PlanetAmbient category");
                }
            }
        }

        // Driven once per frame from UniverseScreen.Update, so a cue that is still valid must
        // survive repeated calls. The MULTI-entry list is what makes this load-bearing: with a
        // single-entry list a re-pick returns the same string, so it passes either way.
        [TestMethod]
        public void TheCueIsKeptWhileTheNextColonyStillCarriesIt()
        {
            GameAudio.SetPlanetAmbience(null);
            AssertEqual(null, GameAudio.PlanetAmbienceCue);

            GameAudio.SetPlanetAmbience(new[] { "sd_planet_barren_01" });
            AssertEqual("sd_planet_barren_01", GameAudio.PlanetAmbienceCue);

            // a colony whose list merely contains the playing cue keeps it, frame after frame
            string[] shared = { "sd_planet_water_01", "sd_planet_barren_01", "sd_planet_forest_01" };
            for (int i = 0; i < 100; ++i)
            {
                GameAudio.SetPlanetAmbience(shared);
                AssertEqual("sd_planet_barren_01", GameAudio.PlanetAmbienceCue,
                    "a cue present in the new list must not be re-picked");
            }

            // a colony that does not carry it swaps the cue
            GameAudio.SetPlanetAmbience(new[] { "sd_planet_water_01" });
            AssertEqual("sd_planet_water_01", GameAudio.PlanetAmbienceCue);

            // leaving the colony screen is silence
            GameAudio.SetPlanetAmbience(Empty<string>.Array);
            AssertEqual(null, GameAudio.PlanetAmbienceCue);
        }

        // The real move this guards, now that Barren carries two cues: Barren -> Barren can be
        // playing a cue the next planet also lists, so it continues, while Barren -> Desert is
        // playing one the next planet does NOT list, so it must be replaced - even though both
        // colonies share sd_planet_barren_01.
        [TestMethod]
        public void MovingColoniesChecksThePlayingCueAgainstTheNewList()
        {
            string[] barren = { "sd_planet_barren_01", "sd_planet_gasgiant_01" };
            string[] desert = { "sd_planet_barren_01" };

            GameAudio.SetPlanetAmbience(null);
            GameAudio.SetPlanetAmbience(new[] { "sd_planet_gasgiant_01" });
            AssertEqual("sd_planet_gasgiant_01", GameAudio.PlanetAmbienceCue);

            for (int i = 0; i < 100; ++i)
            {
                GameAudio.SetPlanetAmbience(barren);
                AssertEqual("sd_planet_gasgiant_01", GameAudio.PlanetAmbienceCue,
                    "a Barren colony lists the gas giant cue, so it must keep playing");
            }

            GameAudio.SetPlanetAmbience(desert);
            AssertEqual("sd_planet_barren_01", GameAudio.PlanetAmbienceCue,
                "the playing cue is absent from the Desert list, so it must be replaced");

            GameAudio.SetPlanetAmbience(null);
        }

        // Birds and waves are an effect, not a score. SetVolume routes by a substring match on
        // "Music", so renaming the category would silently move it to the music slider.
        [TestMethod]
        public void PlanetAmbienceFollowsTheEffectsVolume()
        {
            AudioConfig config = new();
            config.SetVolume(music: 0.2f, effects: 0.9f);

            AssertEqual(0.0001f, 0.9f, config.GetCategory("PlanetAmbient").Volume,
                "planet ambience must follow the effects slider");
            AssertEqual(0.0001f, 0.2f, config.GetCategory("Music").Volume,
                "this test only means something while the two sliders differ");
        }

        [TestMethod]
        public void EveryCueInAListIsReachable()
        {
            string[] cues = { "sd_planet_barren_01", "sd_planet_water_01", "sd_planet_forest_01" };
            var seen = new HashSet<string>();
            for (int i = 0; i < 200; ++i)
            {
                GameAudio.SetPlanetAmbience(null);
                GameAudio.SetPlanetAmbience(cues);
                seen.Add(GameAudio.PlanetAmbienceCue);
            }

            AssertEqual(cues.Length, seen.Count, "expected the random pick to reach every cue in the list");
            GameAudio.SetPlanetAmbience(null);
        }
    }
}
