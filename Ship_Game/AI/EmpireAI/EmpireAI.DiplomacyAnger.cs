using System.Collections.Generic;
using Ship_Game.Gameplay;
using Ship_Game.GameScreens.DiplomacyScreen;

namespace Ship_Game.AI {

    public sealed partial class EmpireAI
    {
        private readonly int FirstDemand  = 20;
        private readonly int SecondDemand = 75;

        void ChangeToFriendlyIfPossible(Relationship usToThem, Empire them)
        {
            switch (OwnerEmpire.Personality)
            {
                case PersonalityType.Pacifist:
                    if (usToThem.TurnsKnown > FirstDemand && usToThem.Treaty_NAPact
                        || usToThem.Trust > 50f && usToThem.TotalAnger < 10)
                    {
                        usToThem.ChangeToFriendly();
                    }

                    break;
            }
        }

        void ChangeToNeutralIfPossible(Relationship usToThem, Empire them)
        {
            if (usToThem.AtWar)
                return;

            switch (OwnerEmpire.Personality)
            {
                case PersonalityType.Pacifist:
                    if (!usToThem.Treaty_NAPact || usToThem.Trust < 50f && usToThem.TotalAnger > 10)
                    {
                        usToThem.ChangeToNeutral();
                    }

                    break;
            }
        }

        void ChangeToHostileIfPossible(Relationship usToThem, Empire them)
        {
            switch (OwnerEmpire.Personality)
            {
                case PersonalityType.Pacifist:
                    if (!usToThem.Treaty_NAPact || usToThem.Trust.AlmostEqual(0) && usToThem.TotalAnger > 25)
                    {
                        usToThem.ChangeToHostile();
                    }

                    break;
            }
        }
    }
}