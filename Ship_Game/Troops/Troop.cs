using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using Ship_Game.Ships;
using System;
using System.IO;
using System.Xml.Serialization;
using SDGraphics;
using SDUtils;
using Ship_Game.Data.Serialization;
using Ship_Game.Universe;
using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;

namespace Ship_Game
{
    [StarDataType]
    public sealed class Troop // Initial refactor by Fat Bastard - March 16, 2019. FB: Added Launch and Land Logic - April 27, 2019
    {
        [StarData] public string Name;
        [StarData] public string RaceType;
        [StarData] public int first_frame = 1;
        [StarData] public bool animated;
        [StarData] public string idle_path;
        [StarData] public string Icon;
        [StarData] public string MovementCue;
        [StarData] public int Level;
        [StarData] public int AttackTimerBase = 10;
        [StarData] public int MoveTimerBase = 10;
        [StarData] public int num_idle_frames;
        [StarData] public int num_attack_frames;
        [StarData] public int idle_x_offset;
        [StarData] public int idle_y_offset;
        [StarData] public int attack_width = 128;
        [StarData] public string attack_path;
        [StarData] public bool facingRight;
        [StarData] public string Description;
        [StarData] public string OwnerString;
        [StarData] public int BoardingStrength;
        [StarData] public int MaxStoredActions = 1;
        [StarData] public float MoveTimer;   // FB - use UpdateMoveTimer or ResetMoveTimer
        [StarData] public float AttackTimer; // FB - use UpdateAttackTimer or ResetAttackTimer
        [StarData] public float MovingTimer = 1f;
        [StarData] public int AvailableMoveActions   = 1; // FB - use UpdateMoveActions 
        [StarData] public int AvailableAttackActions = 1; // FB - use UpdateAttackActions
        [StarData] public string TexturePath;
        [StarData] public bool Idle = true;
        [StarData] public int WhichFrame = 1;
        [StarData] public float Strength; // FB - Do not modify this directly. use DamageTroop and HealTroop
        [StarData] public float StrengthMax; 
        [StarData] public int HardAttack; // FB - use NetHardAttack
        [StarData] public int SoftAttack; // FB - use NetSoftAttack
        [StarData] public string Class;
        [StarData] public TargetType TargetType;
        [StarData] public float Cost;
        [StarData] public string sound_attack;
        [StarData] public int Range;
        [StarData] public float Launchtimer = 10f; // FB - use UpdateLaunchTimer or ResetLaunchTimer
        [StarData] public string Type;

        [XmlIgnore][JsonIgnore] public Planet HostPlanet { get; private set; }
        [XmlIgnore][JsonIgnore] Empire Owner;
        [XmlIgnore][JsonIgnore] public Ship HostShip { get; private set; }
        [XmlIgnore][JsonIgnore] public Rectangle FromRect { get; private set; }
        [XmlIgnore][JsonIgnore] public Rectangle ClickRect { get; private set; }
        [XmlIgnore][JsonIgnore] public bool Hovered;
        [XmlIgnore][JsonIgnore] public static float Consumption = 0.1f; // Consumption of food per turn (or prod, if cybernetic)

        [XmlIgnore][JsonIgnore] float UpdateTimer;
        [XmlIgnore][JsonIgnore] public string DisplayName   => DisplayNameEmpire(Owner);
        [XmlIgnore][JsonIgnore] public float ActualCost     => Cost * CurrentGame.ProductionPace;
        [XmlIgnore][JsonIgnore] public bool CanMove         => AvailableMoveActions > 0;
        [XmlIgnore][JsonIgnore] public bool CanAttack       => AvailableAttackActions > 0;
        [XmlIgnore][JsonIgnore] public int ActualHardAttack => (int)(HardAttack + 0.05f * Level * HardAttack);
        [XmlIgnore][JsonIgnore] public int ActualSoftAttack => (int)(SoftAttack + 0.05f * Level * SoftAttack);
        [XmlIgnore][JsonIgnore] public Empire Loyalty       => Owner ??= EmpireManager.GetEmpireByName(OwnerString);
        [XmlIgnore][JsonIgnore] public int ActualRange      => Level < 5 ? Range : Range + 1;  // veterans have bigger range
        [XmlIgnore][JsonIgnore] public bool IsHealthFull    => Strength.AlmostEqual(ActualStrengthMax);
        [XmlIgnore][JsonIgnore] public bool IsWounded       => !IsHealthFull;
        [XmlIgnore][JsonIgnore] public bool CanLaunchWounded => CanMove;

