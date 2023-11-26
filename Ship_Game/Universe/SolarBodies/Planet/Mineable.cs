using SDGraphics;
using SDUtils;
using Ship_Game.Data.Serialization;
using Ship_Game.Universe;

namespace Ship_Game
{
    [StarDataType]
    public class Mineable
    {
        public const int MaximumMiningStations = 4;
        [StarData] public readonly Planet P;
        [StarData] public readonly Good ResourceType;
        [StarData] public readonly float Richness; // how much of the resource is exctracted per turn

        [StarData] public Empire Owner { get; private set; }

        public static SubTexture Icon => ResourceManager.Texture($"NewUI/icon_exotic_resource");

        public bool HasOpsOwner => Owner != null;
        public bool OpsOwnedBySomeoneElseThan(Empire empire) => HasOpsOwner && Owner != empire;
        public bool OpsOwnedByEmpire(Empire empire) => Owner == empire;
        public string CargoId => ResourceType.UID;
        public LocalizedText TranslatedResourceName => new(ResourceType.NameIndex);
        public LocalizedText ResourceDescription => new(ResourceType.DescriptionIndex);
        public SubTexture ExoticResourceIcon => ResourceManager.Texture($"Goods/{CargoId}");
        public float RefiningRatio => ResourceType.RefiningRatio; // How much  of the resource is processed per turn
        public ExoticBonusType ExoticBonusType => ResourceType.ExoticBonusType;


        public bool CanAddMiningStationFor(Empire empire) => (Owner == empire || !HasOpsOwner) && MiningOpsSize < MaximumMiningStations;
        public bool MiningOpsExistFor(Empire empire) => Owner == empire && MiningOpsSize > 0;
        int MiningOpsSize => HasOpsOwner ? Owner.AI.CountGoals(g => g.IsMiningOpsGoal(P)) : 0;


        public bool AreMiningOpsPresentBy(Empire empire)
        {
            return P.OrbitalStations.Any(o => o.IsMiningStation && o.Loyalty == empire);
        }

        public bool AreMiningOpsPresent()
        {
            return P.OrbitalStations.Any(o => o.IsMiningStation);
        }

        public void ChangeOwner(Empire empire)
        {
            Owner = empire;
        }


        public Mineable(Planet planet)
        {
            P = planet;
            planet.Universe.AddMineablePlanet(planet);
            ResourceType = GetRandomResourceType(planet.Universe);
            Richness = planet.Universe.Random.RollDie(ResourceType.MaxRichness);

        }

        public Mineable()
        {
        }

        Good GetRandomResourceType(UniverseState universe)
        {
            Array<Good> resources = new();
            foreach (Good good in ResourceManager.TransportableGoods.Filter(g => g.IsGasGiantMineable))
            {
                int weight = good.Weight;
                for (int i = 0; i < weight; i++)
                    resources.Add(good);
            }

            if (resources.Count == 0)
            {
                throw new("Could not find any Gas Mineable in 'Goods.yaml' while Mining Ops are enabled. " +
                          "Check the Vanilla file for referece or set 'MiningChance' to on all Gas Giants in PlanetTypes.yaml");
            }

            return universe.Random.Item(resources);
        }
    }
}
