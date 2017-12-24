using System;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Ship_Game;

namespace Ship_Game
{
    public sealed class TechEntry
    {
        [Serialize(0)] public string UID;
        [Serialize(1)] public float Progress;
        [Serialize(2)] public bool Discovered;
        [Serialize(3)] public bool Unlocked;
        [Serialize(4)] public int  Level;
        [Serialize(5)] public string AcquiredFrom = "";
        [Serialize(6)] public bool shipDesignsCanuseThis = true;
        [Serialize(7)] public float maxOffensiveValueFromthis = 0;

        [XmlIgnore][JsonIgnore]
        public float TechCost => Tech.Cost * (float)Math.Max(1, Math.Pow(2.0, Level)) * UniverseScreen.GamePaceStatic;

        //add initializer for tech
        [XmlIgnore][JsonIgnore]
        public Technology Tech => ResourceManager.TechTree[UID];
        [XmlIgnore][JsonIgnore]
        public Array<string> ConqueredSource = new Array<string>();
        public TechnologyType TechnologyType => Tech.TechnologyType;
        public int MaxLevel => Tech.MaxLevel;        

        private bool CheckSource(string unlockType, Empire empire)
        {
            if (unlockType == null)
                return true;
            if (unlockType == "ALL")
                return true;
            if (unlockType == AcquiredFrom)
                return true;
            if (unlockType == empire.data.Traits.ShipType)
                return true;
            if (ConqueredSource.Contains(unlockType))
                return true;
            return false;
        }

        public Array<string> UnLockHulls(Empire empire)
        {
            var techHulls = Tech.HullsUnlocked;
            if (techHulls.IsEmpty) return null;
            var hullList = new Array<string>();                      
            foreach (Technology.UnlockedHull unlockedHull in techHulls)
            {
                if (!CheckSource(unlockedHull.ShipType, empire))
                    continue;
                
                empire.UnlockEmpireHull(unlockedHull.Name, UID);
                hullList.Add(unlockedHull.Name);                
               
            }
            empire.UpdateShipsWeCanBuild(hullList);
            return hullList;
        }

        public Array<Technology.UnlockedMod> GetUnlockableModules(Empire empire)
        {
            var modulesUnlocked = new Array<Technology.UnlockedMod>();
            //Added by McShooterz: Race Specific modules
            foreach (Technology.UnlockedMod unlockedMod in Tech.ModulesUnlocked)
            {
                if (!CheckSource(unlockedMod.Type, empire))
                    continue;
                modulesUnlocked.Add(unlockedMod);
            }
            return modulesUnlocked;
        }

        public void UnlockModules(Empire empire)
        {
            //Added by McShooterz: Race Specific modules
            foreach (Technology.UnlockedMod unlockedMod in GetUnlockableModules(empire))            
                empire.UnlockEmpireShipModule(unlockedMod.ModuleUID, UID);            
        }

        public void UnlockTroops(Empire empire) //  Array<Technology.UnlockedTroop> unlockedTroops, string shipType, string techType = null)
        {
            foreach (Technology.UnlockedTroop unlockedTroop in Tech.TroopsUnlocked)
            {
                if (!CheckSource(unlockedTroop.Type, empire))
                    continue;

                empire.UnlockEmpireTroop(unlockedTroop.Name);

            }
        }
        public void UnlockBuildings(Empire empire)
        {
            foreach (Technology.UnlockedBuilding unlockedBuilding in Tech.BuildingsUnlocked)
            {
                if (!CheckSource(unlockedBuilding.Type, empire))
                    continue;
                empire.UnlockEmpireBuilding(unlockedBuilding.Name);
                
            }
        }
        public bool TriggerAnyEvents(Empire empire)
        {
            bool triggered = false;
            if (empire.isPlayer)
            {
                foreach (Technology.TriggeredEvent triggeredEvent in Tech.EventsTriggered)
                {
                    string type = triggeredEvent.Type;
                    if (CheckSource(type, empire))
                        continue;
                    if (triggeredEvent.CustomMessage != null)
                        Empire.Universe.NotificationManager.AddNotify(triggeredEvent, triggeredEvent.CustomMessage);
                    else
                        Empire.Universe.NotificationManager.AddNotify(triggeredEvent);
                    triggered = true;
                }
            }
            return triggered;
        }
       
        public int CountTechsToOneInList(Array<string> techList, Empire empire)
        {
            int count = 0;
            foreach (Technology.LeadsToTech leadTo in Tech.LeadsTo)
            {
                if (!techList.Contains(leadTo.UID)) continue;
                count++;
                return count + empire.GetTechEntry(leadTo.UID).CountTechsToOneInList(techList, empire);
            }
            return count;

        }

