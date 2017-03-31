using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship_Game.Gameplay
{
    // Ship_Cargo.cs -- All the data related to Cargo
    public sealed partial class Ship
    {
        private readonly Map<string, float> CargoDict          = new Map<string, float>();
        private readonly Map<string, float> MaxGoodStorageDict = new Map<string, float>();
        private readonly Map<string, float> ResourceDrawDict   = new Map<string, float>();

        public float CargoSpaceUsed
        {
            get
            {
                float used = 0f;
                foreach (KeyValuePair<string, float> kv in CargoDict)
                    used += kv.Value;
                return used;
            }
        }
        public void CargoClear()
        {
            foreach (KeyValuePair<string, float> kv in CargoDict)
                CargoDict[kv.Key] = 0;
        }

        public float GetCargo(string key)
        {
            return CargoDict.TryGetValue(key, out float value) ? value : 0f;
        }

        public Map<string, float> GetCargo()
        {
            return CargoDict;
        }

        public void AddCargo(string cargoId, float amount)
        {
            if (CargoDict.TryGetValue(cargoId, out float current))
                amount += current;
            CargoDict[cargoId] = amount;
        }

        public void SetCargo(string cargoId, float amount)
        {
            CargoDict[cargoId] = amount;
        }

        // Unloads the specified amount of cargo (or all, by default) (or none if there's no cargo)
        // If there's less than maxCargoToUnload, all available cargo will be unloaded
        public float UnloadCargo(string cargoId, float maxCargoToUnload = 9999999f)
        {
            if (!CargoDict.TryGetValue(cargoId, out float cargo))
                return 0f;

            if (cargo <= maxCargoToUnload) {
                CargoDict[cargoId] = 0f;
                return cargo;
            }

            CargoDict[cargoId] = cargo - maxCargoToUnload;
            return maxCargoToUnload;
        }
    }
}