        [XmlIgnore][JsonIgnore] public SubTexture IconTexture =>  ResourceManager.Texture("TroopIcons/" + Icon + "_icon");

        /// <summary>
        /// A troop can be launched if it is at full health and can move
        /// unless there is no planet owner of the troop belongs to the player and then
        /// it can launch if it can move
        /// </summary>
        [XmlIgnore] [JsonIgnore] public bool CanLaunch =>
                HostPlanet?.Owner == null || Loyalty.isPlayer ? CanMove : CanMove && IsHealthFull;

        SpriteSystem.TextureAtlas TroopAnim;

        void InitializeAtlas(GameScreen screen)
        {
            string defaultTexture = "Textures/Troops/" + TexturePath;
            string folder = Path.GetDirectoryName(defaultTexture).NormalizedFilePath();
            TroopAnim = screen.TransientContent.LoadTextureAtlas(folder);
        }

        SubTexture GetAnimation(string name)
        {
            return TroopAnim.TryGetTexture(Path.GetFileName(name), out SubTexture t) ? t : null;
        }

        SubTexture GetDefaultTex() => GetAnimation(TexturePath) ?? ResourceManager.RootContent.DefaultTexture();
        SubTexture GetIdleTex(int whichFrame) => GetAnimation($"{idle_path}{whichFrame:00}") ?? GetDefaultTex();
        SubTexture GetAttackTex(int whichFrame) => GetAnimation($"{attack_path}{whichFrame:00}") ?? GetDefaultTex();

        public string DisplayNameEmpire(Empire empire = null)
        {
            empire = Owner ?? empire;
            if (empire == null || !empire.data.IsRebelFaction) return Name;
            return empire.data.TroopName.Text;
        }

        public Troop Clone()
        {
            var t        = (Troop)MemberwiseClone();
            t.HostPlanet = null;
            t.Owner      = null;
            t.HostShip   = null;
            return t;
        }