        public void Unlock(Empire empire)
        {
            if (Tech.MaxLevel > 1)
            {
                Level++;
                if (Level == Tech.MaxLevel)
                {
                    Progress = TechCost ;
                    Unlocked = true;
                }
                else
                {
                    Unlocked = false;
                    Progress = 0;
                }
            }
            else
            {
                Progress = Tech.Cost ;
                Unlocked = true;
            }
            RaceRestrictonCheck(empire);
            TriggerAnyEvents(empire);
            UnlockModules(empire);
            UnlockTroops(empire);
            UnLockHulls(empire);
            UnlockBuildings(empire);
            DoRevelaedTechs(empire);
            UnlockBonus(empire);
        }

        public void RaceRestrictonCheck(Empire empire)
        {
            if (Tech.RootNode != 0) return;
            foreach (Technology.LeadsToTech leadsToTech in Tech.LeadsTo)
            {
                //added by McShooterz: Prevent Racial tech from being discovered by unintentional means                    
                empire.SetEmpireTechDiscovered(leadsToTech.UID);
            }
        }
        public void DoRevelaedTechs(Empire empire)
        {
            // Added by The Doctor - reveal specified 'secret' techs with unlocking of techs, via Technology XML
            foreach (Technology.RevealedTech revealedTech in Tech.TechsRevealed)
            {
                if (!CheckSource(revealedTech.Type, empire))
                    continue;
                empire.SetEmpireTechRevealed(revealedTech.RevUID);
                
            }
        }

        
        

