using System;
using System.Collections.Generic;
using System.Windows.Forms;
using SDGraphics;
using SDUtils;
using Ship_Game.Data.Serialization;

namespace Ship_Game.Ships
{
    [StarDataType]
    public struct Cargo
    {
        [StarData] public string CargoId;
        [StarData] public float Amount;
        [StarData] public Goods Good;
        public Cargo(string id, float amount, Goods type = Goods.None) 
        {
            CargoId      = id;
            Amount       = amount;
            Good         = type;
        }
    }

    // Ship_Cargo.cs -- All the data related to Cargo
    public partial class Ship
    {
        [StarData] CargoContainer Cargo;

        public float CargoSpaceMax;
        public float CargoSpaceUsed    => Cargo?.TotalCargo ?? 0;
        public float CargoSpaceFree    => CargoSpaceMax - CargoSpaceUsed;
        public float PassengerModifier => Loyalty.data.Traits.PassengerModifier;

        public float MiningShipCargoSpaceMax => CargoSpaceMax*0.5f;
        public float MaxSupplyForMiningStation => MiningShipCargoSpaceMax - (Loyalty.IsCybernetic ?  GetProduction() : GetFood());
        
        public bool OrdnanceChanged { get; private set; }
        public float OrdnancePercent { get; private set; }

        // WARNING: do not use during constants initialization!
        public float ChangeOrdnance(float amount)
        {
            if (amount.AlmostZero() || (amount > 0f && Ordinance >= OrdinanceMax))
                return amount; // no ordnance was used to refill

            float ordnanceLeft = (amount - (OrdinanceMax - Ordinance)).Clamped(0, amount);
            OrdnanceChanged = true;
            SetOrdnance(Ordinance + amount);
            return ordnanceLeft;
        }

        // @note Should only be used for testing or by ChangeOrdnance
        public void SetOrdnance(float newOrdnance)
        {
            Ordinance = newOrdnance.Clamped(0, OrdinanceMax);
            UpdateOrdnancePercentage();
        }

        // Used for log and debug ordnance percentage bug
        public void TrackOrdnancePercentageBug()
        {
            if (OrdinanceMax > 1 && OrdnancePercent.NotEqual(Ordinance / OrdinanceMax))
            {
                Log.Warning($"{VanityName} Ordnance percentage not equal true ordnance percentage. Updating percentage now.");
                UpdateOrdnancePercentage();
            }
        }

        void UpdateOrdnancePercentage()
        {
            float percent = OrdinanceMax > 1 ? (Ordinance + Carrier.OrdnanceInSpace) / OrdinanceMax : 1f;
            OrdnancePercent = percent.Clamped(0, 1f);
        }

        public float ShipOrdLaunchCost => (Mass / 5f) * GlobalStats.Defaults.HangarCombatShipCostMultiplier;
        public float ShipRetrievalOrd  => ShipOrdLaunchCost * HealthPercent;

        [StarDataType]
        sealed class CargoContainer
        {
            [StarData] public float TotalCargo; // Food + Production + Colonists + OtherCargo
            [StarData] readonly float MaxCargo;
            [StarData] public float Food;
            [StarData] public float Production;
            [StarData] public float Colonists;
            // this can be any other kind of cargo.
            // to save on memory usage, we only initialize this on demand
            [StarData] public Cargo[] Other = Empty<Cargo>.Array;

            [StarDataConstructor]
            public CargoContainer(float maxCargo) { MaxCargo = maxCargo; }

            // this search is deliberately linear. the amount of cargo items is usually 0 or 1-2
            int IndexOf(string cargoId)
            {
                for (int i = 0; i < Other.Length; ++i)
                    if (Other[i].CargoId == cargoId) return i;
                return -1;
            }

            public float GetOther(string cargoId)
            {
                int i = IndexOf(cargoId);
                return i != -1 ? Other[i].Amount : 0f;
            }

            public float LoadOther(string cargoId, float amount, Goods good = Goods.None)
            {
                int i = IndexOf(cargoId);
                if (i == -1) {
                    i = Other.Length;
                    Array.Resize(ref Other, i + 1); // Add new slot
                    Other[i].CargoId = cargoId;
                }
                Other[i].Good = good;
                return LoadCargoRef(ref Other[i].Amount, amount);
            }

            public float UnloadOther(string cargoId, float maxAmount = 9999999f)
            {
                int i = IndexOf(cargoId);
                return i != -1 ? UnloadCargoRef(ref Other[i].Amount, maxAmount) : 0f;
            }

            public float UnloadCargoRef(ref float cargo, float maxAmount)
            {
                float unload = cargo.Clamped(0f, maxAmount);
                cargo      -= unload;
                TotalCargo -= unload;
                return unload;
            }

            public float LoadCargoRef(ref float cargo, float amount)
            {
                float load = amount.Clamped(0f, MaxCargo - TotalCargo);
                cargo      += load;
                TotalCargo += load;
                return load;
            }
        }

        public void ClearCargo()
        {
            Cargo = null; // :)
        }

