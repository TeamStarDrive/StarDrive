using System;
using System.Collections.Generic;
using SDGraphics;
using SDUtils;
using Ship_Game.AI;
using Ship_Game.AI.StrategyAI.WarGoals;
using Ship_Game.Data.Serialization;
using Ship_Game.Gameplay;
using Ship_Game.Utils;

namespace Ship_Game
{
    public partial class Empire
    {
        public PersonalityType Personality => data.DiplomaticPersonality.TraitName;

        public bool IsCunning    => Personality == PersonalityType.Cunning;
        public bool IsXenophobic => Personality == PersonalityType.Xenophobic;
        public bool IsRuthless   => Personality == PersonalityType.Ruthless;
        public bool IsAggressive => Personality == PersonalityType.Aggressive;
        public bool IsHonorable  => Personality == PersonalityType.Honorable;
        public bool IsPacifist   => Personality == PersonalityType.Pacifist;

        [StarData] public bool AlliedWithPlayer { get; private set; } // Used for player UI visability of allied ships

        [StarData] public War[] AllActiveWars { get; private set; } = Array.Empty<War>();
        [StarData] public int ActiveWarPreparations { get; private set; }


        public void SetAlliedWithPlayer(bool value)
        {
            AlliedWithPlayer = value;
        }

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
                Universe.Notifications.AddTreatyBreak(this, type);
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
            foreach (Relationship rel in ActiveRelations)
            {
                if (rel.Known || isPlayer)
                {
                    rel.UpdateRelationship(this, rel.Them);
                    if (takeTurn && !IsFaction)
                    {
                        rel.AdvanceRelationshipTurn(this, rel.Them);
                    }

                    if (rel.AtWar)
                    {
                        if (!rel.Them.IsFaction)
                            atWarCount++;
                        wars.Add(rel.ActiveWar);
                    }
                }
                else if (!rel.Known && rel.Them == Universe.Unknown)
                {
                    Empire.SetRelationsAsKnown(this, rel.Them);
                }
            }
            AllActiveWars = wars.ToArray();
            ActiveWarPreparations = AI.CountGoals(g => g.Type == GoalType.PrepareForWar);
            AtWarCount = atWarCount;
        }

        // The FlatMap is used for fast lookup
        // Active relations are used for iteration
        [StarData] Relationship[] RelationsMap = Empty<Relationship>.Array;
        [StarData] Relationship[] ActiveRelations = Empty<Relationship>.Array;

        public Relationship[] AllRelations => ActiveRelations;

        [StarData] SmallBitSet KnownEmpires;

        /// <returns>Get relations with another empire. NULL if there is no relations</returns> 
        public Relationship GetRelationsOrNull(Empire withEmpire)
        {
            if (withEmpire == null) return null;
            if (withEmpire == this) return null; // disallow relationship with ourselves
            int index = withEmpire.Id - 1;
            return index < RelationsMap.Length ? RelationsMap[index] : null;
        }

        /// <returns>Get relations with another empire. False if there is no relations</returns> 
        public bool GetRelations(Empire withEmpire, out Relationship relations)
        {
            return (relations = GetRelationsOrNull(withEmpire)) != null;
        }

        /// <returns>Our relations with another empire. Throws if relation not found</returns>
        public Relationship GetRelations(Empire withEmpire)
        {
            Relationship relations = GetRelationsOrNull(withEmpire);
            if (relations != null)
                return relations;
            throw new KeyNotFoundException($"No relationship by us:'{Name}' with:'{withEmpire.Name}'");
        }

        void AddNewRelationToThem(Empire them, Relationship rel)
        {
            if (this == them)
                throw new InvalidOperationException($"Empire cannot create Relationship to itself '{them}'");

            int index = them.Id - 1;
            if (index >= RelationsMap.Length)
            {
                int newSize = Math.Max(Universe.NumEmpires, RelationsMap.Length);
                Array.Resize(ref RelationsMap, newSize);
            }

            if (RelationsMap[index] != null)
                throw new InvalidOperationException($"Empire RelationsMap already contains '{them}'");

            RelationsMap[index] = rel;

            Array.Resize(ref ActiveRelations, ActiveRelations.Length + 1);
            ActiveRelations[ActiveRelations.Length - 1] = rel;
        }

