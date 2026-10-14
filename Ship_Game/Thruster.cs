using System;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;
using SDGraphics;
using Ship_Game.Data;
using Ship_Game.Data.Mesh;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;
using Vector3 = SDGraphics.Vector3;
using Matrix = SDGraphics.Matrix;
using XnaMatrix = Microsoft.Xna.Framework.Matrix;
using XnaVector4 = Microsoft.Xna.Framework.Vector4;

namespace Ship_Game
{
    public sealed class Thruster
    {
        public StaticMesh ThrustMesh;
        public Texture3D Noise;
        public float Scale;
        public Ship Parent;
        public Vector3 LocalPos;
        public Vector3 WorldPos;

        public Effect Effect;
        public EffectTechnique technique;

        public EffectParameter shader_matrices;
        public EffectParameter thrust_color;
        public EffectParameter effect_tick;
        public EffectParameter effect_noise;

        public Color[] colors = new Color[2];
        public XnaVector4[] v4colors = new XnaVector4[2];

        public float heat = 1f;
        public float tick;

        public Matrix world_matrix;
        public Matrix inverse_scale_transpose;
        public XnaMatrix[] matrices_combined = new XnaMatrix[3];

        public Thruster()
        {
        }

        public Thruster(Ship owner, float scale, Vector3 position)
        {
            Parent = owner;
            Scale = scale;
            LocalPos = position;
            UpdatePosition(owner.Position, owner.YRotation, owner.Direction3D);
        }

        public void Update(Vector3 direction, float thrustSize, float thrustSpeed, Color thrust0, Color thrust1)
            => Update(direction, Vector3.UnitZ, thrustSize, thrustSpeed, thrust0, thrust1);

        public void Update(Vector3 direction, Vector3 up, float thrustSize, float thrustSpeed, Color thrust0, Color thrust1)
        {
            heat = thrustSize.Clamped(0f, 1f);
            tick += thrustSpeed;
            colors[0] = thrust0;
            colors[1] = thrust1;

            world_matrix = Matrix.CreateScale(Scale)
                         * Matrix.CreateWorld(WorldPos, direction, up);
            inverse_scale_transpose  = Matrix.Transpose(Matrix.Invert(world_matrix));
        }

        public void Draw(ref Matrix view, ref Matrix project)
        {
            // defense-in-depth: Thrust.mgfxo restored §3.3 (2026-05-04); guard
            // catches missing-file regressions instead of NRE-ing the draw call.
            if (Effect == null || technique == null)
                return;

            matrices_combined[0] = world_matrix;
            matrices_combined[1] = (world_matrix * view) * project;
            matrices_combined[2] = inverse_scale_transpose;
            Effect.CurrentTechnique = technique;
            shader_matrices.SetValue(matrices_combined);
            v4colors[0] = colors[0].ToVector4();
            v4colors[1] = colors[1].ToVector4();
            v4colors[0].W = heat;
            thrust_color.SetValue(v4colors);
            effect_tick.SetValue(tick);
            effect_noise.SetValue(Noise);

            ThrustMesh.Draw(Effect);
        }

        static int ContentId;
        static StaticMesh DefaultModel;
        static Texture3D DefaultNoise;
        static Effect DefaultEffect;
        static readonly object ThrusterLocker = new();

        static void InitializeDefaultEffects(GameContentManager content)
        {
            lock (ThrusterLocker)
            {
                if (DefaultModel != null && ContentId == ResourceManager.ContentId)
                    return;
                ContentId = ResourceManager.ContentId;
                DefaultModel = content.LoadStaticMesh("Effects/ThrustCylinderB");
                DefaultNoise = content.Load<Texture3D>("Effects/NoiseVolume");
                DefaultEffect = content.Load<Effect>("Effects/Thrust");
            }
        }

        public void LoadAndAssignDefaultEffects(GameContentManager content)
        {
            InitializeDefaultEffects(content);
            LoadAndAssignEffects(DefaultModel, DefaultNoise, DefaultEffect);
        }

        void LoadAndAssignEffects(StaticMesh thrustCylinder, Texture3D noiseTexture, Effect effect)
        {
            ThrustMesh = thrustCylinder;
            Noise = noiseTexture;
            Effect = effect;
            technique = effect?.Techniques["thrust_technique"];
            shader_matrices = effect?.Parameters["world_matrices"];
            thrust_color = effect?.Parameters["thrust_color"];
            effect_tick = effect?.Parameters["ticks"];
            effect_noise = effect?.Parameters["noise_texture"];
            world_matrix = Matrix.Identity;
        }

        public void UpdatePosition(Vector2 center, float yRotation, in Vector3 dir, float deltaZ = 0)
        {
            WorldPos = GetPosition(center, yRotation, dir, LocalPos, deltaZ);
        }

        public static Vector3 GetPosition(in Vector2 center, float yRotation, in Vector3 fwd, in Vector3 thrusterPos, float deltaZ = 0)
        {
            Vector2 dir2d = fwd.ToVec2();
            Vector2 right = dir2d.RightVector();
            // Full RotateY(yRotation) on (X, _, Z) — matches the ship mesh
            // transform in Ship_Update.cs. Without the Z*sin cross-term, back-
            // of-deck thrusters (Z>0) drift the wrong way during bank turns.
            float cosY = (float)Math.Cos(yRotation);
            float sinY = (float)Math.Sin(yRotation);
            float bankedX = thrusterPos.X*cosY + thrusterPos.Z*sinY;
            float bankedZ = thrusterPos.Z*cosY - thrusterPos.X*sinY;
            Vector2 pos = center - dir2d*thrusterPos.Y + right*bankedX;
            return new Vector3(pos, bankedZ+deltaZ);
        }
    }
}