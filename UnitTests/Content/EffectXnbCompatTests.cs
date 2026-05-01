using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game;

namespace UnitTests.Content
{
    /// <summary>
    /// Phase 2.2 stub-contract test. The XNA 3.1-baked Effect XNBs listed below cannot be
    /// loaded by MonoGame's MGFX-based Effect reader (D3DX fx_2_0 bytecode mismatch).
    /// Until each is rewritten in HLSL and MGFX-compiled, GameContentManager returns
    /// null for these names instead of throwing. This test pins that contract.
    ///
    /// When an effect is restored: remove its name from this list AND from
    /// GameContentManager.Phase2BrokenEffectXnbs.
    /// </summary>
    [TestClass]
    public class EffectXnbCompatTests : StarDriveTest
    {
        static readonly string[] StubbedEffects =
        {
            "Effects/BeamFX",
            "Effects/scale",
            "Effects/Thrust",
            "Effects/desaturate",
            "Effects/BasicFogOfWar",
            "Effects/PlanetHalo",
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
                Assert.Fail("Phase 2.2 stub contract broken — these effects loaded successfully and should be removed from the stub list:\n" + string.Join("\n", unexpected));
        }
    }
}
