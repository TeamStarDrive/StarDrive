using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class Troop
	{
		public string Name;

		public string RaceType;

		public int first_frame = 1;

		public bool animated;

		public string idle_path;

		public string Icon;

		public string MovementCue;

		public int Level;

		public bool Defender;

		public int AttackTimerBase = 10;

		public int MoveTimerBase = 10;

		public int num_idle_frames;

		public int num_attack_frames;

		public int idle_x_offset;

		public int idle_y_offset;

		public int attack_width = 128;

		private Planet p;

		public string attack_path;

		public int fps = 10;

		public bool facingRight;

		public string Description;

		public string OwnerString;

		private Empire Owner;

		public int BoardingStrength;

		private Ship ship;

		public int MaxStoredActions = 1;

		public float MoveTimer;

		public float AttackTimer;

		public float MovingTimer = 1f;

		private Rectangle fromRect;

		public int AvailableMoveActions = 1;

		public int AvailableAttackActions = 1;

		public string TexturePath;

		public bool Idle = true;

		public int WhichFrame = 1;

		private float updateTimer;

		public float Strength;

		public float StrengthMax;

		public int Movement;

		public int Initiative;

		public int HardAttack;

		public int SoftAttack;

		public string Class;

		public int Kills;

		public string TargetType;

		public int Experience;

		public int Entrenchment;

		public float Cost;

		public string sound_attack;

		public float MaintenanceCost;

		public float Range;

		private string fmt = "00";
        public float Launchtimer = 10f;

		public Troop()
		{
		}

		public void DoAttack()
		{
			this.Idle = false;
			this.WhichFrame = this.first_frame;
		}

		public void Draw(SpriteBatch spriteBatch, Rectangle drawRect)
		{
			if (!this.facingRight)
			{
				this.DrawFlip(spriteBatch, drawRect);
				return;
			}
			if (!this.animated)
			{
				spriteBatch.Draw(ResourceManager.TextureDict[string.Concat("Troops/", this.TexturePath)], drawRect, Color.White);
				return;
			}
			if (this.Idle)
			{
				Rectangle SourceRect = new Rectangle(this.idle_x_offset, this.idle_y_offset, 128, 128);
				spriteBatch.Draw(ResourceManager.TextureDict[string.Concat("Troops/", this.idle_path, this.WhichFrame.ToString(this.fmt))], drawRect, new Rectangle?(SourceRect), Color.White);
				return;
			}
			float scale = (float)drawRect.Width / 128f;
			drawRect.Width = (int)((float)this.attack_width * scale);
            //changed sourcerect to sourcerect2 to prevent redefinition
			Rectangle SourceRect2 = new Rectangle(this.idle_x_offset, this.idle_y_offset, this.attack_width, 128);
			if (ResourceManager.TextureDict[string.Concat("Troops/", this.attack_path, this.WhichFrame.ToString(this.fmt))].Height <= 128)
			{
				spriteBatch.Draw(ResourceManager.TextureDict[string.Concat("Troops/", this.attack_path, this.WhichFrame.ToString(this.fmt))], drawRect, new Rectangle?(SourceRect2), Color.White);
				return;
			}
			SourceRect2.Y = SourceRect2.Y - this.idle_y_offset;
			SourceRect2.Height = SourceRect2.Height + this.idle_y_offset;
			Rectangle r = drawRect;
			r.Y = r.Y - (int)(scale * (float)this.idle_y_offset);
			r.Height = r.Height + (int)(scale * (float)this.idle_y_offset);
			spriteBatch.Draw(ResourceManager.TextureDict[string.Concat("Troops/", this.attack_path, this.WhichFrame.ToString(this.fmt))], r, new Rectangle?(SourceRect2), Color.White);
		}

		public void DrawFlip(SpriteBatch spriteBatch, Rectangle drawRect)
		{
			if (!this.animated)
			{
				Rectangle? nullable = null;
				spriteBatch.Draw(ResourceManager.TextureDict[string.Concat("Troops/", this.TexturePath)], drawRect, nullable, Color.White, 0f, Vector2.Zero, SpriteEffects.FlipHorizontally, 1f);
				return;
			}
			if (this.Idle)
			{
				Rectangle SourceRect = new Rectangle(this.idle_x_offset, this.idle_y_offset, 128, 128);
				spriteBatch.Draw(ResourceManager.TextureDict[string.Concat("Troops/", this.idle_path, this.WhichFrame.ToString(this.fmt))], drawRect, new Rectangle?(SourceRect), Color.White, 0f, Vector2.Zero, SpriteEffects.FlipHorizontally, 1f);
				return;
			}
			float scale = (float)drawRect.Width / 128f;
			drawRect.X = drawRect.X - (int)((float)this.attack_width * scale - (float)drawRect.Width);
			drawRect.Width = (int)((float)this.attack_width * scale);
            //changed sourcerect to sourcerect2, again
			Rectangle SourceRect2 = new Rectangle(this.idle_x_offset, this.idle_y_offset, this.attack_width, 128);
			if (ResourceManager.TextureDict[string.Concat("Troops/", this.attack_path, this.WhichFrame.ToString(this.fmt))].Height <= 128)
			{
				spriteBatch.Draw(ResourceManager.TextureDict[string.Concat("Troops/", this.attack_path, this.WhichFrame.ToString(this.fmt))], drawRect, new Rectangle?(SourceRect2), Color.White, 0f, Vector2.Zero, SpriteEffects.FlipHorizontally, 1f);
				return;
			}
			SourceRect2.Y = SourceRect2.Y - this.idle_y_offset;
			SourceRect2.Height = SourceRect2.Height + this.idle_y_offset;
			Rectangle r = drawRect;
			r.Height = r.Height + (int)(scale * (float)this.idle_y_offset);
			r.Y = r.Y - (int)(scale * (float)this.idle_y_offset);
			spriteBatch.Draw(ResourceManager.TextureDict[string.Concat("Troops/", this.attack_path, this.WhichFrame.ToString(this.fmt))], r, new Rectangle?(SourceRect2), Color.White, 0f, Vector2.Zero, SpriteEffects.FlipHorizontally, 1f);
		}

		public void DrawIcon(SpriteBatch spriteBatch, Rectangle drawRect)
		{
			spriteBatch.Draw(ResourceManager.TextureDict[string.Concat("TroopIcons/", this.Icon, "_icon")], drawRect, Color.White);
		}

		public Rectangle GetFromRect()
		{
			return this.fromRect;
		}

		public int GetHardAttack()
		{
			return (int)((float)this.HardAttack + 0.1f * (float)this.Level * (float)this.HardAttack);
		}

		public Empire GetOwner()
		{
			if (this.Owner == null)
			{
				this.Owner = EmpireManager.GetEmpireByName(this.OwnerString);
			}
			return this.Owner;
		}

		public Planet GetPlanet()
		{
			return this.p;
		}

		public Ship GetShip()
		{
			return this.ship;
		}

		public int GetSoftAttack()
		{
			return (int)((float)this.SoftAttack + 0.1f * (float)this.Level * (float)this.SoftAttack);
		}

		public Ship Launch()
		{
			if (this.p == null)
			{
				return null;
			}
			foreach (PlanetGridSquare pgs in this.p.TilesList)
			{
				if (!pgs.TroopsHere.Contains(this))
				{
					continue;
				}
				pgs.TroopsHere.Clear();
				this.p.TroopsHere.Remove(this);
			}
			Ship retShip = ResourceManager.CreateTroopShipAtPoint((this.Owner.data.DefaultTroopShip != null) ? this.Owner.data.DefaultTroopShip : this.Owner.data.DefaultSmallTransport, this.Owner, this.p.Position, this);
			this.p = null;
			return retShip;
		}

		public void SetFromRect(Rectangle from)
		{
			this.fromRect = from;
		}

		public void SetOwner(Empire e)
		{
			this.Owner = e;
			if (e != null)
			{
				this.OwnerString = e.data.Traits.Name;
			}
		}

		public void SetPlanet(Planet p)
		{
			if (p == null)
			{
				p = null;
				return;
			}
			this.p = p;
			if (!p.TroopsHere.Contains(this))
			{
				p.TroopsHere.Add(this);
			}
		}

		public void SetShip(Ship s)
		{
			this.ship = s;
		}

		public void Update(float elapsedTime)
		{
			Troop troop = this;
			troop.updateTimer = troop.updateTimer - elapsedTime;
			if (this.updateTimer <= 0f)
			{
				if (!this.Idle)
				{
                    try //added by gremlin hot fix to stop troop crashing.
                    {
                        this.updateTimer = 0.75f / (float)this.num_attack_frames;
                        Troop whichFrame = this;
                        whichFrame.WhichFrame = whichFrame.WhichFrame + 1;

                        if (this.WhichFrame > this.num_attack_frames - (this.first_frame == 1 ? 0 : 1))
                        {
                            this.WhichFrame = this.first_frame;
                            this.Idle = true;
                        }
                    }
                    catch { }
				}
				else
				{
					this.updateTimer = 1f / (float)this.num_idle_frames;
					Troop whichFrame1 = this;
					whichFrame1.WhichFrame = whichFrame1.WhichFrame + 1;
					if (this.WhichFrame > this.num_idle_frames - (this.first_frame == 1 ? 0 : 1))
					{
						this.WhichFrame = this.first_frame;
						return;
					}
				}
			}
		}

        //Added by McShooterz
        public void AddKill()
        {
            this.Kills++;
            this.Experience++;
            if (this.Experience == 1 + this.Level)
            {
                this.Experience -= 1 + this.Level;
                this.Level++;
            }
        }

        //Added by McShooterz
        public float GetStrengthMax()
        {
            if (this.StrengthMax <= 0)
                this.StrengthMax = Ship_Game.ResourceManager.TroopsDict[this.Name].Strength;
            return this.StrengthMax + this.Level / 2 + (int)(this.StrengthMax * this.Owner.data.Traits.GroundCombatModifier);
        }
	}
}