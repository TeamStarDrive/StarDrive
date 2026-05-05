using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game;

namespace UnitTests.Content
{
    /// <summary>
    /// Phase 2.2 stub contract + Phase 3.3 restoration pin.
    ///
    /// Effects in <see cref="StubbedEffects"/> still return null from GameContentManager
    /// (XNA 3.1 D3DX fx_2_0 bytecode incompatible with MGFX, no .mgfxo sibling yet).
    /// As each is hand-rewritten and shipped as .mgfxo, move its name from StubbedEffects
    /// to <see cref="RestoredEffects"/>. The matching set in
    /// GameContentManager.Phase2BrokenEffectXnbs must drop the entry too — without it
    /// the .xnb→.mgfxo fallback works but the stub still short-circuits if the .mgfxo
    /// is ever absent.
    /// </summary>
    [TestClass]
    public class EffectXnbCompatTests : StarDriveTest
    {
        static readonly string[] StubbedEffects =
        {
        };

        // Phase 3.3 restored — each entry is a hand-rewritten .fx compiled by mgfxc
        // 3.8.1.303 and shipped as game/Content/<asset>.mgfxo. The .xnb→.mgfxo
        // fallback in GameContentManager.LoadAsset reads the .mgfxo, ignoring the
        // legacy XNA 3.1 .xnb still present on disk for mod compatibility.
        static readonly (string asset, string technique, string firstPass)[] RestoredEffects =
        {
            ("Effects/desaturate",     "Desaturate",       "Pass1"),
            ("Effects/PlanetHalo",     "Planet",           "P1"),
            ("Effects/scale",          "Technique1",       "Pass1"),
            ("Effects/Thrust",         "thrust_technique", "P1"),
            ("Effects/BeamFX",         "Technique1",       "Pass1"),
            ("Effects/BasicFogOfWar",  "BasicFogOfWar",    "Pass1"),
        };

        [TestMethod]
        public void StubbedEffectXnbs_ReturnNullWithoutThrowing()
        {
            var unexpected = new List<string>();
            foreach (string asset in StubbedEffects)
            {
                Effect fx = ResourceManager.RootContent.Load<Effect>(asset);
                if (fx != null)
                    unexpected.Add($"{asset}: expected null (stub), got {fx.GetType().Name}");
            }

            if (unexpected.Count > 0)
                Assert.Fail("Stub contract broken — these effects loaded successfully and should move to RestoredEffects (and drop from GameContentManager.Phase2BrokenEffectXnbs):\n" + string.Join("\n", unexpected));
        }

        [TestMethod]
        public void RestoredEffectXnbs_LoadViaMgfxoSibling()
        {
            foreach ((string asset, string technique, string firstPass) in RestoredEffects)
            {
                Effect fx = ResourceManager.RootContent.Load<Effect>(asset);
                Assert.IsNotNull(fx, $"{asset}: load returned null — .mgfxo sibling missing or fallback wiring broken.");
                Assert.IsNotNull(fx.CurrentTechnique, $"{asset}: CurrentTechnique is null");
                Assert.AreEqual(technique, fx.CurrentTechnique.Name, $"{asset}: technique name mismatch");
                Assert.IsTrue(fx.CurrentTechnique.Passes.Count >= 1, $"{asset}: no passes in technique");
                Assert.AreEqual(firstPass, fx.CurrentTechnique.Passes[0].Name, $"{asset}: first pass name mismatch");
            }
        }
    }
}
