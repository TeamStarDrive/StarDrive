using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Lights;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class ExplosionManager
	{
		public static UniverseScreen universeScreen;

		public static BatchRemovalCollection<Explosion> ExplosionList;

		private static string fmt;

		private static Random random;

		static ExplosionManager()
		{
			ExplosionManager.ExplosionList = new BatchRemovalCollection<Explosion>();
			ExplosionManager.fmt = "00000.##";
			ExplosionManager.random = new Random();
		}

		public ExplosionManager()
		{
		}

		public static void AddExplosion(Vector3 Position, float radius, float intensity, float duration)
		{
			if (radius <= 0f)
			{
				radius = 1f;
			}
			Explosion newExp = new Explosion()
			{
				duration = 2.25f,
				pos = Position
			};
			if (ExplosionManager.universeScreen.viewState == UniverseScreen.UnivScreenState.ShipView)
			{
				newExp.light = new PointLight();
				newExp.Radius = radius;
				newExp.light.World = Matrix.Identity * Matrix.CreateTranslation(Position);
				newExp.light.Position = Position;
				newExp.light.Radius = radius;
				newExp.light.ObjectType = ObjectType.Dynamic;
				newExp.light.DiffuseColor = new Vector3(0.9f, 0.8f, 0.7f);
				newExp.light.Intensity = intensity;
				newExp.light.Enabled = true;
				lock (GlobalStats.ObjectManagerLocker)
				{
					ExplosionManager.universeScreen.ScreenManager.inter.LightManager.Submit(newExp.light);
				}
			}
			switch ((int)RandomMath2.RandomBetween(0f, 2f))
			{
				case 0:
				{
					newExp.AnimationTexture = "sd_explosion_12a_cc/sd_explosion_12a_cc_00000";
					newExp.AnimationBasePath = "sd_explosion_12a_cc/sd_explosion_12a_cc_";
					break;
				}
				case 1:
				{
					newExp.AnimationTexture = "sd_explosion_14a_cc/sd_explosion_14a_cc_00000";
					newExp.AnimationBasePath = "sd_explosion_14a_cc/sd_explosion_14a_cc_";
					break;
				}
				case 2:
				{
					newExp.AnimationTexture = "sd_explosion_07a_cc/sd_explosion_07a_cc_00000";
					newExp.AnimationBasePath = "sd_explosion_07a_cc/sd_explosion_07a_cc_00000";
					break;
				}
			}
			newExp.Rotation = (float)RandomMath2.RandomBetween(0f, 6.28318548f);
			lock (GlobalStats.ExplosionLocker)
			{
				ExplosionManager.ExplosionList.Add(newExp);
			}
		}

		public static void AddExplosion(Vector3 Position, float radius, float intensity, float duration, int nosparks)
		{
			if (radius == 0f)
			{
				radius = 1f;
			}
			Explosion newExp = new Explosion()
			{
				sparks = false,
				duration = 2.25f,
				pos = Position
			};
			if (ExplosionManager.universeScreen.viewState == UniverseScreen.UnivScreenState.ShipView)
			{
				newExp.light = new PointLight();
				newExp.Radius = radius;
				newExp.light.World = Matrix.Identity * Matrix.CreateTranslation(Position);
				newExp.light.Position = Position;
				newExp.light.Radius = radius;
				newExp.light.ObjectType = ObjectType.Dynamic;
				newExp.light.DiffuseColor = new Vector3(0.9f, 0.8f, 0.7f);
				newExp.light.Intensity = intensity;
				newExp.light.Enabled = true;
				lock (GlobalStats.ObjectManagerLocker)
				{
					ExplosionManager.universeScreen.ScreenManager.inter.LightManager.Submit(newExp.light);
				}
			}
			switch ((int)RandomMath2.RandomBetween(0f, 2f))
			{
				case 0:
				{
					newExp.AnimationTexture = "sd_explosion_12a_cc/sd_explosion_12a_cc_00000";
					newExp.AnimationBasePath = "sd_explosion_12a_cc/sd_explosion_12a_cc_";
					break;
				}
				case 1:
				{
					newExp.AnimationTexture = "sd_explosion_14a_cc/sd_explosion_14a_cc_00000";
					newExp.AnimationBasePath = "sd_explosion_14a_cc/sd_explosion_14a_cc_";
					break;
				}
				case 2:
				{
					newExp.AnimationTexture = "sd_explosion_07a_cc/sd_explosion_07a_cc_00000";
					newExp.AnimationBasePath = "sd_explosion_07a_cc/sd_explosion_07a_cc_00000";
					break;
				}
			}
			newExp.Rotation = (float)RandomMath2.RandomBetween(0f, 6.28318548f);
			lock (GlobalStats.ExplosionLocker)
			{
				ExplosionManager.ExplosionList.Add(newExp);
			}
		}

		public static void AddExplosion(Vector3 Position, float radius, float intensity, float duration, ShipModule mod)
		{
			if (radius == 0f)
			{
				radius = 1f;
			}
			Explosion newExp = new Explosion()
			{
				duration = 2.25f,
				pos = Position
			};
			if (ExplosionManager.universeScreen.viewState == UniverseScreen.UnivScreenState.ShipView)
			{
				newExp.light = new PointLight();
				newExp.Radius = radius;
				newExp.light.World = Matrix.Identity * Matrix.CreateTranslation(Position);
				newExp.light.Position = Position;
				newExp.light.Radius = radius;
				newExp.light.ObjectType = ObjectType.Dynamic;
				newExp.light.DiffuseColor = new Vector3(0.9f, 0.8f, 0.7f);
				newExp.light.Intensity = intensity;
				newExp.light.Enabled = true;
				lock (GlobalStats.ObjectManagerLocker)
				{
					ExplosionManager.universeScreen.ScreenManager.inter.LightManager.Submit(newExp.light);
				}
			}
			switch ((int)RandomMath2.RandomBetween(0f, 2f))
			{
				case 0:
				{
					newExp.AnimationTexture = "sd_explosion_12a_cc/sd_explosion_12a_cc_00000";
					newExp.AnimationBasePath = "sd_explosion_12a_cc/sd_explosion_12a_cc_";
					break;
				}
				case 1:
				{
					newExp.AnimationTexture = "sd_explosion_14a_cc/sd_explosion_14a_cc_00000";
					newExp.AnimationBasePath = "sd_explosion_14a_cc/sd_explosion_14a_cc_";
					break;
				}
				case 2:
				{
					newExp.AnimationTexture = "sd_explosion_07a_cc/sd_explosion_07a_cc_00000";
					newExp.AnimationBasePath = "sd_explosion_07a_cc/sd_explosion_07a_cc_00000";
					break;
				}
			}
			lock (GlobalStats.ExplosionLocker)
			{
				ExplosionManager.ExplosionList.Add(newExp);
			}
		}

		public static void AddExplosion(Vector3 Position, float radius, float intensity, float duration, bool Shockwave)
		{
			if (radius == 0f)
			{
				radius = 1f;
			}
			Explosion newExp = new Explosion()
			{
				hasShockwave = Shockwave,
				pos = Position,
				duration = duration
			};
			if (ExplosionManager.universeScreen.viewState == UniverseScreen.UnivScreenState.ShipView)
			{
				newExp.light = new PointLight();
				newExp.Radius = radius;
				newExp.light.World = Matrix.Identity * Matrix.CreateTranslation(Position);
				newExp.light.Position = Position;
				newExp.light.Radius = radius;
				newExp.light.ObjectType = ObjectType.Dynamic;
				newExp.light.DiffuseColor = new Vector3(0.9f, 0.8f, 0.7f);
				newExp.light.Intensity = intensity;
				newExp.light.Enabled = true;
				lock (GlobalStats.ObjectManagerLocker)
				{
					ExplosionManager.universeScreen.ScreenManager.inter.LightManager.Submit(newExp.light);
				}
			}
			lock (GlobalStats.ExplosionLocker)
			{
				ExplosionManager.ExplosionList.Add(newExp);
			}
		}

		public static void AddExplosionNoFlames(Vector3 Position, float radius, float intensity, float duration)
		{
			if (radius == 0f)
			{
				radius = 1f;
			}
			Explosion newExp = new Explosion()
			{
				duration = 2.25f,
				pos = Position
			};
			if (ExplosionManager.universeScreen.viewState == UniverseScreen.UnivScreenState.ShipView)
			{
				newExp.light = new PointLight();
				newExp.Radius = radius;
				newExp.light.World = Matrix.Identity * Matrix.CreateTranslation(Position);
				newExp.light.Position = Position;
				newExp.light.Radius = radius;
				newExp.light.ObjectType = ObjectType.Dynamic;
				newExp.light.DiffuseColor = new Vector3(0.9f, 0.8f, 0.7f);
				newExp.light.Intensity = intensity;
				newExp.light.Enabled = true;
				lock (GlobalStats.ObjectManagerLocker)
				{
					ExplosionManager.universeScreen.ScreenManager.inter.LightManager.Submit(newExp.light);
				}
			}
			newExp.AnimationTexture = null;
			newExp.AnimationBasePath = null;
			newExp.Animation = 0;
			newExp.Rotation = (float)RandomMath2.RandomBetween(0f, 6.28318548f);
			lock (GlobalStats.ExplosionLocker)
			{
				ExplosionManager.ExplosionList.Add(newExp);
			}
		}

		public static void AddProjectileExplosion(Vector3 Position, float radius, float intensity, float duration, string which)
		{
			if (radius == 0f)
			{
				radius = 1f;
			}
			Explosion newExp = new Explosion()
			{
				duration = 2.25f,
				ExpColor = which,
				pos = Position
			};
			if (ExplosionManager.universeScreen.viewState == UniverseScreen.UnivScreenState.ShipView)
			{
				newExp.light = new PointLight();
				newExp.Radius = radius;
				newExp.light.World = Matrix.Identity * Matrix.CreateTranslation(Position);
				newExp.light.Position = Position;
				newExp.light.Radius = radius;
				newExp.light.ObjectType = ObjectType.Dynamic;
				newExp.light.DiffuseColor = new Vector3(0.9f, 0.8f, 0.7f);
				newExp.light.Intensity = intensity;
				newExp.light.Enabled = true;
				lock (GlobalStats.ObjectManagerLocker)
				{
					ExplosionManager.universeScreen.ScreenManager.inter.LightManager.Submit(newExp.light);
				}
			}
			switch ((int)RandomMath2.RandomBetween(0f, 2f))
			{
				case 0:
				{
					newExp.AnimationTexture = "sd_explosion_12a_cc/sd_explosion_12a_cc_00000";
					newExp.AnimationBasePath = "sd_explosion_12a_cc/sd_explosion_12a_cc_";
					break;
				}
				case 1:
				{
					newExp.AnimationTexture = "sd_explosion_14a_cc/sd_explosion_14a_cc_00000";
					newExp.AnimationBasePath = "sd_explosion_14a_cc/sd_explosion_14a_cc_";
					break;
				}
				case 2:
				{
					newExp.AnimationTexture = "sd_explosion_07a_cc/sd_explosion_07a_cc_00000";
					newExp.AnimationBasePath = "sd_explosion_07a_cc/sd_explosion_07a_cc_00000";
					break;
				}
			}
			newExp.Rotation = (float)RandomMath2.RandomBetween(0f, 6.28318548f);
			lock (GlobalStats.ExplosionLocker)
			{
				ExplosionManager.ExplosionList.Add(newExp);
			}
		}

		public static void AddWarpExplosion(Vector3 Position, float radius, float intensity, float duration)
		{
			if (radius == 0f)
			{
				radius = 1f;
			}
			Explosion newExp = new Explosion()
			{
				duration = 2.25f,
				pos = Position
			};
			if (ExplosionManager.universeScreen.viewState == UniverseScreen.UnivScreenState.ShipView)
			{
				newExp.light = new PointLight();
				newExp.Radius = radius;
				newExp.light.World = Matrix.Identity * Matrix.CreateTranslation(Position);
				newExp.light.Position = Position;
				newExp.light.Radius = radius;
				newExp.light.ObjectType = ObjectType.Dynamic;
				newExp.light.DiffuseColor = new Vector3(0.9f, 0.8f, 0.7f);
				newExp.light.Intensity = intensity;
				newExp.light.Enabled = true;
				lock (GlobalStats.ObjectManagerLocker)
				{
					ExplosionManager.universeScreen.ScreenManager.inter.LightManager.Submit(newExp.light);
				}
			}
			newExp.AnimationFrames = 59;
			newExp.AnimationTexture = "sd_shockwave_01/sd_shockwave_01_00000";
			newExp.AnimationBasePath = "sd_shockwave_01/sd_shockwave_01_";
			newExp.Rotation = (float)RandomMath2.RandomBetween(0f, 6.28318548f);
			lock (GlobalStats.ExplosionLocker)
			{
				ExplosionManager.ExplosionList.Add(newExp);
			}
		}

		private static Vector3 RandomPointOnCircle(float radius, Vector3 Center)
		{
			double angle = ExplosionManager.random.NextDouble() * 3.14159265358979 * 2;
			float x = (float)Math.Cos(angle);
			float y = (float)Math.Sin(angle);
			return new Vector3(Center.X + x * radius, Center.Y, y * radius);
		}

		private static Vector3 RandomSpherePoint(float radius, Vector3 Center)
		{
			Vector3 v = Vector3.Zero;
			do
			{
				v.X = 2f * (float)ExplosionManager.random.NextDouble() - 1f;
				v.Y = 2f * (float)ExplosionManager.random.NextDouble() - 1f;
				v.Z = 2f * (float)ExplosionManager.random.NextDouble() - 1f;
			}
			while (v.LengthSquared() == 0f || v.LengthSquared() > 1f);
			v.Normalize();
			v = v * radius;
			v = v + Center;
			return v;
		}

		public static void Update(float elapsedTime)
		{
			for (int i = 0; i < ExplosionManager.ExplosionList.Count; i++)
			{
				if (ExplosionManager.ExplosionList[i] != null)
				{
					Explosion item = ExplosionManager.ExplosionList[i];
					item.duration = item.duration - elapsedTime;
					Explosion explosion = ExplosionManager.ExplosionList[i];
					explosion.shockWaveTimer = explosion.shockWaveTimer + elapsedTime;
					if (ExplosionManager.ExplosionList[i].light != null)
					{
						PointLight intensity = ExplosionManager.ExplosionList[i].light;
						intensity.Intensity = intensity.Intensity - 0.2f;
					}
					ExplosionManager.ExplosionList[i].color = new Color(255f, 255f, 255f, 255f * ExplosionManager.ExplosionList[i].duration / 0.2f);
					if (ExplosionManager.ExplosionList[i].Animation == 1)
					{
						if (ExplosionManager.ExplosionList[i].ExpColor != "Blue_1")
						{
							if (ExplosionManager.ExplosionList[i].AnimationFrame < ExplosionManager.ExplosionList[i].AnimationFrames)
							{
								Explosion animationFrame = ExplosionManager.ExplosionList[i];
								animationFrame.AnimationFrame = animationFrame.AnimationFrame + 1;
							}
							string remainder = ExplosionManager.ExplosionList[i].AnimationFrame.ToString(ExplosionManager.fmt);
							ExplosionManager.ExplosionList[i].AnimationTexture = string.Concat(ExplosionManager.ExplosionList[i].AnimationBasePath, remainder);
						}
						else
						{
							if (ExplosionManager.ExplosionList[i].AnimationFrame < 88)
							{
								Explosion item1 = ExplosionManager.ExplosionList[i];
								item1.AnimationFrame = item1.AnimationFrame + 1;
							}
							string remainder = ExplosionManager.ExplosionList[i].AnimationFrame.ToString(ExplosionManager.fmt);
							ExplosionManager.ExplosionList[i].AnimationTexture = string.Concat("sd_explosion_03_photon_256/sd_explosion_03_photon_256_", remainder);
						}
					}
					if (ExplosionManager.ExplosionList[i].duration <= 0f)
					{
						ExplosionManager.ExplosionList.QueuePendingRemoval(ExplosionManager.ExplosionList[i]);
						if (ExplosionManager.ExplosionList[i].light != null)
						{
							lock (GlobalStats.ObjectManagerLocker)
							{
								ExplosionManager.universeScreen.ScreenManager.inter.LightManager.Remove(ExplosionManager.ExplosionList[i].light);
							}
						}
					}
					if (!ExplosionManager.ExplosionList[i].sparkWave && ExplosionManager.ExplosionList[i].sparks)
					{
						int j = 0;
						while (j < 20)
						{
							j++;
						}
					}
					else if (ExplosionManager.ExplosionList[i].sparkWave)
					{
						for (int j = 0; j < 20; j++)
						{
							float single = 12.5f / ExplosionManager.ExplosionList[i].duration;
						}
					}
					if (ExplosionManager.ExplosionList[i].duration <= 0f)
					{
						ExplosionManager.ExplosionList.QueuePendingRemoval(ExplosionManager.ExplosionList[i]);
					}
				}
				else
				{
					ExplosionManager.ExplosionList.QueuePendingRemoval(ExplosionManager.ExplosionList[i]);
				}
			}
			ExplosionManager.ExplosionList.ApplyPendingRemovals();
		}
	}
}