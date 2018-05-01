using Ship_Game;
using Ship_Game.AI;
using Ship_Game.UI;

namespace Ship_Game.UI
{
    
    public class CombatStateToggles
    {
        public CombatState CombatState;
        Array<ToggleButton> CombatStatusButtons = new Array<ToggleButton>();

        public CombatStateToggles(ShipAI ai)
        {
            CombatState = ai.CombatState;
        }
        public CombatStateToggles(CombatState combatState)
        {
            CombatState = combatState;
        }
        private void CheckToggleButton(InputState input)
        {            
            foreach (ToggleButton toggleButton in CombatStatusButtons)
            {
                if (toggleButton.HandleInput(input))
                {
                    GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                    switch (toggleButton.Action)
                    {
                        case "attack":
                            CombatState = CombatState.AttackRuns;
                            break;
                        case "arty":
                            CombatState = CombatState.Artillery;
                            break;
                        case "hold":
                            CombatState = CombatState.HoldPosition;
                            break;
                        case "orbit_left":
                            CombatState = CombatState.OrbitLeft;
                            break;
                        case "broadside_left":
                            CombatState = CombatState.BroadsideLeft;
                            break;
                        case "orbit_right":
                            CombatState = CombatState.OrbitRight;
                            break;
                        case "broadside_right":
                            CombatState = CombatState.BroadsideRight;
                            break;
                        case "evade":
                            CombatState = CombatState.Evade;
                            break;
                        case "short":
                            CombatState = CombatState.ShortRange;
                            break;
                    }
                }

                switch (toggleButton.Action)
                {
                    case "attack":
                        toggleButton.Active = CombatState == CombatState.AttackRuns;
                        continue;
                    case "arty":
                        toggleButton.Active = CombatState == CombatState.Artillery;
                        continue;
                    case "hold":
                        toggleButton.Active = CombatState == CombatState.HoldPosition;
                        continue;
                    case "orbit_left":
                        toggleButton.Active = CombatState == CombatState.OrbitLeft;
                        continue;
                    case "broadside_left":
                        toggleButton.Active = CombatState == CombatState.BroadsideLeft;
                        continue;
                    case "orbit_right":
                        toggleButton.Active = CombatState == CombatState.OrbitRight;
                        continue;
                    case "broadside_right":
                        toggleButton.Active = CombatState == CombatState.BroadsideRight;
                        continue;
                    case "evade":
                        toggleButton.Active = CombatState == CombatState.Evade;
                        continue;
                    case "short":
                        toggleButton.Active = CombatState == CombatState.ShortRange;
                        continue;
                    default:
                        continue;
                }
            }
        }
    }
}