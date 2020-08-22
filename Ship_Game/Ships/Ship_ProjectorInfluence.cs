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
        float OwnerInfluenceTimer = -100;
        float GetBuffer() => 0.02f;
        struct ForeignInfluence
        {
            public Empire Foreign;
            public Relationship Relationship; // our relation with this foreign empire
            public float Timer;
            public ForeignInfluence(float defaultTimer)
            {
                Foreign = null;
                Relationship = null;
                Timer = defaultTimer;
            }
        }
        int InfluenceCount;
        ForeignInfluence[] Influences;

        void ResetProjectorInfluence()
        {
            InOwnerInfluence    = false;
            InfluenceCount      = 0;
            Influences          = null;
            OwnerInfluenceTimer = -100;
        }

        public void UpdateInfluence(float elapsedTime)
        {
            OwnerInfluenceTimer -= elapsedTime;
            InOwnerInfluence     = OwnerInfluenceTimer + GetBuffer() > 0;
            
            if (InfluenceCount < 1) return;

            for (int i = 0; i < InfluenceCount; i++)
            {
                var influence    = Influences[i];
                influence.Timer -= elapsedTime;
                Influences[i]    = influence;
                if (influence.Foreign != null && influence.Timer + GetBuffer() < 0)
                {
                    int last = --InfluenceCount;
                    Influences[i] = Influences[last];
                    Influences[last] = default;
                }
            }
        }

        /// Optimized quite heavily to handle the most common case
        public void SetProjectorInfluence(Empire empire, bool isInsideInfluence)
        {
            if (empire == loyalty)
            {
                InOwnerInfluence    = isInsideInfluence;
                OwnerInfluenceTimer = loyalty.updateContactsTimer;
            }
            else if (isInsideInfluence) // set foreign influence (may already exist)
            {
                for (int index = 0; index < InfluenceCount; ++index)
                    if (Influences[index].Foreign == empire) // it's already set?
                    {
                        ref ForeignInfluence influence = ref Influences[index];
                        influence.Timer = empire.updateContactsTimer;
                        return;
                    }

                var relation =loyalty.GetRelations(empire);
                if (relation == null) 
                    return;

                if (Influences == null)
                    Influences = new ForeignInfluence[4];
                else if (Influences.Length == InfluenceCount)
                    Array.Resize(ref Influences, Influences.Length*2);

                ref ForeignInfluence dst = ref Influences[InfluenceCount++];
                dst.Foreign              = empire;
                dst.Relationship         = relation;
                dst.Timer                = empire.updateContactsTimer;
            }
        }

        public bool IsInBordersOf(Empire empire)
        {
            if (empire == loyalty) return InOwnerInfluence;

            return Influences?.Any(i=> i.Foreign == empire && i.Timer + GetBuffer() > 0) ?? false;
        }

        public IEnumerable<Empire> GetProjectorInfluenceEmpires()
        {
            if (InOwnerInfluence)
                yield return loyalty;
            for (int i = 0; i < InfluenceCount; ++i)
            {
                var influence =Influences[i];
                if (influence.Timer + GetBuffer() > 0)
                    yield return influence.Foreign;
            }
        }

        public bool IsInFriendlyProjectorRange
        {
            get
            {
                if (InOwnerInfluence)
                    return true;

                for (int i = 0; i < InfluenceCount; ++i)
                {
                    var influence =Influences[i];
                    if (influence.Timer + GetBuffer() > 0 )
                    {
                        Relationship r = influence.Relationship;
                        if (r.Treaty_Alliance || r.Treaty_Trade && IsFreighter)
                            return true;
                    }
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
                {
                    var influence =Influences[i];
                    if (influence.Timer + GetBuffer() > 0 )
                        if (influence.Relationship.AtWar )
                            return true;

                }
                return false;
            }
        }
    }
}