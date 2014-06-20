using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
using System;
using System.Collections.ObjectModel;

namespace Ship_Game
{
	public class Thruster
	{
		public Model model;

		public Texture3D Noise;

		public float tscale;

		public Ship Parent;

		public Vector2 XMLPos;

		public Vector3 WorldPos;

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

		public float allscale = 1f;

		private float distanceToParentCenter;

		private float offsetAngle;

		private Vector2 ThrusterCenter;

		public Thruster()
		{
		}

		public void draw(ref Matrix view, ref Matrix project, Effect effect)
		{
			this.matrices_combined[0] = this.world_matrix;
			this.matrices_combined[1] = (this.world_matrix * view) * project;
			this.matrices_combined[2] = this.inverse_scale_transpose;
			effect.CurrentTechnique = this.technique;
			this.shader_matrices.SetValue(this.matrices_combined);
			this.v4colors[0] = this.colors[0].ToVector4();
			this.v4colors[1] = this.colors[1].ToVector4();
			this.v4colors[0].W = this.heat;
			this.thrust_color.SetValue(this.v4colors);
			this.effect_tick.SetValue(this.tick);
			this.effect_noise.SetValue(this.Noise);
			this.model.Meshes[0].Draw();
		}

		public float findAngleToTarget(Vector2 origin, Vector2 target)
		{
			float theta;
			float tX = target.X;
			float tY = target.Y;
			float centerX = origin.X;
			float centerY = origin.Y;
			float angle_to_target = 0f;
			if (tX > centerX && tY < centerY)
			{
				theta = (float)Math.Atan((double)((tY - centerY) / (tX - centerX)));
				theta = theta * 180f / 3.14159274f;
				angle_to_target = 90f - Math.Abs(theta);
			}
			else if (tX > centerX && tY > centerY)
			{
				theta = (float)Math.Atan((double)((tY - centerY) / (tX - centerX)));
				angle_to_target = 90f + theta * 180f / 3.14159274f;
			}
			else if (tX < centerX && tY > centerY)
			{
				theta = (float)Math.Atan((double)((tY - centerY) / (tX - centerX)));
				theta = theta * 180f / 3.14159274f;
				angle_to_target = 270f - Math.Abs(theta);
				angle_to_target = -angle_to_target;
			}
			else if (tX < centerX && tY < centerY)
			{
				theta = (float)Math.Atan((double)((tY - centerY) / (tX - centerX)));
				angle_to_target = 270f + theta * 180f / 3.14159274f;
				angle_to_target = -angle_to_target;
			}
			if (tX == centerX && tY < centerY)
			{
				angle_to_target = 0f;
			}
			else if (tX > centerX && tY == centerY)
			{
				angle_to_target = 90f;
			}
			else if (tX == centerX && tY > centerY)
			{
				angle_to_target = 180f;
			}
			else if (tX < centerX && tY == centerY)
			{
				angle_to_target = 270f;
			}
			return angle_to_target;
		}

		public void InitializeForViewing()
		{
			Vector2 RelativeShipCenter = new Vector2(512f, 512f);
			this.ThrusterCenter = new Vector2()
			{
				X = this.XMLPos.X + 256f,
				Y = this.XMLPos.Y + 256f
			};
			this.distanceToParentCenter = (float)Math.Sqrt((double)((this.ThrusterCenter.X - RelativeShipCenter.X) * (this.ThrusterCenter.X - RelativeShipCenter.X) + (this.ThrusterCenter.Y - RelativeShipCenter.Y) * (this.ThrusterCenter.Y - RelativeShipCenter.Y)));
			this.offsetAngle = (float)Math.Abs(this.findAngleToTarget(RelativeShipCenter, this.ThrusterCenter));
			this.SetPosition();
		}

		public void load_and_assign_effects(ContentManager content, string filename, string noisefilename, Effect effect)
		{
			this.model = content.Load<Model>(filename);
			this.Noise = content.Load<Texture3D>(noisefilename);
			this.model.Meshes[0].MeshParts[0].Effect = effect;
			this.technique = effect.Techniques["thrust_technique"];
			this.shader_matrices = effect.Parameters["world_matrices"];
			this.thrust_color = effect.Parameters["thrust_color"];
			this.effect_tick = effect.Parameters["ticks"];
			this.effect_noise = effect.Parameters["noise_texture"];
			this.world_matrix = Matrix.Identity;
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
			double angle = (double)(MathHelper.ToRadians(this.offsetAngle) + this.Parent.Rotation + 1.57079637f);
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
			double angle = (double)(MathHelper.ToRadians(this.offsetAngle) + this.Parent.Rotation + 1.57079637f);
			float distance = this.distanceToParentCenter;
			this.ThrusterCenter.Y = centerPoint.Y + distance * -(float)Math.Sin(angle);
			this.ThrusterCenter.X = centerPoint.X + distance * -(float)Math.Cos(angle);
			float zPos = -(float)Math.Sin((double)(this.Parent.yRotation / distance)) + 10f;
			this.WorldPos = new Vector3(this.ThrusterCenter, zPos);
		}

		public void update(Vector3 Put_me_at, Vector3 Point_me_at, Vector3 Scale_factors, float thrustsize, float thrustspeed, Color color_at_exhaust, Color color_at_end, Vector3 camera_position)
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