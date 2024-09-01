using System;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Ship_Game.Audio;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;
using Ship_Game.Universe;
using Ship_Game.UI;

namespace Ship_Game.GameScreens.ShipDesign
{
    public sealed class ShipDesignLoadScreen : GameScreen
    {
        readonly ShipDesignScreen Screen;
        UniverseState Universe => Screen.ParentUniverse.UState;

        bool ShowOnlyPlayerDesigns;
        readonly bool UnlockAllDesigns;

        UITextEntry Filter;
        string DefaultFilterText;
        PlayerDesignToggleButton PlayerDesignsToggle;
        ScrollList<DesignListItem> AvailableDesignsList;
        ShipInfoOverlayComponent ShipInfoOverlay;

        IShipDesign SelectedShip;
        IShipDesign SelectedWIP;

        Array<Ships.ShipDesign> WIPs = new Array<Ships.ShipDesign>();

        public ShipDesignLoadScreen(ShipDesignScreen screen, bool unlockAll) : base(screen, toPause: null)
        {
            Screen = screen;
            IsPopup = true;
            TransitionOnTime  = 0.25f;
            TransitionOffTime = 0.25f;
            UnlockAllDesigns = unlockAll;
        }

        class DesignListItem : ScrollListItem<DesignListItem>
        {
            readonly ShipDesignLoadScreen Screen;
            public readonly IShipDesign Design;
            public readonly bool IsWIP;
            
            public DesignListItem(ShipDesignLoadScreen screen, string headerText) : base(headerText)
            {
                Screen = screen;
            }

            public DesignListItem(ShipDesignLoadScreen screen, IShipDesign design, bool isWIP)
            {
                Screen = screen;
                Design = design;
                IsWIP = isWIP;

                if (!isWIP)
                {
                    if (!design.IsReadonlyDesign && !design.IsFromSave)
                        AddCancel(new(-30, 0), "Delete this Ship Design", 
                            () => PromptDeleteShip(design.Name));
                }
                else
                {
                    AddCancel(new(-30, -45), "Delete this WIP Design", 
                        () => PromptDeleteShip(design.Name));
                    
                    AddDelete(new(-30, 15), "Delete all related versions of this WIP Design",
                        () => PromptDeleteWIPVersions(design.Name));
                }
            }

            void PromptDeleteShip(string shipId)
            {
                if (Screen.Universe.Ships.Any(s => s.Name == shipId))
                {
                    GameAudio.NegativeClick();
                    Screen.ScreenManager.AddScreen(new MessageBoxScreen(Screen.Screen, $"{shipId} currently exist the universe." +
                                                                       " You cannot delete a design with this name.",
                                                                       MessageBoxButtons.Ok));
                    return;
                }

                if (HelperFunctions.DesignInQueue(Screen.Screen, shipId, out string playerPlanets))
                {
                    GameAudio.NegativeClick();
                    if (playerPlanets.NotEmpty())
                    {
                        Screen.ScreenManager.AddScreen(new MessageBoxScreen
                            (Screen, $"{shipId} currently exist the your planets' build queue." +
                                     $" You cannot delete this design name.\n Related planets: {playerPlanets}.",
                                     MessageBoxButtons.Ok));
                    }
                    else
                    {
                        Screen.ScreenManager.AddScreen(new MessageBoxScreen
                            (Screen, $"{shipId} currently exist the universe (maybe by another empire). " +
                                    "You cannot delete this design name.", MessageBoxButtons.Ok));
                    }

                    return;
                }

                Screen.ScreenManager.AddScreen(new MessageBoxScreen(Screen, $"Confirm Delete: {shipId}")
                {
                    Accepted = () => Screen.DeleteAccepted(shipId)
                });
            }

            void PromptDeleteWIPVersions(string shipId)
            {
                string shipPrefix = ShipDesignWIP.GetWipShipNameAndNum(shipId);
                Screen.ScreenManager.AddScreen(new MessageBoxScreen(Screen, $"Confirm Delete All WIP Versions: {shipPrefix}")
                {
                    Accepted = () => Screen.DeleteWIPVersionAccepted(shipId)
                });
            }

