using System;
using System.Collections.Generic;
using Ship_Game.Gameplay;

namespace Ship_Game.Ships
{
    public partial class Ship
    {
        // Every ship in the game is checked against Subspace influence nodes
        // This could be Planets, SolarSystem projectors, Subspace projectors/ships
        // This is an optimized lookup system, because these properties are queried every frame
        bool InOwnerInfluence;
        struct ForeignInfluence
        {
            public Empire Foreign;
            public Relationship Relationship; // our relation with this foreign empire
        }
        int InfluenceCount;
        ForeignInfluence[] Influences;

        void ResetProjectorInfluence()
        {
            InOwnerInfluence = false;
            InfluenceCount = 0;
            Influences = null;
        }

        /// Optimized quite heavily to handle the most common case
        public void SetProjectorInfluence(Empire empire, bool isInsideInfluence)
        {
            if (empire == loyalty)
            {
                InOwnerInfluence = isInsideInfluence;
            }
            else if (isInsideInfluence) // set foreign influence (may already exist)
            {
                for (int index = 0; index < InfluenceCount; ++index)
                    if (Influences[index].Foreign == empire) // it's already set?
                        return;

                if (Influences == null)
                    Influences = new ForeignInfluence[4];
                else if (Influences.Length == InfluenceCount)
                    Array.Resize(ref Influences, Influences.Length*2);

                ref ForeignInfluence dst = ref Influences[InfluenceCount++];
                dst.Foreign = empire;
                dst.Relationship = loyalty.GetRelations(empire);
            }
            else // unset
            {
                for (int index = 0; index < InfluenceCount; ++index)
                {
                    if (Influences[index].Foreign == empire)
                    {
                        // RemoveAtSwapLast algorithm
                        int last = --InfluenceCount;
                        Influences[index] = Influences[last];
                        Influences[last]  = default;
                        return;
                    }
                }
            }
        }

        public IEnumerable<Empire> GetProjectorInfluenceEmpires()
        {
            if (InOwnerInfluence)
                yield return loyalty;
            for (int i = 0; i < InfluenceCount; ++i)
                yield return Influences[i].Foreign;
        }

        public bool IsInFriendlyProjectorRange
        {
            get
            {
                if (InOwnerInfluence)
                    return true;

                for (int i = 0; i < InfluenceCount; ++i)
                {
                    Relationship r = Influences[i].Relationship;
                    if (r.Treaty_Alliance || r.Treaty_Trade && IsFreighter)
                        return true;
                }
                return false;
            }
        }

        public bool IsInHostileProjectorRange
        {
            get
            {
                if (InOwnerInfluence)
                    return false;

                for (int i = 0; i < InfluenceCount; ++i)
                    if (Influences[i].Relationship.AtWar)
                        return true;
                return false;
            }
        }
    }
}