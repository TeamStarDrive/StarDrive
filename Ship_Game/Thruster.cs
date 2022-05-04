using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Data;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;
using Vector3 = SDGraphics.Vector3;

namespace Ship_Game
{
    public sealed class Thruster
    {
        public Model model;
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
        public Vector4[] v4colors = new Vector4[2];

        public float heat = 1f;
        public float tick;

        public Matrix world_matrix;
        public Matrix inverse_scale_transpose;
        public Matrix[] matrices_combined = new Matrix[3];

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
        {
            heat = thrustSize.Clamped(0f, 1f);
            tick += thrustSpeed;
            colors[0] = thrust0;
            colors[1] = thrust1;

            world_matrix = Matrix.CreateScale(Scale)
                         * Matrix.CreateWorld(WorldPos, direction, Vector3.UnitZ);
            inverse_scale_transpose  = Matrix.Transpose(Matrix.Invert(world_matrix));
        }

        public void Draw(ref Matrix view, ref Matrix project)
        {
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
            model.Meshes[0].Draw();
        }

        static int ContentId;
        static Model DefaultModel;
        static Texture3D DefaultNoise;
        static Effect DefaultEffect;
        static readonly object ThrusterLocker = new object();

        static void InitializeDefaultEffects(GameContentManager content)
        {
            lock (ThrusterLocker)
            {
                if (DefaultModel != null && ContentId == ResourceManager.ContentId)
                    return;
                ContentId = ResourceManager.ContentId;
                DefaultModel  = content.Load<Model>("Effects/ThrustCylinderB");
                DefaultNoise  = content.Load<Texture3D>("Effects/NoiseVolume");
                DefaultEffect = content.Load<Effect>("Effects/Thrust");

                DefaultModel.Meshes[0].MeshParts[0].Effect = DefaultEffect;
            }
        }

        public void LoadAndAssignDefaultEffects(GameContentManager content)
        {
            InitializeDefaultEffects(content);
            LoadAndAssignEffects(DefaultModel, DefaultNoise, DefaultEffect);
        }

        void LoadAndAssignEffects(Model thrustCylinder, Texture3D noiseTexture, Effect effect)
        {
            model           = thrustCylinder;
            Noise           = noiseTexture;
            Effect          = effect;
            technique       = effect?.Techniques["thrust_technique"];
            shader_matrices = effect?.Parameters["world_matrices"];
            thrust_color    = effect?.Parameters["thrust_color"];
            effect_tick     = effect?.Parameters["ticks"];
            effect_noise    = effect?.Parameters["noise_texture"];
            world_matrix    = Matrix.Identity;
        }

        public void UpdatePosition(Vector2 center, float yRotation, in Vector3 dir)
        {
            WorldPos = GetPosition(center, yRotation, dir, LocalPos);
        }

        public static Vector3 GetPosition(in Vector2 center, float yRotation, in Vector3 fwd, in Vector3 thrusterPos)
        {
            Vector2 dir2d = fwd.ToVec2();
            Vector2 right = dir2d.RightVector();
            float zPos = thrusterPos.Z + (float)Math.Sin(yRotation) * -thrusterPos.X;
            Vector2 pos = center - dir2d*thrusterPos.Y + right*thrusterPos.X;
            return new Vector3(pos, zPos);
        }

        // 3D position calc is a bit different
        public static Vector3 GetPosition(in Vector3 center, in Vector3 fwd, in Vector3 thrusterPos)
        {
            Vector3 right = fwd.Cross(Vector3.Up); // forward x up = right
            Vector3 up = fwd.Cross(Vector3.Left);  // forward x left = up
            return center + fwd*thrusterPos.Y + right*thrusterPos.X + up*thrusterPos.Z;
        }
    }
}