        public void UnlockBonus(Empire empire)
        {
            var data = empire.data;
            if (Tech.BonusUnlocked.Count < 1)
                return;
            //update ship stats if a bonus was unlocked
            
            empire.TriggerAllShipStatusUpdate();
            
            foreach (Technology.UnlockedBonus unlockedBonus in Tech.BonusUnlocked)
            {
                //Added by McShooterz: Race Specific bonus
                string type = unlockedBonus.Type;
                if (type != null && type != data.Traits.ShipType && type != AcquiredFrom)
                    continue;
       
                if (unlockedBonus.Tags.Count > 0)
                {
                    foreach (string index in unlockedBonus.Tags)
                    {
                        var tagmod = data.WeaponTags[index];
                        switch (unlockedBonus.BonusType)
                        {
                            case "Weapon_Speed": tagmod.Speed += unlockedBonus.Bonus; continue;
                            case "Weapon_Damage": tagmod.Damage += unlockedBonus.Bonus; continue;
                            case "Weapon_ExplosionRadius": tagmod.ExplosionRadius += unlockedBonus.Bonus; continue;
                            case "Weapon_TurnSpeed": tagmod.Turn += unlockedBonus.Bonus; continue;
                            case "Weapon_Rate": tagmod.Rate += unlockedBonus.Bonus; continue;
                            case "Weapon_Range": tagmod.Range += unlockedBonus.Bonus; continue;
                            case "Weapon_ShieldDamage": tagmod.ShieldDamage += unlockedBonus.Bonus; continue;
                            case "Weapon_ArmorDamage": tagmod.ArmorDamage += unlockedBonus.Bonus; continue;
                            case "Weapon_HP": tagmod.HitPoints += unlockedBonus.Bonus; continue;
                            case "Weapon_ShieldPenetration": tagmod.ShieldPenetration += unlockedBonus.Bonus; continue;
                            case "Weapon_ArmourPenetration": tagmod.ArmourPenetration += unlockedBonus.Bonus; continue;
                            default: continue;
                        }
                    }                    
                }

                switch (unlockedBonus.BonusType ?? unlockedBonus.Name)
                {
                    case "Xeno Compilers":
                    case "Research Bonus": data.Traits.ResearchMod += unlockedBonus.Bonus; break;
                    case "FTL Spool Bonus":
                        if (unlockedBonus.Bonus < 1) data.SpoolTimeModifier *= 1.0f - unlockedBonus.Bonus; // i.e. if there is a 0.2 (20%) bonus unlocked, the spool modifier is 1-0.2 = 0.8* existing spool modifier...
                        else if (unlockedBonus.Bonus >= 1) data.SpoolTimeModifier = 0f; // insta-warp by modifier
                        break;
                    case "Top Guns":
                    case "Bonus Fighter Levels":
                        data.BonusFighterLevels += (int)unlockedBonus.Bonus;
                        empire.IncreaseEmpireShipRoleLevel(ShipData.RoleName.fighter, (int)unlockedBonus.Bonus);                        
                        break;
                    case "Mass Reduction":
                    case "Percent Mass Adjustment": data.MassModifier += unlockedBonus.Bonus; break;
                    case "ArmourMass": data.ArmourMassModifier += unlockedBonus.Bonus; break;
                    case "Resistance is Futile":
                    case "Allow Assimilation": data.Traits.Assimilators = true; break;
                    case "Cryogenic Suspension":
                    case "Passenger Modifier": data.Traits.PassengerModifier += unlockedBonus.Bonus; break;
                    case "ECM Bonus":
                    case "Missile Dodge Change Bonus": data.MissileDodgeChance += unlockedBonus.Bonus; break;
                    case "Set FTL Drain Modifier": data.FTLPowerDrainModifier = unlockedBonus.Bonus; break;
                    case "Super Soldiers":
                    case "Troop Strength Modifier Bonus": data.Traits.GroundCombatModifier += unlockedBonus.Bonus; break;
                    case "Fuel Cell Upgrade":
                    case "Fuel Cell Bonus": data.FuelCellModifier += unlockedBonus.Bonus; break;
                    case "Trade Tariff":
                    case "Bonus Money Per Trade": data.Traits.Mercantile += unlockedBonus.Bonus; break;
                    case "Missile Armor":
                    case "Missile HP Bonus": data.MissileHPModifier += unlockedBonus.Bonus; break;
                    case "Hull Strengthening":
                    case "Module HP Bonus":
                        data.Traits.ModHpModifier += unlockedBonus.Bonus;
                        empire.RecalculateMaxHP = true;       //So existing ships will benefit from changes to ModHpModifier -Gretman
                        break;
                    case "Reaction Drive Upgrade":
                    case "STL Speed Bonus": data.SubLightModifier += unlockedBonus.Bonus; break;
                    case "Reactive Armor":
                    case "Armor Explosion Reduction": data.ExplosiveRadiusReduction += unlockedBonus.Bonus; break;
                    case "Slipstreams":
                    case "In Borders FTL Bonus": data.Traits.InBordersSpeedBonus += unlockedBonus.Bonus; break;
                    case "StarDrive Enhancement":
                    case "FTL Speed Bonus": data.FTLModifier += unlockedBonus.Bonus * data.FTLModifier; break;
                    case "FTL Efficiency":
                    case "FTL Efficiency Bonus": data.FTLPowerDrainModifier -= unlockedBonus.Bonus * data.FTLPowerDrainModifier; break;
                    case "Spy Offense":
                    case "Spy Offense Roll Bonus": data.OffensiveSpyBonus += unlockedBonus.Bonus; break;
                    case "Spy Defense":
                    case "Spy Defense Roll Bonus": data.DefensiveSpyBonus += unlockedBonus.Bonus; break;
                    case "Increased Lifespans":
                    case "Population Growth Bonus": data.Traits.ReproductionMod += unlockedBonus.Bonus; break;
                    case "Set Population Growth Min": data.Traits.PopGrowthMin = unlockedBonus.Bonus; break;
                    case "Set Population Growth Max": data.Traits.PopGrowthMax = unlockedBonus.Bonus; break;
                    case "Xenolinguistic Nuance":
                    case "Diplomacy Bonus": data.Traits.DiplomacyMod += unlockedBonus.Bonus; break;
                    case "Ordnance Effectiveness":
                    case "Ordnance Effectiveness Bonus": data.OrdnanceEffectivenessBonus += unlockedBonus.Bonus; break;
                    case "Tachyons":
                    case "Sensor Range Bonus": data.SensorModifier += unlockedBonus.Bonus; break;
                    case "Privatization": data.Privatization = true; break;
                    // Doctor: Adding an actually configurable amount of civilian maintenance modification; privatisation is hardcoded at 50% but have left it in for back-compatibility.
                    case "Civilian Maintenance": data.CivMaintMod -= unlockedBonus.Bonus; break;
                    case "Armor Piercing":
                    case "Armor Phasing": data.ArmorPiercingBonus += (int)unlockedBonus.Bonus; break;
                    case "Kulrathi Might":
                        data.Traits.ModHpModifier += unlockedBonus.Bonus;
                        empire.RecalculateMaxHP = true; //So existing ships will benefit from changes to ModHpModifier -Gretman
                        break;
                    case "Subspace Inhibition": data.Inhibitors = true; break;
                    // Added by McShooterz: New Bonuses
                    case "Production Bonus": data.Traits.ProductionMod += unlockedBonus.Bonus; break;
                    case "Construction Bonus": data.Traits.ShipCostMod -= unlockedBonus.Bonus; break;
                    case "Consumption Bonus": data.Traits.ConsumptionModifier -= unlockedBonus.Bonus; break;
                    case "Tax Bonus": data.Traits.TaxMod += unlockedBonus.Bonus; break;
                    case "Repair Bonus": data.Traits.RepairMod += unlockedBonus.Bonus; break;
                    case "Maintenance Bonus": data.Traits.MaintMod -= unlockedBonus.Bonus; break;
                    case "Power Flow Bonus": data.PowerFlowMod += unlockedBonus.Bonus; break;
                    case "Shield Power Bonus": data.ShieldPowerMod += unlockedBonus.Bonus; break;
                    case "Ship Experience Bonus": data.ExperienceMod += unlockedBonus.Bonus; break;
                }
            }
        }
    }
}