        public IEnumerable<Cargo> EnumLoadedCargo()
        {
            if (Cargo == null) yield break;
            if (Cargo.Food       > 0f) yield return new Cargo("Food",           Cargo.Food, Goods.Food);
            if (Cargo.Production > 0f) yield return new Cargo("Production",     Cargo.Production, Goods.Production);
            if (Cargo.Colonists  > 0f) yield return new Cargo("Colonists_1000", Cargo.Colonists * PassengerModifier, Goods.Colonists);

            Cargo[] other = Cargo.Other;
            for (int i = 0; i < other.Length; ++i)
                if (other[i].Amount > 0f)
                    yield return other[i];
        }

        public float GetCargo(Goods good)
        {
            if (Cargo == null) return 0f;
            switch (good)
            {
                case Goods.Food:       return GetFood();
                case Goods.Production: return GetProduction();
                case Goods.Colonists:  return GetColonists();
                default:               return 0f;
            }
        }
   
        public Cargo GetCargo()
        {
            foreach(var cargo in EnumLoadedCargo())
            {
                if (cargo.Amount > 0)
                    return cargo;
            }
            return new Cargo("", 0);
        }
        public float GetColonists()  => Cargo?.Colonists * PassengerModifier ?? 0f;
        public float GetProduction() => Cargo?.Production ?? 0f;
        public float GetFood()       => Cargo?.Food       ?? 0f;
        public float GetOtherCargo(string cargoId) => Cargo?.GetOther(cargoId) ?? 0;
        
        // Lazy Init cargo module, only when we actually LoadCargo
        CargoContainer CargoCont => Cargo ?? (Cargo = new CargoContainer(CargoSpaceMax));

        // Tries to load cargo onto the ship
        // Will return the amount of cargo actually loaded onto the ship cargo hold
        public float LoadCargo(string cargoId, float amount)
        {
            CargoContainer cargo = CargoCont;
            if (cargoId == "Food")           return LoadFood(amount);
            if (cargoId == "Production")     return LoadProduction(amount);
            if (cargoId == "Colonists_1000") return LoadColonists(amount);
            return cargo.LoadOther(cargoId, amount);
        }
        public float LoadCargo(Goods good, float amount)
        {
            if (GetCargo().Good != good) ClearCargo();
            var cargoCont = CargoCont;
            return cargoCont.LoadOther(good.ToString(), amount, good);
        }       
        public float LoadColonists(float amount)
        {
            // Colonists get special treatment due to Cryogenic Freezing and Manifest Destiny passenger modifiers
            float mod = PassengerModifier;
            // if mod is 0f, we have a serious bug during savegame loading
            return CargoCont.LoadCargoRef(ref Cargo.Colonists, amount / mod) * mod;
        }
        public float LoadProduction(float amount)
        {
            return CargoCont.LoadCargoRef(ref Cargo.Production, amount);
        }
        public float LoadFood(float amount)
        {
            return CargoCont.LoadCargoRef(ref Cargo.Food, amount);
        } 

        // Unloads the specified amount of cargo (or all, by default) (or none if there's no cargo)
        // If there's less than maxAmount, all available cargo will be unloaded
        public float UnloadCargo(string cargoId, float maxAmount = 9999999f)
        {
            if (Cargo == null) return 0f;
            if (cargoId == "Food")           return UnloadFood(maxAmount);
            if (cargoId == "Production")     return UnloadProduction(maxAmount);
            if (cargoId == "Colonists_1000") return UnloadColonists(maxAmount);
            return Cargo.UnloadOther(cargoId, maxAmount);
        }

        // Colonists get special treatment due to Cryogenic Freezing and Manifest Destiny passenger modifiers
        public float UnloadColonists(float maxAmount = 9999999f)  => Cargo?.UnloadCargoRef(ref Cargo.Colonists, maxAmount) * PassengerModifier ?? 0f;
        public float UnloadProduction(float maxAmount = 9999999f) => Cargo?.UnloadCargoRef(ref Cargo.Production, maxAmount) ?? 0f;
        public float UnloadFood(float maxAmount = 9999999f)       => Cargo?.UnloadCargoRef(ref Cargo.Food, maxAmount)       ?? 0f;

        // FB - for colony ships when a planet is colonized
        public ColonyEquipment StartingEquipment()
        {
            float addFood          = 0;
            float addProd          = 0;
            float addColonists     = 0;
            var specialBuildingIDs = new Array<string>();

            foreach (ShipModule module in ModuleSlotList)
            {
                addFood      += module.NumberOfFood;
                addProd      += module.NumberOfEquipment;
                addColonists += module.NumberOfColonists;
                if (module.DeployBuildingOnColonize.NotEmpty())
                    specialBuildingIDs.Add(module.DeployBuildingOnColonize);
            }

            return new ColonyEquipment(addFood, addProd, addColonists, specialBuildingIDs);
        }

        public void TransferCargoUponRefit(Ship newship)
        {
            if (IsResearchStation)
            {
                newship.LoadProduction(GetProduction());
            }
            else if (IsMiningStation)
            {
                if (Loyalty.NonCybernetic)
                    newship.LoadFood(GetFood());
                else
                    newship.LoadProduction(GetProduction());

                string cargoId = TetheredTo?.Mining?.CargoId ?? "";
                if (cargoId.NotEmpty())
                    newship.LoadCargo(cargoId, GetOtherCargo(cargoId));
            }
        }
    }
}
