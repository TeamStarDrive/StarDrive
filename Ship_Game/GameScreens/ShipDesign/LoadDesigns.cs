using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;
using Ship_Game.Ships;

namespace Ship_Game.GameScreens.ShipDesignScreen
{
    public sealed class LoadDesigns : GameScreen
    {
        public PlayerDesignToggleButton PlayerDesignsToggle;

        bool ShowAllDesigns = true;

        readonly Ship_Game.ShipDesignScreen Screen;

        UITextEntry EnterNameArea;
        ScrollList2<DesignListItem> AvailableDesignsList;
        ShipInfoOverlayComponent ShipInfoOverlay;

        public string ShipToDelete = "";

        ShipData selectedWIP;

        Array<UIButton> ShipsToLoad = new Array<UIButton>();

        public LoadDesigns(Ship_Game.ShipDesignScreen screen) : base(screen)
        {
            Screen            = screen;
            IsPopup           = true;
            TransitionOnTime  = 0.25f;
            TransitionOffTime = 0.25f;
        }

        class DesignListItem : ScrollListItem<DesignListItem>
        {
            readonly LoadDesigns Screen;
            public Ship Ship;
            public ShipData WipHull;
            
            public DesignListItem(LoadDesigns screen, string headerText) : base(headerText)
            {
                Screen = screen;
            }

            public DesignListItem(LoadDesigns screen, Ship ship)
            {
                Screen = screen;
                Ship = ship;
                if (!ship.IsReadonlyDesign && !ship.FromSave)
                    AddCancel(new Vector2(-30, 0), "Delete this Ship Design", 
                        () => PromptDeleteShip(Ship.Name, Screen.DeleteAccepted));
            }

            public DesignListItem(LoadDesigns screen, ShipData wipHull)
            {
                Screen = screen;
                WipHull = wipHull;
                AddCancel(new Vector2(-30, 0), "Delete this WIP Hull", 
                    () => PromptDeleteShip(WipHull.Name, Screen.DeleteDataAccepted));
            }
            
