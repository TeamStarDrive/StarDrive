﻿using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.GamerServices;
using Ship_Game.AI;
using Ship_Game.AI.StrategyAI.WarGoals;
using Ship_Game.Empires.Components;
using Ship_Game.Gameplay;

namespace Ship_Game
{
    public struct OurRelationsToThem
    {
        public Empire Them;
        public Relationship Rel;
        public void Deconstruct(out Empire them, out Relationship rel)
        {
            them = Them;
            rel = Rel;
        }
    }

    public partial class Empire
    {
        public PersonalityType Personality => data.DiplomaticPersonality.TraitName;

        public bool IsCunning    => Personality == PersonalityType.Cunning;
        public bool IsXenophobic => Personality == PersonalityType.Xenophobic;
        public bool IsRuthless   => Personality == PersonalityType.Ruthless;
        public bool IsAggressive => Personality == PersonalityType.Aggressive;
        public bool IsHonorable  => Personality == PersonalityType.Honorable;
        public bool IsPacifist   => Personality == PersonalityType.Pacifist;

        public War[] AllActiveWars { get; private set; } = new War[0];
        public int ActiveWarPreparations { get; private set; } 


        void SignBilateralTreaty(Empire them, TreatyType type, bool value)
        {
            if (GetRelations(them, out Relationship usToThem))
            {
                usToThem.SetTreaty(this, type, value);
                if (them.GetRelations(this, out Relationship themToUs))
                    themToUs.SetTreaty(them, type, value);
            }
        }

        // Sign Bilateral treaty
        public void SignTreatyWith(Empire them, TreatyType type)
        {
            SignBilateralTreaty(them, type, true);
        }
        
        // Break Bilateral treaty
        public void BreakTreatyWith(Empire them, TreatyType type)
        {
            AddTreatyBreakNotification(them, type);
            SignBilateralTreaty(them, type, false);
        }

        void AddTreatyBreakNotification(Empire them, TreatyType type)
        {
            if (!them.isPlayer)
                return;

            bool notify;
            switch (type)
            {
                case TreatyType.Alliance:      notify = IsAlliedWith(them);        break;
                case TreatyType.OpenBorders:   notify = IsOpenBordersTreaty(them); break;
                case TreatyType.Trade:         notify = IsTradeTreaty(them);       break;
                case TreatyType.NonAggression: notify = IsNAPactWith(them);        break;
                default:                       notify = false;                     break;
            }

            if (notify)
                Universe.NotificationManager.AddTreatyBreak(this, type);
        }

        public void BreakAllTreatiesWith(Empire them, bool includingPeace = false)
        {
            BreakTreatyWith(them, TreatyType.Alliance);
            BreakTreatyWith(them, TreatyType.OpenBorders);
            BreakTreatyWith(them, TreatyType.NonAggression);
            BreakTreatyWith(them, TreatyType.Trade);

            if (includingPeace)
                BreakTreatyWith(them, TreatyType.Peace);
        }

        public void BreakAllianceWith(Empire them)
        {
            BreakTreatyWith(them, TreatyType.Alliance);
            BreakTreatyWith(them, TreatyType.OpenBorders);
            BreakTreatyWith(them, TreatyType.NonAggression);
        }

        public void SignAllianceWith(Empire them)
        {
            SignTreatyWith(them, TreatyType.Alliance);
            SignTreatyWith(them, TreatyType.OpenBorders);
            SignTreatyWith(them, TreatyType.NonAggression);
        }

        public void EndPeaceWith(Empire them)
        {
            BreakTreatyWith(them, TreatyType.Peace);
        }

        public void LaunchTroopsAfterPeaceSigned(Empire them)
        {
            var theirPlanets = them.GetPlanets();
            foreach (Planet planet in theirPlanets)
                planet.ForceLaunchInvadingTroops(this);
        }

        public float GetWarOffensiveRatio()
        {
            float territorialism = 1 - (data.DiplomaticPersonality?.Territorialism ?? 100) / 100f;
            float militaryRatio  = Research.Strategy.MilitaryRatio;
            float opportunism    = data.DiplomaticPersonality?.Opportunism ?? 1;
            return (territorialism + militaryRatio + opportunism) / 3;
        }

        public float GetExpansionRatio()
        {
            float territorialism = (data.DiplomaticPersonality?.Territorialism ?? 1) / 100f;
            float opportunism    = data.DiplomaticPersonality?.Opportunism ?? 1;
            float expansion      = Research.Strategy.ExpansionRatio;
            float cybernetic     = IsCybernetic ? 1 : 0;

            return (territorialism + expansion + opportunism + cybernetic);
        }

