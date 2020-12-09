using System;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI.StrategyAI.WarGoals;
using Ship_Game.Gameplay;

namespace Ship_Game
{
    public class EmpireManager
    {
        public static readonly Array<Empire> Empires = new Array<Empire>();
        public static int NumEmpires { get; private set; }
        static readonly Map<string, Empire> EmpireDict = new Map<string, Empire>();

        static Empire PlayerEmpire;
        static Empire CordrazineEmpire;

        static Empire RemnantsFaction;
        static Empire UnknownFaction;
        static Empire CorsairsFaction;
        static Empire DummyEmpire;

        /// @todo These should be initialized ONCE during loading, leaving like this for future refactor
        public static Empire Player     => PlayerEmpire     ?? (PlayerEmpire     = FindPlayerEmpire());
        public static Empire Cordrazine => CordrazineEmpire ?? (CordrazineEmpire = GetEmpireByName("Cordrazine Collective"));

        // Special factions
        public static Empire Remnants => RemnantsFaction ?? (RemnantsFaction = GetEmpireByName("The Remnant"));
        public static Empire Unknown  => UnknownFaction  ?? (UnknownFaction  = GetEmpireByName("Unknown"));
        public static Empire Corsairs => CorsairsFaction ?? (CorsairsFaction = GetEmpireByName("Corsairs"));

        // @note This is used as a placeholder empire for entities that have no logical allegiance
        //       withing the known universe. They belong to the mythical `Void` -- pure Chaos of nothingness
        public static Empire Void => DummyEmpire ?? (DummyEmpire = CreateVoidEmpire());

        public static Empire[] NonPlayerMajorEmpires =>
            Empires.Filter(empire => !empire.isFaction && !empire.isPlayer);

        public static Empire[] NonPlayerEmpires =>
            Empires.Filter(empire => !empire.isPlayer);

        public static Empire[] ActiveNonPlayerEmpires =>
            Empires.Filter(empire => !empire.isPlayer && !empire.data.Defeated);

        public static Empire[] ActiveNonPlayerMajorEmpires =>
            Empires.Filter(empire => !empire.isFaction && !empire.isPlayer && !empire.data.Defeated);

        public static Empire[] ActiveMajorEmpires => 
            Empires.Filter(empire => !empire.isFaction && !empire.data.Defeated);

        public static Empire[] MajorEmpires   => Empires.Filter(empire => !empire.isFaction);
        public static Empire[] Factions       => Empires.Filter(empire => empire.isFaction);
        public static Empire[] PirateFactions => Empires.Filter(empire => empire.WeArePirates);

        public static Empire FindDuplicateEmpire(Empire empire)
        {
            if (Empires.ContainsRef(empire))
                return empire;
            return GetEmpireByName(empire.data.Traits.Name);
        }

        public static void Add(Empire e)
        {
            // avoid duplicate entries, due to some bad design code structuring...
            if (FindDuplicateEmpire(e) != null)
                return;

            Empires.Add(e);
            e.Id = ++NumEmpires;
        }

        public static void Clear()
        {
            NumEmpires = 0;
            Empires.Clear();
            EmpireDict.Clear();
            PlayerEmpire     = null;
            CordrazineEmpire = null;
            RemnantsFaction  = null;
            UnknownFaction   = null;
            CorsairsFaction  = null;
        }


        public static Empire GetEmpireById(int empireId)
        {
            return empireId == 0 ? null : Empires[empireId-1];
        }

        public static Empire GetEmpireByName(string name)
        {
            if (name.IsEmpty())
                return null;
            if (EmpireDict.TryGetValue(name, out Empire e))
                return e;
            foreach (Empire empire in Empires)
            {
                if (empire.data.Traits.Name == name)
                {
                    EmpireDict.Add(name, empire);
                    return empire;
                }
            }
            return null;
        }

        public static Empire GetEmpireByShipType(string shipType)
        {
            if (shipType.IsEmpty())
                return null;

            foreach (Empire empire in Empires)
            {
                if (empire.data.Traits.ShipType == shipType)
                    return empire;
            }

            return null;
        }

        static Empire FindPlayerEmpire()
        {
            foreach (Empire empire in Empires)
                if (empire.isPlayer)
                    return empire;
            return null;
        }

        public static Array<Empire> GetPlayerAllies() => GetAllies(Player);
        public static Array<Empire> GetAllies(Empire e)
        {
            var allies = new Array<Empire>();
            if (e.isFaction)
                return allies;

            for (int i = 0; i < Empires.Count; i++)
            {
                Empire empire = Empires[i];
                if (empire != e && e.IsAlliedWith(empire))
                    allies.Add(empire);
            }

            return allies;
        }

        public static Array<Empire> GetEnemies(Empire e)
        {
            var enemies = new Array<Empire>();

            for (int i = 0; i < Empires.Count; i++)
            {
                Empire empire = Empires[i];
                if (e.IsEmpireHostile(empire))
                    enemies.Add(empire);
            }
            return enemies;
        }

        public static Array<Empire> GetTradePartners(Empire e)
        {
            var allies = new Array<Empire>();
            if (e.isFaction)
                return allies;

            foreach (Empire empire in Empires)
                if (!empire.isPlayer && e.IsTradeTreaty(empire))
                    allies.Add(empire);
            return allies;
        }

