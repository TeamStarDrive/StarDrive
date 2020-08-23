using System;
using Ship_Game.AI;
using Ship_Game.Commands.Goals;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using System.Windows.Forms;
using Ship_Game.Fleets;
using Ship_Game.AI.Tasks;

namespace Ship_Game
{
    using static RandomMath;
    using static HelperFunctions;
    public class Remnants
    {
        public const int MaxLevel = 20;
        public readonly Empire Owner;
        public readonly BatchRemovalCollection<Goal> Goals;
        public float StoryTriggerKillsXp { get; private set; }
        public bool Activated { get; private set; }
        public static bool Armageddon;
        public RemnantStory Story { get; private set; }
        public float Production { get; private set; }
        public int Level { get; private set; }
        public Map<RemnantShipType, float> ShipCosts { get; private set; } = new Map<RemnantShipType, float>();

        public Remnants(Empire owner, bool fromSave, BatchRemovalCollection<Goal> goals)
        {
            Owner = owner;
            Goals = goals;

            if (!fromSave)
                Story = InitAndPickStory(goals);

            CalculateShipCosts();
        }

        public void RestoreFromSave(SavedGame.EmpireSaveData sData)
        {
            Activated           = sData.RemnantStoryActivated;
            StoryTriggerKillsXp = sData.RemnantStoryTriggerKillsXp;
            Story               = (RemnantStory)sData.RemnantStoryType;
            Production          = sData.RemnantProduction;
            Level               = sData.RemnantLevel;
        }

        public void IncrementKills(int exp)
        {
            StoryTriggerKillsXp += exp;
            float expTrigger = ShipRole.GetMaxExpValue() * EmpireManager.MajorEmpires.Length;
            if (StoryTriggerKillsXp >= expTrigger && !Activated)
            {
                Empire.Universe.NotificationManager.AddNotify(ResourceManager.EventsDict["RemnantTech1"]);
                Activate();
            }

            // for testing
            if (!Activated)
                Activate();
        }

        void Activate()
        {
            // todo - create relevant goals by story
            Activated = true;
            Level++;
            // create story goal
            Goals.Add(new RemnantStoryBalancers(Owner));
        }

        public bool TryLevelUpByDate(out int newLevel)
        {
            newLevel        = 0;
            int turnsPassed = (int)(Empire.Universe.StarDate * 10);
            if (turnsPassed % 1000 == 0) // todo divider by game difficulty
            {
                Level = (Level + 1).UpperBound(MaxLevel); // todo notify player depending on espionage str
                newLevel = Level;
                return true;
            }

            return false;
        }

        public bool CanDoAnotherEngagement(out int numRaids)
        {
            numRaids = Goals.Count(g => g.IsRaid);
            return numRaids < Level;
        }

        public bool CreatePortal(out Ship portal, out string systemName)
        {
            portal             = null;
            SolarSystem system = null;

            if (!GetRadiatingStars(out SolarSystem[] systems)) // Prefer stars which emit radiation
                if (!GetLoneSystem(out system)) // Try a lone system
                    if (!GetUnownedSystems(out systems)) // Fallback to any unowned system
                        return false; // Could not find a spot

            if (system == null)
                system = systems.RandItem();

            Vector2 pos = system.Position.GenerateRandomPointOnCircle(20000);
            systemName  = system.Name;
            return SpawnShip(RemnantShipType.Portal, pos, out portal);
        }

        public bool CreateShip(Ship portal, out Ship ship)
        {
            ship = null;
            RemnantShipType type = SelectShipForCreation();
            if (!ShipCosts.TryGetValue(type, out float cost) || Production < cost)
                return false;

            if (!SpawnShip(type, portal.Center, out ship))
                return false;

            GenerateProduction(-cost);
            return true;
        }

        void CalculateShipCosts()
        {
            AddShipCost(Owner.data.RemnantFighter, RemnantShipType.Fighter);
            AddShipCost(Owner.data.RemnantCorvette, RemnantShipType.Corvette);
            AddShipCost(Owner.data.RemnantSupportSmall, RemnantShipType.SmallSupport);
            AddShipCost(Owner.data.RemnantAssimilator, RemnantShipType.Assimilator);
            AddShipCost(Owner.data.RemnantCarrier, RemnantShipType.Carrier);
            AddShipCost(Owner.data.RemnantMotherShip, RemnantShipType.Mothership);
            AddShipCost(Owner.data.RemnantExterminator, RemnantShipType.Exterminator);
        }

