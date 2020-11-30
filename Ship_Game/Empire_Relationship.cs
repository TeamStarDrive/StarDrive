using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.GamerServices;
using Ship_Game.AI;
using Ship_Game.AI.StrategyAI.WarGoals;
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
        public Theater[] AllActiveWarTheaters { get; private set; } = new Theater[0];


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
            SignBilateralTreaty(them, type, false);
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
            BreakTreatyWith(them, TreatyType.NonAggression);
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
            return (1 + territorialism + militaryRatio + opportunism) / 4;
        }

        public float GetExpansionRatio()
        {
            float territorialism = (data.DiplomaticPersonality?.Territorialism ?? 1) / 100f;
            float opportunism    = data.DiplomaticPersonality?.Opportunism ?? 1;
            float expansion      = Research.Strategy.ExpansionRatio;
            float cybernetic     = IsCybernetic ? 1 : 0;

            return (territorialism + expansion + opportunism + cybernetic);
        }

        public void UpdateRelationships()
        {
            int atWarCount = 0;
            var wars = new Array<War>();
            foreach ((Empire them, Relationship rel) in ActiveRelations)
            {
                if (rel.Known || isPlayer)
                {
                    rel.UpdateRelationship(this, them);
                    if (rel.AtWar)
                    {
                        if (!them.isFaction)
                            atWarCount++;
                        wars.Add(rel.ActiveWar);
                    }
                }
            }
            AllActiveWars = wars.ToArray();
            AtWarCount = atWarCount;
            var theaters = new Array<Theater>();
            foreach (var war in AllActiveWars)
            {
                if (war.WarTheaters.ActiveTheaters != null)
                    theaters.AddRange(war.WarTheaters.ActiveTheaters);
            }
            AllActiveWarTheaters = theaters.ToArray();
        }

        public static void UpdateBilateralRelations(Empire us, Empire them)
        {
            us.GetRelations(them).UpdateRelationship(us, them);
            them.GetRelations(us).UpdateRelationship(them, us);
        }

        // The FlatMap is used for fast lookup
        // Active relations are used for iteration
        readonly Array<OurRelationsToThem> RelationsMap = new Array<OurRelationsToThem>();
        readonly Array<OurRelationsToThem> ActiveRelations = new Array<OurRelationsToThem>();

        public IReadOnlyList<OurRelationsToThem> AllRelations => ActiveRelations;
        
        /// <returns>Get relations with another empire. NULL if there is no relations</returns> 
        public Relationship GetRelationsOrNull(Empire withEmpire)
        {

            int index = (withEmpire?.Id ?? int.MaxValue) - 1;
            if (index < RelationsMap.Count)
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
            if (index >= RelationsMap.Count)
            {
                RelationsMap.Resize(Math.Max(EmpireManager.NumEmpires, RelationsMap.Count));
            }

            if (RelationsMap[index].Them != null)
                throw new InvalidOperationException($"Empire RelationsMap already contains '{them}'");
            
            var usToThem = new OurRelationsToThem{ Them = them, Rel = rel };
            RelationsMap[index] = usToThem;
            ActiveRelations.Add(usToThem);
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

        public bool IsAtWarWith(Empire otherEmpire)
        {
            return GetRelationsOrNull(otherEmpire)?.AtWar == true || isFaction && !IsNAPactWith(otherEmpire);
        }

        public bool IsAtWar => AllActiveWars.Length > 0;

        public bool IsAlliedWith(Empire otherEmpire)
        {
            return GetRelationsOrNull(otherEmpire)?.Treaty_Alliance == true;
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
    }
}
