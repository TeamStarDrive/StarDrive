using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Data;
using Ship_Game.Data.Mesh;
using Ship_Game.Data.Texture;

namespace Ship_Game.Universe
{
    public class PlanetRenderer : IDisposable
    {
        public Model MeshSphere;
        public Model MeshRings;
        public Model MeshGlowRing;
        public Model MeshGlowFresnel;
        public Model MeshAtmosphere;

        public BasicEffect FxRings;
        public BasicEffect FxClouds;
        public BasicEffect FxAtmoColor;
        public BasicEffect FxGlow;
        public Effect PlanetHaloFx;

        public Texture2D TexClouds;
        public Texture2D TexRings;
        public Texture2D TexAtmosphere;
        public Texture2D TexGlow;
        public Texture2D TexFresnel;

        Vector3 CamPos;
        GraphicsDevice Device;

        public PlanetRenderer(GameContentManager content)
        {
            MeshSphere = content.LoadModel("Model/SpaceObjects/planet_sphere.obj");
            MeshRings  = content.LoadModel("Model/SpaceObjects/planet_rings.obj");
            MeshGlowRing    = content.LoadModel("Model/SpaceObjects/planet_glow_ring.obj");
            MeshGlowFresnel = content.LoadModel("Model/SpaceObjects/planet_glow_fresnel.obj");
            MeshAtmosphere   = content.LoadModel("Model/SpaceObjects/atmo_sphere.obj");
            
            TexClouds      = content.Load<Texture2D>("Model/SpaceObjects/earthcloudmap.dds");
            TexRings = content.Load<Texture2D>("Model/SpaceObjects/planet_rings.dds");
            TexAtmosphere = content.Load<Texture2D>("Model/SpaceObjects/AtmosphereColor.dds");

            TexGlow = content.Load<Texture2D>("Model/SpaceObjects/planet_glow.png");
            ImageUtils.ConvertToAlphaMap(TexGlow, preMultiplied:false);
            TexFresnel = content.Load<Texture2D>("Model/SpaceObjects/planet_fresnel.png");
            ImageUtils.ConvertToAlphaMap(TexFresnel, preMultiplied:false);

            FxRings = new BasicEffect(content.Device, null);
            FxRings.Texture = TexRings;
            FxRings.TextureEnabled = true;
            FxRings.DiffuseColor = new Vector3(1f, 1f, 1f);

            FxClouds = new BasicEffect(content.Device, null);
            FxClouds.Texture = TexClouds;
            FxClouds.TextureEnabled = true;
            FxClouds.DiffuseColor = new Vector3(1f, 1f, 1f);
            FxClouds.LightingEnabled = true;
            FxClouds.DirectionalLight0.DiffuseColor  = new Vector3(1f, 1f, 1f);
            FxClouds.DirectionalLight0.SpecularColor = new Vector3(1f, 1f, 1f);
            FxClouds.SpecularPower = 4;

            FxAtmoColor = new BasicEffect(content.Device, null);
            FxAtmoColor.Texture = TexAtmosphere;
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

            PlanetHaloFx = content.Load<Effect>("Effects/PlanetHalo");
        }

        public void Dispose()
        {
            MeshSphere = null;
            MeshRings = null;
            MeshGlowRing = null;
            MeshAtmosphere = null;
            TexClouds = null;
            TexRings = null;
            TexAtmosphere = null;

            FxRings?.Dispose();
            FxClouds?.Dispose();
            FxAtmoColor?.Dispose();
            FxGlow?.Dispose();

            FxRings = null;
            FxClouds = null;
            FxAtmoColor = null;
            FxGlow = null;

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
            SetViewProjection(FxClouds, view, projection);
            SetViewProjection(FxGlow, view, projection);
            SetViewProjection(FxAtmoColor, view, projection);
            SetViewProjection(FxRings, view, projection);
            PlanetHaloFx.Parameters["View"].SetValue(view);
            PlanetHaloFx.Parameters["Projection"].SetValue(projection);

            RenderState rs = device.RenderState;
            device.SamplerStates[0].AddressU = TextureAddressMode.Wrap;
            device.SamplerStates[0].AddressV = TextureAddressMode.Wrap;
            rs.AlphaBlendEnable = true;
            rs.AlphaBlendOperation = BlendFunction.Add;
            rs.SourceBlend = Blend.SourceAlpha;
            rs.DestinationBlend = Blend.InverseSourceAlpha;
            rs.DepthBufferWriteEnable = false;
        }

        public void EndRendering()
        {
            RenderState rs = Device.RenderState;
            rs.DepthBufferWriteEnable = true;
            rs.CullMode = CullMode.CullCounterClockwiseFace;
            rs.AlphaBlendEnable = false;
        }

