using System;
using SDGraphics;
using SDUtils;
using Ship_Game.Data.Serialization;
using Ship_Game.Universe.SolarBodies;

// ReSharper disable once CheckNamespace
namespace Ship_Game
{
    public partial class Planet
    {
        [StarDataConstructor]
        Planet() : base(0, GameObjectType.Planet)
        {
            GeodeticManager = new GeodeticManager(this);
            Money = new ColonyMoney(this);
        }

        [StarDataDeserialized]
        void OnDeserialized()
        {
            // TODO: just for save compatibility, remove later
            Troops ??= new(this);

            UpdatePositionOnly();
            InitPlanetType(PType, Scale, fromSave: true);

            foreach (Building b in BuildingList)
                UpdatePlanetStatsFromPlacedBuilding(b);

            UpdateMaxPopulation();
            UpdateIncomes();
        }
    }
}
