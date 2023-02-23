using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Ship_Game.Data.Serialization;

namespace Ship_Game.Universe.SolarBodies
{
    [StarDataType]
    public class Volcano // Created by Fat Bastard, Mar 2021
    {
        [StarData] public bool Active { get; private set; }
        [StarData] public bool Erupting { get; private set; }
        [StarData] public float ActivationChance { get; private set; }
        [StarData] public readonly PlanetGridSquare Tile;
        [StarData] public readonly Planet P;
        const float MaxActivationChance = 0.1f;

        [StarDataConstructor]
        Volcano() {}

        public Volcano(PlanetGridSquare tile, Planet planet)
        {
            Tile = tile;
            P = planet;
            ActivationChance = InitActivationChance();
            CreateDormantVolcano();
        }

        [StarDataDeserialized(typeof(UniverseParams))]
        public void OnDeserialized()
        {
        }

        public Empire Player => P.Universe.Player;
        public bool Dormant => !Active;
        float DeactivationChance => ActivationChance * 5;
        float ActiveEruptionChance => ActivationChance * 10;
        float InitActivationChance() => P.Random.Float(0.01f, MaxActivationChance) * P.Universe.P.VolcanicActivity;
        string ActiveVolcanoTexPath => "Buildings/icon_Active_Volcano_64x64";
        string DormantVolcanoTexPath => "Buildings/icon_Dormant_Volcano_64x64";
        string EruptingVolcanoTexPath => "Buildings/icon_Erupting_Volcano_64x64";
        public bool ShouldNotifyPlayer => P.Owner == Player || P.AnyOfOurTroops(Player);

        void CreateLavaPool(PlanetGridSquare tile) // Must get a tile with no Volcano on it
        {
            if (tile.BuildingOnTile && P.Owner == Player)
                P.Universe.Notifications.AddBuildingDestroyedByLava(P, tile.Building);

            int bid = Building.Lava1Id;
            P.DestroyTile(tile);
            switch (P.Random.RollDie(3))
            {
                case 2: bid = Building.Lava2Id; break;
                case 3: bid = Building.Lava3Id; break;
            }

            Building b = ResourceManager.CreateBuilding(P, bid);
            tile.PlaceBuilding(b, P);
        }

        void CreateVolcanoBuilding(int bid)
        {
            Building b = ResourceManager.CreateBuilding(P, bid);
            Tile.PlaceBuilding(b, P);
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
            if (!P.Random.RollDice(ActivationChance))
                return;

            RemoveVolcanoBeforeReplacing();
            Active     = true;
            CreateVolcanoBuilding(Building.ActiveVolcanoId);
            if (!P.Universe.P.DisableVolcanoWarning && ShouldNotifyPlayer)
                P.Universe.Notifications.AddVolcanoRelated(P, Localizer.Token(GameText.ADormantVolcanoBecameActivenit), ActiveVolcanoTexPath);
        }

        void TryErupt()
        {
            if (!P.Random.RollDice(ActiveEruptionChance))
                return;

            RemoveVolcanoBeforeReplacing();
            string message = Localizer.Token(GameText.AnActiveVolcanoErupted);
            Erupting       = true;
            Erupt(out string eruptionSeverityText);
            message = $"{message}\n{eruptionSeverityText}";
            CreateVolcanoBuilding(Building.EruptingVolcanoId);
            if (P.Random.RollDice(5) && P.BasePopPerTile > 300)
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
            if (!P.Random.RollDice(1))
                return;

            CreateDormantVolcano();
            string message   = Localizer.Token(GameText.AVolcanoEruptionEndedntheEnvironment);
            ActivationChance = InitActivationChance();
            if (P.Random.RollDice(ActiveEruptionChance * 2))
            {
                float increaseBy = P.Random.RollDice(75) ? 0.1f : 0.2f;
                message = $"{message}\n{Localizer.Token(GameText.ANewMineralVainWas)} {increaseBy.String(1)}.";
                P.MineralRichness += increaseBy;
            }

            if (!P.Universe.P.DisableVolcanoWarning && ShouldNotifyPlayer)
                P.Universe.Notifications.AddVolcanoRelated(P, message, DormantVolcanoTexPath);
        }

        bool TryDeactivate()
        {
            if (!P.Random.RollDice(DeactivationChance))
                return false;

            Active   = false;
            Erupting = false;
            CreateDormantVolcano();
            if (!P.Universe.P.DisableVolcanoWarning && ShouldNotifyPlayer)
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

                PlanetGridSquare tile = P.Random.RandItem(potentialLavaTiles);
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
            if      (numLavaPoolsCreated == 0) text = Localizer.Token(GameText.EruptionIsContainedToThe);
            else if (numLavaPoolsCreated <= 3) text = Localizer.Token(GameText.EruptionIsSmallSomeLava);
            else                               text = Localizer.Token(GameText.EruptionIsMassiveManyLava);

            return text;
        }

        Array<PlanetGridSquare> GetPotentialLavaTiles(PlanetGridSquare tile)
        {
            Array<PlanetGridSquare> tiles = new Array<PlanetGridSquare>();
            for (int i = 0; i < P.TilesList.Count; i++)
            {
                PlanetGridSquare t = P.TilesList[i];
                if (!t.ImmuneToLava && t.InRangeOf(tile, 1))
                    tiles.Add(t);
            }

            return tiles;
        }

        int GetNumLavaPools(int maxSeverity)
        {
            int numLavaPools;
            switch (P.Random.RollDie(maxSeverity))
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
            if (!planet.Random.RollDice(2))
                return;

            // Remove the Lava Pool
            string lavaPath = tile.BuildingOnTile ? tile.Building.IconPath64 : "";
            planet.DestroyTile(tile);
            int effects = planet.Random.RollDie(100);
            if (effects > 50)
            {
                int threshold = planet.PType.Category == PlanetCategory.Volcanic ? 75 : 90;
                planet.MakeTileHabitable(tile);
                if (effects > threshold && GetBuildingsCreatedFromLava(out Building[] potentials))
                {
                    // Lava solidifies into a special building 
                    Building b = ResourceManager.CreateBuilding(planet, planet.Random.RandItem(potentials));
                    tile.PlaceBuilding(b, planet);
                    if (planet.ParentSystem.HasPlanetsOwnedBy(planet.Universe.Player) || planet.ParentSystem.ShipList.
                            Any(s => s.Loyalty.isPlayer && s.Position.InRadius(planet.Position, s.SensorRange)))
                    {
                        string message = $"{Localizer.Token(GameText.ALavaPoolHasSolidified)}" +
                                         $"\n{Localizer.Token(GameText.BuildingCreatedFromLava)}" +
                                         $" {b.TranslatedName.Text}";

                        planet.Universe.Notifications.AddVolcanoRelated(planet, message, b.IconPath64);
                    }
                }
                else
                {
                    // Just make the tile habitable
                    if (planet.OwnerIsPlayer && !planet.Universe.P.DisableVolcanoWarning)
                        planet.Universe.Notifications.AddVolcanoRelated(planet, Localizer.Token(GameText.ALavaPoolHasSolidified), lavaPath);
                }
            }
        }

        static bool GetBuildingsCreatedFromLava(out Building[] possibleBuildings)
        {
            possibleBuildings = ResourceManager.BuildingsDict.FilterValues(b => b.CanBeCreatedFromLava);
            return possibleBuildings.Length > 0;
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
