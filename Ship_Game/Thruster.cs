using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
using System;
using System.Collections.ObjectModel;

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

        //public float allscale = 1f;          //Not referenced in code, removing to save memory

        private float distanceToParentCenter;

        private float offsetAngle;

        private Vector2 ThrusterCenter;


        public void Draw(ref Matrix view, ref Matrix project)
        {
            this.matrices_combined[0] = this.world_matrix;
            this.matrices_combined[1] = (this.world_matrix * view) * project;
            this.matrices_combined[2] = this.inverse_scale_transpose;
            Effect.CurrentTechnique = this.technique;
            this.shader_matrices.SetValue(this.matrices_combined);
            this.v4colors[0] = this.colors[0].ToVector4();
            this.v4colors[1] = this.colors[1].ToVector4();
            this.v4colors[0].W = this.heat;
            this.thrust_color.SetValue(this.v4colors);
            this.effect_tick.SetValue(this.tick);
            this.effect_noise.SetValue(this.Noise);
            this.model.Meshes[0].Draw();
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

        private static Model DefaultModel;
        private static Texture3D DefaultNoise;
        private static Effect DefaultEffect;
        private static readonly object ThrusterLocker = new object();

        private static void InitializeDefaultEffects(GameContentManager content)
        {
            lock (ThrusterLocker)
            {
                if (DefaultModel != null)
                    return;
                DefaultModel  = content.Load<Model>("Effects/ThrustCylinderB");
                DefaultNoise  = content.Load<Texture3D>("Effects/NoiseVolume");
                DefaultEffect = content.Load<Effect>("Effects/Thrust");

                DefaultModel.Meshes[0].MeshParts[0].Effect = DefaultEffect;
            }
        }

        public void LoadAndAssignDefaultEffects(GameContentManager content)
        {
            if (DefaultModel == null) InitializeDefaultEffects(content);
            LoadAndAssignEffects(DefaultModel, DefaultNoise, DefaultEffect);
        }

        private void LoadAndAssignEffects(Model thrustCylinder, Texture3D noiseTexture, Effect effect)
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


        public void prepare_effect(ref Matrix view, ref Matrix project, Effect effect)
        {
            this.matrices_combined[0] = this.world_matrix;
            this.matrices_combined[1] = (this.world_matrix * view) * project;
            this.matrices_combined[2] = this.inverse_scale_transpose;
            this.shader_matrices.SetValue(this.matrices_combined);
            this.v4colors[0] = this.colors[0].ToVector4();
            this.v4colors[1] = this.colors[1].ToVector4();
            this.v4colors[0].W = this.heat;
            this.thrust_color.SetValue(this.v4colors);
            this.effect_tick.SetValue(this.tick);
            this.effect_noise.SetValue(this.Noise);
            effect.CommitChanges();
        }

        public void set_technique(Effect effect)
        {
            effect.CurrentTechnique = this.technique;
        }

        public void SetPosition()
        {
            Vector2 centerPoint = this.Parent.Center;
            double angle = (double)(offsetAngle.ToRadians() + this.Parent.Rotation + 1.57079637f);
            float distance = this.distanceToParentCenter;
            this.ThrusterCenter.Y = centerPoint.Y + distance * -(float)Math.Sin(angle);
            this.ThrusterCenter.X = centerPoint.X + distance * -(float)Math.Cos(angle);
            float xDistance = 256f - this.XMLPos.X;
            float zPos = (float)Math.Sin((double)this.Parent.yRotation) * xDistance + 15f;
            this.WorldPos = new Vector3(this.ThrusterCenter, zPos);
        }

        public void SetPosition(Vector2 Center)
        {
            Vector2 centerPoint = Center;
            double angle = (double)(offsetAngle.ToRadians() + this.Parent.Rotation + 1.57079637f);
            float distance = this.distanceToParentCenter;
            this.ThrusterCenter.Y = centerPoint.Y + distance * -(float)Math.Sin(angle);
            this.ThrusterCenter.X = centerPoint.X + distance * -(float)Math.Cos(angle);
            float zPos = -(float)Math.Sin((double)(this.Parent.yRotation / distance)) + 10f;
            this.WorldPos = new Vector3(this.ThrusterCenter, zPos);
        }

        public void Update(Vector3 Put_me_at, Vector3 Point_me_at, Vector3 Scale_factors, float thrustsize, float thrustspeed, Color color_at_exhaust, Color color_at_end, Vector3 camera_position)
        {
            this.heat = MathHelper.Clamp(thrustsize, 0f, 1f);
            Thruster thruster = this;
            thruster.tick = thruster.tick + thrustspeed;
            this.colors[0] = color_at_end;
            this.colors[1] = color_at_exhaust;
            this.world_matrix = Matrix.Identity;
            this.world_matrix.Forward = Point_me_at;
            this.dir_to_camera.X = Put_me_at.X - camera_position.X;
            this.dir_to_camera.Y = Put_me_at.Y - camera_position.Y;
            this.dir_to_camera.Z = Put_me_at.Z - camera_position.Z;
            Vector3.Cross(ref Point_me_at, ref this.dir_to_camera, out this.Up);
            this.Up.Normalize();
            Vector3.Cross(ref Point_me_at, ref this.Up, out this.Right);
            this.world_matrix.Right = this.Right;
            this.world_matrix.Up = this.Up;
            Matrix.CreateScale(ref Scale_factors, out this.scale);
            Matrix.Multiply(ref this.world_matrix, ref this.scale, out this.world_matrix);
            this.inverse_scale_transpose = Matrix.Transpose(Matrix.Invert(this.world_matrix));
            this.world_matrix.Translation = Put_me_at;
        }
    }
}