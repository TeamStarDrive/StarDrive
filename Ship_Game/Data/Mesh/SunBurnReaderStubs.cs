using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using SDUtils;
using SynapseGaming.LightingSystem.Effects.Forward;
using SynapseGaming.LightingSystem.Processors;

namespace Ship_Game.Data.Mesh
{
    // Phase 3.4 step 2: SunBurn ContentTypeReader stubs.
    //
    // The 122 static-sunburn ship/hull/projectile/station XNBs in the §3.1 inventory all
    // have an identical reader chain: ModelReader, StringReader, VertexDeclarationReader,
    // VertexBufferReader, IndexBufferReader, and one SunBurn type — the
    // LightingMaterialReader_Pro from `SynapseGaming-SunBurn-Pro 1.3.2.8`. SDSunBurn is
    // excluded from the build (Phase 1.9), so without a stub here MonoGame's
    // ContentTypeReaderManager.GetTypeReader fails with "Could not find ContentTypeReader
    // Type" and the entire Model load aborts.
    //
    // The stub mirrors the original reader's byte layout exactly (taken verbatim from
    // SynapseGaming-SunBurn-Pro/SynapseGaming/LightingSystem/Processors/Forward/LightingMaterialReader_Pro.cs)
    // and returns a `LightingEffect` populated with the parsed material data. LightingEffect
    // is itself a Phase-1.9 stub (BasicEffect-derived; in `SunBurnStubs.cs`) so the GPU can
    // render with diffuse texture + lighting via BasicEffect's defaults until §3.4+ adds
    // a real shader path.
    public static class SunBurnReaderStubs
    {
        // Exact-string match required by ContentTypeReaderManager.GetTypeReader. Confirmed via
        // §3.1 inventory CSV (asset-survey.csv) — every static-sunburn XNB references this
        // exact, fully-qualified type name including the SunBurn assembly version + key.
        const string LightingMaterialReader_Pro_TypeKey =
            "SynapseGaming.LightingSystem.Processors.Forward.LightingMaterialReader_Pro, " +
            "SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd";

        static bool Registered;

        public static void Register()
        {
            if (Registered) return;
            Registered = true;
            ContentTypeReaderManager.AddTypeCreator(LightingMaterialReader_Pro_TypeKey,
                () => new LightingMaterialReader_ProStub());
        }
    }

    /// <summary>
    /// Stub for SynapseGaming-SunBurn-Pro 1.3.2.8's LightingMaterialReader_Pro. Byte layout
    /// taken verbatim from the decompiled source. Returns a LightingEffect (BasicEffect-derived
    /// stub) so MonoGame's ModelReader can store it in ModelMeshPart.Effect and the codebase's
    /// existing rendering paths that expect SunBurn material properties continue to work.
    /// </summary>
    public class LightingMaterialReader_ProStub : ContentTypeReader<LightingEffect>
    {
        protected override LightingEffect Read(ContentReader input, LightingEffect existingInstance)
        {
            // Honor the same cache contract the original reader used. GameContentManager
            // implements IEffectCache via the LoadedAssets dictionary; repeat loads of the
            // same Model XNB short-circuit here.
            var cache = input.ContentManager as IEffectCache;
            string cacheKey = input.AssetName + ".sunburnfx";
            if (cache != null && cache.TryGetEffect(cacheKey, out LightingEffect cached))
                return cached;

            var service = (IGraphicsDeviceService)input.ContentManager.ServiceProvider.GetService(typeof(IGraphicsDeviceService));
            GraphicsDevice device = service.GraphicsDevice;
            var fx = new LightingEffect(device);

            fx.MaterialName = input.ReadString();
            if (string.IsNullOrEmpty(fx.MaterialName))
                fx.MaterialName = input.AssetName;

            fx.MaterialFile             = input.ReadString();
            fx.ProjectFile              = input.ReadString();
            fx.DiffuseMapFile           = input.ReadString();
            fx.DiffuseMapTexture        = input.ReadExternalReference<Texture2D>();
            // DiffuseAmbient and Parallax aren't stored on the BaseMaterialEffect stub
            // (Phase 1.9 didn't include them). Read and discard the bytes so the stream
            // pointer stays aligned for the rest of the reader.
            _ = input.ReadString();                                  // DiffuseAmbientMapFile
            _ = input.ReadExternalReference<Texture2D>();            // DiffuseAmbientMapTexture
            fx.EmissiveMapFile          = input.ReadString();
            fx.EmissiveMapTexture       = input.ReadExternalReference<Texture2D>();
            fx.NormalMapFile            = input.ReadString();
            fx.NormalMapTexture         = input.ReadExternalReference<Texture2D>();
            fx.SpecularColorMapFile     = input.ReadString();
            fx.SpecularColorMapTexture  = input.ReadExternalReference<Texture2D>();
            _ = input.ReadString();                                  // ParallaxMapFile
            _ = input.ReadExternalReference<Texture2D>();            // ParallaxMapTexture (original called CoreUtils.ConvertToLuminance8 — gone)
            fx.Skinned                  = input.ReadBoolean();
            fx.DoubleSided              = input.ReadBoolean();
            var transparencyMode        = (TransparencyMode)input.ReadInt32();
            float transparency          = input.ReadSingle();
            fx.SetTransparencyModeAndMap(transparencyMode, transparency, fx.DiffuseMapTexture);
            fx.SpecularPower                 = input.ReadSingle();
            fx.SpecularAmount                = input.ReadSingle();
            fx.FresnelReflectBias            = input.ReadSingle();
            fx.FresnelReflectOffset          = input.ReadSingle();
            fx.FresnelMicrofacetDistribution = input.ReadSingle();
            fx.ParallaxScale                 = input.ReadSingle();
            fx.ParallaxOffset                = input.ReadSingle();
            Vector4 diffuse  = input.ReadVector4();
            Vector4 emissive = input.ReadVector4();
            fx.DiffuseColor  = new Vector3(diffuse.X, diffuse.Y, diffuse.Z);
            fx.EmissiveColor = new Vector3(emissive.X, emissive.Y, emissive.Z);
            fx.AddressModeU  = (TextureAddressMode)input.ReadInt32();
            fx.AddressModeV  = (TextureAddressMode)input.ReadInt32();
            fx.AddressModeW  = (TextureAddressMode)input.ReadInt32();

            // BlockUtil.SkipBlock — int32 length, skip that many bytes. Original was
            // wrapped in try/catch; we do the same to match its silent-tolerant behavior
            // for content with truncated tail blocks.
            try
            {
                int blockSize = input.ReadInt32();
                if (blockSize > 0) input.ReadBytes(blockSize);
            }
            catch { /* match original */ }

            int sentinel = input.ReadInt32();
            if (sentinel != 1234)
                throw new ContentLoadException(
                    $"LightingMaterialReader_ProStub: sentinel mismatch ({sentinel} != 1234) in '{input.AssetName}' — material XNB layout drift");

            cache?.AddEffect(cacheKey, fx);
            return fx;
        }
    }
}
