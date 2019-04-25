using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Data;
using Ship_Game.Ships;

namespace Ship_Game
{
    public sealed class Thruster
    {
        public Model model;
        public Texture3D Noise;
        public float tscale;
        public Ship Parent;
        public Vector2 XMLPos;
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
        public Matrix scale;
        public Matrix[] matrices_combined = new Matrix[3];

        public Vector3 Up;
        public Vector3 Right;
        public Vector3 dir_to_camera;

        float distanceToParentCenter;
        float offsetAngle;
        Vector2 ThrusterCenter;

        public Thruster()
        {
        }

        public Thruster(Ship owner, float scale, Vector2 position)
        {
            Parent = owner;
            tscale = scale;
            XMLPos = position;
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

        public void InitializeForViewing()
        {
            var relativeShipCenter = new Vector2(512f, 512f);
            ThrusterCenter = new Vector2
            {
                X = XMLPos.X + 256f,
                Y = XMLPos.Y + 256f
            };
            distanceToParentCenter = ThrusterCenter.Distance(relativeShipCenter);
            offsetAngle            = relativeShipCenter.AngleToTarget(ThrusterCenter);
            SetPosition();
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

        public void SetPosition()
        {
            double angle = (offsetAngle.ToRadians() + Parent.Rotation + 1.57079637f);
            float distance = distanceToParentCenter;
            ThrusterCenter.Y = Parent.Center.Y + distance * -(float)Math.Sin(angle);
            ThrusterCenter.X = Parent.Center.X + distance * -(float)Math.Cos(angle);
            float xDistance = 256f - XMLPos.X;
            float zPos = (float)Math.Sin(Parent.yRotation) * xDistance + 15f;
            WorldPos = new Vector3(ThrusterCenter, zPos);
        }

        public void Update(Vector3 direction, float thrustsize, float thrustspeed, Vector3 camera_position, Color thrust0, Color thrust1)
        {
            var scaleFactors = new Vector3(tscale);
            heat = thrustsize.Clamped(0f, 1f);
            tick = tick + thrustspeed;
            colors[0] = thrust0;
            colors[1] = thrust1;
            world_matrix = Matrix.Identity;
            world_matrix.Forward = direction;
            dir_to_camera = WorldPos - camera_position;
            Vector3.Cross(ref direction, ref dir_to_camera, out Up);
            Up.Normalize();
            Vector3.Cross(ref direction, ref Up, out Right);
            world_matrix.Right = Right;
            world_matrix.Up = Up;
            Matrix.CreateScale(ref scaleFactors, out scale);
            Matrix.Multiply(ref world_matrix, ref scale, out world_matrix);
            inverse_scale_transpose = Matrix.Transpose(Matrix.Invert(world_matrix));
            world_matrix.Translation = WorldPos;
        }
    }
}