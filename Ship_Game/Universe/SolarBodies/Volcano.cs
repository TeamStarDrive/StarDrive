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
        private const float MaxActivationChance = 0.1f;

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
        float DeactivationChance       => ActivationChance * 5;
        float ActiveEruptionChance     => ActivationChance * 10;
        float InitActivationChance()   => RandomMath.RandomBetween(0.01f, MaxActivationChance) * GlobalStats.VolcanicActivity;
        string ActiveVolcanoTexPath    => "Buildings/icon_Active_Volcano_64x64";
        string DormantVolcanoTexPath   => "Buildings/icon_Dormant_Volcano_64x64";
        string EruptingVolcanoTexPath  => "Buildings/icon_Erupting_Volcano_64x64";
        public bool ShouldNotifyPlayer => P.Owner == Player || P.AnyOfOurTroops(Player);

        void CreateLavaPool(PlanetGridSquare tile) // Must get a tile with no Volcano on it
        {
            if (tile.BuildingOnTile && P.Owner == Player)
                P.Universe.Notifications.AddBuildingDestroyedByLava(P, tile.Building);

            int bid = Building.Lava1Id;
            P.DestroyTile(tile);
            switch (RandomMath.RollDie(3))
            {
                case 2: bid = Building.Lava2Id; break;
                case 3: bid = Building.Lava3Id; break;
            }

            Building b = ResourceManager.CreateBuilding(P.Universe, bid);
            tile.PlaceBuilding(b, P);
            P.SetHasDynamicBuildings(true);
        }

        void CreateVolcanoBuilding(int bid)
        {
            Building b = ResourceManager.CreateBuilding(P.Universe, bid);
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
                P.Universe.Notifications.AddVolcanoRelated(P, Localizer.Token(GameText.ADormantVolcanoBecameActivenit), ActiveVolcanoTexPath);
        }

        void TryErupt()
        {
            if (!RandomMath.RollDice(ActiveEruptionChance))
                return;

            RemoveVolcanoBeforeReplacing();
            string message = Localizer.Token(GameText.AnActiveVolcanoErupted);
            Erupting       = true;
            Erupt(out string eruptionSeverityText);
            message = $"{message}\n{eruptionSeverityText}";
            CreateVolcanoBuilding(Building.EruptingVolcanoId);
            if (RandomMath.RollDice(5) && P.BasePopPerTile > 300)
            {
                P.BasePopPerTile *= 0.9f;
                message = $"{message}\n{Localizer.Token(GameText.TheEnvironmentSufferedBothPermanent)}";
            }
            else
            {
                message = $"{message}\n{Localizer.Token(GameText.TheEnvironmentSufferedTemporaryDamage)}";
            }

            if (ShouldNotifyPlayer)
                P.Universe.Notifications.AddVolcanoRelated(P, message, EruptingVolcanoTexPath);
        }

        void TryCalmDown()
        {
            if (!RandomMath.RollDice(1))
                return;

            CreateDormantVolcano();
            string message   = Localizer.Token(GameText.AVolcanoEruptionEndedntheEnvironment);
            ActivationChance = InitActivationChance();
            if (RandomMath.RollDice(ActiveEruptionChance * 2))
            {
                float increaseBy = RandomMath.RollDice(75) ? 0.1f : 0.2f;
                message = $"{message}\n{Localizer.Token(GameText.ANewMineralVainWas)} {increaseBy.String(1)}.";
                P.MineralRichness += increaseBy;
            }

            if (!GlobalStats.DisableVolcanoWarning && ShouldNotifyPlayer)
                P.Universe.Notifications.AddVolcanoRelated(P, message, DormantVolcanoTexPath);
        }

        bool TryDeactivate()
        {
            if (!RandomMath.RollDice(DeactivationChance))
                return false;

            Active   = false;
            Erupting = false;
            CreateDormantVolcano();
            if (!GlobalStats.DisableVolcanoWarning && ShouldNotifyPlayer)
                P.Universe.Notifications.AddVolcanoRelated(P, Localizer.Token(GameText.AnActiveVolcanoBecameDormant), DormantVolcanoTexPath);

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
            if (numLavaPoolsCreated == 0)     text = Localizer.Token(GameText.EruptionIsContainedToThe);
            else if (numLavaPoolsCreated <=3) text = Localizer.Token(GameText.EruptionIsSmallSomeLava);
            else                              text = Localizer.Token(GameText.EruptionIsMassiveManyLava);

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
                if (planet.OwnerIsPlayer && !GlobalStats.DisableVolcanoWarning)
                    planet.Universe.Notifications.AddVolcanoRelated(planet, Localizer.Token(GameText.ALavaPoolHasSolidified), lavaPath);
            }
        }

        void RemoveVolcanoBeforeReplacing()
        {
            P.DestroyBuildingOn(Tile);
            P.DestroyTile(Tile);
        }


        /// <summary>
        /// This will remove the Volcano and the class. Use it when you want to completely get rid of the Volcano
        /// </summary>
        public static void RemoveVolcano(PlanetGridSquare tile, Planet planet)
        {
            bool wasHabitable = tile.Habitable;
            bool wasTerraformable = tile.Terraformable;
            planet.DestroyBuildingOn(tile);
            planet.DestroyTile(tile);
            tile.Volcano = null;
            planet.ResetHasDynamicBuildings();

            if (wasHabitable)
                planet.MakeTileHabitable(tile);
            else
                tile.Terraformable = wasTerraformable;
        }

        public string ActivationChanceText(out Color color)
        {
            color = Color.Green;
            if (Erupting)
                return "";

            string text;
            if (Dormant)
            {
                if      (ActivationChance < 0.1f * MaxActivationChance)  text = Localizer.Token(GameText.VeryLow2);
                else if (ActivationChance < 0.33f * MaxActivationChance) text = Localizer.Token(GameText.Low2);
                else if (ActivationChance < 0.66f * MaxActivationChance) text = Localizer.Token(GameText.Medium2);
                else                                                     text = Localizer.Token(GameText.High2);

                color = Color.Yellow;
                return $"{text} {Localizer.Token(GameText.ActivationChance)}";
            }

            if (Active)
            {
                if      (ActiveEruptionChance < 0.25f) text = Localizer.Token(GameText.Medium2);
                else if (ActiveEruptionChance < 0.5f)  text = Localizer.Token(GameText.High2);
                else if (ActiveEruptionChance < 0.75f) text = Localizer.Token(GameText.VeryHigh2);
                else                                   text = Localizer.Token(GameText.ExtremelyHigh);

                color = Color.Red;
                return $"{text} {Localizer.Token(GameText.EruptionChance)}";
            }

            return "";
        }
    }
}
