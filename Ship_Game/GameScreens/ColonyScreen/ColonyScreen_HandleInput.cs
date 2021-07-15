using Microsoft.Xna.Framework;
using Ship_Game.Audio;
using Ship_Game.Ships;

namespace Ship_Game
{
    public partial class ColonyScreen
    {
        // Gets the item which we want to use for detail info text
        object GetHoveredDetailItem(InputState input)
        {
            if (BuildableList.HitTest(input.CursorPosition))
            {
                foreach (BuildableListItem e in BuildableList.AllEntries)
                {
                    if (e.Hovered)
                    {
                        if (e.Building != null) return e.Building;
                        if (e.Troop != null) return e.Troop;
                    }
                }
            }

            foreach (PlanetGridSquare pgs in P.TilesList)
            {
                if (pgs.TroopsAreOnTile)
                {
                    using (pgs.TroopsHere.AcquireReadLock())
                        for (int i = 0; i < pgs.TroopsHere.Count; ++i)
                            if (pgs.TroopsHere[i].ClickRect.HitTest(input.CursorPosition))
                                return pgs.TroopsHere[i];
                }
            }

            foreach (PlanetGridSquare pgs in P.TilesList)
                if (pgs.ClickRect.HitTest(input.CursorPosition))
                    return pgs;

            return null; // default: use planet description text
        }

        public override bool HandleInput(InputState input)
        {
            // always get the currently hovered item
            DetailInfo = GetHoveredDetailItem(input);
            if (DetailInfo != null) // if hovering over an item, show the Description TAB
                PFacilities.SelectedIndex = 1;

            // WORKAROUND: disable left-right cycle if player is hovering over
            //             the build area and wants to type Filter text
            bool isHoveringOverBuildArea = RightMenu.HitTest(input.CursorPosition);
            if (!isHoveringOverBuildArea && HandleCycleColoniesLeftRight(input))
                return true;

            P.UpdateIncomes(false);

            // We are monitoring AI Colonies
            if (P.Owner != EmpireManager.Player && !Log.HasDebugger)
            {
                // Input not captured, let Universe Screen manager what happens
                return false;
            }

            if (HandleTroopSelect(input))
                return true;

            // update all Added UI elements
            if (base.HandleInput(input))
                return true;

            if (HandleExportImportButtons(input))
                return true;

            return false;
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

                if (pgs.TroopsAreOnTile)
                {
                    using (pgs.TroopsHere.AcquireWriteLock())
                    {
                        for (int i = 0; i < pgs.TroopsHere.Count; ++i)
                        {
                            Troop troop = pgs.TroopsHere[i];
                            if (troop.ClickRect.HitTest(MousePos))
                            {
                                if (input.RightMouseClick && troop.Loyalty == EmpireManager.Player)
                                {
                                    Ship troopShip = troop.Launch(pgs);
                                    if (troopShip != null)
                                    {
                                        GameAudio.TroopTakeOff();
                                        ClickedTroop = true;
                                    }
                                    else
                                    {
                                        GameAudio.NegativeClick();
                                    }
                                }

                                return true;
                            }
                        }
                    }
                }
            }

            if (!ClickedTroop && (P.Owner.isPlayer || Empire.Universe.Debug))
            {
                foreach (PlanetGridSquare pgs in P.TilesList)
                {
                    if (pgs.ClickRect.HitTest(input.CursorPosition))
                    {
                        var bRect = new Rectangle(pgs.ClickRect.X + pgs.ClickRect.Width / 2 - 32,
                            pgs.ClickRect.Y + pgs.ClickRect.Height / 2 - 32, 50, 50);
                        if (pgs.BuildingOnTile && bRect.HitTest(input.CursorPosition) && Input.RightMouseClick)
                        {
                            if (pgs.Building.Scrappable)
                            {
                                ToScrap = pgs.Building;
                                string message = $"Do you wish to scrap {pgs.Building.TranslatedName.Text}? "
                                               + "Half of the building's construction cost will be recovered to your storage.";
                                var messageBox = new MessageBoxScreen(Empire.Universe, message);
                                messageBox.Accepted = ScrapAccepted;
                                ScreenManager.AddScreen(messageBox);
                            }

                            ClickedTroop = true;
                            return true;
                        }

                        var bioRect = new Rectangle(pgs.ClickRect.X,pgs.ClickRect.Y, 20, 20);
                        if (pgs.Biosphere 
                            && (pgs.NoBuildingOnTile || pgs.BuildingOnTile)
                            && bioRect.HitTest(input.CursorPosition) && Input.RightMouseClick)
                        {
                            BioToScrap     = pgs;
                            string message = Localizer.Token(GameText.DoYouWishToScrap);
                            var messageBox = new MessageBoxScreen(Empire.Universe, message);
                            messageBox.Accepted = ScrapBioAccepted;
                            ScreenManager.AddScreen(messageBox);
                            ClickedTroop = true;
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        void OnChangeColony(int change)
        {
            var planets = P.Owner.GetPlanets();
            int newIndex = planets.IndexOf(P) + change;
            if (newIndex >= planets.Count) newIndex = 0;
            else if (newIndex < 0) newIndex = planets.Count - 1;

            Planet nextOrPrevPlanet = planets[newIndex];
            if (nextOrPrevPlanet != P)
            {
                Empire.Universe.workersPanel = new ColonyScreen(Empire.Universe, nextOrPrevPlanet, Eui,
                    GovernorDetails.CurrentTabIndex, PFacilities.SelectedIndex);
            }
        }

        bool HandleCycleColoniesLeftRight(InputState input)
        {
            bool canView = (Empire.Universe.Debug || P.Owner == EmpireManager.Player);
            if (canView && (input.Left || input.Right))
            {
                int change = input.Left ? -1 : +1;
                OnChangeColony(change);
                return true; // planet changed, ColonyScreen will be replaced
            }

            return false;
        }

        bool HandleExportImportButtons(InputState input)
        {
            if (FoodDropDown.r.HitTest(input.CursorPosition) && input.LeftMouseClick)
            {
                FoodDropDown.Toggle();
                GameAudio.AcceptClick();
                P.FS = (Planet.GoodState) ((int) P.FS + (int) Planet.GoodState.IMPORT);
                if (P.FS > Planet.GoodState.EXPORT)
                    P.FS = Planet.GoodState.STORE;
                return true;
            }

            if (ProdDropDown.r.HitTest(input.CursorPosition) && input.LeftMouseClick)
            {
                ProdDropDown.Toggle();
                GameAudio.AcceptClick();
                P.PS = (Planet.GoodState) ((int) P.PS + (int) Planet.GoodState.IMPORT);
                if (P.PS > Planet.GoodState.EXPORT)
                    P.PS = Planet.GoodState.STORE;
                return true;
            }
            return false;
        }
    }
}