        public void ChangeLoyalty(Empire newOwner)
        {
            Owner = newOwner;
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

        public string StrengthText => $"Strength: {Strength:0.}";

        public void Draw(UniverseState us, SpriteBatch batch, Vector2 pos, Vector2 size)
        {
            Draw(us, batch, new Rectangle(pos, size));
        }

        public void SetCombatScreenRect(PlanetGridSquare tile, int width)
        {
            Rectangle rect = tile.ClickRect;
            if (tile.TroopsHere.Count < 2)
            {
                ClickRect = new Rectangle(rect.X + rect.Width / 2 - width / 2,
                                       rect.Y + rect.Height / 2 - width / 2,
                                       width, width);
            }
            else // 2 troops on tile
            {
                if (tile.TroopsHere[0] == this)
                {
                    ClickRect = new Rectangle(rect.X + rect.Width / 2 + width / 10,
                                               rect.Y + (int)(rect.Height / 1.33) - width / 2,
                                               (int)(width * 0.8), (int)(width * 0.8));
                }
                else
                {
                    ClickRect = new Rectangle(rect.X + rect.Width / 2 - (int)(width / 1.2f),
                                              rect.Y + rect.Height / 3 - width / 2,
                                               (int)(width * 0.8), (int)(width * 0.8));
                }
            }
        }

        public void SetColonyScreenRect(PlanetGridSquare tile)
        {
            Rectangle rect = tile.ClickRect;
            if (tile.TroopsHere.Count < 2)
            {
                ClickRect = new Rectangle(rect.X + rect.Width - 48, rect.Y, 48, 48);
            }
            else // 2 troops on tile
            {
                if (tile.TroopsHere[0] == this)
                    ClickRect = new Rectangle(rect.X + rect.Width - 48, rect.Y + 36, 48, 48);
                else
                    ClickRect = new Rectangle(rect.X, rect.Y, 48, 48);
            }
        }

        public void FaceEnemy(PlanetGridSquare targetTile, PlanetGridSquare ourTile)
        {
            if (targetTile != ourTile)
                facingRight = targetTile.X >= ourTile.X;
            else // troops are on the same tile
                facingRight = ClickRect.X < ourTile.ClickRect.X + ClickRect.Width / 2;
        }

        public void Draw(UniverseState us, SpriteBatch sb, Rectangle drawRect)
        {
            if (TroopAnim == null)
                InitializeAtlas(us.Screen);

            if (!animated)
            {
                Draw(sb, GetDefaultTex(), drawRect, flip: !facingRight);
            }
            else
            {
                SubTexture tex = Idle ? GetIdleTex(WhichFrame) : GetAttackTex(WhichFrame);
                DrawTexture(sb, tex, drawRect, flip: !facingRight);
            }
        }

        void DrawTexture(SpriteBatch sb, SubTexture tex, Rectangle drawRect, bool flip)
        {
            Troop t = ResourceManager.GetTroopTemplate(Name);
            int x_offset = t.idle_x_offset;
            int y_offset = t.idle_y_offset;
            int width = Idle ? tex.Width : t.attack_width;

            float scale = drawRect.Width / 128f;
            drawRect.Width = (int)(width * scale);

            if (tex.Height <= 128) //
            {
                var srcRect = new Rectangle(x_offset, y_offset, width-x_offset, tex.Height-y_offset);
                Draw(sb, tex, drawRect, srcRect, flip);
            }
            else // for Kulrathi their Height box is bigger
            {
                var srcRect = new Rectangle(x_offset, 0, width, y_offset+128);
                int offset = (int)(scale * y_offset);
                var r = new Rectangle(drawRect.X, drawRect.Y - offset, drawRect.Width, drawRect.Height + offset);
                Draw(sb, tex, r, srcRect, flip);
            }
        }

        static void Draw(SpriteBatch sb, SubTexture tex, in Rectangle r, in Rectangle srcRect, bool flip)
        {
            if (flip)
                sb.Draw(tex, r, srcRect, Color.White, 0f, Vector2.Zero, SpriteEffects.FlipHorizontally, 1f);
            else
                sb.Draw(tex, r, srcRect, Color.White);
        }

        static void Draw(SpriteBatch sb, SubTexture tex, in Rectangle r, bool flip)
        {
            if (flip)
                sb.Draw(tex, r, Color.White, 0f, Vector2.Zero, SpriteEffects.FlipHorizontally, 1f);
            else
                sb.Draw(tex, r, Color.White);
        }

        public void DrawIcon(SpriteBatch spriteBatch, Rectangle drawRect)
        {
            spriteBatch.Draw(IconTexture, drawRect, Color.White);
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
        }

        public void SetShip(Ship s)
        {
            HostShip = s;
        }

        public void Update(float elapsedTime)
        {
            UpdateTimer -= elapsedTime;
            if (UpdateTimer > 0f)
                return;

            if (!Idle)
            {
                UpdateTimer = 0.75f / num_attack_frames;
                if (WhichFrame >= num_attack_frames)
                {
                    WhichFrame = first_frame;
                    Idle = true;
                }
                else
                {
                    WhichFrame++;
                }
            }
            else
            {
                UpdateTimer = 1f / num_idle_frames;
                if (WhichFrame >= num_idle_frames)
                {
                    WhichFrame = first_frame;
                }
                else
                {
                    WhichFrame++;
                }
            }

            Log.Write(ConsoleColor.Blue, $"Frame={WhichFrame} first={first_frame} idle={num_idle_frames} attack={num_attack_frames}");
        }

        // Added by McShooterz, FB: changed it to level up every kill with decreasing chances
        // since troops are dying like flies
        public void LevelUp()
        {
            if (RandomMath.RollDie(10) > Level)
                Level = (Level + 1).Clamped(0,10);
        }

        /// <summary>
        /// Damages the troop also removes the troop from relevant lists, if it was destroyed.
        /// </summary>
        public void DamageTroop(float amount, Planet planet, PlanetGridSquare tile, out bool dead)
        {
            dead     = false;
            Strength = (Strength - amount).Clamped(0, ActualStrengthMax);
            if (Strength < 1)
            {
                planet.TroopsHere.Remove(this); // not using RemoveSwapLast since the order of troop is important for allied invasion
                if (tile.TroopsHere.Any(t => t == this))
                    tile.TroopsHere.Remove(this);
                else
                    planet.SearchAndRemoveTroopFromTile(this);

                dead = true;
            }
        }

        public void DamageTroop(Ship combatShip, ref float damage)
        {
            float oldStrength = Strength;
            Strength = oldStrength - damage; // deal the damage
            
            // only discount damage taken down to 0HP, leave the rest intact
            float damageDealt = (oldStrength - Math.Max(0, Strength));
            damage -= damageDealt;

            if (Strength <= 0)
                combatShip.RemoveAnyTroop(this); // Die!
        }

        public void HealTroop(float amount)
        {
            Strength = (Strength + amount).UpperBound(ActualStrengthMax);
        }

        public void KillTroop(Planet planet, PlanetGridSquare tile)
        {
            DamageTroop(ActualStrengthMax, planet, tile, out _);
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
        public Ship Launch(bool ignoreMovement = false)
        {
            if (HostPlanet == null)
                return null;

            foreach (PlanetGridSquare tile in HostPlanet.TilesList)
            {
                if (tile.TroopsHere.ContainsRef(this))
                    return LaunchToSpace(tile, ignoreMovement);
            }
            // Tile not found
            return null;
        }

        // Launch a troop from a specific tile
        public Ship Launch(PlanetGridSquare tile, bool ignoreMovement = false)
        {
            return HostPlanet != null ? LaunchToSpace(tile, ignoreMovement) : null;
        }

        // Launch a troop which was created in a planet but there was no room for it.
        public Ship Launch(Planet planet)
        {
            return CreateShipForTroop(planet);
        }

        Ship LaunchToSpace(PlanetGridSquare tile, bool ignoreMovement = false)
        {
            if (!CanLaunch && !ignoreMovement)
                return null;

            using (HostPlanet.TroopsHere.AcquireWriteLock())
            {
                using (tile.TroopsHere.AcquireWriteLock())
                {
                    tile.TroopsHere.Remove(this);
                    HostPlanet.TroopsHere.Remove(this);
                    Ship troopShip = CreateShipForTroop(HostPlanet);
                    HostPlanet     = null;
                    return troopShip;
                }
            }
        }

        Ship CreateShipForTroop(Planet planet)
        {
            Vector2 createAt = planet.Position + RandomMath.Vector2D(planet.Radius * 2);
            return Ship.CreateTroopShipAtPoint(planet.Universe, Owner.data.DefaultTroopShip, Owner, createAt, this);
        }

        // FB - this is the main logic for land troops
        // if tile != null, it will assign troops to nearest available tile
        public bool TryLandTroop(Planet planet, PlanetGridSquare tile = null)
        {
            planet = planet ?? HostPlanet;
            if (planet.GetFreeTiles(Loyalty) == 0)
                return false;

            return tile != null ? AssignTroopToNearestAvailableTile(tile, planet)
                                : AssignTroopToRandomFreeTile(planet);
        }

        // FB - For newly recruited troops (so they will be able to launch or move immediately)
        public bool PlaceNewTroop(Planet planet)
        {
            return planet.GetFreeTiles(Loyalty) > 0 && AssignTroopToRandomFreeTile(planet, resetMove: false);
        }

        bool AssignTroopToNearestAvailableTile(PlanetGridSquare tile, Planet planet)
        {
            if (tile.IsTileFree(Loyalty))
            {
                AssignTroopToTile(planet, tile);
                return true;
            }

            PlanetGridSquare[] nearbyFreeTiles = planet.TilesList.Filter(
                pgs => pgs.IsTileFree(Loyalty) && tile.InRangeOf(pgs, 1));

            if (nearbyFreeTiles.Length == 0)
                return AssignTroopToRandomFreeTile(planet); // Fallback to assign troop to any available tile if no close tile available

            PlanetGridSquare randomNearbyFreeTile = nearbyFreeTiles.RandItem();
            AssignTroopToTile(planet, randomNearbyFreeTile);
            return true;
        }

        bool AssignTroopToRandomFreeTile(Planet planet, bool resetMove = true)
        {
            PlanetGridSquare[] freeTiles = planet.TilesList.Filter(t => t.IsTileFree(Loyalty));
            if (freeTiles.Length == 0)
                return false;

            PlanetGridSquare tileToLand = PickTileToLand(planet, freeTiles);
            AssignTroopToTile(planet, tileToLand, resetMove);
            // some buildings can injure landing troops
            if (Owner != planet.Owner)
                DamageTroop(planet.TotalInvadeInjure, planet, tileToLand,  out bool _);

            tileToLand.CheckAndTriggerEvent(planet, Loyalty);
            planet.SetInGroundCombat(Loyalty, notify: true);
            return true;
        }

        void AssignTroopToTile(Planet planet, PlanetGridSquare tile, bool resetMove = true)
        {
            planet.AddTroop(this, tile);
            RemoveTroopFromHostShip();
            facingRight = tile.X < planet.TileMaxX / 2;
            if (resetMove)
            {
                UpdateMoveActions(-1);
                ResetMoveTimer();
                UpdateAttackActions(-1);
                AttackTimer = 3f; // Land delay
            }

            planet.SetInGroundCombat(Loyalty, notify: true);
        }

        PlanetGridSquare PickTileToLand(Planet planet, PlanetGridSquare[] freeTiles)
        {
            if (!planet.RecentCombat && planet.GetEnemyAssets(Loyalty) == 0)
                return freeTiles.RandItem(); // Non Combat landing

            SDUtils.Array<PlanetGridSquare> bestTiles = new SDUtils.Array<PlanetGridSquare>();
            int bestScore = int.MinValue;
            for (int i = 0; i < freeTiles.Length; ++i)
            {
                PlanetGridSquare tile = freeTiles[i];
                int score = CombatLandingTileScore(tile, planet);
                if (score > bestScore) // this is the new best tile
                {
                    bestTiles.Clear();
                    bestScore = score;
                    bestTiles.Add(tile);
                }
                else if (score == bestScore)
                {
                    bestTiles.Add(tile); // add to possible list of tiles
                }
            }

            return bestTiles.Count > 0 ? bestTiles.RandItem() : freeTiles.RandItem();
        }

        int CombatLandingTileScore(PlanetGridSquare tile, Planet planet)
        {
            int score  = 0;
            Ping ping  = new Ping(tile, planet, 1);
            for (int x = ping.Left; x <= ping.Right; ++x)
            {
                for (int y = ping.Top; y <= ping.Bottom; ++y)
                {
                    PlanetGridSquare checkedTile = planet.TilesList[x * ping.Width + y];
                    score += checkedTile.CalculateNearbyTileScore(this, planet.Owner);
                }
            }

            return score;
        }

        public bool AcquireTarget(PlanetGridSquare tile, Planet planet, out PlanetGridSquare targetTile)
        {
            int bestScore = 0;
            targetTile    = null;
            Ping ping     = new Ping(tile, planet, ActualRange);
            for (int x = ping.Left; x <= ping.Right; ++x)
            {
                for (int y = ping.Top; y <= ping.Bottom; ++y)
                {
                    PlanetGridSquare checkedTile = planet.TilesList[x * ping.Width + y];
                    int score = checkedTile.CalculateTargetValue(this, planet);
                    if (score > bestScore)
                    {
                        bestScore  = score;
                        targetTile = checkedTile;
                    }
                }
            }

            return bestScore > 0;
        }

        struct Ping
        {
            public readonly int Left;
            public readonly int Right;
            public readonly int Top;
            public readonly int Bottom;
            public readonly int Width;

            public Ping(PlanetGridSquare tile, Planet planet, int pingSize)
            {
                Left   = (tile.X - pingSize).LowerBound(0);
                Right  = (tile.X + pingSize).UpperBound(planet.TileMaxX - 1);
                Top    = (tile.Y - pingSize).LowerBound(0);
                Bottom = (tile.Y + pingSize).UpperBound(planet.TileMaxY - 1);
                Width  = planet.TileMaxY;
            }
        }

        void RemoveTroopFromHostShip()
        {
            if (HostShip == null)
                return;

            HostShip.RemoveAnyTroop(this);

            // Remove the ship if it was the default single troop. They are designed to vanish once landing the troop.
            // Assault Shuttles are designed to try to get back to their hangars 
            if (HostShip.IsDefaultTroopShip || HostShip.IsDefaultAssaultShuttle && !HostShip.IsHangarShip)
            {
                HostShip.QueueTotalRemoval();
                HostShip = null;
            }
        }

        public void LandOnShip(Ship ship)
        {
            RemoveTroopFromHostShip();
            ship.AddTroop(this);

            // new host ship since the troop has landed on a new ship
            // NOTE: it is completely fine if this is an enemy ship
            HostShip = ship; 
        }
    }
}