        void AddShipCost(string shipName, RemnantShipType type)
        {
            Ship ship  = ResourceManager.GetShipTemplate(shipName);
            float cost = ship.BaseCost;
            ShipCosts.Add(type, cost);
        }

        RemnantShipType SelectShipForCreation()
        {
            int roll = RollDie(20) + Level;
            if (roll == 1 + Level) return RemnantShipType.Fighter;
            if (roll < 8)          return RemnantShipType.Fighter;
            if (roll < 12)         return RemnantShipType.SmallSupport;
            if (roll < 17)         return RemnantShipType.Corvette;
            if (roll < 21)         return RemnantShipType.Assimilator;
            if (roll < 25)         return RemnantShipType.Carrier;
            if (roll < 30)         return RemnantShipType.Mothership;

            return RemnantShipType.Exterminator;
        }

        bool SpawnShip(RemnantShipType shipType, Vector2 where, out Ship remnantShip)
        {
            remnantShip = null;
            string shipName;
            switch (shipType)
            {
                default:
                case RemnantShipType.Fighter:      shipName = Owner.data.RemnantFighter;      break;
                case RemnantShipType.Corvette:     shipName = Owner.data.RemnantCorvette;     break;
                case RemnantShipType.SmallSupport: shipName = Owner.data.RemnantSupportSmall; break;
                case RemnantShipType.Assimilator:  shipName = Owner.data.RemnantAssimilator;  break;
                case RemnantShipType.Carrier:      shipName = Owner.data.RemnantCarrier;      break;
                case RemnantShipType.Mothership:   shipName = Owner.data.RemnantMotherShip;   break;
                case RemnantShipType.Exterminator: shipName = Owner.data.RemnantExterminator; break;
                case RemnantShipType.Portal:       shipName = Owner.data.RemnantPortal;       break;
            }

            if (shipName.NotEmpty())
            {
                remnantShip = Ship.CreateShipAtPoint(shipName, Owner, where);
                if (remnantShip == null)
                    Log.Warning($"Could not spawn required Remnant ship named {shipName} for {Owner.Name}, check race xml");
                else
                    remnantShip.IsGuardian = true; // TODO maybe this should only apply to starting remnants
            }
            else
            {
                Log.Warning($"Remnant ship name was empty for {Owner.Name}, check race xml for typos");
            }

            return remnantShip != null;
        }

        public void GenerateProduction(float amount)
        {
            Production += amount;
        }

        public int NumPortals()
        {
            return Owner.GetShips().Count(s => s.Name == Owner.data.RemnantPortal && s.Active);
        }

        public bool GetPortals(out Ship[] portals)
        {
            portals = Owner.GetShips().Filter(s => s.Name == Owner.data.RemnantPortal && s.Active);
            return portals.Length > 0;
        }

        float PlanetQuality(Planet planet)
        {
            float fertilityMod = 1;
            float richnessMod  = 1;
            if (EmpireManager.Player.IsCybernetic)
            {
                fertilityMod = 0.5f;
                richnessMod  = planet.IsBarrenType ? 6f : 3f;
            }

            float quality = planet.BaseFertility * fertilityMod 
                            + planet.MineralRichness * richnessMod 
                            + planet.MaxPopulationBillionFor(EmpireManager.Remnants);

            // Boost the quality score for planets that are very rich
            if (planet.MineralRichness > 1.5f)
                quality += 2;

            if (planet.BaseFertility > 1.5f)
                quality += 2;

            return quality;
        }

        public void GenerateRemnantPresence(Planet p)
        {
            if (p.ParentSystem.isStartingSystem)
                return; // Don't create Remnants on starting systems

            float quality   = PlanetQuality(p);
            int dieModifier = (int)CurrentGame.Difficulty * 5 - 5; // easy -5, brutal +10
            int d100        = RollDie(100) + dieModifier;

            switch (GlobalStats.ExtraRemnantGS) // Added by Gretman, Refactored by FB (including all remnant methods)
            {
                case ExtraRemnantPresence.VeryRare:   VeryRarePresence(quality, d100, p);   break;
                case ExtraRemnantPresence.Rare:       RarePresence(quality, d100, p);       break;
                case ExtraRemnantPresence.Normal:     NormalPresence(quality, d100, p);     break;
                case ExtraRemnantPresence.More:       MorePresence(quality, d100, p);       break;
                case ExtraRemnantPresence.MuchMore:   MuchMorePresence(quality, d100, p);   break;
                case ExtraRemnantPresence.Everywhere: EverywherePresence(quality, d100, p); break;
            }
        }