        public void UpdateRelationships(bool takeTurn)
        {
            int atWarCount = 0;
            var wars = new Array<War>();
            foreach ((Empire them, Relationship rel) in ActiveRelations)
            {
                if (rel.Known || isPlayer)
                {
                    rel.UpdateRelationship(this, them);
                    if (takeTurn && !isFaction)
                    {
                        rel.AdvanceRelationshipTurn(this, them);
                    }

                    if (rel.AtWar)
                    {
                        if (!them.isFaction)
                            atWarCount++;
                        wars.Add(rel.ActiveWar);
                    }
                }
                else if (!rel.Known)
                {
                    rel.CanAttack = true;
                }
            }
            AllActiveWars = wars.ToArray();
            ActiveWarPreparations = EmpireAI.Goals.Count(g => g.type == GoalType.PrepareForWar);
            AtWarCount = atWarCount;
        }

        public static void UpdateBilateralRelations(Empire us, Empire them)
        {
            us.GetRelations(them).UpdateRelationship(us, them);
            them.GetRelations(us).UpdateRelationship(them, us);
        }

        // The FlatMap is used for fast lookup
        // Active relations are used for iteration
        OurRelationsToThem[] RelationsMap = Empty<OurRelationsToThem>.Array;
        OurRelationsToThem[] ActiveRelations = Empty<OurRelationsToThem>.Array;

        public OurRelationsToThem[] AllRelations => ActiveRelations;
        
        /// <returns>Get relations with another empire. NULL if there is no relations</returns> 
        public Relationship GetRelationsOrNull(Empire withEmpire)
        {
            int index = (withEmpire?.Id ?? int.MaxValue) - 1;
            if (index < RelationsMap.Length)
            {
                OurRelationsToThem usToThem = RelationsMap[index];
                if (usToThem.Them != null)
                    return usToThem.Rel;
            }
            return null;
        }

        /// <returns>Get relations with another empire. False if there is no relations</returns> 
        public bool GetRelations(Empire withEmpire, out Relationship relations)
        {
            relations = GetRelationsOrNull(withEmpire);
            return relations != null;
        }

        /// <returns>Our relations with another empire. Throws if relation not found</returns>
        public Relationship GetRelations(Empire withEmpire)
        {
            Relationship relations = GetRelationsOrNull(withEmpire);
            if (relations != null)
                return relations;
            throw new KeyNotFoundException($"No relationship by us:'{Name}' with:{withEmpire.Name}");
        }

        void AddNewRelationToThem(Empire them, Relationship rel)
        {
            int index = them.Id - 1;
            if (index >= RelationsMap.Length)
            {
                int newSize = Math.Max(EmpireManager.NumEmpires, RelationsMap.Length);
                Array.Resize(ref RelationsMap, newSize);
            }

            if (RelationsMap[index].Them != null)
                throw new InvalidOperationException($"Empire RelationsMap already contains '{them}'");
            
            var usToThem = new OurRelationsToThem{ Them = them, Rel = rel };
            RelationsMap[index] = usToThem;

            Array.Resize(ref ActiveRelations, ActiveRelations.Length + 1);
            ActiveRelations[ActiveRelations.Length - 1] = usToThem;
        }
        
        // TRUE if we know the other empire
        public bool IsKnown(Empire otherEmpire)
        {
            return GetRelationsOrNull(otherEmpire)?.Known == true;
        }

        public bool IsNAPactWith(Empire otherEmpire)
        {
            return GetRelationsOrNull(otherEmpire)?.Treaty_NAPact == true;
        }

        /// <summary>
        ///  Always at war with Unknown or Remnants
        /// </summary>
        public bool IsAtWarWith(Empire otherEmpire)
        {
            if (this == otherEmpire)
                return false;

            // rebel factions seems not to enter the relationship data so no relations are being retrieved
            if (otherEmpire?.data.IsRebelFaction == true)
                return true;

            return GetRelationsOrNull(otherEmpire)?.AtWar == true
                   || data.IsRebelFaction
                   || this == EmpireManager.Unknown
                   || WeAreRemnants
                   || (otherEmpire?.isFaction == true && !IsNAPactWith(otherEmpire));
        }

        public bool IsPreparingForWarWith(Empire otherEmpire)
        {
            if (this == otherEmpire)
                return false;

            Relationship rel = GetRelations(otherEmpire);
            return !rel.AtWar && rel.PreparingForWar;
        }

