using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using Ship_Game.Ships;
using System;
using System.Xml.Serialization;

namespace Ship_Game
{
    public sealed class Troop // Initial refactor by Fat Bastard - March 16, 2019. FB: Added Launch and Land Logic - April 27, 2019
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
        [Serialize(21)] public float MoveTimer;   // FB - use UpdateMoveTimer or ResetMoveTimer
        [Serialize(22)] public float AttackTimer; // FB - use UpdateAttackTimer or ResetAttackTimer
        [Serialize(23)] public float MovingTimer = 1f;
        [Serialize(24)] public int AvailableMoveActions   = 1; // FB - use UpdateMoveActions 
        [Serialize(25)] public int AvailableAttackActions = 1; // FB - use UpdateAttackActions
        [Serialize(26)] public string TexturePath;
        [Serialize(27)] public bool Idle = true;
        [Serialize(28)] public int WhichFrame = 1;
        [Serialize(29)] public float Strength; // FB - Do not modify this directly. use DamageTroop and HealTroop
        [Serialize(30)] public float StrengthMax; 
        [Serialize(31)] public int HardAttack; // FB - use NetHardAttack
        [Serialize(32)] public int SoftAttack; // FB - use NetSoftAttack
        [Serialize(33)] public string Class;
        [Serialize(34)] public TargetType TargetType;
        [Serialize(35)] public float Cost;
        [Serialize(36)] public string sound_attack;
        [Serialize(37)] public int Range;
        [Serialize(38)] public float Launchtimer = 10f; // FB - use UpdateLaunchTimer or ResetLaunchTimer
        [Serialize(39)] public string Type;

        [XmlIgnore][JsonIgnore] public Planet HostPlanet { get; private set; }
        [XmlIgnore][JsonIgnore] private Empire Owner;
        [XmlIgnore][JsonIgnore] public Ship HostShip { get; private set; }
        [XmlIgnore][JsonIgnore] public Rectangle FromRect { get; private set; }

        [XmlIgnore][JsonIgnore] private float UpdateTimer;
        [XmlIgnore][JsonIgnore] public string DisplayName    => DisplayNameEmpire(Owner);
        [XmlIgnore] [JsonIgnore] public float ActualCost     => Cost * CurrentGame.Pace;
        [XmlIgnore] [JsonIgnore] public bool CanMove         => AvailableMoveActions > 0;
        [XmlIgnore] [JsonIgnore] public bool CanAttack       => AvailableAttackActions > 0;
        [XmlIgnore] [JsonIgnore] public int ActualHardAttack => (int)(HardAttack + 0.05f * Level * HardAttack);
        [XmlIgnore] [JsonIgnore] public int ActualSoftAttack => (int)(SoftAttack + 0.05f * Level * SoftAttack);
        [XmlIgnore] [JsonIgnore] public Empire Loyalty       => Owner ?? (Owner = EmpireManager.GetEmpireByName(OwnerString));
        [XmlIgnore] [JsonIgnore] public int ActualRange      => Level < 3   ? Range : Range + 1;  // veterans have bigger range

        [XmlIgnore] [JsonIgnore] public SubTexture TextureDefault => ResourceManager.Texture("Troops/" + TexturePath);

        private string WhichFrameString => WhichFrame.ToString("00");

        //@HACK the animation index and firstframe value are coming up with bad values for some reason. i could not figure out why
        //so here i am forcing it to draw troop template first frame if it hits a problem. in the update method i am refreshing the firstframe value as well. 
        private SubTexture TextureIdleAnim => ResourceManager.TextureOrDefault(
            "Troops/" + idle_path + WhichFrameString,
            "Troops/" + idle_path + ResourceManager.GetTroopTemplate(Name).first_frame.ToString("0000"));

        private SubTexture TextureAttackAnim => ResourceManager.TextureOrDefault(
            "Troops/" + attack_path + WhichFrameString,
            "Troops/" + idle_path + ResourceManager.GetTroopTemplate(Name).first_frame.ToString("0000"));

        public string DisplayNameEmpire(Empire empire = null)
        {
            empire = Owner ?? empire;
            if (empire == null || !empire.data.IsRebelFaction) return Name;
            return Localizer.Token(empire.data.TroopNameIndex);
        }

        public Troop Clone()
        {
            var t        = (Troop)MemberwiseClone();
            t.HostPlanet = null;
            t.Owner      = null;
            t.HostShip   = null;
            return t;
        }

        public void DoAttack()
        {
            Idle       = false;
            WhichFrame = first_frame;
        }

        public void UpdateAttackActions(int amount)
        {
            AvailableAttackActions = (AvailableAttackActions + amount).Clamped(0, MaxStoredActions);
        }

