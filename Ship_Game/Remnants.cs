using System;
using Ship_Game.AI;
using Ship_Game.Commands.Goals;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using System.Windows.Forms;

namespace Ship_Game
{
    using static RandomMath;
    public class Remnants
    {
        public readonly Empire Owner;
        public readonly BatchRemovalCollection<Goal> Goals;
        private float StoryTriggerKillsXp;
        private bool StoryActivated;
        public static bool Armageddon;

        public Remnants(Empire owner, bool fromSave, BatchRemovalCollection<Goal> goals)
        {
            Owner = owner;
            Goals = goals;

            if (!fromSave)
                goals.Add(new RemnantAI(Owner));
        }

        public void RestoreFromSave(SavedGame.EmpireSaveData sData)
        {
            StoryActivated      = sData.RemnantStoryActivated;
            StoryTriggerKillsXp = sData.RemnantStoryTriggerKillsXp;
        }

        public void IncrementKills(int exp)
        {
            StoryTriggerKillsXp += exp;
            float expTrigger = ShipRole.GetMaxExpValue();
            if (StoryTriggerKillsXp >= expTrigger && !StoryActivated)
            {
                Empire.Universe.NotificationManager.AddNotify(ResourceManager.EventsDict["RemnantTech1"]);
                StoryActivated = true;
            }
        }

        float QualityForRemnants(Planet planet)
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

            float quality   = QualityForRemnants(p);
            int dieModifier = (int)CurrentGame.Difficulty * 5 - 5; // easy -5, brutal +10
            int d100        = RollDie(100) + dieModifier;

            switch (GlobalStats.ExtraRemnantGS) // Added by Gretman, Refactored by FB (including all remnant methods)
            {
                case ExtraRemnantPresence.VeryRare:   VeryRareRemnantPresence(quality, d100, p);   break;
                case ExtraRemnantPresence.Rare:       RareRemnantPresence(quality, d100, p);       break;
                case ExtraRemnantPresence.Normal:     NormalRemnantPresence(quality, d100, p);     break;
                case ExtraRemnantPresence.More:       MoreRemnantPresence(quality, d100, p);       break;
                case ExtraRemnantPresence.MuchMore:   MuchMoreRemnantPresence(quality, d100, p);   break;
                case ExtraRemnantPresence.Everywhere: EverywhereRemnantPresence(quality, d100, p); break;
            }
        }

        void VeryRareRemnantPresence(float quality, int d100, Planet p)
        {
            if (quality > 12f && d100 >= 70)
                AddMinorRemnantShips(p);
        }

        void RareRemnantPresence(float quality, int d100, Planet p)
        {
            if (quality > 12f && d100 >= 60)
                AddMajorRemnantShips(p); // RedFox, changed the rare remnant to Major
        }

        void NormalRemnantPresence(float quality, int d100, Planet p)
        {
            if (quality > 15f)
            {
                if (d100 >= 30) AddMinorRemnantShips(p);
                if (d100 >= 50) AddMajorRemnantShips(p);
                if (d100 >= 70) AddSupportRemnantShips(p);
                if (d100 >= 90) AddTorpedoRemnantShips(p);
            }
            else if (quality > 10f)
            {
                if (d100 >= 50) AddMinorRemnantShips(p);
                if (d100 >= 60) AddMiniRemnantShips(p);
                if (d100 >= 70) AddSupportRemnantShips(p);
                if (d100 >= 85) AddMajorRemnantShips(p);
            }
            else if (quality > 6f)
            {
                if (d100 >= 50) AddMiniRemnantShips(p);
                if (d100 >= 60) AddMinorRemnantShips(p);
                if (d100 >= 70) AddSupportRemnantShips(p);
                if (d100 >= 85) AddMinorRemnantShips(p);
            }
        }

        void MoreRemnantPresence(float quality, int d100, Planet p)
        {
            NormalRemnantPresence(quality, RollDie(100), p);
            if (quality >= 15f)
            {
                if (d100 >= 25) AddMinorRemnantShips(p);
                if (d100 >= 45) AddMajorRemnantShips(p);
                if (d100 >= 65) AddSupportRemnantShips(p);
                if (d100 >= 95) AddCarrierRemnantShips(p);
            }
            else if (quality >= 10f)
            {
                if (d100 >= 45) AddMinorRemnantShips(p);
                if (d100 >= 65) AddSupportRemnantShips(p);
                if (d100 >= 95) AddMajorRemnantShips(p);
            }
            else if (quality >= 8f && d100 >= 50)
                AddMinorRemnantShips(p);
        }