        public bool IsAtWar => AllActiveWars.Length > 0;

        public bool IsAtWarWithMajorEmpire => AllActiveWars.Any(w => !w.Them.isFaction);

        public bool IsAlliedWith(Empire otherEmpire)
        {
            return GetRelationsOrNull(otherEmpire)?.Treaty_Alliance == true;
        }

        public bool IsFriendlyWith(Empire otherEmpire)
        {
            var rel = GetRelationsOrNull(otherEmpire);
            return rel?.Posture == Posture.Friendly || rel?.Treaty_Alliance == true; ;
        }

        public bool IsPeaceTreaty(Empire otherEmpire)
        {
            return GetRelationsOrNull(otherEmpire)?.Treaty_Peace == true;
        }

        public bool IsTradeOrOpenBorders(Empire otherEmpire)
        {
            return GetRelations(otherEmpire, out Relationship rel)
                && (rel.Treaty_Trade || rel.Treaty_OpenBorders);
        }

        public bool IsTradeTreaty(Empire otherEmpire)
        {
            return GetRelationsOrNull(otherEmpire)?.Treaty_Trade == true;
        }

        public bool IsOpenBordersTreaty(Empire otherEmpire)
        {
            return GetRelationsOrNull(otherEmpire)?.Treaty_OpenBorders == true;
        }

        public void AddRelation(Empire empire)
        {
            if (!GetRelations(empire, out _))
                AddNewRelationToThem(empire, new Relationship(empire.data.Traits.Name));
        }

        public void SetRelationsAsKnown(Empire empire)
        {
            AddRelation(empire);
            GetRelations(empire).Known = true;
            if (!empire.IsKnown(this))
                empire.SetRelationsAsKnown(this);
        }

        public static void InitializeRelationships(Array<Empire> empires,
                                                   UniverseData.GameDifficulty difficulty)
        {
            foreach (Empire ourEmpire in empires)
            {
                foreach (Empire them in empires)
                {
                    if (ourEmpire == them)
                        continue;

                    var rel = new Relationship(them.data.Traits.Name);

                    if (them.isPlayer && difficulty > UniverseData.GameDifficulty.Normal) // TODO see if this increased anger bit can be removed
                    {
                        float difficultyRatio = (int) difficulty / 10f;
                        float trustMod = difficultyRatio * (100 - ourEmpire.data.DiplomaticPersonality.Trustworthiness).LowerBound(0);
                        rel.Trust -= trustMod;

                        float territoryMod = difficultyRatio * (100 - ourEmpire.data.DiplomaticPersonality.Territorialism).LowerBound(0);
                        rel.AddAngerTerritorialConflict(territoryMod);
                    }

                    ourEmpire.AddNewRelationToThem(them, rel);
                }
            }
        }

        public static void InitializeRelationships(Array<SavedGame.EmpireSaveData> savedEmpires)
        {
            foreach (SavedGame.EmpireSaveData d in savedEmpires)
            {
                Empire ourEmpire = EmpireManager.GetEmpireByName(d.Name);
                foreach (Relationship relSave in d.Relations)
                {
                    Empire empire = EmpireManager.GetEmpireByName(relSave.Name);
                    relSave.ActiveWar?.SetCombatants(ourEmpire, empire);
                    relSave.Risk = new EmpireRiskAssessment(relSave);
                    relSave.KnownInformation = new EmpireInformation(relSave);
                    relSave.KnownInformation.Update(relSave.IntelligenceLevel);
                    ourEmpire.AddNewRelationToThem(empire, relSave);
                }
            }
        }

        public void DamageRelationship(Empire e, string why, float amount, Planet p)
        {
            if (GetRelations(e, out Relationship rel))
            {
                if (why == "Colonized Owned System" || why == "Destroyed Ship")
                    rel.DamageRelationship(this, e, why, amount, p);
            }
        }

        public float AlliancesValueMultiplierThirdParty(Empire them, out bool decline)
        {
            float multiplier = 1;
            decline          = false;
            Relationship rel = GetRelations(them);
            if (TheyAreAlliedWithOurEnemies(them, out _))
            {
                rel.AddAngerDiplomaticConflict(PersonalityModifiers.AddAngerAlliedWithEnemies3RdParty);
                multiplier = PersonalityModifiers.AllianceValueAlliedWithEnemy;
                if (IsXenophobic)
                    decline = true;
            }

            rel.Trust *= multiplier;
            return multiplier;
        }

