using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game.Universe.SolarBodies
{
    public class Volcano // Created by Fat Bastard, Mar 2021
    {
        public bool Active { get; private set; }
        public bool Erupting { get; private set; }
        public float ActivationChance { get; private set; }
        public readonly PlanetGridSquare Tile;
        public readonly Planet P;

        public Volcano(PlanetGridSquare tile, Planet planet)
        {
            ActivationChance = InitActivationChance();
            Tile             = tile;
            P                = planet;
            CreateDormantVolcano();
        }

        // From save
        public Volcano(SavedGame.PGSData data, PlanetGridSquare tile, Planet planet)
        {
            ActivationChance = data.VolcanoActivationChance;
            Active           = data.VolcanoActive;
            Erupting         = data.VolcanoErupting;
            Tile             = tile;
            P                = planet;
        }

        public Empire Player           => EmpireManager.Player;
        public bool Dormant            => !Active;
        float DeactivationChance       => ActivationChance * 3;
        float ActiveEruptionChance     => ActivationChance * 15;
        float InitActivationChance()   => RandomMath.RandomBetween(0.05f, 0.1f) * GlobalStats.VolcanicActivity;
        string ActiveVolcanoTexPath    => "Buildings/icon_Active_Volcano_64x64";
        string DormantVolcanoTexPath   => "Buildings/icon_Dormant_Volcano_64x64";
        string EruptingVolcanoTexPath  => "Buildings/icon_Erupting_Volcano_64x64";
        public bool ShouldNotifyPlayer => P.Owner == Player || P.AnyOfOurTroops(Player);

        void CreateLavaPool(PlanetGridSquare tile) // Must get a tile with no Volcano on it
        {
            if (tile.BuildingOnTile && P.Owner == Player)
                Empire.Universe.NotificationManager.AddBuildingDestroyedByLava(P, tile.Building);

            int bid = Building.Lava1Id;
            P.DestroyTile(tile);
            switch (RandomMath.RollDie(3))
            {
                case 2: bid = Building.Lava2Id; break;
                case 3: bid = Building.Lava3Id; break;
            }

            Building b = ResourceManager.CreateBuilding(bid);
            tile.PlaceBuilding(b, P);
            P.SetHasDynamicBuildings(true);
        }

        void CreateVolcanoBuilding(int bid)
        {
            Building b = ResourceManager.CreateBuilding(bid);
            Tile.PlaceBuilding(b, P);
            P.SetHasDynamicBuildings(true);
        }

        void CreateDormantVolcano()
        {
            RemoveVolcanoBeforeReplacing();
            Active     = false;
            Erupting   = false;
            CreateVolcanoBuilding(Building.VolcanoId);
        }

        public void Evaluate()
        {
            if (Dormant)
            {
                TryActivate();
            }
            else if (Active && !Erupting)
            {
                if (TryDeactivate())
                    return;

                TryErupt();
            }
            else if (Erupting)
            {
                TryCalmDown();
            }
        }

        void TryActivate()
        {
            if (!RandomMath.RollDice(ActivationChance))
                return;

            RemoveVolcanoBeforeReplacing();
            Active     = true;
            CreateVolcanoBuilding(Building.ActiveVolcanoId);
            if (!GlobalStats.DisableVolcanoWarning && ShouldNotifyPlayer)
                Empire.Universe.NotificationManager.AddVolcanoRelated(P, new LocalizedText(4256).Text, ActiveVolcanoTexPath);
        }

        void TryErupt()
        {
            if (!RandomMath.RollDice(ActiveEruptionChance))
                return;

            RemoveVolcanoBeforeReplacing();
            string message = new LocalizedText(4260).Text;
            Erupting       = true;
            Erupt(out string eruptionSeverityText);
            message = $"{message}\n{eruptionSeverityText}";
            CreateVolcanoBuilding(Building.EruptingVolcanoId);
            if (RandomMath.RollDice(5) && P.BasePopPerTile > 300)
            {
                P.BasePopPerTile *= 0.9f;
                message = $"{message}\n{new LocalizedText(4262).Text}";
            }
            else
            {
                message = $"{message}\n{new LocalizedText(4261).Text}";
            }

            if (ShouldNotifyPlayer)
                Empire.Universe.NotificationManager.AddVolcanoRelated(P, message, EruptingVolcanoTexPath);
        }

        void TryCalmDown()
        {
            if (!RandomMath.RollDice(1))
                return;

            CreateDormantVolcano();
            string message   = new LocalizedText(4258).Text;
            ActivationChance = InitActivationChance();
            if (RandomMath.RollDice(ActiveEruptionChance * 2))
            {
                float increaseBy   = RandomMath.RollDice(75) ? 0.1f : 0.2f;
                message            = $"{message}\n{new LocalizedText(4259).Text} {increaseBy.String(1)}.";
                P.MineralRichness += increaseBy;
            }

            if (!GlobalStats.DisableVolcanoWarning && ShouldNotifyPlayer)
                Empire.Universe.NotificationManager.AddVolcanoRelated(P, message, DormantVolcanoTexPath);
        }

        bool TryDeactivate()
        {
            if (!RandomMath.RollDice(DeactivationChance))
                return false;

            Active   = false;
            Erupting = false;
            CreateDormantVolcano();
            if (!GlobalStats.DisableVolcanoWarning && ShouldNotifyPlayer)
                Empire.Universe.NotificationManager.AddVolcanoRelated(P, new LocalizedText(4257).Text, DormantVolcanoTexPath);

            return true;
        }

        void Erupt(out string eruptionSeverityText)
        {
            var potentialTiles     = P.TilesList.Filter(t => !t.VolcanoHere & !t.LavaHere);
            int numLavaPoolsWanted = GetNumLavaPools(potentialTiles.Length.UpperBound(16));
            int actualLavaPools    = 0;
            var potentialLavaTiles = GetPotentialLavaTiles(Tile);

            for (int i = 0; i < numLavaPoolsWanted; i++)
            {
                if (potentialLavaTiles.Count == 0)
                    break;

                PlanetGridSquare tile = potentialLavaTiles.RandItem();
                CreateLavaPool(tile);
                actualLavaPools += 1;
                potentialLavaTiles.AddUniqueRef(GetPotentialLavaTiles(tile));
                potentialLavaTiles.Remove(tile);
            }

            eruptionSeverityText = GetEruptionText(actualLavaPools);
        }

        string GetEruptionText(int numLavaPoolsCreated)
        {
            string text;
            if (numLavaPoolsCreated == 0)     text = new LocalizedText(4263).Text;
            else if (numLavaPoolsCreated <=3) text = new LocalizedText(4264).Text;
            else                              text = new LocalizedText(4265).Text;

            return text;
        }

        Array<PlanetGridSquare> GetPotentialLavaTiles(PlanetGridSquare tile)
        {
            Array<PlanetGridSquare> tiles = new Array<PlanetGridSquare>();
            for (int i = 0; i < P.TilesList.Count; i++)
            {
                PlanetGridSquare t = P.TilesList[i];
                if (!t.VolcanoHere && !t.LavaHere && t.InRangeOf(tile, 1))
                    tiles.Add(t);
            }

            return tiles;
        }

        int GetNumLavaPools(int maxSeverity)
        {
            int numLavaPools;
            switch (RandomMath.RollDie(maxSeverity))
            {
                default: numLavaPools = 0; break;
                case 5:  numLavaPools = 1; break;
                case 6:
                case 7:  numLavaPools = 2; break;
                case 8:
                case 9:  numLavaPools = 3; break;
                case 10:
                case 11: numLavaPools = 4; break;
                case 12:
                case 13: numLavaPools = 5; break;
                case 14: numLavaPools = 6; break;
                case 15: numLavaPools = 7; break;
                case 16: numLavaPools = 8; break;
            }

            return numLavaPools;
        }

        public static void UpdateLava(PlanetGridSquare tile, Planet planet)
        {
            if (!RandomMath.RollDice(2))
                return;

            // Remove the Lava Pool
            string lavaPath = tile.BuildingOnTile ? tile.Building.IconPath64 : "";
            planet.DestroyTile(tile);
            if (RandomMath.RollDice(75))
            {
                planet.MakeTileHabitable(tile);
                if (planet.Owner == EmpireManager.Player && !GlobalStats.DisableVolcanoWarning)
                    Empire.Universe.NotificationManager.AddVolcanoRelated(planet, new LocalizedText(4266).Text, lavaPath);
            }
        }

        void RemoveVolcanoBeforeReplacing()
        {
            P.DestroyBuildingOn(Tile);
            P.DestroyTile(Tile);
        }


        /// <summary>
        /// This will remove the Volcano and the class. Use it when you want to completely get rid of theVolcano
        /// </summary>
        public static void RemoveVolcano(PlanetGridSquare tile, Planet planet)
        {
            planet.DestroyBuildingOn(tile);
            planet.DestroyTile(tile);
            tile.Volcano = null;
            planet.ResetHasDynamicBuildings();
        }

        public string ActivationChanceText(out Color color)
        {
            color = Color.Green;
            if (Erupting)
                return "";

            string text;
            if (Dormant)
            {
                if      (ActivationChance < 0.01f) text = new LocalizedText(4243).Text;
                else if (ActivationChance < 0.05f) text = new LocalizedText(4244).Text;
                else if (ActivationChance < 0.1f)  text = new LocalizedText(4245).Text;
                else                               text = new LocalizedText(4246).Text;

                color = Color.Yellow;
                return $"{text} {new LocalizedText(4239).Text}";
            }

            if (Active)
            {
                if      (ActiveEruptionChance < 0.1f) text = new LocalizedText(4245).Text;
                else if (ActiveEruptionChance < 0.5f) text = new LocalizedText(4246).Text;
                else if (ActiveEruptionChance < 1f)   text = new LocalizedText(4247).Text;
                else                                  text = new LocalizedText(4248).Text;

                color = Color.Red;
                return $"{text} {new LocalizedText(4242).Text}";
            }

            return "";
        }
    }
}