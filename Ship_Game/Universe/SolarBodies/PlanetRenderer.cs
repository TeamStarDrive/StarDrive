using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using Ship_Game.Data;
using Ship_Game.Data.Mesh;
using Ship_Game.Graphics;
using SynapseGaming.LightingSystem.Lights;
using Matrix = SDGraphics.Matrix;
using Vector2 = SDGraphics.Vector2;
using Vector3 = SDGraphics.Vector3;

namespace Ship_Game.Universe.SolarBodies
{
    public class PlanetRenderer : IDisposable
    {
        PlanetTypes Types;
        GraphicsDevice Device;

        public Model MeshSphere;
        Model MeshRings;
        Model MeshGlowRing;
        Model MeshGlowFresnel;

        BasicEffect FxPlanet;
        BasicEffect FxRings;
        BasicEffect FxClouds;
        BasicEffect FxAtmoColor;
        BasicEffect FxGlow;
        BasicEffect FxFresnel;
        Effect PlanetHaloFx;

        Texture2D TexRings;
        Texture2D TexAtmosphere;
        Texture2D TexGlow;
        Texture2D TexFresnel;

        Vector3 CamPos;

        public PlanetRenderer(GameContentManager content, PlanetTypes types)
        {
            Types = types;
            MeshSphere = content.RawContent.LoadModel(types.PlanetMesh);
            MeshRings = content.RawContent.LoadModel(types.RingsMesh[0]);
            TexRings = content.RawContent.LoadTexture(types.RingsMesh[1]);

            MeshGlowRing = content.LoadModel(types.GlowEffect[0]);
            TexGlow = content.RawContent.LoadAlphaTexture(types.GlowEffect[1], toPreMultipliedAlpha: false);

            MeshGlowFresnel = content.LoadModel(types.FresnelEffect[0]);
            TexFresnel = content.RawContent.LoadAlphaTexture(types.FresnelEffect[1], toPreMultipliedAlpha: false);

            // this old AtmosphereColor.dds has a weird checkered transparent blue texture
            TexAtmosphere = content.RawContent.LoadTexture("Model/SpaceObjects/AtmosphereColor.dds");

            FxPlanet = new BasicEffect(content.Device, null);
            FxPlanet.TextureEnabled = true;

            FxRings = new BasicEffect(content.Device, null);
            FxRings.TextureEnabled = true;
            FxRings.DiffuseColor = new Vector3(1f, 1f, 1f);

            FxClouds = new BasicEffect(content.Device, null);
            FxClouds.TextureEnabled = true;
            FxClouds.DiffuseColor = new Vector3(1f, 1f, 1f);
            FxClouds.LightingEnabled = true;
            FxClouds.DirectionalLight0.DiffuseColor  = new Vector3(1f, 1f, 1f);
            FxClouds.DirectionalLight0.SpecularColor = new Vector3(1f, 1f, 1f);
            FxClouds.SpecularPower = 4;

            FxAtmoColor = new BasicEffect(content.Device, null);
            FxAtmoColor.TextureEnabled = true;
            FxAtmoColor.LightingEnabled = true;
            FxAtmoColor.DirectionalLight0.DiffuseColor = new Vector3(1f, 1f, 1f);
            FxAtmoColor.DirectionalLight0.Enabled = true;
            FxAtmoColor.DirectionalLight0.SpecularColor = new Vector3(1f, 1f, 1f);
            FxAtmoColor.DirectionalLight0.Direction = new Vector3(0.98f, -0.025f, 0.2f);
            FxAtmoColor.DirectionalLight1.DiffuseColor = new Vector3(1f, 1f, 1f);
            FxAtmoColor.DirectionalLight1.Enabled = true;
            FxAtmoColor.DirectionalLight1.SpecularColor = new Vector3(1f, 1f, 1f);
            FxAtmoColor.DirectionalLight1.Direction = new Vector3(0.98f, -0.025f, 0.2f);

            FxGlow = new BasicEffect(content.Device, null);
            FxGlow.TextureEnabled = true;

            FxFresnel = new BasicEffect(content.Device, null);
            FxFresnel.TextureEnabled = true;

            PlanetHaloFx = content.Load<Effect>("Effects/PlanetHalo");
        }

