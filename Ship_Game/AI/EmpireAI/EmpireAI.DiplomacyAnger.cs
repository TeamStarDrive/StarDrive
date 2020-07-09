using System.Collections.Generic;
using Ship_Game.Gameplay;
using Ship_Game.GameScreens.DiplomacyScreen;

namespace Ship_Game.AI {

    public sealed partial class EmpireAI
    {
        private readonly int FirstDemand  = 20;
        private readonly int SecondDemand = 75;

        private void AssessDiplomaticAnger(Relationship usToThem, Empire them)
        {
            if (!usToThem.Known) 
                return;

            if (usToThem.Anger_MilitaryConflict >= 5 && !usToThem.AtWar && !usToThem.Treaty_Peace)
            {
                DeclareWarOn(them, WarType.DefensiveWar);
                return;
            }
                
            if (usToThem.Anger_TerritorialConflict + usToThem.Anger_FromShipsInOurBorders >= OwnerEmpire.data.DiplomaticPersonality.Territorialism 
                && !usToThem.AtWar && !usToThem.Treaty_OpenBorders && !usToThem.Treaty_Peace)
            {
                usToThem.PreparingForWar = true;
                usToThem.PreparingForWarType = WarType.BorderConflict;
                return;
            }

            WarnAboutShips(usToThem, them);
        }

        void WarnAboutShips(Relationship usToThem, Empire them)
        {
            if (usToThem.Anger_FromShipsInOurBorders > OwnerEmpire.data.DiplomaticPersonality.Territorialism / 4f
                && !usToThem.AtWar && !usToThem.WarnedAboutShips)
            {
                if (them.isPlayer && usToThem.turnsSinceLastContact > FirstDemand)
                    if (!usToThem.WarnedAboutColonizing)
                        DiplomacyScreen.Show(OwnerEmpire, them, "Warning Ships");
                    else if (usToThem.GetContestedSystem(out SolarSystem contested))
                        DiplomacyScreen.Show(OwnerEmpire, them, "Warning Colonized then Ships", contested);

                usToThem.turnsSinceLastContact = 0;
                usToThem.WarnedAboutShips      = true;
            }

            if (!usToThem.WarnedAboutShips || usToThem.AtWar || !OwnerEmpire.IsEmpireAttackable(them))
                return;

            int territorialism = OwnerEmpire.data.DiplomaticPersonality.Territorialism;
            if (them.CurrentMilitaryStrength < OwnerEmpire.CurrentMilitaryStrength * (1f - territorialism * 0.01f))
                DeclareWarOn(them, WarType.ImperialistWar);
        }
        

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