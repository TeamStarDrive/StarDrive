using System.Collections.Generic;
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
            if (TryGetRelations(them, out Relationship usToThem))
            {
                usToThem.SetTreaty(this, type, value);
                if (them.TryGetRelations(this, out Relationship themToUs))
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
            foreach (var kv in Relationships)
            {
                if (kv.Value.Known || isPlayer)
                {
                    kv.Value.UpdateRelationship(this, kv.Key);
                    if (kv.Value.AtWar && !kv.Key.isFaction) atWarCount++;
                }
            }

            AtWarCount = atWarCount;
        }

        readonly Map<Empire, Relationship> Relationships = new Map<Empire, Relationship>();

        public IReadOnlyDictionary<Empire, Relationship> AllRelations => Relationships;
        
        // TRUE if we know the other empire
        public bool IsKnown(Empire otherEmpire)
        {
            return this == otherEmpire
                || (Relationships.TryGetValue(otherEmpire, out Relationship rel)
                    && rel.Known);
        }

        public bool IsAtWarWith(Empire otherEmpire)
        {
            return Relationships.TryGetValue(otherEmpire, out Relationship rel)
                && rel.AtWar;
        }

        public bool IsAlliedWith(Empire otherEmpire)
        {
            return Relationships.TryGetValue(otherEmpire, out Relationship rel)
                && rel.Treaty_Alliance;
        }

        public bool IsTradeOrOpenBorders(Empire otherEmpire)
        {
            return Relationships.TryGetValue(otherEmpire, out Relationship rel)
                && (rel.Treaty_Trade || rel.Treaty_OpenBorders);
        }

        public bool IsTradeTreaty(Empire otherEmpire)
        {
            return Relationships.TryGetValue(otherEmpire, out Relationship rel)
                && rel.Treaty_Trade;
        }

        public Relationship GetRelations(Empire withEmpire)
        {
            Relationships.TryGetValue(withEmpire, out Relationship rel);
            return rel;
        }

        public void AddRelation(Empire empire)
        {
            if (empire == this) return;
            if (!TryGetRelations(empire, out _))
                Relationships.Add(empire, new Relationship(empire.data.Traits.Name));
        }

        public void SetRelationsAsKnown(Empire empire)
        {
            AddRelation(empire);
            Relationships[empire].Known = true;
            if (!empire.IsKnown(this))
                empire.SetRelationsAsKnown(this);
        }

        public bool TryGetRelations(Empire empire, out Relationship relations)
            => Relationships.TryGetValue(empire, out relations);

        public void AddRelationships(Empire e, Relationship i) => Relationships.Add(e, i);

        public void DamageRelationship(Empire e, string why, float amount, Planet p)
        {
            if (!Relationships.TryGetValue(e, out Relationship relationship))
                return;
            if (why == "Colonized Owned System" || why == "Destroyed Ship")
                relationship.DamageRelationship(this, e, why, amount, p);
        }
    }
}
