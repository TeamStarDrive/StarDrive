using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using MsgPack.Serialization;

namespace Ship_Game
{
	public sealed class Troop
	{
        [MessagePackMember(0)] public string Name;
        [MessagePackMember(1)] public string RaceType;
        [MessagePackMember(2)] public int first_frame = 1;
        [MessagePackMember(3)] public bool animated;
        [MessagePackMember(4)] public string idle_path;
        [MessagePackMember(5)] public string Icon;
        [MessagePackMember(6)] public string MovementCue;
        [MessagePackMember(7)] public int Level;
        [MessagePackMember(8)] public int AttackTimerBase = 10;
        [MessagePackMember(9)] public int MoveTimerBase = 10;
        [MessagePackMember(10)] public int num_idle_frames;
        [MessagePackMember(11)] public int num_attack_frames;
        [MessagePackMember(12)] public int idle_x_offset;
        [MessagePackMember(13)] public int idle_y_offset;
        [MessagePackMember(14)] public int attack_width = 128;
        [MessagePackMember(15)] public string attack_path;
        [MessagePackMember(16)] public bool facingRight;
        [MessagePackMember(17)] public string Description;
        [MessagePackMember(18)] public string OwnerString;
        [MessagePackMember(19)] public int BoardingStrength;
        [MessagePackMember(20)] public int MaxStoredActions = 1;
        [MessagePackMember(21)] public float MoveTimer;
        [MessagePackMember(22)] public float AttackTimer;
        [MessagePackMember(23)] public float MovingTimer = 1f;
        [MessagePackMember(24)] public int AvailableMoveActions = 1;
        [MessagePackMember(25)] public int AvailableAttackActions = 1;
        [MessagePackMember(26)] public string TexturePath;
        [MessagePackMember(27)] public bool Idle = true;
        [MessagePackMember(28)] public int WhichFrame = 1;
        [MessagePackMember(29)] public float Strength;
        [MessagePackMember(30)] public float StrengthMax;
        [MessagePackMember(31)] public int HardAttack;
        [MessagePackMember(32)] public int SoftAttack;
        [MessagePackMember(33)] public string Class;
        [MessagePackMember(34)] public int Kills;
        [MessagePackMember(35)] public string TargetType;
        [MessagePackMember(36)] public int Experience;
        [MessagePackMember(37)] public float Cost;
        [MessagePackMember(38)] public string sound_attack;
        [MessagePackMember(39)] public float Range;
        [MessagePackMember(40)] public float Launchtimer = 10f;

        [XmlIgnore][MessagePackIgnore] private Planet p;
        [XmlIgnore][MessagePackIgnore] private Empire Owner;
        [XmlIgnore][MessagePackIgnore] private Ship ship;
        [XmlIgnore][MessagePackIgnore] private Rectangle fromRect;
        [XmlIgnore][MessagePackIgnore] private float updateTimer;

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
            return StrengthMax + Level*0.5f + StrengthMax*(Owner?.data.Traits.GroundCombatModifier ?? 0.0f);
        }

        public float GetCost()
        {
            return Cost * UniverseScreen.GamePaceStatic;
        }
	}
}