using System;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;
using Ship_Game.Ships;

namespace Ship_Game.GameScreens.ShipDesign
{
    public sealed class ShipDesignLoadScreen : GameScreen
    {
        readonly ShipDesignScreen Screen;

        bool ShowOnlyPlayerDesigns;
        readonly bool UnlockAllDesigns;

        UITextEntry Filter;
        string DefaultFilterText;
        PlayerDesignToggleButton PlayerDesignsToggle;
        ScrollList2<DesignListItem> AvailableDesignsList;
        ShipInfoOverlayComponent ShipInfoOverlay;

        Ship SelectedShip;
        ShipData SelectedWIP;

        Array<ShipData> WIPs = new Array<ShipData>();

        public ShipDesignLoadScreen(ShipDesignScreen screen, bool unlockAll) : base(screen)
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
            public readonly Ship Ship;
            public readonly ShipData WipHull;
            
            public DesignListItem(ShipDesignLoadScreen screen, string headerText) : base(headerText)
            {
                Screen = screen;
            }

            public DesignListItem(ShipDesignLoadScreen screen, Ship ship)
            {
                Screen = screen;
                Ship = ship;
                if (!ship.IsReadonlyDesign && !ship.FromSave)
                    AddCancel(new Vector2(-30, 0), "Delete this Ship Design", 
                        () => PromptDeleteShip(Ship.Name));
            }

            public DesignListItem(ShipDesignLoadScreen screen, ShipData wipHull)
            {
                Screen = screen;
                WipHull = wipHull;
                AddCancel(new Vector2(-30, 0), "Delete this WIP Hull", 
                    () => PromptDeleteShip(WipHull.Name));
            }
            
            void PromptDeleteShip(string shipId)
            {
                Screen.ScreenManager.AddScreen(new MessageBoxScreen(Screen, $"Confirm Delete: {shipId}")
                {
                    Accepted = () => Screen.DeleteAccepted(shipId)
                });
            }
            
            public override void Draw(SpriteBatch batch, DrawTimes elapsed)
            {
                base.Draw(batch, elapsed);
                if (Ship != null)
                {
                    var bCursor = new Vector2(X + 35f, Y);
                    batch.Draw(Ship.shipData.Icon, new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);

                    var tCursor = new Vector2(bCursor.X + 40f, bCursor.Y + 3f);
                    batch.DrawString(Fonts.Arial12Bold, Ship.Name, tCursor, Color.White);
                    tCursor.Y = tCursor.Y + Fonts.Arial12Bold.LineSpacing;
                    var role = Ship.BaseHull.Name;
                    batch.DrawString(Fonts.Arial8Bold, role, tCursor, Color.DarkGray);
                    tCursor.X = tCursor.X + Fonts.Arial8Bold.MeasureString(role).X + 8;
                    batch.DrawString(Fonts.Arial8Bold, $"Base Strength: {Ship.BaseStrength.String(0)}", tCursor, Color.Orange);
                }
                else if (WipHull != null)
                {
                    var bCursor = new Vector2(X + 35f, Y);                 
                    batch.Draw(WipHull.Icon, new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);

                    var tCursor = new Vector2(bCursor.X + 40f, bCursor.Y + 3f);
                    batch.DrawString(Fonts.Arial12Bold, WipHull.Name, tCursor, Color.White);
                    tCursor.Y += Fonts.Arial12Bold.LineSpacing;
                    batch.DrawString(Fonts.Arial8Bold, Localizer.GetRole(WipHull.Role, EmpireManager.Player), tCursor, Color.Orange);
                }
                
                base.Draw(batch, elapsed);
            }
        }

        public override void LoadContent()
        {
            Elements.Clear();

            Rect = new Rectangle(ScreenWidth / 2 - 250, ScreenHeight / 2 - 300, 500, 600);
            var background = new Submenu(X + 20, Y + 60, Width - 40, Height - 80);
            background.Background = new Menu1(Rect);
            background.AddTab(Localizer.Token(GameText.AvailableDesigns));

            AvailableDesignsList = Add(new ScrollList2<DesignListItem>(background));
            AvailableDesignsList.EnableItemHighlight = true;
            AvailableDesignsList.OnClick       = OnDesignListItemClicked;
            AvailableDesignsList.OnDoubleClick = OnDesignListItemDoubleClicked;

            PlayerDesignsToggle = Add(new PlayerDesignToggleButton(new Vector2(background.Right - 44, background.Y)));
            PlayerDesignsToggle.OnClick = p =>
            {
                GameAudio.AcceptClick();
                ShowOnlyPlayerDesigns = !ShowOnlyPlayerDesigns;
                PlayerDesignsToggle.IsToggled = !ShowOnlyPlayerDesigns;
                Filter.Text = DefaultFilterText;
                LoadShipTemplates(filter:null);
            };
            
            DefaultFilterText = Localizer.Token(GameText.ChooseAShipToLoad);
            Filter = Add(new UITextEntry(X + 20, Y + 20, background.Width - 120, Fonts.Arial20Bold, DefaultFilterText));
            Filter.AutoCaptureOnKeys = true;
            Filter.AutoCaptureLoseFocusTime = 0.5f;
            Filter.OnTextChanged = LoadShipTemplates;
            Filter.OnTextInputCapture = () =>
            {
                if (Filter.Text == DefaultFilterText)
                    Filter.Text = "";
            };

            ButtonSmall(background.Right - 88, Filter.Y - 2, text:GameText.Load, click: b =>
            {
                LoadShipToScreen();
            });

            ShipInfoOverlay = Add(new ShipInfoOverlayComponent(this));
            AvailableDesignsList.OnHovered = (item) =>
            {
                ShipInfoOverlay.ShowToLeftOf(item?.Pos ?? Vector2.Zero, item?.Ship);
            };

            WIPs.Clear();
            foreach (FileInfo info in Dir.GetFiles(Dir.StarDriveAppData + "/WIP"))
            {
                ShipData newShipData = ShipData.Parse(info, isEmptyHull:false);
                if (newShipData == null)
                    continue;
                if (UnlockAllDesigns || EmpireManager.Player.IsHullUnlocked(newShipData.Hull))
                    WIPs.Add(newShipData);
            }

            LoadShipTemplates(filter:null);
        }