        static Empire CreateVoidEmpire()
        {
            return CreateNewEmpire("Void");
        }

        // Creates a new completely empty empire, with no ID
        public static Empire CreateNewEmpire(string name)
        {
            var empire = new Empire
            {
                data = new EmpireData(),
                Id = -1
            };
            empire.data.Traits = new RacialTrait {Name = name};
            return empire;
        }

        public static Troop CreateRebelTroop(Empire rebelEmpire)
        {
            foreach (string troopType in ResourceManager.TroopTypes)
            {
                if (!rebelEmpire.WeCanBuildTroop(troopType))
                    continue;

                Troop troop = ResourceManager.CreateTroop(troopType, rebelEmpire);
                troop.Description = Localizer.Token(rebelEmpire.data.TroopDescriptionIndex);
                return troop;
            }
            return null;
        }

        public static Empire CreateEmpireFromEmpireData(IEmpireData readOnlyData)
        {
            EmpireData data = readOnlyData.CreateInstance();
            DiplomaticTraits dt = ResourceManager.DiplomaticTraits;
            var empire = new Empire { data = data };

            if      (data.IsFaction) Log.Info($"Creating Faction {data.Traits.Name}");
            else if (data.MinorRace) Log.Info($"Creating MinorRace {data.Traits.Name}");
            else                     Log.Info($"Creating MajorEmpire {data.Traits.Name}");

            if (data.IsFaction)
                empire.isFaction = true;

            DTrait[] dipTraits = dt.DiplomaticTraitsList.Filter(
                dip => !data.ExcludedDTraits.Any(trait => trait == dip.Name));
            data.DiplomaticPersonality = RandomMath.RandItem(dipTraits);

            ETrait[] ecoTraits = dt.EconomicTraitsList.Filter(
                eco => !data.ExcludedETraits.Any(trait => trait == eco.Name));
            data.EconomicPersonality = RandomMath.RandItem(ecoTraits);

            // Added by McShooterz: set values for alternate race file structure
            data.Traits.LoadTraitConstraints();
            empire.dd = ResourceManager.GetDiplomacyDialog(data.DiplomacyDialogPath);
            data.SpyModifier = data.Traits.SpyMultiplier;
            data.Traits = data.Traits;
            data.Traits.Spiritual = data.Traits.Spiritual;
            data.Traits.PassengerModifier += data.Traits.PassengerBonus;
            empire.PortraitName = data.PortraitName;
            empire.EmpireColor = data.Traits.Color;
            empire.Initialize();
            return empire;
        }

        public static Empire CreateRebelsFromEmpireData(IEmpireData readOnlyData, Empire parent)
        {
            EmpireData data = readOnlyData.CreateInstance();
            Empire rebelEmpire = GetEmpireByName(data.RebelName);
            if (rebelEmpire != null) return rebelEmpire;

            var empire = new Empire(parent)
            {
                isFaction = true,
                data = data
            };

            // Added by McShooterz: mod folder support
            DiplomaticTraits dt = ResourceManager.DiplomaticTraits;
            data.DiplomaticPersonality = RandomMath.RandItem(dt.DiplomaticTraitsList);
            data.DiplomaticPersonality = RandomMath.RandItem(dt.DiplomaticTraitsList);
            data.EconomicPersonality   = RandomMath.RandItem(dt.EconomicTraitsList);
            data.EconomicPersonality   = RandomMath.RandItem(dt.EconomicTraitsList);
            data.SpyModifier           = data.Traits.SpyMultiplier;
            empire.PortraitName        = data.PortraitName;
            empire.EmpireColor         = new Color(128, 128, 128, 255);

            empire.InitializeFromSave();

            data.IsRebelFaction  = true;
            data.Traits.Name     = data.RebelName;
            data.Traits.Singular = data.RebelSing;
            data.Traits.Plural   = data.RebelPlur;
            empire.isFaction = true;

            Add(empire);

            foreach (Empire otherEmpire in Empires)
            {
                if (otherEmpire != empire)
                {
                    otherEmpire.AddRelation(empire);
                    empire.AddRelation(otherEmpire);
                    Empire.UpdateBilateralRelations(empire, otherEmpire);
                }
            }

            data.RebellionLaunched = true;
            return empire;
        }

        public static Empire FindRebellion(string rebelName)
        {
            foreach (Empire e in Empires)
            {
                if (e.data.PortraitName == rebelName)
                {
                    Log.Info($"Found Existing Rebel: {e.data.PortraitName}");
                    return e;
                }
            }
            return null;
        }

        public static void RestoreUnserializableDataFromSave()
        {
            if (Empires.IsEmpty)
                Log.Error("must be called after empireList is populated.");
            
            Empire.Universe.WarmUpShipsForLoad();
            foreach (Empire empire in Empires)
            { 
                empire.GetEmpireAI().EmpireDefense = empire.GetEmpireAI().EmpireDefense ?? War.CreateInstance(empire, empire, WarType.EmpireDefense);
                empire.RestoreUnserializableDataFromSave();
                empire.InitEmpireEconomy();
                empire.Pool.UpdatePools();
                empire.GetEmpireAI().WarTasks.RestoreFromSave(empire);
            }
        }
    }
}