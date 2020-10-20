using System;
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
            foreach (KeyValuePair<Empire, Relationship> kv in ActiveRelations)
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

        public struct OurRelationsToThem
        {
            public Empire Them;
            public Relationship Rel;
        }

        // The FlatMap is used for fast lookup
        // Active relations are used for iteration
        readonly Array<OurRelationsToThem> RelationsMap = new Array<OurRelationsToThem>();
        readonly Array<KeyValuePair<Empire, Relationship>> ActiveRelations = new Array<KeyValuePair<Empire, Relationship>>();

        public IReadOnlyList<KeyValuePair<Empire, Relationship>> AllRelations => ActiveRelations;
        
        public bool GetRelations(Empire withEmpire, out Relationship relations)
        {
            int index = withEmpire.Id - 1;
            if (index < RelationsMap.Count)
            {
                OurRelationsToThem usToThem = RelationsMap[index];
                if (usToThem.Them != null)
                {
                    relations = usToThem.Rel;
                    return true;
                }
            }
            relations = null;
            return false;
        }

        public Relationship GetRelations(Empire withEmpire)
        {
            int index = withEmpire.Id - 1;
            if (index < RelationsMap.Count)
            {
                OurRelationsToThem usToThem = RelationsMap[index];
                if (usToThem.Them != null)
                    return usToThem.Rel;
            }
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
            
            RelationsMap[index] = new OurRelationsToThem{ Them = them, Rel = rel };
            ActiveRelations.Add(new KeyValuePair<Empire, Relationship>(them, rel));
        }
        
        // TRUE if we know the other empire
        public bool IsKnown(Empire otherEmpire)
        {
            return GetRelations(otherEmpire, out Relationship rel)
                && rel.Known;
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

                    if (ourEmpire != them && them.isPlayer && difficulty > UniverseData.GameDifficulty.Normal) // TODO see if this increased anger bit can be removed
                    {
                        float difficultyRatio = (int) difficulty / 10f;
                        float trustMod = difficultyRatio * (100 - ourEmpire.data.DiplomaticPersonality.Trustworthiness).LowerBound(0);
                        rel.Trust -= trustMod;

                        float territoryMod = difficultyRatio * (100 - ourEmpire.data.DiplomaticPersonality.Territorialism).LowerBound(0);
                        rel.AddAngerTerritorialConflict(territoryMod);
                    }

                    // We set a dummy relationship to ourselves
                    // This follows the NullObject pattern. The relationship with ourselves is friendly :)
                    //if (ourEmpire == them)
                    //{
                    //    rel.Known = true;
                    //    rel.Treaty_NAPact = true;
                    //    rel.ChangeToFriendly();
                    //}

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
