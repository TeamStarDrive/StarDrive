using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using Ship_Game.Audio;
using Ship_Game.Ships;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace Ship_Game
{
    public partial class ColonyScreen
    {
        int PFacilitiesPlayerTabSelected;
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

        public void OnPFacilitiesTabChange(int tabindex)
        {
            // Using PlayerSelectedTab here to be able to return to the tab the player selected when there is no Detail Info item.
            // So if the player selected the trade tab, then viewed a planet tile and then moved the cursor away, the trade tab will be set again
            if (DetailInfo == null)
                PFacilitiesPlayerTabSelected = tabindex;
        }

        public override bool HandleInput(InputState input)
        {
            // always get the currently hovered item
            DetailInfo = GetHoveredDetailItem(input);

            // If there is a detail info, display the Description TAB, else display last tab the player selected.
            PFacilities.SelectedIndex = DetailInfo == null ? PFacilitiesPlayerTabSelected : 1;

            if (!FilterBuildableItems.HandlingInput && !PlanetName.HandlingInput &&  HandleCycleColoniesLeftRight(input))
                return true;

            FilterBuildableItemsLabel.Color = FilterBuildableItems.HandlingInput ? Color.White : Color.Gray;
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

            if (!ClickedTroop && (P.OwnerIsPlayer || P.Universe.Debug))
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
                                var messageBox = new MessageBoxScreen(P.Universe.Screen, message);
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
                            var messageBox = new MessageBoxScreen(P.Universe.Screen, message);
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
                P.Universe.Screen.workersPanel = new ColonyScreen(P.Universe.Screen, nextOrPrevPlanet, Eui,
                    GovernorDetails.CurrentTabIndex, PFacilitiesPlayerTabSelected);
            }
        }

        bool HandleCycleColoniesLeftRight(InputState input)
        {
            bool canView = (P.Universe.Debug || P.OwnerIsPlayer);
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
