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
        Planet() : base(0, GameObjectType.Planet)
        {
            TroopManager    = new TroopManager(this);
            GeodeticManager = new GeodeticManager(this);
            Money = new ColonyMoney(this);
        }

        [StarDataDeserialized]
        void OnDeserialized()
        {
            UpdatePositionOnly();
            InitPlanetType(PType, Scale, fromSave: true);
            ResetHasDynamicBuildings();

            UpdateIncomes();  // must be before restoring commodities since max storage is set here
        }
    }
}