        public void Dispose()
        {
            MeshSphere = null;
            MeshRings = null;
            MeshGlowRing = null;
            MeshGlowFresnel = null;

            FxPlanet?.Dispose(ref FxPlanet);
            FxRings?.Dispose(ref FxRings);
            FxClouds?.Dispose(ref FxClouds);
            FxAtmoColor?.Dispose(ref FxAtmoColor);
            FxGlow?.Dispose(ref FxGlow);
            FxFresnel?.Dispose(ref FxFresnel);

            TexRings?.Dispose(ref TexRings);
            TexAtmosphere?.Dispose(ref TexAtmosphere);
            TexGlow?.Dispose(ref TexGlow);
            TexFresnel?.Dispose(ref TexFresnel);

            Device = null;
        }

        static void SetViewProjection(BasicEffect fx, in Matrix view, in Matrix projection)
        {
            fx.View = view;
            fx.Projection = projection;
        }

        // update shaders
        public void BeginRendering(GraphicsDevice device, Vector3 cameraPos, in Matrix view, in Matrix projection)
        {
            Device = device;
            CamPos = cameraPos;

            SetViewProjection(FxPlanet, view, projection);
            SetViewProjection(FxClouds, view, projection);
            SetViewProjection(FxGlow, view, projection);
            SetViewProjection(FxFresnel, view, projection);
            SetViewProjection(FxAtmoColor, view, projection);
            SetViewProjection(FxRings, view, projection);
            PlanetHaloFx.Parameters["View"].SetValue(view);
            PlanetHaloFx.Parameters["Projection"].SetValue(projection);

            RenderStates.BasicBlendMode(device, additive:true, depthWrite:false);
        }

        public void EndRendering()
        {
            RenderStates.EnableDepthWrite(Device);
            RenderStates.SetCullMode(Device, CullMode.CullCounterClockwiseFace);
            RenderStates.DisableAlphaBlend(Device);
        }

        // This draws the clouds and atmosphere layers:
        // 1. layer: clouds sphere              (if PlanetType.Clouds == true)
        // 2. layer: fake fresnel effect of the atmosphere
        // 3. layer: fake glow effect around the planet
        // 4. layer: blueish transparent sphere (if PlanetType.Atmosphere == true)
        // 5. layer: subtle halo effect         (if PlanetType.Atmosphere == true)
        // 6. layer: rings                      (if any)
        public void Render(Planet p)
        {
            PlanetType type = p.PType;
            bool drawPlanetGlow = CamPos.Z < 300000.0f && type.Glow;

            if (!p.HasRings && !type.Clouds && !drawPlanetGlow)
                return;

            Vector3 sunToPlanet = (p.Position - p.ParentSystem.Position).ToVec3().Normalized();

            // tilted a bit differently than PlanetMatrix, and they constantly rotate
            Matrix cloudMatrix = default;
            var pos3d = Matrix.CreateTranslation(p.Position3D);
            var tilt = Matrix.CreateRotationX(-RadMath.Deg45AsRads);
            Matrix baseScale = p.ScaleMatrix;

            if (Types.NewRenderer)
            {
                RenderStates.SetCullMode(Device, CullMode.CullCounterClockwiseFace);

                FxPlanet.World = baseScale * Matrix.CreateRotationZ(-p.Zrotate) * tilt * pos3d;
                FxPlanet.Texture = type.DiffuseTex;
                // herp-derp, Specular and Normals not supported

                var lights = p.ParentSystem.Lights;
                SetLight(FxPlanet.DirectionalLight0, sunToPlanet, lights[0]);
                //SetLight(FxPlanet.DirectionalLight1, sunToPlanet, lights[1]);
                //SetLight(FxPlanet.DirectionalLight2, sunToPlanet, lights[2]);
                StaticMesh.Draw(MeshSphere, FxPlanet);
            }

            if (type.Clouds)
            {
                cloudMatrix = baseScale * Matrix.CreateRotationZ(-p.Zrotate / 1.5f) * tilt * pos3d;

                // default is CCW, this means we draw the clouds as usual
                RenderStates.SetCullMode(Device, CullMode.CullCounterClockwiseFace);

                FxClouds.World = Types.CloudsScaleMatrix * cloudMatrix;
                FxClouds.DirectionalLight0.Direction = sunToPlanet;
                FxClouds.DirectionalLight0.Enabled = true;
                StaticMesh.Draw(MeshSphere, FxClouds, type.CloudsMap);

                // for blue atmosphere and planet halo, use CW, which means the sphere is inverted
                RenderStates.SetCullMode(Device, CullMode.CullClockwiseFace);

                if (type.NoAtmosphere == false)
                {
                    // draw blueish transparent atmosphere sphere
                    // it is better visible near planet edges
                    FxAtmoColor.World = Types.AtmosphereScaleMatrix * cloudMatrix;
                    FxAtmoColor.DirectionalLight0.Direction = sunToPlanet;
                    FxAtmoColor.DirectionalLight0.Enabled = true;
                    StaticMesh.Draw(MeshSphere, FxAtmoColor, TexAtmosphere);
                }
            }

            if (drawPlanetGlow)
            {
                RenderPlanetGlow(p, type, pos3d, baseScale);
            }

            if (type.Clouds && type.NoHalo == false) // draw the halo effect
            {
                // inverted sphere
                RenderStates.SetCullMode(Device, CullMode.CullClockwiseFace);
                // This is a small shine effect on top of the atmosphere
                // It is very subtle
                //var diffuseLightDirection = new Vector3(-0.98f, 0.425f, -0.4f);
                //Vector3 camPosition = CamPos.ToVec3f();
                var camPosition = new Vector3(0.0f, 0.0f, 1500f);
                Vector3 diffuseLightDirection = -sunToPlanet;
                PlanetHaloFx.Parameters["World"].SetValue(Types.HaloScaleMatrix * cloudMatrix);
                PlanetHaloFx.Parameters["CameraPosition"].SetValue(camPosition);
                PlanetHaloFx.Parameters["DiffuseLightDirection"].SetValue(diffuseLightDirection);
                StaticMesh.Draw(MeshSphere, PlanetHaloFx);
            }

            if (p.HasRings)
            {
                RenderStates.SetCullMode(Device, CullMode.None);
                FxRings.World = Types.RingsScaleMatrix * baseScale * Matrix.CreateRotationX(p.RingTilt) * pos3d;
                StaticMesh.Draw(MeshRings, FxRings, TexRings);
            }
        }