            void PromptDeleteShip(string shipId, Action onAccept)
            {
                Screen.ShipToDelete = shipId;
                Screen.ScreenManager.AddScreen(new MessageBoxScreen(Screen, $"Confirm Delete: {shipId}")
                {
                    Accepted = onAccept
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
            Rect = new Rectangle(ScreenWidth / 2 - 250, ScreenHeight / 2 - 300, 500, 600);
            var background = new Submenu(X + 20, Y + 60, Width - 40, Height - 80);
            background.Background = new Menu1(Rect);
            background.AddTab(Localizer.Token(198));

            AvailableDesignsList = Add(new ScrollList2<DesignListItem>(background));
            AvailableDesignsList.EnableItemHighlight = true;
            AvailableDesignsList.OnClick = OnDesignListItemClicked;

            PlayerDesignsToggle = Add(new PlayerDesignToggleButton(new Vector2(background.Right - 44, background.Y)));
            PlayerDesignsToggle.OnClick = p =>
            {
                GameAudio.AcceptClick();
                ShowAllDesigns = !ShowAllDesigns;
                PlayerDesignsToggle.IsToggled = ShowAllDesigns;
                ResetSL();
            };
            
            PopulateEntries();
            EnterNameArea = Add(new UITextEntry(new Vector2(X + 20, Y + 20), Localizer.Token(199)));
            ButtonSmall(background.Right - 88, EnterNameArea.Y - 2, text:8, click: b =>
            {
                LoadShipToScreen();
            });

            ShipInfoOverlay = Add(new ShipInfoOverlayComponent(this));
            AvailableDesignsList.OnHovered = (item) =>
            {
                ShipInfoOverlay.ShowToLeftOf(item?.Pos ?? Vector2.Zero, item?.Ship);
            };

            base.LoadContent();
        }

        void OnDesignListItemClicked(DesignListItem item)
        {
            if (item.WipHull != null)
            {
                EnterNameArea.Text = item.WipHull.Name;
                selectedWIP = item.WipHull;
            }
            else if (item.Ship != null)
            {
                EnterNameArea.Text = item.Ship.Name;
            }
        }

        void DeleteAccepted()
        {            
            GameAudio.EchoAffirmative();
            ResourceManager.ShipsDict[ShipToDelete].Deleted = true;
            ShipsToLoad.Clear();
            AvailableDesignsList.Reset();
            ResourceManager.DeleteShip(ShipToDelete);
            LoadContent();
        }

        void DeleteDataAccepted()
        {
            GameAudio.EchoAffirmative();
            ShipsToLoad.Clear();
            AvailableDesignsList.Reset();
            ResourceManager.DeleteShip(ShipToDelete);
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

        void PopulateEntries()
        {
            var WIPs = new Array<ShipData>();
            foreach (FileInfo info in Dir.GetFiles(Dir.StarDriveAppData + "/WIP"))
            {
                ShipData newShipData = ShipData.Parse(info);
                var empire = EmpireManager.Player;
                if (empire.IsHullUnlocked(newShipData.Hull))
                {
                    WIPs.Add(newShipData);
                }
            }

            if (Screen == null) return;

            var shipRoles = new Array<string>();

            foreach (KeyValuePair<string, Ship> Ship in ResourceManager.ShipsDict)
            {
                //added by gremlin HIDING ERRORS
                try
                {
                    if (!ShowAllDesigns && !ResourceManager.ShipsDict[Ship.Key].IsPlayerDesign)
                        continue;

                    if (!EmpireManager.Player.WeCanBuildThis(Ship.Key) ||
                        shipRoles.Contains(Localizer.GetRole(Ship.Value.DesignRole, EmpireManager.Player)) || 
                        Empire.Universe?.Debug != true &&
                        ResourceManager.ShipRoles[Ship.Value.shipData.Role].Protected)
                    {
                        Log.Info($"Ship Design excluded by filter {Ship.Key}");
                        continue;
                    }

                    string headerText = Localizer.GetRole(Ship.Value.DesignRole, EmpireManager.Player);
                    shipRoles.Add(headerText);
                    AvailableDesignsList.AddItem(new DesignListItem(this, headerText));
                }
                catch
                {
                    Log.Warning($"Failed to load ship design {Ship.Key}");
                }
            }

            if (WIPs.Count > 0)
            {
                shipRoles.Add("WIP");
                AvailableDesignsList.AddItem(new DesignListItem(this, "WIP"));
            }

            KeyValuePair<string, Ship>[] ships = ResourceManager.ShipsDict
                .OrderBy(kv => !kv.Value.IsPlayerDesign)
                .ThenBy(kv => kv.Value.BaseHull.ShipStyle != EmpireManager.Player.data.Traits.ShipType)
                .ThenBy(kv => kv.Value.BaseHull.ShipStyle)
                .ThenByDescending(kv => kv.Value.BaseStrength)
                .ThenBy(kv => kv.Value.Name)
                .ToArray();

            foreach (DesignListItem headerItem in AvailableDesignsList.AllEntries.ToArray())
            {
                foreach (KeyValuePair<string, Ship> ship in ships)
                {
                    if (!ship.Value.Deleted
                        && !ship.Value.shipData.IsShipyard
                        && EmpireManager.Player.WeCanBuildThis(ship.Key)
                        && Localizer.GetRole(ship.Value.DesignRole, EmpireManager.Player) == headerItem.HeaderText
                        && (Empire.Universe?.Debug == true || !ship.Value.IsSubspaceProjector)
                        && !ResourceManager.ShipRoles[ship.Value.shipData.Role].Protected)
                    {
                        headerItem.AddSubItem(new DesignListItem(this, ship.Value));
                    }
                }

                if (headerItem.HeaderText == "WIP")
                {
                    foreach (ShipData wipHull in WIPs)
                    {
                        headerItem.AddSubItem(new DesignListItem(this, wipHull));
                    }
                }
            }
        }

        void LoadShipToScreen()
        {
            Ship loadedShip = ResourceManager.GetShipTemplate(EnterNameArea.Text, false);
            loadedShip?.shipData.UpdateBaseHull();
            Screen.ChangeHull(loadedShip?.shipData ?? selectedWIP);                
            ExitScreen();
        }

        void ResetSL()
        {
            AvailableDesignsList.Reset();
            PopulateEntries();            
        }

        public class PlayerDesignToggleButton : ToggleButton
        {
            public PlayerDesignToggleButton(Vector2 pos) : base(pos, ToggleButtonStyle.PlayerDesigns, "SelectionBox/icon_grid")
            {
                IsToggled = true;
                Tooltip = 237;
            }
        }
    }
}