        public void UpdateMoveActions(int amount)
        {
            AvailableMoveActions = (AvailableMoveActions + amount).Clamped(0, MaxStoredActions);
        }

        public void UpdateMoveTimer(float amount)
        {
            if (!CanMove) MoveTimer += amount;
        }

        public void UpdateAttackTimer(float amount)
        {
            if (!CanAttack) AttackTimer += amount;
        }

        public void UpdateLaunchTimer(float amount)
        {
            Launchtimer += amount;
        }

        public void ResetMoveTimer()
        {
            MoveTimer = Math.Max(MoveTimerBase - (int)(Level * 0.5), 5);
        }

        public void ResetAttackTimer()
        {
            AttackTimer = Math.Max(AttackTimerBase - (int)(Level * 0.5), 5);
        }

        public void ResetLaunchTimer()
        {
            Launchtimer = MoveTimerBase; // FB -  yup, MoveTimerBase
        }

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

            float scale     = drawRect.Width / 128f;
            drawRect.Width  = (int)(attack_width * scale);
            var sourceRect2 = new Rectangle(idle_x_offset, idle_y_offset, attack_width, 128);

            SubTexture attackTexture = TextureAttackAnim;
            if (attackTexture.Height <= 128)
            {
                spriteBatch.Draw(attackTexture, drawRect, sourceRect2, Color.White);
                return;
            }
            sourceRect2.Y      -= idle_y_offset;
            sourceRect2.Height += idle_y_offset;
            Rectangle r         = drawRect;
            r.Y                -= (int)(scale * idle_y_offset);
            r.Height           += (int)(scale * idle_y_offset);
            spriteBatch.Draw(attackTexture, r, sourceRect2, Color.White);
        }

        public void DrawFlip(SpriteBatch spriteBatch, Rectangle drawRect)
        {
            if (!animated)
            {
                spriteBatch.Draw(TextureDefault, drawRect, Color.White, 0f, Vector2.Zero, SpriteEffects.FlipHorizontally, 1f);
                return;
            }
            if (Idle)
            {
                var sourceRect = new Rectangle(idle_x_offset, idle_y_offset, 128, 128);
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
            Rectangle r         = drawRect;
            r.Height           += (int)(scale * idle_y_offset);
            r.Y                -= (int)(scale * idle_y_offset);
            spriteBatch.Draw(attackTexture, r, sourceRect2, Color.White, 0f, Vector2.Zero, SpriteEffects.FlipHorizontally, 1f);
        }

        public void DrawIcon(SpriteBatch spriteBatch, Rectangle drawRect)
        {
            var iconTexture = ResourceManager.Texture("TroopIcons/" + Icon + "_icon");
            spriteBatch.Draw(iconTexture, drawRect, Color.White);
        }

        public void SetFromRect(Rectangle from)
        {
            FromRect = from;
        }

        public void SetOwner(Empire e)
        {
            Owner = e;
            if (e != null)
                OwnerString = e.data.Traits.Name;
        }

        public void SetPlanet(Planet newPlanet)
        {
            HostPlanet = newPlanet;
            if (HostPlanet != null && !HostPlanet.TroopsHere.Contains(this))
            {
                HostPlanet.TroopsHere.Add(this);
            }
        }

        public void SetShip(Ship s)
        {
            HostShip = s;
        }

        public void Update(float elapsedTime)
        {
            Troop troop        = this;
            UpdateTimer -= elapsedTime;
            if (UpdateTimer > 0f)
                return;
            first_frame = ResourceManager.GetTroopTemplate(troop.Name).first_frame;
            int whichFrame    = WhichFrame;
            if (!Idle)
            {
                UpdateTimer = 0.75f / num_attack_frames;                
                whichFrame++;
                if (whichFrame <= num_attack_frames - (first_frame == 1 ? 0 : 1))
                {
                    WhichFrame++;
                    return;
                }

                WhichFrame = first_frame;
                Idle       = true;
            }
            else
            {
                UpdateTimer = 1f / num_idle_frames;
                whichFrame++;
                if (whichFrame <= num_idle_frames - (first_frame == 1 ? 0 : 1))
                {
                    WhichFrame++;
                    return;
                }

                WhichFrame = first_frame;
            }
        }

        // Added by McShooterz, FB: changed it to level up every kill since troops are dying like flies
        public void LevelUp()
        {
            Level = (Level +1).Clamped(0,10);
        }

        public void DamageTroop(float amount)
        {
            Strength = (Strength - amount).Clamped(0, ActualStrengthMax);
        }

        public void HealTroop(float amount)
        {
            DamageTroop(-amount);
        }

        public float ActualStrengthMax
        {
            get
            {
                if (StrengthMax <= 0)
                    StrengthMax = ResourceManager.GetTroopTemplate(Name).Strength;

                float modifiedStrength = (StrengthMax + Level) * (1 + Owner?.data.Traits.GroundCombatModifier ?? 1f);
                return (float)Math.Round(modifiedStrength, 0);
            }
        }

        // Launch a troop which it's tile location is unknown
        public Ship Launch()
        {
            if (HostPlanet == null)
                return null;

            foreach (PlanetGridSquare tile in HostPlanet.TilesList)
            {
                if (!tile.TroopsHere.Contains(this))
                    continue;

                return LaunchToSpace(tile);
            }
            // Tile not found
            return null;
        }

        // Launch a troop from a specific tile
        public Ship Launch(PlanetGridSquare tile)
        {
            return HostPlanet != null ? LaunchToSpace(tile) : null;
        }

        // Launch a troop which was created in a planet but there was no room for it.
        public Ship Launch(Planet planet)
        {
            return CreateShipForTroop(planet);
        }

        private Ship LaunchToSpace(PlanetGridSquare tile)
        {
            if (!CanMove)
                return null;

            tile.TroopsHere.Clear();
            HostPlanet.TroopsHere.Remove(this);
            Ship troopShip = CreateShipForTroop(HostPlanet);
            HostPlanet     = null;
            return troopShip;
        }

        private Ship CreateShipForTroop(Planet planet)
        {
            Vector2 createAt = planet.Center + RandomMath.Vector2D(planet.ObjectRadius * 2);
            return Ship.CreateTroopShipAtPoint(Owner.data.DefaultTroopShip, Owner, createAt, this);
        }

        // FB - this is the main logic for land troops. 
        public bool TryLandTroop(Planet planet)
        {
            planet = planet ?? HostPlanet;
            return planet.FreeTiles > 0 && AssignTroopToTile(planet);
        }

        // FB - this is the main logic for land troops if they need the nearest tile from a target tile 
        public bool TryLandTroop(Planet planet, PlanetGridSquare tile)
        {
            planet = planet ?? HostPlanet;
            return planet.FreeTiles > 0 && AssignTroopToNearestAvailableTile(tile, planet);
        }

        private bool AssignTroopToNearestAvailableTile(PlanetGridSquare tile, Planet planet )
        {
            if (tile.IsTileFree)
                AssignTroop(planet, tile);
            else
            {
                Array<PlanetGridSquare> list = new Array<PlanetGridSquare>();
                foreach (PlanetGridSquare pgs in planet.TilesList)
                {
                    if (pgs.IsTileFree && tile.InRangeOf(pgs, 1))
                        list.Add(pgs);
                }

                if (list.Count == 0)
                    return AssignTroopToTile(planet); // Fallback to assign troop to any available tile if no close tile available

                PlanetGridSquare selectedTile = list.RandItem();
                AssignTroop(planet, selectedTile);
            }
            return true;
        }

        private bool AssignTroopToTile(Planet planet)
        {
            var list = new Array<PlanetGridSquare>();
            foreach (PlanetGridSquare tile in planet.TilesList)
            {
                if (tile.IsTileFree)
                    list.Add(tile);
            }

            if (list.Count <= 0)
                return false;

            PlanetGridSquare selectedTile = list.RandItem();
            AssignTroop(planet, selectedTile);
            // some buildings can injure landing troops
            if (Owner != planet.Owner)
                DamageTroop(planet.TotalInvadeInjure);

            selectedTile.CheckAndTriggerEvent(planet, Loyalty);
            return true;
        }

        private void AssignTroop(Planet planet, PlanetGridSquare tile)
        {
            tile.TroopsHere.Add(this);
            planet.TroopsHere.Add(this);
            RemoveHostShip();
            SetPlanet(planet);
            UpdateMoveActions(-1);
            ResetMoveTimer();
            facingRight = tile.x < planet.TileMaxX / 2;
        }

        private void RemoveHostShip()
        {
            if (HostShip == null)
                return;

            HostShip.TroopList.Remove(this);
            // Remove the ship if it was the default single troop. They are designed to vanish once landing the troop.
            // Assault Shuttles are designed to try to get back to their hangars 
            if (HostShip.IsDefaultTroopShip)
            {
                HostShip.QueueTotalRemoval();
                HostShip = null;
            }
        }

        public void LandOnShip(Ship ship)
        {
            ship.TroopList.Add(this);
            RemoveHostShip();
        }
    }
}