        // TRUE if we know the other empire
        public bool IsKnown(Empire otherEmpire)
        {
            return KnownEmpires.IsSet(otherEmpire.Id);
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
                   || this == Universe.Unknown
                   || WeAreRemnants
                   || (otherEmpire?.IsFaction == true && !IsNAPactWith(otherEmpire));
        }

        public bool IsPreparingForWarWith(Empire otherEmpire)
        {
            if (this == otherEmpire)
                return false;

            Relationship rel = GetRelations(otherEmpire);
            return !rel.AtWar && rel.PreparingForWar;
        }

        public bool IsAtWar => AllActiveWars.Length > 0;

        public bool IsAtWarWithMajorEmpire => AllActiveWars.Any(w => !w.Them.IsFaction);

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

        public static void UpdateBilateralRelations(Empire us, Empire them)
        {
            us.GetRelations(them).UpdateRelationship(us, them);
            them.GetRelations(us).UpdateRelationship(them, us);
        }

        // initializes relationship between two empires
        public static void CreateBilateralRelations(Empire us, Empire them)
        {
            if (us == them)
            {
                Log.Error($"CreateBilateralRelations failed (cannot have relations to self): {us}");
                return;
            }

            if (us.GetRelationsOrNull(them) == null)
            {
                us.AddNewRelationToThem(them, rel: new(them));
            }
            if (them.GetRelationsOrNull(us) == null)
            {
                them.AddNewRelationToThem(us, rel: new(us));
            }
        }

        public void SetRelationsAsKnown(Relationship rel, Empire them)
        {
            rel.Known = true;
            KnownEmpires.Set(them.Id);
        }