        void VeryRarePresence(float quality, int d100, Planet p)
        {
            if (quality > 12f && d100 >= 70)
                AddMinorFleet(p);
        }

        void RarePresence(float quality, int d100, Planet p)
        {
            if (quality > 12f && d100 >= 60)
                AddMajorFleet(p); // RedFox, changed the rare remnant to Major
        }

        void NormalPresence(float quality, int d100, Planet p)
        {
            if (quality > 15f)
            {
                if (d100 >= 30) AddMinorFleet(p);
                if (d100 >= 50) AddMajorFleet(p);
                if (d100 >= 70) AddSupportShips(p);
                if (d100 >= 90) AddTorpedoShips(p);
            }
            else if (quality > 10f)
            {
                if (d100 >= 50) AddMinorFleet(p);
                if (d100 >= 60) AddMiniFleet(p);
                if (d100 >= 70) AddSupportShips(p);
                if (d100 >= 85) AddMajorFleet(p);
            }
            else if (quality > 6f)
            {
                if (d100 >= 50) AddMiniFleet(p);
                if (d100 >= 60) AddMinorFleet(p);
                if (d100 >= 70) AddSupportShips(p);
                if (d100 >= 85) AddMinorFleet(p);
            }
        }

        void MorePresence(float quality, int d100, Planet p)
        {
            NormalPresence(quality, RollDie(100), p);
            if (quality >= 15f)
            {
                if (d100 >= 25) AddMinorFleet(p);
                if (d100 >= 45) AddMajorFleet(p);
                if (d100 >= 65) AddSupportShips(p);
                if (d100 >= 95) AddCarriers(p);
            }
            else if (quality >= 10f)
            {
                if (d100 >= 45) AddMinorFleet(p);
                if (d100 >= 65) AddSupportShips(p);
                if (d100 >= 95) AddMajorFleet(p);
            }
            else if (quality >= 8f && d100 >= 50)
                AddMinorFleet(p);
        }

        void MuchMorePresence(float quality, int d100, Planet p)
        {
            MorePresence(quality, RollDie(100), p);
            if (quality >= 15f)
            {
                AddMajorFleet(p);
                if (d100 > 10) AddMinorFleet(p);
                if (d100 > 20) AddSupportShips(p);
                if (d100 > 75) AddCarriers(p);
                if (d100 > 90) AddTorpedoShips(p);
            }
            else if (quality >= 12f)
            {
                if (d100 >= 25) AddMinorFleet(p);
                if (d100 >= 30) AddSupportShips(p);
                if (d100 >= 45) AddMinorFleet(p);
                if (d100 >= 80) AddMiniFleet(p);
            }
            else if (quality >= 10f)
            {
                if (d100 >= 25) AddMinorFleet(p);
                if (d100 >= 50) AddSupportShips(p);
                if (d100 >= 75) AddMajorFleet(p);
            }
            else if (quality >= 8f)
            {
                if (d100 >= 50) AddMinorFleet(p);
                if (d100 >= 75) AddMiniFleet(p);
            }
        }

        void EverywherePresence(float quality, int d100, Planet p)
        {
            MuchMorePresence(quality, RollDie(100), p);
            if (quality >= 18f)
            {
                AddMajorFleet(p);
                AddMinorFleet(p);
                AddSupportShips(p);
                if (d100 >= 50) AddCarriers(p);
                if (d100 >= 70) AddTorpedoShips(p);
                if (d100 >= 90) AddCarriers(p);
            }
            else if (quality >= 15f)
            {
                AddMajorFleet(p);
                if (d100 >= 40) AddSupportShips(p);
                if (d100 >= 60) AddCarriers(p);
                if (d100 >= 80) AddTorpedoShips(p);
                if (d100 >= 95) AddCarriers(p);
            }
            else if (quality >= 12f)
            {
                AddMinorFleet(p);
                if (d100 >= 50) AddSupportShips(p);
                if (d100 >= 90) AddCarriers(p);
            }
            else if (quality >= 10f)
            {
                if (d100 >= 30) AddMinorFleet(p);
                if (d100 >= 50) AddMiniFleet(p);
                if (d100 >= 70) AddSupportShips(p);
            }
            else if (quality >= 8f)
            {
                if (d100 >= 50) AddMiniFleet(p);
                if (d100 >= 90) AddMiniFleet(p);
            }
            if (quality > 6f && d100 > 50)
                AddMiniFleet(p);
        }

