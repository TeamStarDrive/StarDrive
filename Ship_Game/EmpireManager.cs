using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;

namespace Ship_Game
{
    public class EmpireManager
    {
        private static readonly Array<Empire> EmpireList = new Array<Empire>();
        private static readonly Map<string, Empire> EmpireDict = new Map<string, Empire>(); 

        private static Empire PlayerEmpire;
        private static Empire CordrazineEmpire;

        private static Empire RemnantsFaction;
        private static Empire UnknownFaction;
        private static Empire CorsairsFaction;
        private static Empire DummyEmpire;

        public static IReadOnlyList<Empire> Empires => EmpireList;
        public static int NumEmpires => EmpireList.Count;

        /// @todo These should be initialized ONCE during loading, leaving like this for future refactor
        public static Empire Player     => PlayerEmpire     ?? (PlayerEmpire     = FindPlayerEmpire());
        public static Empire Cordrazine => CordrazineEmpire ?? (CordrazineEmpire = GetEmpireByName("Cordrazine Collective"));

        // Special factions
        public static Empire Remnants => RemnantsFaction ?? (RemnantsFaction = GetEmpireByName("The Remnant"));
        public static Empire Unknown  => UnknownFaction  ?? (UnknownFaction  = GetEmpireByName("Unknown"));
        public static Empire Corsairs => CorsairsFaction ?? (CorsairsFaction = GetEmpireByName("Corsairs"));

        // @note This is used as a placeholder empire for entities that have no logical allegiance
        //       withing the known universe. They belong to the mythical `Void` -- pure Chaos of nothingness
        public static Empire Void => DummyEmpire ?? (DummyEmpire = CreateDummyEmpire());

        public static Empire[] AIEmpires =>
            EmpireList.Filter(empire => !empire.isFaction && !empire.data.Defeated && !empire.isPlayer);


        public static Empire FindDuplicateEmpire(Empire empire)
        {
            if (EmpireList.Contains(empire))
                return empire;
            return GetEmpireByName(empire.data.Traits.Name);
        }

        public static void Add(Empire e)
        {
            // avoid duplicate entries, due to some bad design code structuring...
            if (FindDuplicateEmpire(e) != null) return;

            EmpireList.Add(e);
            e.Id = EmpireList.Count;
        }

        public static void Clear()
        {
            EmpireList.Clear();
            EmpireDict.Clear();
            PlayerEmpire     = null;
            CordrazineEmpire = null;
            RemnantsFaction  = null;
            UnknownFaction   = null;
            CorsairsFaction  = null;
        }

        
        public static Empire GetEmpireById(int empireId)
        {
            return empireId == 0 ? null : EmpireList[empireId-1];
        }

        public static Empire GetEmpireByName(string name)
        {
            if (name == null)
                return null;
            if (EmpireDict.TryGetValue(name, out Empire e))
                return e;                        
            foreach (Empire empire in EmpireList)
            {
                if (empire.data.Traits.Name != name) continue;
                EmpireDict.Add(name, empire);
                Log.Info("Added Empire: " + empire.PortraitName);
                return empire;
            }
            return null;
        }

        private static Empire FindPlayerEmpire()
        {
            foreach (Empire empire in EmpireList)
                if (empire.isPlayer)
                    return empire;
            return null;
        }

        public static Array<Empire> GetAllies(Empire e)
        {
            var allies = new Array<Empire>();
            if (e.isFaction)
                return allies;

            foreach (Empire empire in EmpireList)
                if (!empire.isPlayer && e.TryGetRelations(empire, out Relationship r) && r.Known && r.Treaty_Alliance)
                    allies.Add(empire);
            return allies;
        }

        public static Array<Empire> GetTradePartners(Empire e)
        {
            var allies = new Array<Empire>();
            if (e.isFaction)
                return allies;

            foreach (Empire empire in EmpireList)
                if (!empire.isPlayer && e.TryGetRelations(empire, out Relationship r) && r.Known && r.Treaty_Trade)
                    allies.Add(empire);
            return allies;
        }

        private static Empire CreateDummyEmpire()
        {
            var empire = new Empire
            {
                data = new EmpireData(),
                Id = -1
            };
            empire.data.Traits = new RacialTrait {Name = "Void"};
            return empire;
        }

