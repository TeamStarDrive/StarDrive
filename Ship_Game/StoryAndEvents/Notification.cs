using System.Windows.Forms;
using SDGraphics;
using Ship_Game.Audio;
using Ship_Game.Ships;
using Rectangle = SDGraphics.Rectangle;

namespace Ship_Game;

public sealed class Notification
{
    public object ReferencedItem1;
    public GameObject ReferencedItem2;

    public Empire RelevantEmpire;
    public Rectangle ClickRect;
    public Rectangle DestinationRect;

    public float transitionElapsedTime;
    public float transDuration = 1f;
        
    public string Message;
    public string Action; // @TODO - this needs an enum!

    public SubTexture Icon;
    public string IconPath;
        
    public bool Tech;
    public bool ShowMessage;
    public bool Pause = true;

    /** @return TRUE if input was captured */
    public bool HandleInput(InputState input, NotificationManager m)
    {
        if (!ClickRect.HitTest(input.CursorPosition))
        {
            ShowMessage = false;
            return false;
        }

        ShowMessage = true;

        if (input.LeftMouseReleased)
        {
            switch (Action)
            {
                case "SnapToPlanet":
                    m.SnapToPlanet(ReferencedItem1 as Planet);
                    break;
                case "SnapToSystem":
                    m.SnapToSystem(ReferencedItem1 as SolarSystem);
                    break;
                case "CombatScreen":
                    m.SnapToCombat(ReferencedItem1 as Planet);
                    break;
                case "LoadEvent":
                    ((ExplorationEvent)ReferencedItem1)?.TriggerExplorationEvent(m.Screen);
                    break;
                case "ResearchScreen":
                    m.ScreenManager.AddScreen(new ResearchPopup(m.Screen, ReferencedItem1 as string));
                    break;
                case "SnapToExpandSystem":
                    m.SnapToExpandedSystem(ReferencedItem2 as Planet, ReferencedItem1 as SolarSystem);
                    break;
                case "ShipDesign":
                    m.ScreenManager.AddScreen(new ShipDesignScreen(m.Screen, m.Screen.EmpireUI));
                    break;
                case "SnapToShip":
                    m.SnapToShip(ReferencedItem1 as Ship);
                    break;
            }
            return true;
        }
        if (input.RightMouseClick && Action != "LoadEvent")
        {
            GameAudio.SubBassWhoosh();
            // ADDED BY SHAHMATT (to unpause game on right clicking notification icon)
            if (GlobalStats.PauseOnNotification && Pause)
                m.Screen.UState.Paused = false;

            return true;
        }
        return false;
    }
}