        public bool TheyAreAlliedWithOurEnemies(Empire them, out Array<Empire> alliedEmpires)
        {
            alliedEmpires = new Array<Empire>();
            bool allied = false;
            foreach ((Empire other, Relationship rel) in ActiveRelations)
            {
                if (!other.isFaction
                    && rel.Known
                    && rel.AtWar
                    && other.IsAlliedWith(them))
                {
                    allied = true;
                    alliedEmpires.Add(other);
                }
            }

            return allied;
        }

        public void RespondToPlayerThirdPartyTreatiesWithEnemies(Empire them, Empire empireTheySignedWith, bool treatySigned)
        {
            if (!them.isPlayer)
                return; // works only for player

            string dialog    = treatySigned ? "CUTTING_DEALS_WITH_ENEMY" : "TRIED_CUTTING_DEALS_WITH_ENEMY";
            float multiplier = treatySigned ? 1 : 0.5f;
            float spyDefense = GetSpyDefense();
            Relationship rel = GetRelations(them);
            if (treatySigned || RandomMath.RollDice(spyDefense * 5))
            {
                rel.turnsSinceLastContact = 0;
                them.AddToDiplomacyContactView(this, dialog);
                switch (Personality)
                {
                    case PersonalityType.Aggressive:
                        rel.Trust -= 75 * multiplier;
                        rel.AddAngerDiplomaticConflict(25 * multiplier);
                        BreakAllianceWith(them);
                        if (IsAtWarWith(empireTheySignedWith))
                            GetRelations(empireTheySignedWith).RequestPeaceNow(this);

                        break;
                    case PersonalityType.Ruthless:
                        rel.Trust -= 75 * multiplier;
                        rel.AddAngerDiplomaticConflict(30 * multiplier);
                        BreakAllTreatiesWith(them);
                        if (IsAtWarWith(empireTheySignedWith))
                            GetRelations(empireTheySignedWith).RequestPeaceNow(this);

                        break;
                    case PersonalityType.Xenophobic:
                        rel.Trust -= 150 * multiplier;
                        rel.AddAngerDiplomaticConflict(75 * multiplier);
                        BreakAllianceWith(them);
                        rel.PrepareForWar(WarType.ImperialistWar, this);
                        if (IsAtWarWith(empireTheySignedWith))
                            GetRelations(empireTheySignedWith).RequestPeaceNow(this);

                        break;
                    case PersonalityType.Pacifist:
                        rel.AddAngerDiplomaticConflict(5 * multiplier);
                        if (treatySigned && IsAtWarWith(empireTheySignedWith))
                            SignPeaceWithEmpireTheySignedWith();

                        break;
                    case PersonalityType.Cunning:
                        rel.AddAngerDiplomaticConflict(20 * multiplier);
                        rel.Trust -= 50 * multiplier;
                        rel.PrepareForWar(WarType.ImperialistWar, this);
                        if (treatySigned && IsAtWarWith(empireTheySignedWith))
                            SignPeaceWithEmpireTheySignedWith();

                        break;
                    case PersonalityType.Honorable:
                        rel.AddAngerDiplomaticConflict(100 * multiplier);
                        rel.Trust -= 50 * multiplier;
                        if (IsAtWarWith(empireTheySignedWith))
                            SignPeaceWithEmpireTheySignedWith();

                        them.AddToDiplomacyContactView(this, "DECLAREWAR");
                        return;
                }
            }

            // Local Method
            void SignPeaceWithEmpireTheySignedWith()
            {
                EmpireAI.AcceptOffer(new Offer { PeaceTreaty = true }, new Offer { PeaceTreaty = true },
                    this, empireTheySignedWith, Offer.Attitude.Respectful);
            }
        }

