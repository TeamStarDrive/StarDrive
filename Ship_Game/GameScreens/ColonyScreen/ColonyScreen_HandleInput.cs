using Microsoft.Xna.Framework;
using Ship_Game.Audio;

namespace Ship_Game
{
    public partial class ColonyScreen
    {
        void HandleDetailInfo()
        {
            DetailInfo = null;
            foreach (BuildableListItem e in BuildableList.AllEntries)
            {
                if (e.Hovered)
                {
                    if (e.Troop != null) DetailInfo = e.Troop;
                    if (e.Building != null) DetailInfo = e.Building;
                }
            }

            if (DetailInfo == null)
                DetailInfo = P.Description;
        }

        public override bool HandleInput(InputState input)
        {
            pFacilities.HandleInput(input);

            if (HandleCycleColoniesLeftRight(input))
                return true;

            P.UpdateIncomes(false);

            HandleDetailInfo();

            // We are monitoring AI Colonies
            if (P.Owner != EmpireManager.Player && !Log.HasDebugger)
            {
                // Input not captured, let Universe Screen manager what happens
                return false;
            }

            HandlePlanetNameChangeTextBox(input);

            if (HandleTroopSelect(input))
                return true;

            HandleExportImportButtons(input);

            return base.HandleInput(input);
        }

        bool HandleTroopSelect(InputState input)
        {
            ClickedTroop = false;
            foreach (PlanetGridSquare pgs in P.TilesList)
            {
                if (!pgs.ClickRect.HitTest(MousePos))
                {
                    pgs.Highlighted = false;
                }
                else
                {
                    if (!pgs.Highlighted)
                    {
                        GameAudio.ButtonMouseOver();
                    }

                    pgs.Highlighted = true;
                }
                /* TODO - fix that
                if (pgs.TroopsHere.Count <= 0 || !pgs.Troop1ClickRect.HitTest(MousePos))
                    continue;

                DetailInfo = pgs.SingleTroop;
                if (input.RightMouseClick && pgs.SingleTroop.Loyalty == EmpireManager.Player)
                {
                    GameAudio.TroopTakeOff();
                    pgs.SingleTroop.Launch(pgs);
                    ClickedTroop = true;
                    DetailInfo = null;
                }*/

                return true;
            }

            if (!ClickedTroop)
            {
                foreach (PlanetGridSquare pgs in P.TilesList)
                {
                    if (pgs.ClickRect.HitTest(input.CursorPosition))
                    {
                        DetailInfo = pgs;
                        var bRect = new Rectangle(pgs.ClickRect.X + pgs.ClickRect.Width / 2 - 32,
                            pgs.ClickRect.Y + pgs.ClickRect.Height / 2 - 32, 64, 64);
                        if (pgs.building != null && bRect.HitTest(input.CursorPosition) && Input.RightMouseClick)
                        {
                            if (pgs.building.Scrappable)
                            {
                                ToScrap = pgs.building;
                                string message = string.Concat("Do you wish to scrap ",
                                    Localizer.Token(pgs.building.NameTranslationIndex),
                                    "? Half of the building's construction cost will be recovered to your storage.");
                                var messageBox = new MessageBoxScreen(Empire.Universe, message);
                                messageBox.Accepted = ScrapAccepted;
                                ScreenManager.AddScreenDeferred(messageBox);
                            }

                            ClickedTroop = true;
                            return true;
                        }
                    }
                    /* TODO - fix that
                    if (pgs.TroopsHere.Count <= 0 || !pgs.Troop1ClickRect.HitTest(input.CursorPosition))
                        continue;*/

                    DetailInfo = pgs.TroopsHere;
                }
            }

            return false;
        }

        bool HandleCycleColoniesLeftRight(InputState input)
        {
            if     (RightColony.Rect.HitTest(input.CursorPosition)) ToolTip.CreateTooltip(2279);
            else if (LeftColony.Rect.HitTest(input.CursorPosition)) ToolTip.CreateTooltip(2280);

            bool canView = (Empire.Universe.Debug || P.Owner == EmpireManager.Player);
            if (!canView)
                return false;

            int change = 0;
            if (input.Right || RightColony.HandleInput(input) && input.LeftMouseClick)
                change = +1;
            else if (input.Left || LeftColony.HandleInput(input) && input.LeftMouseClick)
                change = -1;

            if (change != 0)
            {
                var planets = P.Owner.GetPlanets();
                int newIndex = planets.IndexOf(P) + change;
                if (newIndex >= planets.Count) newIndex = 0;
                else if (newIndex < 0) newIndex = planets.Count - 1;

                Planet nextOrPrevPlanet = planets[newIndex];
                if (nextOrPrevPlanet != P)
                {
                    Empire.Universe.workersPanel = new ColonyScreen(Empire.Universe, nextOrPrevPlanet, eui);
                }

                return true; // planet changed, ColonyScreen will be replaced
            }

            return false;
        }

        void HandlePlanetNameChangeTextBox(InputState input)
        {
            if (!EditNameButton.HitTest(input.CursorPosition))
            {
                EditHoverState = 0;
            }
            else
            {
                EditHoverState = 1;
                if (input.LeftMouseClick)
                {
                    PlanetName.HandlingInput = true;
                }
            }

            if (!PlanetName.HandlingInput)
            {
                GlobalStats.TakingInput = false;
                bool empty = true;
                string text = PlanetName.Text;
                int num = 0;
                while (num < text.Length)
                {
                    if (text[num] == ' ')
                    {
                        num++;
                    }
                    else
                    {
                        empty = false;
                        break;
                    }
                }

                if (empty)
                {
                    int ringnum = 1;
                    foreach (SolarSystem.Ring ring in P.ParentSystem.RingList)
                    {
                        if (ring.planet == P)
                        {
                            PlanetName.Text = string.Concat(P.ParentSystem.Name, " ",
                                RomanNumerals.ToRoman(ringnum));
                        }

                        ringnum++;
                    }
                }
            }
            else
            {
                GlobalStats.TakingInput = true;
                PlanetName.HandleTextInput(ref PlanetName.Text, input);
            }
        }

        void HandleExportImportButtons(InputState input)
        {
            if (foodDropDown.r.HitTest(input.CursorPosition) && input.LeftMouseClick)
            {
                foodDropDown.Toggle();
                GameAudio.AcceptClick();
                P.FS = (Planet.GoodState) ((int) P.FS + (int) Planet.GoodState.IMPORT);
                if (P.FS > Planet.GoodState.EXPORT)
                    P.FS = Planet.GoodState.STORE;
            }

            if (prodDropDown.r.HitTest(input.CursorPosition) && input.LeftMouseClick)
            {
                prodDropDown.Toggle();
                GameAudio.AcceptClick();
                P.PS = (Planet.GoodState) ((int) P.PS + (int) Planet.GoodState.IMPORT);
                if (P.PS > Planet.GoodState.EXPORT)
                    P.PS = Planet.GoodState.STORE;
            }
        }
    }
}