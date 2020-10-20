using System.Collections.Generic;
using Microsoft.Xna.Framework.GamerServices;
using Ship_Game.AI;
using Ship_Game.Gameplay;

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

        void SetTreatyWith(Empire them, TreatyType type, bool value)
        {
            if (GetRelations(them, out Relationship usToThem))
            {
                usToThem.SetTreaty(this, type, value);
                if (them.GetRelations(this, out Relationship themToUs))
                    themToUs.SetTreaty(them, type, value);
            }
        }

        public void SignTreatyWith(Empire them, TreatyType type)
        {
            SetTreatyWith(them, type, true);
        }

        public void BreakTreatyWith(Empire them, TreatyType type)
        {
            SetTreatyWith(them, type, false);
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

        public void UpdateRelationships()
        {
            int atWarCount = 0;
            foreach (KeyValuePair<Empire, Relationship> kv in Relationships)
            {
                Empire them = kv.Key;
                Relationship rel = kv.Value;
                if (rel.Known || isPlayer)
                {
                    rel.UpdateRelationship(this, them);
                    if (rel.AtWar && !them.isFaction)
                        atWarCount++;
                }
            }

            AtWarCount = atWarCount;
        }

        readonly Map<Empire, Relationship> Relationships = new Map<Empire, Relationship>();

        public IReadOnlyDictionary<Empire, Relationship> AllRelations => Relationships;
        
        public bool GetRelations(Empire empire, out Relationship relations)
        {
            return Relationships.TryGetValue(empire, out relations);
        }

        public Relationship GetRelations(Empire withEmpire)
        {
            if (GetRelations(withEmpire, out Relationship rel))
                return rel;
            throw new KeyNotFoundException($"No relationship by us:'{Name}' with:{withEmpire.Name}");
        }

        // TRUE if we know the other empire
        public bool IsKnown(Empire otherEmpire)
        {
            return this == otherEmpire
                || (GetRelations(otherEmpire, out Relationship rel)
                    && rel.Known);
        }

        public bool IsAtWarWith(Empire otherEmpire)
        {
            return GetRelations(otherEmpire, out Relationship rel)
                && rel.AtWar;
        }

        public bool IsAlliedWith(Empire otherEmpire)
        {
            return GetRelations(otherEmpire, out Relationship rel)
                && rel.Treaty_Alliance;
        }

        public bool IsTradeOrOpenBorders(Empire otherEmpire)
        {
            return GetRelations(otherEmpire, out Relationship rel)
                && (rel.Treaty_Trade || rel.Treaty_OpenBorders);
        }

        public bool IsTradeTreaty(Empire otherEmpire)
        {
            return GetRelations(otherEmpire, out Relationship rel)
                && rel.Treaty_Trade;
        }

        public void AddRelation(Empire empire)
        {
            if (empire == this) return;
            if (!GetRelations(empire, out _))
                Relationships.Add(empire, new Relationship(empire.data.Traits.Name));
        }

        void SetRelationship(Empire e, Relationship rel)
        {
            Relationships.Add(e, rel);
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

                    ourEmpire.SetRelationship(them, rel);
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
                    ourEmpire.SetRelationship(empire, relSave);
                }
            }
        }

        public void SetRelationsAsKnown(Empire empire)
        {
            AddRelation(empire);
            Relationships[empire].Known = true;
            if (!empire.IsKnown(this))
                empire.SetRelationsAsKnown(this);
        }

        public void DamageRelationship(Empire e, string why, float amount, Planet p)
        {
            if (GetRelations(e, out Relationship relationship))
            {
                if (why == "Colonized Owned System" || why == "Destroyed Ship")
                    relationship.DamageRelationship(this, e, why, amount, p);
            }
        }
    }
}