        void MuchMoreRemnantPresence(float quality, int d100, Planet p)
        {
            MoreRemnantPresence(quality, RollDie(100), p);
            if (quality >= 15f)
            {
                AddMajorRemnantShips(p);
                if (d100 > 10) AddMinorRemnantShips(p);
                if (d100 > 20) AddSupportRemnantShips(p);
                if (d100 > 75) AddCarrierRemnantShips(p);
                if (d100 > 90) AddTorpedoRemnantShips(p);
            }
            else if (quality >= 12f)
            {
                if (d100 >= 25) AddMinorRemnantShips(p);
                if (d100 >= 30) AddSupportRemnantShips(p);
                if (d100 >= 45) AddMinorRemnantShips(p);
                if (d100 >= 80) AddMiniRemnantShips(p);
            }
            else if (quality >= 10f)
            {
                if (d100 >= 25) AddMinorRemnantShips(p);
                if (d100 >= 50) AddSupportRemnantShips(p);
                if (d100 >= 75) AddMajorRemnantShips(p);
            }
            else if (quality >= 8f)
            {
                if (d100 >= 50) AddMinorRemnantShips(p);
                if (d100 >= 75) AddMiniRemnantShips(p);
            }
        }

        void EverywhereRemnantPresence(float quality, int d100, Planet p)
        {
            MuchMoreRemnantPresence(quality, RollDie(100), p);
            if (quality >= 18f)
            {
                AddMajorRemnantShips(p);
                AddMinorRemnantShips(p);
                AddSupportRemnantShips(p);
                if (d100 >= 50) AddCarrierRemnantShips(p);
                if (d100 >= 70) AddTorpedoRemnantShips(p);
                if (d100 >= 90) AddCarrierRemnantShips(p);
            }
            else if (quality >= 15f)
            {
                AddMajorRemnantShips(p);
                if (d100 >= 40) AddSupportRemnantShips(p);
                if (d100 >= 60) AddCarrierRemnantShips(p);
                if (d100 >= 80) AddTorpedoRemnantShips(p);
                if (d100 >= 95) AddCarrierRemnantShips(p);
            }
            else if (quality >= 12f)
            {
                AddMinorRemnantShips(p);
                if (d100 >= 50) AddSupportRemnantShips(p);
                if (d100 >= 90) AddCarrierRemnantShips(p);
            }
            else if (quality >= 10f)
            {
                if (d100 >= 30) AddMinorRemnantShips(p);
                if (d100 >= 50) AddMiniRemnantShips(p);
                if (d100 >= 70) AddSupportRemnantShips(p);
            }
            else if (quality >= 8f)
            {
                if (d100 >= 50) AddMiniRemnantShips(p);
                if (d100 >= 90) AddMiniRemnantShips(p);
            }
            if (quality > 6f && d100 > 50)
                AddMiniRemnantShips(p);
        }

        void AddMajorRemnantShips(Planet p)
        {
            AddMinorRemnantShips(p);
            if (RollDice(50))
                AddMinorRemnantShips(p);

            if (RollDice(25))
                AddMinorRemnantShips(p);

            if (RollDice(10))
                AddRemnantGuardians(1, "Ancient Assimilator", p);
        }

        void AddMinorRemnantShips(Planet p)
        {
            int numXenoFighters = RollDie(5) + 1;
            int numDrones = RollDie(3);

            AddRemnantGuardians(numXenoFighters, "Xeno Fighter", p);
            AddRemnantGuardians(numDrones, "Heavy Drone", p);
        }

        void AddMiniRemnantShips(Planet p)  //Added by Gretman
        {
            int numXenoFighters = RollDie(3);

            AddRemnantGuardians(numXenoFighters, "Xeno Fighter", p);
            AddRemnantGuardians(1, "Heavy Drone", p);
        }

        void AddSupportRemnantShips(Planet p)  //Added by Gretman
        {
            int numSupportDrones = RollDie(4);
            AddRemnantGuardians(numSupportDrones, "Support Drone", p);
        }

        void AddCarrierRemnantShips(Planet p)  //Added by Gretman
        {
            AddRemnantGuardians(1, "Ancient Carrier", p);
            if (RollDice(20)) // 20% chance for another carrier
                AddRemnantGuardians(1, "Ancient Carrier", p);
        }

        void AddTorpedoRemnantShips(Planet p)  //Added by Gretman
        {
            AddRemnantGuardians(1, "Ancient Torpedo Cruiser", p);
            if (RollDice(10)) // 10% chance for another torpedo cruiser
                AddRemnantGuardians(1, "Ancient Torpedo Cruiser", p);
        }

        void AddRemnantGuardians(int numShips, string shipName, Planet p)
        {
            for (int i = 0; i < numShips; ++i)
            {
                Ship guardian = Ship.CreateShipAt(shipName, EmpireManager.Remnants, p, RandomMath.Vector2D(p.ObjectRadius * 2), true);
                guardian.IsGuardian = true;
            }
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
}