            public override void Draw(SpriteBatch batch, DrawTimes elapsed)
            {
                base.Draw(batch, elapsed);
                if (Design != null)
                {
                    var icon = new Vector2(X + 35f, Y);
                    batch.Draw(Design.Icon, new RectF(icon.X, icon.Y, 29, 30), Color.White);


                    var p = new Vector2(icon.X + 40f, icon.Y + 3f);
                    batch.DrawString(Fonts.Arial12Bold, Design.Name, p, Color.White);
                    p.Y += Fonts.Arial12Bold.LineSpacing;

                    if (!IsWIP)
                    {
                        var hullName = Design.BaseHull.VisibleName ?? "<VisibleName was null>";
                        batch.DrawString(Fonts.Arial8Bold, hullName, p, Color.DarkGray);
                        p.X += Fonts.Arial8Bold.TextWidth(hullName) + 8;
                    }
                    else
                    {
                        var roleName = Localizer.GetRole(Design.Role, Screen.Screen.Player);
                        batch.DrawString(Fonts.Arial8Bold, roleName, p, Color.Orange);
                        p.X += Fonts.Arial8Bold.TextWidth(roleName) + 8;
                    }

                    batch.DrawString(Fonts.Arial8Bold, $"Base Strength: {Design.BaseStrength.String(0)}", p, Color.Orange);
                }
            }
        }

        public override void LoadContent()
        {
            Elements.Clear();

            Rect = new(ScreenWidth / 2 - 250, ScreenHeight / 2 - 300, 500, 600);

            RectF designsRect = new(X + 20, Y + 60, Width - 40, Height - 80);
            var designs = Add(new SubmenuScrollList<DesignListItem>(designsRect, GameText.AvailableDesigns));
            designs.SetBackground(new Menu1(Rect));

            AvailableDesignsList = designs.List;
            AvailableDesignsList.EnableItemHighlight = true;
            AvailableDesignsList.OnClick       = OnDesignListItemClicked;
            AvailableDesignsList.OnDoubleClick = OnDesignListItemDoubleClicked;

            PlayerDesignsToggle = Add(new PlayerDesignToggleButton(new Vector2(designs.Right - 44, designs.Y-1)));
            PlayerDesignsToggle.IsToggled = ShowOnlyPlayerDesigns = !Screen.Player.Universe.P.ShowAllDesigns;
            PlayerDesignsToggle.OnClick = p =>
            {
                GameAudio.AcceptClick();
                Screen.Player.Universe.P.ShowAllDesigns = !Screen.Player.Universe.P.ShowAllDesigns;
                ShowOnlyPlayerDesigns = !Screen.Player.Universe.P.ShowAllDesigns;
                PlayerDesignsToggle.IsToggled = !Screen.Player.Universe.P.ShowAllDesigns;
                Filter.Text = DefaultFilterText;
                LoadShipTemplates(filter:null);
            };
            
            DefaultFilterText = Localizer.Token(GameText.ChooseAShipToLoad);
            Filter = Add(new UITextEntry(X + 20, Y + 20, designs.Width - 120, Fonts.Arial20Bold, DefaultFilterText));
            Filter.AutoCaptureOnKeys = true;
            Filter.AutoCaptureLoseFocusTime = 0.5f;
            Filter.OnTextChanged = LoadShipTemplates;
            Filter.OnTextInputCapture = () =>
            {
                if (Filter.Text == DefaultFilterText)
                    Filter.Text = "";
            };

            ButtonSmall(designs.Right - 88, Filter.Y - 2, text:GameText.Load, click: b =>
            {
                LoadShipToScreen();
            });

            ShipInfoOverlay = Add(new ShipInfoOverlayComponent(this, Universe));
            AvailableDesignsList.OnHovered = (item) =>
            {
                ShipInfoOverlay.ShowToLeftOf(item?.Pos ?? Vector2.Zero, item?.Design);
            };

            WIPs.Clear();
            foreach (FileInfo info in Dir.GetFiles(Dir.StarDriveAppData + "/WIP", "design"))
            {
                Ships.ShipDesign newShipData = Ships.ShipDesign.Parse(info);
                if (newShipData == null)
                    continue;
                if (UnlockAllDesigns || Screen.Player.WeCanShowThisWIP(newShipData))
                    WIPs.Add(newShipData);
            }

            LoadShipTemplates(filter:null);
        }

        void OnDesignListItemClicked(DesignListItem item)
        {
            if (item.Design != null)
            {
                if (item.IsWIP)
                    SelectedWIP = item.Design;
                else
                    SelectedShip = item.Design;
            }
        }

        void OnDesignListItemDoubleClicked(DesignListItem item)
        {
            OnDesignListItemClicked(item);
            LoadShipToScreen();
        }

        void DeleteAccepted(string shipToDelete)
        {
            ResourceManager.DeleteShip(Universe, shipToDelete);
            PostDeleteDesign();
        }

