using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game.Universe.SolarBodies
{
    public class Volcano // Created by Fat Bastard, Mar 2021
    {
        public bool Active { get; private set; }
        public bool Erupting { get; private set; }
        public float ActivationChance { get; private set; }

        public Volcano(PlanetGridSquare tile, Planet planet)
        {
            Active           = false;
            Erupting         = false;
            ActivationChance = RandomMath.RandomBetween(0.1f, 1f);
            CreateVolcanoBuilding(tile, planet);
        }

        public bool Dormant        => !Active;
        float DeactivationChance   => ActivationChance * 2;
        float ActiveEruptionChance => ActivationChance * 10;

        void CreateVolcanoBuilding(PlanetGridSquare tile, Planet planet)
        {
            planet.DestroyTile(tile);
            Building b = ResourceManager.CreateBuilding(Building.VolcanoId);
            tile.PlaceBuilding(b, planet);
            planet.HasDynamicBuildings = true;
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
            else
            {
                TryCalmDown();
            }
        }

        void TryActivate()
        {
            if (RandomMath.RollDice(ActivationChance))
                Active = true;
        }

        void TryErupt()
        {
            if (RandomMath.RollDice(ActiveEruptionChance))
                Erupting = true;
        }

        void TryCalmDown()
        {
            if (RandomMath.RollDice(ActiveEruptionChance))
            {
                Erupting = false;
                ActivationChance = RandomMath.RandomBetween(0.1f, 1f);
            }
        }

        bool TryDeactivate()
        {
            if (RandomMath.RollDice(DeactivationChance))
            {
                Active = false;
                return true;
            }

            return false;
        }
    }
}