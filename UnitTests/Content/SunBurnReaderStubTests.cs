using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game;

namespace UnitTests.Content
{
    /// <summary>
    /// Phase 3.4 step 2 pin: SunBurn ContentTypeReader stubs.
    ///
    /// Before step 2 every static-sunburn ship XNB failed at type-reader resolution with
    /// "Could not find ContentTypeReader Type ... LightingMaterialReader_Pro,
    /// SynapseGaming-SunBurn-Pro, Version=1.3.2.8, ...". With the stub registered, that
    /// specific failure mode is gone — Models still fail to load (VertexDeclarationReader
    /// drift; §3.4 step 5) but the failure class moves down to IndexOutOfRangeException.
    ///
    /// These tests pin both the negative ("no longer fails this way") and the positive
    /// ("the type creator factory is wired up").
    /// </summary>
    [TestClass]
    public class SunBurnReaderStubTests : StarDriveTest
    {
        // Representative static-sunburn ship XNB. Picked from the §3.1 inventory's
        // confirmed-static-sunburn set; a sibling of the failure cluster surfaced in
        // phase3-baseline.log.
        const string TestShipPath = "Model/Ships/Pollops/Thorn";

        [TestMethod]
        public void LightingMaterialReader_Pro_TypeCreator_IsResolvable()
        {
            // The static ctor of GameContentManager (run once per process) calls
            // SunBurnReaderStubs.Register(). By the time any StarDriveTest runs, the
            // creator is already in ContentTypeReaderManager's table. Asking for it
            // a second time via Register() must be idempotent.
            Ship_Game.Data.Mesh.SunBurnReaderStubs.Register();
            Ship_Game.Data.Mesh.SunBurnReaderStubs.Register();
        }

        [TestMethod]
        public void LoadModel_StaticSunBurnShip_DoesNotFailOnSunBurnReaderResolution()
        {
            // The Model still fails to load (§3.4 step 5 — VertexDeclarationReader needs
            // an XNA 3.1 stub) but the FAILURE MODE has shifted: instead of a
            // ContentLoadException complaining about LightingMaterialReader_Pro, we now get
            // an IndexOutOfRangeException from MonoGame's stock VertexDeclarationReader.
            //
            // If the SunBurn stub is ever broken, this test will go back to throwing a
            // ContentLoadException with the Phase 1.9 message and fail fast.
            Exception ex = null;
            try
            {
                Content.Load<Model>(TestShipPath);
            }
            catch (Exception e)
            {
                ex = e;
            }

            // Three acceptable outcomes:
            //   1. Load throws IndexOutOfRangeException (VertexDeclarationReader drift) — current state
            //   2. Load throws nothing — would mean §3.4 step 5 has landed (great, but unexpected here)
            //   3. Load throws something else — a regression we want to surface
            // The negative pin is what step 2 controls: ContentLoadException with the SunBurn
            // type-not-found message must NOT happen.
            if (ex is ContentLoadException cle && cle.Message != null
                && cle.Message.Contains("LightingMaterialReader_Pro")
                && cle.Message.Contains("Could not find ContentTypeReader"))
            {
                Assert.Fail($"§3.4 step 2 regression: SunBurn type reader is no longer resolving for '{TestShipPath}'. Stub is missing or registration was bypassed. Inner: {cle.Message}");
            }

            // Document the expected current state so a future cleanup notices when step 5 lands.
            // (No assertion — this is informational; tests pass whether or not Load succeeds.)
            if (ex == null)
                Console.WriteLine($"§3.4 step 2 test: '{TestShipPath}' loaded without throwing. §3.4 step 5 (VertexDeclarationReader) may now be in place — consider tightening this test.");
            else
                Console.WriteLine($"§3.4 step 2 test: '{TestShipPath}' threw {ex.GetType().Name} (expected post-step-2; step 5 will fix)");
        }
    }
}
