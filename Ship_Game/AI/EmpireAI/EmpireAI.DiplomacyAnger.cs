using System.Collections.Generic;
using Ship_Game.Gameplay;
using Ship_Game.GameScreens.DiplomacyScreen;

namespace Ship_Game.AI {

    public sealed partial class EmpireAI
    {
        private int FirstDemand  = 20;
        private int SecondDemand = 75;

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
                return;
            }
            if (!usToThem.WarnedAboutShips || usToThem.AtWar || !OwnerEmpire.IsEmpireAttackable(them)) 
                return;

            if (them.CurrentMilitaryStrength < OwnerEmpire.CurrentMilitaryStrength * (1f - OwnerEmpire.data.DiplomaticPersonality.Territorialism * 0.01f))
                DeclareWarOn(them, WarType.ImperialistWar);
        }
        
    }
}