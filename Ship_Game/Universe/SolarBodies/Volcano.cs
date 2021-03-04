using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

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
            ActivationChance = RandomMath.RandomBetween(0f, 1f);
            Tile             = tile;
            P                = planet;
            CreateDormantVolcano();
        }

        public Volcano(PlanetGridSquare tile, Planet planet, float activationChance, bool active, bool erupting)
        {
            ActivationChance = activationChance;
            Active           = active;
            Erupting         = erupting;
            Tile             = tile;
            P                = planet;
        }

        public bool Dormant        => !Active;
        float DeactivationChance   => ActivationChance * 3;
        float ActiveEruptionChance => ActivationChance * 10;
        float CalmDownChance       => ActiveEruptionChance;

        void CreateVolcanoBuilding(int bid)
        {
            Building b = ResourceManager.CreateBuilding(bid);
            Tile.PlaceBuilding(b, P);
            P.SetHasDynamicBuildings(true);
        }

        void CreateDormantVolcano()
        {
            P.DestroyTileWithVolcano(Tile);
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
            else if (Active)
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
            if (RandomMath.RollDice(ActivationChance)) // todo msg player
            {
                P.DestroyTileWithVolcano(Tile);
                Active     = true;
                CreateVolcanoBuilding(Building.ActiveVolcanoId);
            }
        }

        void TryErupt()
        {
            if (RandomMath.RollDice(ActiveEruptionChance)) // todo msg player
            {
                P.DestroyTileWithVolcano(Tile);
                Erupting   = true;
                CreateVolcanoBuilding(Building.EruptingVolcanoId);
                if (RandomMath.RollDice(ActiveEruptionChance))
                    P.AddMaxBaseFertility(-0.1f); // todo msg player
            }
        }

        void TryCalmDown()
        {
            if (RandomMath.RollDice(CalmDownChance)) // todo msg player
            {
                CreateDormantVolcano();
                ActivationChance = RandomMath.RandomBetween(0.1f, 1f);
                if (RandomMath.RollDice(ActiveEruptionChance))
                    P.MineralRichness += 0.1f;
            }
        }

        bool TryDeactivate()
        {
            if (RandomMath.RollDice(DeactivationChance)) // todo msg player
            {
                Active   = false;
                Erupting = false;
                CreateDormantVolcano();
                return true;
            }

            return false;
        }

        public static void UpdateLava(PlanetGridSquare tile, Planet planet)
        {
            if (!RandomMath.RollDice(2))
                return;

            planet.DestroyTileWithVolcano(tile);
            if (RandomMath.RollDice(50))
                planet.MakeTileHabitable(tile); // todo msg player
        }

        public static void RemoveVolcano(PlanetGridSquare tile, Planet planet) // todo After Terraforming
        {
            planet.DestroyTileWithVolcano(tile);
            tile.Volcano = null;
        }

        public string ActivationChanceText(out Color color)
        {
            color = Color.Green;
            if (Erupting)
                return "";

            string text;
            if (Dormant)
            {
                if      (ActivationChance < 0.1f)  text = new LocalizedText(4243).Text;
                else if (ActivationChance < 0.33f) text = new LocalizedText(4244).Text;
                else if (ActivationChance < 0.66f) text = new LocalizedText(4245).Text;
                else                               text = new LocalizedText(4246).Text;

                color = Color.Yellow;
                return $"{text} {new LocalizedText(4239)}";
            }

            if (Active)
            {
                if (ActiveEruptionChance < 1f)        text = new LocalizedText(4245).Text;
                else if (ActiveEruptionChance < 3.3f) text = new LocalizedText(4246).Text;
                else if (ActiveEruptionChance < 6.6f) text = new LocalizedText(4247).Text;
                else                                  text = new LocalizedText(4248).Text;

                color = Color.Red;
                return $"{text} {new LocalizedText(4242)}";
            }

            return "";
        }
    }
}