        void OnDesignListItemClicked(DesignListItem item)
        {
            if (item.WipHull != null)
            {
                SelectedWIP = item.WipHull;
            }
            else if (item.Ship != null)
            {
                SelectedShip = item.Ship;
            }
        }

        void OnDesignListItemDoubleClicked(DesignListItem item)
        {
            OnDesignListItemClicked(item);
            LoadShipToScreen();
        }

        void DeleteAccepted(string shipToDelete)
        {
            GameAudio.EchoAffirmative();
            ResourceManager.DeleteShip(shipToDelete);
            ShipInfoOverlay.Hide();
            LoadContent();
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
            batch.Begin();            
            base.Draw(batch, elapsed);
            PlayerDesignsToggle.Draw(batch, elapsed);
            batch.End();
        }

        void LoadShipTemplates(string filter)
        {
            // if filter is set to the default prompt, treat it as if there is no filter
            if (filter == DefaultFilterText)
                filter = null;

            SelectedShip = null;
            SelectedWIP = null;

            Ship[] ships = ResourceManager.GetShipTemplates()
                .Filter(s => filter.IsEmpty() || s.Name.Contains(filter))
                .OrderBy(s => !s.IsPlayerDesign)
                .ThenBy(s => s.BaseHull.ShipStyle != EmpireManager.Player.data.Traits.ShipType)
                .ThenBy(s => s.BaseHull.ShipStyle)
                .ThenByDescending(s => s.BaseStrength)
                .ThenBy(s => s.Name)
                .ToArray().Filter(CanShowDesign);

            AvailableDesignsList.Reset();

            if (filter.IsEmpty())
            {
                var shipsByRole = new Map<string, Array<Ship>>();

                foreach (Ship ship in ships)
                {
                    string role = Localizer.GetRole(ship.DesignRole, EmpireManager.Player);
                    if (!shipsByRole.TryGetValue(role, out Array<Ship> roleShips))
                    {
                        shipsByRole[role] = roleShips = new Array<Ship>();
                    }
                    roleShips.Add(ship);
                }

                var shipsByRoleArray = shipsByRole.ToArray();
                Array.Sort(keys:shipsByRole.Keys.ToArray(), shipsByRoleArray);

                foreach (var roleAndShips in shipsByRoleArray)
                {
                    string role = roleAndShips.Key;
                    DesignListItem group = AvailableDesignsList.AddItem(new DesignListItem(this, role));
                    foreach (Ship ship in roleAndShips.Value)
                        group.AddSubItem(new DesignListItem(this, ship));
                }

                if (WIPs.Count > 0)
                {
                    DesignListItem wip = AvailableDesignsList.AddItem(new DesignListItem(this, "WIP"));
                    foreach (ShipData wipHull in WIPs)
                        wip.AddSubItem(new DesignListItem(this, wipHull));
                }
            }
            else
            {
                foreach (Ship ship in ships)
                    AvailableDesignsList.AddItem(new DesignListItem(this, ship));
                foreach (ShipData wipHull in WIPs)
                    AvailableDesignsList.AddItem(new DesignListItem(this, wipHull));
            }
        }

        bool CanShowDesign(Ship ship)
        {
            if (ShowOnlyPlayerDesigns && !ship.IsPlayerDesign)
                return false;

            if (UnlockAllDesigns)
                return true;

            string role = Localizer.GetRole(ship.DesignRole, EmpireManager.Player);
            return !ship.Deleted
                && !ship.shipData.IsShipyard
                && EmpireManager.Player.WeCanBuildThis(ship.Name)
                && (role.IsEmpty() || role == Localizer.GetRole(ship.DesignRole, EmpireManager.Player))
                && (Empire.Universe?.Debug == true || !ship.IsSubspaceProjector)
                && !ResourceManager.ShipRoles[ship.shipData.Role].Protected;
        }

        void LoadShipToScreen()
        {
            Screen.ChangeHull(SelectedShip?.shipData ?? SelectedWIP);                
            ExitScreen();
        }

        public class PlayerDesignToggleButton : ToggleButton
        {
            public PlayerDesignToggleButton(Vector2 pos) : base(pos, ToggleButtonStyle.PlayerDesigns, "SelectionBox/icon_grid")
            {
                IsToggled = true;
                Tooltip = GameText.ToggleToDisplayOnlyPlayerdesigned;
            }
        }
    }
}