        void DeleteWIPVersionAccepted(string wipToDelete)
        {
            ShipDesignWIP.RemoveRelatedWiPs(Universe, wipToDelete);
            PostDeleteDesign();
        }

        void PostDeleteDesign()
        {
            GameAudio.EchoAffirmative();
            ShipInfoOverlay.Hide();
            LoadContent();
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
            batch.SafeBegin();            
            base.Draw(batch, elapsed);
            PlayerDesignsToggle.Draw(batch, elapsed);
            batch.SafeEnd();
        }

        void LoadShipTemplates(string filter)
        {
            // if filter is set to the default prompt, treat it as if there is no filter
            if (filter == DefaultFilterText)
                filter = null;

            filter = filter?.ToLower();

            SelectedShip = null;
            SelectedWIP = null;

            Ship[] ships = ResourceManager.Ships.Ships
                .Filter(s => CanShowDesign(s, filter))
                .OrderBy(s => !s.ShipData.IsPlayerDesign)
                .ThenBy(s => s.BaseHull.Style != Screen.Player.data.Traits.ShipType)
                .ThenBy(s => s.BaseHull.Style)
                .ThenByDescending(s => s.BaseStrength)
                .ThenBy(s => s.Name).ToArr();

            AvailableDesignsList.Reset();

            if (filter.IsEmpty())
            {
                var shipsByRole = new Map<string, Array<Ship>>();

                foreach (Ship ship in ships)
                {
                    string role = Localizer.GetRole(ship.DesignRole, Screen.Player);
                    if (!shipsByRole.TryGetValue(role, out Array<Ship> roleShips))
                    {
                        shipsByRole[role] = roleShips = new Array<Ship>();
                    }
                    roleShips.Add(ship);
                }

                var shipsByRoleArray = shipsByRole.ToArray();
                Array.Sort(keys:shipsByRole.Keys.ToArr(), shipsByRoleArray);

                foreach (var roleAndShips in shipsByRoleArray)
                {
                    string role = roleAndShips.Key;
                    DesignListItem group = AvailableDesignsList.AddItem(new DesignListItem(this, role));
                    foreach (Ship ship in roleAndShips.Value)
                        group.AddSubItem(new DesignListItem(this, ship.ShipData, isWIP:false));
                }

                if (WIPs.Count > 0)
                {
                    DesignListItem wip = AvailableDesignsList.AddItem(new DesignListItem(this, "WIP"));
                    foreach (Ships.ShipDesign wipHull in WIPs)
                        wip.AddSubItem(new DesignListItem(this, wipHull, isWIP:true));
                }
            }
            else
            {
                foreach (Ship ship in ships)
                    AvailableDesignsList.AddItem(new DesignListItem(this, ship.ShipData, isWIP:false));
                foreach (Ships.ShipDesign wipHull in WIPs)
                    AvailableDesignsList.AddItem(new DesignListItem(this, wipHull, isWIP:true));
            }
        }

        bool CanShowDesign(Ship ship, string filter)
        {
            if (filter.NotEmpty() &&
                !ship.Name.ToLower().Contains(filter) &&
                !ship.BaseHull.HullName.ToLower().Contains(filter))
            {
                return false;
            }

            IShipDesign design = ship.ShipData;
            if (ShowOnlyPlayerDesigns && !design.IsPlayerDesign)
                return false;

            if (UnlockAllDesigns) // if universe is DeveloperUniverse, all designs are visible
                return !design.Deleted;

            return !design.Deleted
                && !design.IsShipyard
                && Screen.Player.WeCanBuildThis(design)
                && (!design.IsSubspaceProjector || Screen.EnableDebugFeatures) // ignore subspace projectors (unless debug features are enabled)
                && (!design.IsDysonSwarmSat || Screen.EnableDebugFeatures) // ignore Dyson Swarm Sats (unless debug features are enabled)
                && (!design.IsUnitTestShip || Screen.EnableDebugFeatures) // ignore unit testing ships (unless debug features are enabled)
                && ResourceManager.ShipRoles.TryGetValue(design.Role, out ShipRole sr) && !sr.Protected;
        }

        void LoadShipToScreen()
        {
            Screen.ChangeHull(SelectedShip ?? SelectedWIP);
            ExitScreen();
        }

        public class PlayerDesignToggleButton : ToggleButton
        {
            public PlayerDesignToggleButton(Vector2 pos) : base(pos, ToggleButtonStyle.Grid, "SelectionBox/icon_grid")
            {
                Tooltip = GameText.ToggleToDisplayOnlyPlayerdesigned;
            }
        }
    }
}
