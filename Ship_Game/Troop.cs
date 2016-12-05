using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public sealed class Troop
	{
		public string Name;
		public string RaceType;
		public int first_frame = 1;
		public bool animated;
		public string idle_path;
		public string Icon;
		public string MovementCue;
		public int Level;
        public int AttackTimerBase = 10;
		public int MoveTimerBase = 10;
		public int num_idle_frames;
		public int num_attack_frames;
		public int idle_x_offset;
		public int idle_y_offset;
		public int attack_width = 128;
		private Planet p;
		public string attack_path;
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
        public int HardAttack;
		public int SoftAttack;
		public string Class;
		public int Kills;
		public string TargetType;
		public int Experience;
        public float Cost;
		public string sound_attack;
        public float Range;
	    public float Launchtimer = 10f;

		public Troop()
		{
		}

        public Troop Clone()
        {
            var t = (Troop)MemberwiseClone();
            t.p     = null;
            t.Owner = null;
            t.ship  = null;
            return t;
        }

		public void DoAttack()
		{
			Idle = false;
			WhichFrame = first_frame;
		}

        private string WhichFrameString => WhichFrame.ToString("00");
	    private Texture2D TextureDefault    => ResourceManager.TextureDict["Troops/"+TexturePath];
        private Texture2D TextureIdleAnim   => ResourceManager.TextureDict["Troops/"+idle_path+WhichFrameString];
        private Texture2D TextureAttackAnim => ResourceManager.TextureDict["Troops/"+attack_path+WhichFrameString];

	    public void Draw(SpriteBatch spriteBatch, Rectangle drawRect)
		{
			if (!facingRight)
			{
				DrawFlip(spriteBatch, drawRect);
				return;
			}
			if (!animated)
			{
				spriteBatch.Draw(TextureDefault, drawRect, Color.White);
				return;
			}
			if (Idle)
			{
				Rectangle sourceRect = new Rectangle(idle_x_offset, idle_y_offset, 128, 128);
				spriteBatch.Draw(TextureIdleAnim, drawRect, sourceRect, Color.White);
				return;
			}

			float scale = drawRect.Width / 128f;
			drawRect.Width = (int)(attack_width * scale);
			Rectangle sourceRect2 = new Rectangle(idle_x_offset, idle_y_offset, attack_width, 128);

            Texture2D attackTexture = TextureAttackAnim;
			if (attackTexture.Height <= 128)
			{
				spriteBatch.Draw(attackTexture, drawRect, sourceRect2, Color.White);
				return;
			}
			sourceRect2.Y      -= idle_y_offset;
			sourceRect2.Height += idle_y_offset;
			Rectangle r = drawRect;
			r.Y      -= (int)(scale * idle_y_offset);
			r.Height += (int)(scale * idle_y_offset);
			spriteBatch.Draw(attackTexture, r, sourceRect2, Color.White);
		}

		public void DrawFlip(SpriteBatch spriteBatch, Rectangle drawRect)
		{
			if (!animated)
			{
				spriteBatch.Draw(TextureDefault, drawRect, null, Color.White, 0f, Vector2.Zero, SpriteEffects.FlipHorizontally, 1f);
				return;
			}
			if (Idle)
			{
				Rectangle sourceRect = new Rectangle(idle_x_offset, idle_y_offset, 128, 128);
				spriteBatch.Draw(TextureIdleAnim, drawRect, sourceRect, Color.White, 0f, Vector2.Zero, SpriteEffects.FlipHorizontally, 1f);
				return;
			}
			float scale = drawRect.Width / 128f;
			drawRect.X = drawRect.X - (int)(attack_width * scale - drawRect.Width);
			drawRect.Width = (int)(attack_width * scale);

			Rectangle sourceRect2 = new Rectangle(idle_x_offset, idle_y_offset, attack_width, 128);
            var attackTexture = TextureAttackAnim;
			if (attackTexture.Height <= 128)
			{
				spriteBatch.Draw(attackTexture, drawRect, sourceRect2, Color.White, 0f, Vector2.Zero, SpriteEffects.FlipHorizontally, 1f);
				return;
			}
			sourceRect2.Y      -= idle_y_offset;
			sourceRect2.Height += idle_y_offset;
			Rectangle r = drawRect;
			r.Height += (int)(scale * idle_y_offset);
			r.Y      -= (int)(scale * idle_y_offset);
			spriteBatch.Draw(attackTexture, r, sourceRect2, Color.White, 0f, Vector2.Zero, SpriteEffects.FlipHorizontally, 1f);
		}

		public void DrawIcon(SpriteBatch spriteBatch, Rectangle drawRect)
		{
            var iconTexture = ResourceManager.TextureDict["TroopIcons/" + Icon + "_icon"];
			spriteBatch.Draw(iconTexture, drawRect, Color.White);
		}

		public Rectangle GetFromRect()
		{
			return fromRect;
		}

		public int GetHardAttack()
		{
			return (int)(HardAttack + 0.1f * Level * HardAttack);
		}

		public Empire GetOwner()
		{
		    return Owner ?? (Owner = EmpireManager.GetEmpireByName(OwnerString));
		}

		public Planet GetPlanet()
		{
			return p;
		}

		public Ship GetShip()
		{
			return ship;
		}

		public int GetSoftAttack()
		{
			return (int)(SoftAttack + 0.1f * Level * SoftAttack);
		}

		public Ship Launch()
		{
            if (p == null)
				return null;

			foreach (PlanetGridSquare pgs in p.TilesList)
			{
				if (!pgs.TroopsHere.Contains(this))
					continue;

				pgs.TroopsHere.Clear();
                p.TroopsHere.Remove(this);
			}
			Ship retShip = ResourceManager.CreateTroopShipAtPoint(Owner.data.DefaultTroopShip, Owner, p.Position, this);
            p = null;
			return retShip;
		}

		public void SetFromRect(Rectangle from)
		{
			fromRect = from;
		}

		public void SetOwner(Empire e)
		{
			Owner = e;
			if (e != null)
				OwnerString = e.data.Traits.Name;
		}

		public void SetPlanet(Planet newPlanet)
		{
			p = newPlanet;
			if (p != null && !p.TroopsHere.Contains(this))
			{
				p.TroopsHere.Add(this);
			}
		}

		public void SetShip(Ship s)
		{
			ship = s;
		}

		public void Update(float elapsedTime)
		{
			Troop troop = this;
			troop.updateTimer -= elapsedTime;
		    if (updateTimer > 0f)
                return;
		    if (!Idle)
		    {
		        updateTimer = 0.75f / num_attack_frames;
                ++WhichFrame;
		        if (WhichFrame <= num_attack_frames - (first_frame == 1 ? 0 : 1))
                    return;

		        WhichFrame = first_frame;
		        Idle = true;
		    }
		    else
		    {
		        updateTimer = 1f / num_idle_frames;
                ++WhichFrame;
		        if (WhichFrame <= num_idle_frames - (first_frame == 1 ? 0 : 1))
                    return;

		        WhichFrame = first_frame;
		    }
		}

        //Added by McShooterz
        public void AddKill()
        {
            Kills++;
            Experience++;
            if (Experience != 1 + Level)
                return;
            Experience -= 1 + Level;
            Level++;
        }

        // Added by McShooterz
        public float GetStrengthMax()
        {
            if (StrengthMax <= 0)
                StrengthMax = ResourceManager.TroopsDict[Name].Strength;
            return StrengthMax + Level*0.5f + (int)(StrengthMax * Owner.data.Traits.GroundCombatModifier);
        }

        public float GetCost()
        {
            return Cost * UniverseScreen.GamePaceStatic;
        }
	}
}