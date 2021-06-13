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
        const float InfluenceTimerBuffer = 0.017f;
        
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

        public void ResetProjectorInfluence()
        {
            InOwnerInfluence = false;
            InfluenceCount   = 0;
            Influences       = null;
            OwnerInfluenceTimer = -100;
        }

        void UpdateInfluence(FixedSimTime timeStep)
        {
            OwnerInfluenceTimer -= timeStep.FixedTime;
            InOwnerInfluence     = OwnerInfluenceTimer + InfluenceTimerBuffer > 0;
            
            if (InfluenceCount < 1) return;

            for (int i = 0; i < InfluenceCount; i++)
            {
                ref ForeignInfluence influence = ref Influences[i];
                influence.Timer -= timeStep.FixedTime;
                if (influence.Foreign != null && influence.Timer + InfluenceTimerBuffer < 0)
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
                OwnerInfluenceTimer = loyalty.MaxContactTimer;
            }
            else if (isInsideInfluence) // set foreign influence (may already exist)
            {
                for (int index = 0; index < InfluenceCount; ++index)
                {
                    ref ForeignInfluence influence = ref Influences[index];
                    if (influence.Foreign == empire) // it's already set?
                    {
                        influence.Timer = empire.MaxContactTimer;
                        return;
                    }
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
                dst.Timer                = empire.MaxContactTimer;
            }
        }

        public bool IsInBordersOf(Empire empire)
        {
            if (empire == loyalty) return InOwnerInfluence;

            return Influences?.Any(i=> i.Foreign == empire && i.Timer + InfluenceTimerBuffer > 0) ?? false;
        }

        public IEnumerable<Empire> GetProjectorInfluenceEmpires()
        {
            if (InOwnerInfluence)
                yield return loyalty;
            for (int i = 0; i < InfluenceCount; ++i)
            {
                if (Influences[i].Timer + InfluenceTimerBuffer > 0)
                    yield return Influences[i].Foreign;
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
                    ref ForeignInfluence influence = ref Influences[i];
                    if (influence.Timer + InfluenceTimerBuffer > 0)
                    {
                        Relationship r = influence.Relationship;
                        if (r?.Treaty_Alliance == true || r?.Treaty_Trade == true && IsFreighter)
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
                    ref ForeignInfluence influence = ref Influences[i];
                    if (influence.Timer + InfluenceTimerBuffer > 0 )
                        if (influence.Relationship?.AtWar == true)
                            return true;

                }
                return false;
            }
        }
    }
}