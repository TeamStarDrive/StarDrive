using System;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game
{
    public sealed class Troop
    {
        [Serialize(0)] public string Name;
        [Serialize(1)] public string RaceType;
        [Serialize(2)] public int first_frame = 1;
        [Serialize(3)] public bool animated;
        [Serialize(4)] public string idle_path;
        [Serialize(5)] public string Icon;
        [Serialize(6)] public string MovementCue;
        [Serialize(7)] public int Level;
        [Serialize(8)] public int AttackTimerBase = 10;
        [Serialize(9)] public int MoveTimerBase = 10;
        [Serialize(10)] public int num_idle_frames;
        [Serialize(11)] public int num_attack_frames;
        [Serialize(12)] public int idle_x_offset;
        [Serialize(13)] public int idle_y_offset;
        [Serialize(14)] public int attack_width = 128;
        [Serialize(15)] public string attack_path;
        [Serialize(16)] public bool facingRight;
        [Serialize(17)] public string Description;
        [Serialize(18)] public string OwnerString;
        [Serialize(19)] public int BoardingStrength;
        [Serialize(20)] public int MaxStoredActions = 1;
        [Serialize(21)] public float MoveTimer;
        [Serialize(22)] public float AttackTimer;
        [Serialize(23)] public float MovingTimer = 1f;
        [Serialize(24)] public int AvailableMoveActions = 1;
        [Serialize(25)] public int AvailableAttackActions = 1;
        [Serialize(26)] public string TexturePath;
        [Serialize(27)] public bool Idle = true;
        [Serialize(28)] public int WhichFrame = 1;
        [Serialize(29)] public float Strength;
        [Serialize(30)] public float StrengthMax;
        [Serialize(31)] public int HardAttack;
        [Serialize(32)] public int SoftAttack;
        [Serialize(33)] public string Class;
        [Serialize(34)] public int Kills;
        [Serialize(35)] public string TargetType;
        [Serialize(36)] public int Experience;
        [Serialize(37)] public float Cost;
        [Serialize(38)] public string sound_attack;
        [Serialize(39)] public float Range;
        [Serialize(40)] public float Launchtimer = 10f;
        [Serialize(41)] public string Type;

        [XmlIgnore][JsonIgnore] private Planet p;
        [XmlIgnore][JsonIgnore] private Empire Owner;
        [XmlIgnore][JsonIgnore] private Ship ship;
        [XmlIgnore][JsonIgnore] private Rectangle fromRect;
        [XmlIgnore][JsonIgnore] private float updateTimer;        
        [XmlIgnore][JsonIgnore] public string DisplayName => DisplayNameEmpire(Owner);

        public string DisplayNameEmpire(Empire empire = null)
        {
            empire = Owner ?? empire;
            if (empire == null || !empire.data.IsRebelFaction) return Name;
            return Localizer.Token(empire.data.TroopNameIndex);
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

        [XmlIgnore][JsonIgnore]
        public Texture2D TextureDefault => ResourceManager.Texture("Troops/"+TexturePath);

        //@HACK the animation index and firstframe value are coming up with bad values for some reason. i could not figure out why
        //so here i am forcing it to draw troop template first frame if it hits a problem. in the update method i am refreshing the firstframe value as well. 
        private Texture2D TextureIdleAnim   => ResourceManager.Texture("Troops/"+idle_path+WhichFrameString, "Troops/" + idle_path+
            ResourceManager.GetTroopTemplate(Name).first_frame.ToString("0000"));

        private Texture2D TextureAttackAnim => ResourceManager.Texture("Troops/" + attack_path + WhichFrameString, "Troops/" + idle_path +
            ResourceManager.GetTroopTemplate(Name).first_frame.ToString("0000"));

        public string StrengthText => $"Strength: {Strength:0.}";

        //@todo split this into methods of animated and non animated. or always draw animated and move the animation logic 
        // to a central location to be used by any animated image. 
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
                var sourceRect = new Rectangle(idle_x_offset, idle_y_offset, 128, 128);
                spriteBatch.Draw(TextureIdleAnim, drawRect, sourceRect, Color.White);
                return;
            }

            float scale = drawRect.Width / 128f;
            drawRect.Width = (int)(attack_width * scale);
            var sourceRect2 = new Rectangle(idle_x_offset, idle_y_offset, attack_width, 128);

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
            Ship retShip = Ship.CreateTroopShipAtPoint(Owner.data.DefaultTroopShip, Owner, p.Center, this);
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
            troop.first_frame = ResourceManager.GetTroopTemplate(troop.Name).first_frame;
            int whichFrame = WhichFrame;
            if (!Idle)
            {
                updateTimer = 0.75f / num_attack_frames;                
                whichFrame++;
                if (whichFrame <= num_attack_frames - (first_frame == 1 ? 0 : 1))
                {
                    WhichFrame++;
                    return;
                }

                WhichFrame = first_frame;
                Idle = true;
            }
            else
            {
                updateTimer = 1f / num_idle_frames;
                whichFrame++;
                if (whichFrame <= num_idle_frames - (first_frame == 1 ? 0 : 1))
                {
                    WhichFrame++;
                    return;
                }

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
                StrengthMax = ResourceManager.GetTroopTemplate(Name).Strength;
            return StrengthMax + Level*0.5f + StrengthMax*(Owner?.data.Traits.GroundCombatModifier ?? 0.0f);
        }

        public float GetCost()
        {
            return Cost * UniverseScreen.GamePaceStatic;
        }
        public bool AssignTroopToNearestAvailableTile(Troop t, PlanetGridSquare tile, Planet planet )
        {
            Array<PlanetGridSquare> list = new Array<PlanetGridSquare>();
            foreach (PlanetGridSquare planetGridSquare in planet.TilesList)
            {
                if (planetGridSquare.TroopsHere.Count < planetGridSquare.number_allowed_troops
                    && (planetGridSquare.building == null || planetGridSquare.building != null && planetGridSquare.building.CombatStrength == 0)
                    && (Math.Abs(tile.x - planetGridSquare.x) <= 1 && Math.Abs(tile.y - planetGridSquare.y) <= 1))
                    list.Add(planetGridSquare);
            }
            if (list.Count > 0)
            {
                int index = (int)RandomMath.RandomBetween(0.0f, list.Count);
                PlanetGridSquare planetGridSquare1 = list[index];
                foreach (PlanetGridSquare planetGridSquare2 in planet.TilesList)
                {
                    if (planetGridSquare2 == planetGridSquare1)
                    {
                        planetGridSquare2.TroopsHere.Add(t);
                        planet.TroopsHere.Add(t);
                        t.SetPlanet(planet);
                        return true;

                    }
                }
            }
            return false;

        }

        public bool AssignTroopToTile(Planet planet = null)
        {
            planet = planet ?? p;
            var list = new Array<PlanetGridSquare>();
            foreach (PlanetGridSquare planetGridSquare in planet.TilesList)
            {
                if (planetGridSquare.TroopsHere.Count < planetGridSquare.number_allowed_troops 
                    && (planetGridSquare.building == null || planetGridSquare.building != null && planetGridSquare.building.CombatStrength == 0))
                    list.Add(planetGridSquare);
            }
            if (list.Count > 0)
            {
                int index = (int)RandomMath.RandomBetween(0.0f, list.Count);
                PlanetGridSquare planetGridSquare = list[index];
                foreach (PlanetGridSquare eventLocation in planet.TilesList)
                {
                    if (eventLocation != planetGridSquare) continue;

                    eventLocation.TroopsHere.Add(this);
                    planet.TroopsHere.Add(this);
                    if (Owner != planet.Owner)
                        Strength = (Strength - planet.TotalInvadeInjure).Clamped(0, StrengthMax);

                    SetPlanet(planet);
                    if (string.IsNullOrEmpty(eventLocation.building?.EventTriggerUID) 
                        || eventLocation.TroopsHere.Count <= 0 || eventLocation.TroopsHere[0].GetOwner().isFaction)
                        return true;
                    ResourceManager.Event(eventLocation.building.EventTriggerUID).TriggerPlanetEvent(planet, eventLocation.TroopsHere[0].GetOwner(), eventLocation, Empire.Universe);
                }
            }
            return false;
        }

    }
}