        public void RespondPlayerStoleColony(Relationship usToPlayer)
        {
            usToPlayer.Trust -= DifficultyModifiers.TrustLostStoleColony;
            Empire player     = EmpireManager.Player;
            switch (usToPlayer.StolenSystems.Count)
            {
                case 0:
                    Log.Warning("RespondPlayerStoleColony called with 0 stolen systems.");
                    return;
                case 1:
                    switch (Personality)
                    {
                        case PersonalityType.Xenophobic: BreakAllTreatiesWith(player);                 break;
                        case PersonalityType.Honorable:  BreakTreatyWith(player, TreatyType.Alliance); break;
                    }

                    break;
                case 2:
                    switch (Personality)
                    {
                        case PersonalityType.Aggressive:
                        case PersonalityType.Honorable:
                        case PersonalityType.Cunning:
                            BreakTreatyWith(player, TreatyType.Alliance); 
                            BreakTreatyWith(player, TreatyType.OpenBorders); 
                            break;
                        case PersonalityType.Ruthless:
                            BreakTreatyWith(player, TreatyType.Alliance);
                            break;
                        case PersonalityType.Xenophobic:
                            player.AddToDiplomacyContactView(this, "DECLAREWAR");
                            break;
                    }

                    break;
                default: // 3 and above
                    switch (Personality)
                    {
                        case PersonalityType.Aggressive: 
                        case PersonalityType.Ruthless:
                        case PersonalityType.Xenophobic:
                        case PersonalityType.Honorable:
                        case PersonalityType.Cunning:
                        case PersonalityType.Pacifist when usToPlayer.StolenSystems.Count >= 4:
                            player.AddToDiplomacyContactView(this, "DECLAREWAR");
                            break;
                        case PersonalityType.Pacifist: 
                            BreakAllTreatiesWith(player);
                            break;
                    }

                    break;
            }
        }

        void AddToDiplomacyContactView(Empire empire, string dialog)
        {
            DiplomacyContactQueue.Add(new KeyValuePair<int, string>(empire.Id, dialog));
        }

        public float ColonizationDetectionChance(Relationship usToThem, Empire them)
        {
            int minChance = 0;
            if (usToThem.Treaty_NAPact)      minChance = 1;
            if (usToThem.Treaty_Trade)       minChance = 2;
            if (usToThem.Treaty_OpenBorders) minChance = 4;

            // Note - Allied parties will always detect colonization efforts since they share scan info.
            return (GetSpyDefense() - them.GetSpyDefense()).LowerBound(minChance);
        }

        public bool TryGetActiveWars(out Array<War> activeWars)
        {
            activeWars = new Array<War>();
            foreach ((Empire them, Relationship rel) in AllRelations)
            {
                if (rel.ActiveWar != null && !them.isFaction && !them.data.Defeated)
                    activeWars.Add(rel.ActiveWar);
            }

            return activeWars.Count > 0;
        }

        /// <summary>
        /// This will Get a grade from 1 to 10 indicating if our wars in bad state or good
        /// 10 is very good, 1 is bad.
        /// If there are no wars, it returns 5f
        /// </summary>
        public float GetAverageWarGrade()
        {
            if (!TryGetActiveWars(out Array<War> activeWars))
                return 5;

            return activeWars.Sum(w => w.GetGrade()) / activeWars.Count;
        }

        public bool ProcessAllyCallToWar(Empire ally, Empire enemy, out string dialog)
        {
            dialog = "JoinWar_Reject_TooDangerous";
            if (GetAverageWarGrade().Less(5) && !IsHonorable)
                return false; // We have our hands in other wars and it is not looking good for us

            float combinedStr = OffensiveStrength + ally.OffensiveStrength;
            if (IsAlliedWith(enemy))
            {
                switch (Personality)
                {
                    case PersonalityType.Pacifist:
                    case PersonalityType.Honorable:
                        dialog = "JoinWar_Allied_DECLINE";
                        return false;
                    case PersonalityType.Aggressive when OffensiveStrength > enemy.CurrentMilitaryStrength: 
                    case PersonalityType.Ruthless   when combinedStr > enemy.CurrentMilitaryStrength:
                        dialog = "JoinWar_Allied_OK";
                        return true;
                    case PersonalityType.Xenophobic when combinedStr > enemy.CurrentMilitaryStrength:       break;
                    case PersonalityType.Cunning    when combinedStr > enemy.CurrentMilitaryStrength:       break;
                }

            }

            float enemyStr = IsAlliedWith(enemy) ? enemy.CurrentMilitaryStrength : KnownEmpireStrength(enemy);
            if (enemyStr.AlmostZero())
                return false;

            float ratio = combinedStr / enemyStr;
            if (ratio > PersonalityModifiers.AllyCallToWarRatio)
            {
                dialog = "JoinWar_Allied_OK";
                return true;
            }

            dialog = "JoinWar_Reject_TooDangerous";
            return false;
        }

        public bool IsLosingInWarWith(Empire enemy)
        {
            Relationship relations = GetRelations(enemy);
            if (relations.AtWar)
            {
                WarState state = relations.ActiveWar.GetWarScoreState();
                return state == WarState.LosingBadly || state == WarState.LosingSlightly;
            }

            return false;
        }
    }
}