        void RenderPlanetGlow(Planet p, PlanetType type, in Matrix pos3d, in Matrix baseScale)
        {
            RenderStates.SetCullMode(Device, CullMode.CullCounterClockwiseFace);

            // rotate the glow effect always towards the camera by getting direction from camera to planet
            // TODO: our camera works in coordinate space where +Z is out of the screen and -Z is background
            // TODO: but our 3D coordinate system works with -Z out of the screen and +Z is background
            // HACK: planetPos Z is flipped
            Vector3 planetPos = p.Position3D * new Vector3(1, 1, -1);

            // HACK: flip XZ so the planet glow mesh faces correctly towards us
            Vector3 camToPlanet = planetPos - CamPos;
            camToPlanet.X = -camToPlanet.X;
            camToPlanet.Z = -camToPlanet.Z;

            var rot = Matrix.CreateLookAt(Vector3.Zero, camToPlanet.Normalized(), Vector3.Up);
            Matrix world = Types.GlowScaleMatrix * baseScale * rot * pos3d;

            var glow = new Vector3(type.GlowColor.X, type.GlowColor.Y, type.GlowColor.Z);

            if (type.Fresnel > 0f)
            {
                FxFresnel.World = world;
                FxFresnel.DiffuseColor = glow;
                FxFresnel.Alpha = type.GlowColor.W * type.Fresnel;
                StaticMesh.Draw(MeshGlowFresnel, FxFresnel, TexFresnel);
            }

            {
                FxGlow.World = world;
                FxGlow.DiffuseColor = glow;
                FxGlow.Alpha = type.GlowColor.W;
                //FxGlow.EmissiveColor = glow;
                StaticMesh.Draw(MeshGlowRing, FxGlow, TexGlow);
            }
        }

        void SetLight(BasicDirectionalLight light, in Vector3 direction, ILight sunburnLight)
        {
            light.Enabled = true;
            light.Direction = direction;
            light.DiffuseColor = sunburnLight.CompositeColorAndIntensity;
        }
    }
}