        public static void SetRelationsAsKnown(Empire us, Empire them)
        {
            if (us == them)
            {
                Log.Error($"SetRelationsAsKnown failed (cannot set self as known): {us}");
                return;
            }

            CreateBilateralRelations(us, them);

            us.SetRelationsAsKnown(us.GetRelations(them), them);

            if (!them.IsKnown(us))
            {
                them.SetRelationsAsKnown(them.GetRelations(us), us);
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
            foreach (Relationship rel in ActiveRelations)
            {
                if (!rel.Them.IsFaction
                    && rel.Known
                    && rel.AtWar
                    && rel.Them.IsAlliedWith(them))
                {
                    allied = true;
                    alliedEmpires.Add(rel.Them);
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
            if (treatySigned || Random.RollDice(spyDefense * 5))
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
                        if (!PersonalityModifiers.CanWeSurrenderToPlayerAfterBetrayal)
                            rel.DoNotSurrenderToThem = true;
                        return;
                }
            }

            // Local Method
            void SignPeaceWithEmpireTheySignedWith()
            {
                AI.AcceptOffer(new Offer { PeaceTreaty = true }, new Offer { PeaceTreaty = true },
                    this, empireTheySignedWith, Offer.Attitude.Respectful);
            }
        }

        public void RespondPlayerStoleColony(Relationship usToPlayer)
        {
            Empire player = Universe.Player;
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
                        case PersonalityType.Pacifist   when usToPlayer.StolenSystems.Count >= 4:
                        case PersonalityType.Cunning:   usToPlayer.PrepareForWar(WarType.DefensiveWar, player);   break;
                        case PersonalityType.Aggressive:
                        case PersonalityType.Ruthless:  usToPlayer.PrepareForWar(WarType.ImperialistWar, player); break;
                        case PersonalityType.Xenophobic:
                        case PersonalityType.Honorable: player.AddToDiplomacyContactView(this, "DECLAREWAR");     break;
                        case PersonalityType.Pacifist:  BreakAllTreatiesWith(player);                             break;
                    }

                    break;
            }
        }

        public void AddToDiplomacyContactView(Empire empire, string dialog)
        {
            if (dialog == "DECLAREWAR" && IsAtWarWith(empire))
                return;
            DiplomacyContactQueue.Add(new DiplomacyQueueItem{ EmpireId = empire.Id, Dialog = dialog});
        }

        public bool TryGetActiveWars(out Array<War> activeWars)
        {
            activeWars = new Array<War>();
            foreach (Relationship rel in AllRelations)
            {
                if (rel.ActiveWar != null && !rel.Them.IsFaction && !rel.Them.IsDefeated)
                    activeWars.Add(rel.ActiveWar);
            }

            return activeWars.Count > 0;
        }

        public bool TryGetActiveWarWithPlayer(out War war)
        {
            war = null;
            if (TryGetActiveWars(out Array<War> activeWars))
                war = activeWars.Find(war => war.Them == Universe.Player);

            return war != null;
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
                    case PersonalityType.Xenophobic when combinedStr > enemy.CurrentMilitaryStrength: break;
                    case PersonalityType.Cunning    when combinedStr > enemy.CurrentMilitaryStrength: break;
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

        public bool WarnedOwnersAboutThisSystem(SolarSystem system)
        {
            foreach (Empire empire in system.OwnerList)
            {
                if (empire != this && !empire.IsFaction && WarnedThemAboutThisSystem(system, empire))
                    return true;
            }

            return false;
        }

        public bool WarnedThemAboutThisSystem(SolarSystem s, Empire them)
        {
            Relationship rel = GetRelations(them);
            return rel?.WarnedSystemsList.Contains(s) == true;
        }

        /// <summary>
        /// Will try to merge into other empires or surrender to the enemy, based on personality
        /// it returns true if the empire was absorbed to another
        /// </summary>
        /// <param name="enemy"></param>
        /// <returns></returns>
        public bool TryMergeOrSurrender(Empire enemy)
        {
            var potentialEmpires = GlobalStats.RestrictAIPlayerInteraction
                ? Universe.ActiveNonPlayerMajorEmpires
                : Universe.ActiveMajorEmpires;

            potentialEmpires = potentialEmpires.Filter(e => e != this && GetRelationsOrNull(e)?.CanMergeWithThem == true);
            if (potentialEmpires.Length == 0)
                return false; // in some cases, all empires will reject the merge

            switch (Personality)
            {
                default:
                case PersonalityType.Aggressive: return TryMergeOrSurrenderAggressive(enemy, potentialEmpires);
                case PersonalityType.Ruthless:   return TryMergeOrSurrenderRuthless(enemy, potentialEmpires);
                case PersonalityType.Xenophobic: return TryMergeOrSurrenderXenophobic(enemy, potentialEmpires);
                case PersonalityType.Honorable:  return TryMergeOrSurrenderHonorable(enemy, potentialEmpires);
                case PersonalityType.Cunning:    return TryMergeOrSurrenderCunning(enemy, potentialEmpires);
                case PersonalityType.Pacifist:   return TryMergeOrSurrenderPacifist(enemy, potentialEmpires);
            }
        }

        // Aggressive AIs will surrender to the enemy if the enemy is aggressive or is the player.
        // If not, they will try to merge with the strongest allied empire or
        // the strongest empire which is at war.
        bool TryMergeOrSurrenderAggressive(Empire enemy, Empire[] potentialEmpires)
        {
            if (enemy.IsAggressive || enemy.isPlayer)
                return MergeWith(enemy, enemy);
            
            var strongest = potentialEmpires.FindMax(e => e.CurrentMilitaryStrength);
            if (strongest.IsAlliedWith(this) || strongest.IsAtWarWithMajorEmpire)
                return MergeWith(strongest, enemy);

            return false;
        }

        // Ruthless AIs will try to merge with the closest ruthless empire or closest allied empire
        bool TryMergeOrSurrenderRuthless(Empire enemy, Empire[] potentialEmpires)
        {
            var closest = potentialEmpires.FindMin(e => e.WeightedCenter.SqDist(WeightedCenter));
            if (closest.IsRuthless || closest.IsAlliedWith(this))
                return MergeWith(closest, enemy);

            return false;
        }

        // Xenophobic AIs will try to merge with the strongest empire, if they are allied with it.
        bool TryMergeOrSurrenderXenophobic(Empire enemy, Empire[] potentialEmpires)
        {
            var strongest = potentialEmpires.FindMax(e => e.CurrentMilitaryStrength);
            if (strongest.IsAlliedWith(this))
                return MergeWith(strongest, enemy); 

            return false;
        }

        // Honorable AIs will try to merge with the closest allied empire or closest 
        // honoable empire, if not at war with it.
        bool TryMergeOrSurrenderHonorable(Empire enemy, Empire[] potentialEmpires)
        {
            var closestAllyOrHonorable = potentialEmpires
                .FindMinFiltered(e => e.IsAlliedWith(this) || e.IsHonorable && !e.IsAtWarWith(this)
                , e => e.WeightedCenter.SqDist(WeightedCenter));

            if (closestAllyOrHonorable != null)
                return MergeWith(closestAllyOrHonorable, enemy); 

            return false;
        }

        // Pacifist AIs will try to merge with the closest empire which is not at war with someone
        // or with closest Pacifist/Player empire.
        bool TryMergeOrSurrenderPacifist(Empire enemy, Empire[] potentialEmpires)
        {
            var closestNotAtWar = potentialEmpires
                .FindMinFiltered(e => !e.IsAtWarWithMajorEmpire, e => e.WeightedCenter.SqDist(WeightedCenter));

            if (closestNotAtWar == null)
            {
                var closestPacifist = potentialEmpires
                    .FindMinFiltered(e => e.IsPacifist || e.isPlayer, e => e.WeightedCenter.SqDist(WeightedCenter));

                if (closestPacifist != null)
                    return MergeWith(closestPacifist, enemy);
            }
            else
            {
                return MergeWith(closestNotAtWar, enemy);
            }

            return false;
        }

        // Cunning AIs will try to merge with the best empire around (including the enemies)
        bool TryMergeOrSurrenderCunning(Empire enemy, Empire[] potentialEmpires)
        {
            var biggest = potentialEmpires.FindMax(e => e.TotalScore);
            if (biggest != null)
                return MergeWith(biggest, enemy);
            return false;
        }

        bool MergeWith(Empire absorber, Empire enemy)
        {
            if (absorber == this)
            {
                Log.Error($"Empire.MergeWith tried merge with self: {Name}");
                return false;
            }

            if (absorber.isPlayer)
            {
                string dialogue = enemy.isPlayer ? "SURRENDER" : "OFFER_MERGE";
                Relationship rel = GetRelationsOrNull(Universe.Player);
                if (rel != null && rel.turnsSinceLastContact > rel.TryPlayerSurrenderTimer)
                    rel.OfferMergeOrSurrenderToPlayer(this, dialogue);
                else
                    return false;
            }
            else
            {
                absorber.AbsorbEmpire(this);
                Universe.Notifications.AddEmpireMergedOrSurrendered(this,
                    GetMergeNotificationMessage(absorber, enemy));
            }

            return true; // return data.defeated // in case the player refused
        }

        string GetMergeNotificationMessage(Empire absorber, Empire enemy)
        {
            if (absorber == enemy) // AI A surrendered to AI B due to losing war with them
                return $"{Name} {Localizer.Token(GameText.HasSurrenderedTo2)} {absorber.Name}";

            if (enemy.isPlayer) // AI A merged with AI B due to a losing war with the player
                return  $"{Name} {Localizer.Token(GameText.HasMergedWith)} {absorber.Name}" +
                      $"\n{Localizer.Token(GameText.DueToLosingWarUS)}";

            // AI A merged with AI B due to a losing war with AI C
            return $"{Name} {Localizer.Token(GameText.HasMergedWith)} {absorber.Name}" +
                      $"\n{Localizer.Token(GameText.DueToLosingWarThem)} {enemy.Name}";
        }
    }
}
