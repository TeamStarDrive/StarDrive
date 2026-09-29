using System;
using Ship_Game.AI;
using Ship_Game.Audio;
using Ship_Game.Commands.Goals;
using Ship_Game.GameScreens.DiplomacyScreen;
using Ship_Game.Ships;

namespace Ship_Game
{
    public partial class UniverseScreen
    {
        public void OrderScrap(Ship s)
        {
            s.AI.OrderScrapShip();
        }

        public void ContactLeader(Ship s)
        {
            Empire leaderLoyalty = s.Loyalty;
            if (leaderLoyalty.IsFaction)
                Encounter.ShowEncounterPopUpPlayerInitiated(s.Loyalty, this);
            else
                DiplomacyScreen.Show(s.Loyalty, Player, "Greeting");
        }

        public void RefitTo(Ship s)
        {
            ScreenManager.AddScreen(new RefitToWindow(this, s));
        }

        public void OrderScuttle(Ship s)
        {
            s.ScuttleTimer = 10f;
        }

        public void DoExplore(Ship s)
        {
            s.AI.OrderExplore();
        }

        void MarkForColonization(Planet p)
        {
            int graceTurns = p.ColonyGraceTurnsLeft(Player, out Empire lostBy);
            if (graceTurns > 0)
            {
                GameAudio.NegativeClick();
                ToolTip.CreateFloatingText(Planet.ColonyGraceTip(lostBy, graceTurns), "", Input.CursorPosition, 5);
                return;
            }

            Player.AI.AddGoalAndEvaluate(new MarkForColonization(p, Player, isManual:true));
        }
    }
}