        void AddMajorFleet(Planet p)
        {
            AddMinorFleet(p);
            if (RollDice(50))
                AddMinorFleet(p);

            if (RollDice(25))
                AddMinorFleet(p);

            if (RollDice(10))
                AddGuardians(1, RemnantShipType.Assimilator, p);
        }

        void AddMinorFleet(Planet p)
        {
            int numXenoFighters = RollDie(5) + 1;
            int numDrones = RollDie(3);

            AddGuardians(numXenoFighters, RemnantShipType.Fighter, p);
            AddGuardians(numDrones, RemnantShipType.Corvette, p);
        }

        void AddMiniFleet(Planet p)  //Added by Gretman
        {
            int numXenoFighters = RollDie(3);

            AddGuardians(numXenoFighters, RemnantShipType.Fighter, p);
            AddGuardians(1, RemnantShipType.Corvette, p);
        }

        void AddSupportShips(Planet p)  //Added by Gretman
        {
            int numSupportDrones = RollDie(4);
            AddGuardians(numSupportDrones, RemnantShipType.SmallSupport, p);
        }

        void AddCarriers(Planet p)  //Added by Gretman
        {
            AddGuardians(1, RemnantShipType.Carrier, p);
            if (RollDice(20)) // 20% chance for another carrier
                AddGuardians(1, RemnantShipType.Carrier, p);
        }

        void AddTorpedoShips(Planet p)  //Added by Gretman
        {
            AddGuardians(1, RemnantShipType.Cruiser, p);
            if (RollDice(10)) // 10% chance for another torpedo cruiser
                AddGuardians(1, RemnantShipType.Cruiser, p);
        }

        void AddGuardians(int numShips, RemnantShipType type, Planet p)
        {
            for (int i = 0; i < numShips; ++i)
            {
                Vector2 pos = p.Center.GenerateRandomPointInsideCircle(p.ObjectRadius * 2);
                if (SpawnShip(type, pos, out Ship ship))
                    ship.OrderToOrbit(p);
            }
        }

        RemnantStory InitAndPickStory(BatchRemovalCollection<Goal> goals)
        {
            switch (RollDie(1)) // todo 1 is for testing  should be 4
            {
                default:
                case 1: goals.Add(new RemnantAI(Owner)); return RemnantStory.AncientBalancers;
                case 2: goals.Add(new RemnantAI(Owner)); return RemnantStory.AncientExterminators;
                case 3: goals.Add(new RemnantAI(Owner)); return RemnantStory.ColonizeGalaxy;
                case 4: goals.Add(new RemnantAI(Owner)); return RemnantStory.None;
            }
        }

        public enum RemnantStory
        {
            None,
            AncientBalancers,
            AncientExterminators,
            ColonizeGalaxy
        }
    }

    /*
    public void CheckArmageddon()
    {
        if (Armageddon)
        {
            if (!Paused) ArmageddonTimer -= elapsedTime;
            if (ArmageddonTimer < 0.0)
            {
                ArmageddonTimer = 300f;
                ++ArmageddonCounter;
                if (ArmageddonCounter > 5)
                    ArmageddonCounter = 5;
                for (int i = 0; i < ArmageddonCounter; ++i)
                {
                    Ship exterminator = Ship.CreateShipAtPoint("Remnant Exterminator", EmpireManager.Remnants,
                            player.GetWeightedCenter() + new Vector2(RandomMath.RandomBetween(-500000f, 500000f),
                                RandomMath.RandomBetween(-500000f, 500000f)));
                    exterminator.AI.DefaultAIState = AIState.Exterminate;
                }
            }
        }
    }*/

    public enum RemnantShipType
    {
        Fighter,
        Corvette,
        SmallSupport,
        Carrier,
        Assimilator,
        Cruiser,
        Mothership,
        Exterminator,
        Portal
    }
}