        bool CanDrawPlanetGlow(Planet p) => CamPos.Z < 300000.0f && p.Type.Glow.HasValue;

        // This draws the clouds and atmosphere layers:
        // 1. layer: clouds sphere              (if PlanetType.Clouds == true)
        // 2. layer: fake fresnel effect of the atmosphere
        // 3. layer: fake glow effect around the planet
        // 4. layer: blueish transparent sphere (if PlanetType.Atmosphere == true)
        // 5. layer: subtle halo effect         (if PlanetType.Atmosphere == true)
        // 6. layer: rings                      (if any)
        public void Render(Planet p)
        {
            if (!p.HasRings && !p.Type.Clouds && !p.Type.Atmosphere && !CanDrawPlanetGlow(p))
                return;

            bool drawPlanetGlow = CanDrawPlanetGlow(p);

            Vector3 sunToPlanet = (p.Center - p.ParentSystem.Position).ToVec3().Normalized();

            if (p.Type.Clouds)
            {
                // default is CCW, this means we draw the clouds as usual
                Device.RenderState.CullMode = CullMode.CullCounterClockwiseFace;

                var cloudsFx = FxClouds;
                cloudsFx.World = SolarSystemBody.PlanetCloudsScale * p.CloudMatrix;

                cloudsFx.DirectionalLight0.Direction = sunToPlanet;
                cloudsFx.DirectionalLight0.Enabled = true;
                StaticMesh.Draw(MeshSphere, cloudsFx);
            }

            if (p.Type.Atmosphere)
            {
                // for blue atmosphere and planet halo, use CW, which means the sphere is inverted
                Device.RenderState.CullMode = CullMode.CullClockwiseFace;

                // draw blueish transparent atmosphere sphere
                // it is better visible near planet edges
                FxAtmoColor.World = SolarSystemBody.PlanetBlueAtmosphereScale * p.CloudMatrix;
                FxAtmoColor.DirectionalLight0.Direction = sunToPlanet;
                FxAtmoColor.DirectionalLight0.Enabled = true;
                StaticMesh.Draw(MeshSphere, FxAtmoColor);
            }

            if (drawPlanetGlow)
            {
                Device.RenderState.CullMode = CullMode.CullCounterClockwiseFace;

                // rotate the glow effect always towards the camera by getting direction from camera to planet
                // TODO: our camera works in coordinate space where +Z is out of the screen and -Z is background
                // TODO: but our 3D coordinate system works with -Z out of the screen and +Z is background
                // HACK: planetPos Z is flipped
                Vector3 planetPos = p.Center3D * new Vector3(1, 1, -1);

                // HACK: flip XZ so the planet glow mesh faces correctly towards us
                Vector3 camToPlanet = planetPos - CamPos;
                camToPlanet.X = -camToPlanet.X;
                //camToPlanet.Y = -camToPlanet.Y;
                camToPlanet.Z = -camToPlanet.Z;

                var scale = Matrix.CreateScale(10f * p.Scale);
                var rot = Matrix.CreateLookAt(Vector3.Zero, camToPlanet.Normalized(), Vector3.Up);
                var pos3d = Matrix.CreateTranslation(p.Center3D);
                Matrix glowMatrix = scale * rot * pos3d;
                var color = p.Type.Glow.Value.ToVector4();
                FxGlow.DiffuseColor = new Vector3(color.X, color.Y, color.Z);
                FxGlow.World = glowMatrix;
                FxGlow.Alpha = color.W;
                StaticMesh.Draw(MeshGlowFresnel, FxGlow, TexFresnel);
                StaticMesh.Draw(MeshGlowRing, FxGlow, TexGlow);
            }

            if (p.Type.Atmosphere)
            {
                // inverted sphere
                Device.RenderState.CullMode = CullMode.CullClockwiseFace;
                // This is a small shine effect on top of the atmosphere
                // It is very subtle
                //var diffuseLightDirection = new Vector3(-0.98f, 0.425f, -0.4f);
                //Vector3 camPosition = CamPos.ToVec3f();
                var camPosition = new Vector3(0.0f, 0.0f, 1500f);
                Vector3 diffuseLightDirection = -sunToPlanet;
                PlanetHaloFx.Parameters["World"].SetValue(SolarSystemBody.PlanetHaloScale * p.CloudMatrix);
                PlanetHaloFx.Parameters["CameraPosition"].SetValue(camPosition);
                PlanetHaloFx.Parameters["DiffuseLightDirection"].SetValue(diffuseLightDirection);
                StaticMesh.Draw(MeshSphere, PlanetHaloFx);
            }

            if (p.HasRings)
            {
                Device.RenderState.CullMode = CullMode.None;
                FxRings.World = SolarSystemBody.PlanetRingsScale * p.RingWorld;
                StaticMesh.Draw(MeshRings, FxRings);
            }
        }
    }
}