        public static Empire CreateRebelsFromEmpireData(EmpireData data, Empire parent)
        {
            var rebelEmpire = GetEmpireByName(data.RebelName);
            if (rebelEmpire != null) return rebelEmpire;


            var empire = new Empire(parent)
            {
                isFaction = true,
                data = CreatingNewGameScreen.CopyEmpireData(data)
                
            };
            //Added by McShooterz: mod folder support
            DiplomaticTraits diplomaticTraits = ResourceManager.DiplomaticTraits;
            int index1                        = RandomMath.InRange(diplomaticTraits.DiplomaticTraitsList.Count);
            int index2                        = RandomMath.InRange(diplomaticTraits.DiplomaticTraitsList.Count);
            int index3                        = RandomMath.InRange(diplomaticTraits.EconomicTraitsList.Count);
            int index4                        = RandomMath.InRange(diplomaticTraits.EconomicTraitsList.Count);
            empire.data.DiplomaticPersonality = diplomaticTraits.DiplomaticTraitsList[index1];
            empire.data.DiplomaticPersonality = diplomaticTraits.DiplomaticTraitsList[index2];
            empire.data.EconomicPersonality   = diplomaticTraits.EconomicTraitsList[index3];
            empire.data.EconomicPersonality   = diplomaticTraits.EconomicTraitsList[index4];
            empire.data.SpyModifier           = data.Traits.SpyMultiplier;
            empire.PortraitName               = data.PortraitName;
            empire.EmpireColor                = new Color(128, 128, 128, 255);

            empire.InitializeFromSave();
            
            empire.data.IsRebelFaction = true;
            empire.data.Traits.Name = data.RebelName;
            empire.data.Traits.Singular = data.RebelSing;
            empire.data.Traits.Plural = data.RebelPlur;
            empire.isFaction = true;
            Add(empire);
            foreach (Empire key in Empires)
            {
                key.AddRelation(empire);
                empire.AddRelation(key);
            }
            data.RebellionLaunched = true;
         
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
        public static Empire CreateEmpireFromEmpireData(EmpireData data)
        {
            DiplomaticTraits traits = ResourceManager.DiplomaticTraits;
            var empire = new Empire();
            Log.Info($"Creating Empire {data.PortraitName}");
            if (data.Faction == 1)
                empire.isFaction = true;
            do
            {
                int diplomaticTraitIndex = (int)RandomMath.RandomBetween(0.0f, traits.DiplomaticTraitsList.Count);
                data.DiplomaticPersonality = traits.DiplomaticTraitsList[diplomaticTraitIndex];
            }
            while (!CheckPersonality(data));

            do
            {
                int economicTraitIndex = (int)RandomMath.RandomBetween(0.0f, traits.EconomicTraitsList.Count);
                data.EconomicPersonality = traits.EconomicTraitsList[economicTraitIndex];
            }
            while (!CheckEPersonality(data));

            empire.data = data;
            //Added by McShooterz: set values for alternate race file structure
            data.Traits.LoadTraitConstraints();
            empire.dd = ResourceManager.DDDict[data.DiplomacyDialogPath];
            empire.data.SpyModifier = data.Traits.SpyMultiplier;
            empire.data.Traits.Spiritual = data.Traits.Spiritual;
            empire.data.Traits.PassengerModifier += data.Traits.PassengerBonus;
            empire.PortraitName = data.PortraitName;
            empire.data.Traits = data.Traits;
            empire.EmpireColor = new Color((byte)data.Traits.R, (byte)data.Traits.G, (byte)data.Traits.B);
            empire.Initialize();
            return empire;
        }
        private static bool CheckPersonality(EmpireData data)
        {
            foreach (string str in data.ExcludedDTraits)
            {
                if (str == data.DiplomaticPersonality.Name)
                    return false;
            }
            return true;
        }
        private static bool CheckEPersonality(EmpireData data)
        {
            foreach (string str in data.ExcludedETraits)
            {
                if (str == data.EconomicPersonality.Name)
                    return false;
            }
            return true;
        }
    }
}