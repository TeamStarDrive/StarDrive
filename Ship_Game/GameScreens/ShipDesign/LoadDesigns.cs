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
        Rectangle Window;
        Vector2 TitlePosition;

        Vector2 EnternamePos;
        UITextEntry EnterNameArea = new UITextEntry();

        Menu1 loadMenu;
        Submenu SaveShips;
        ScrollList<DesignListItem> ShipDesigns;

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

        class DesignListItem : ScrollList<DesignListItem>.Entry
        {
            public ModuleHeader Header;
            public Ship Ship;
            public ShipData Hull;

            public override bool HandleInput(InputState input)
            {
                return base.HandleInput(input);
            }

            public override void Draw(SpriteBatch batch)
            {
                if (Header != null)
                {
                    Header.Pos = Pos;
                    Header.Draw(batch);
                }
                else if (Ship != null)
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
                    
                    if (!Ship.IsReadonlyDesign && !Ship.FromSave)
                    {
                        base.DrawCancel(batch);
                    }
                }
                else if (Hull != null)
                {
                    var bCursor = new Vector2(X + 35f, Y);                 
                    batch.Draw(Hull.Icon, new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);

                    var tCursor = new Vector2(bCursor.X + 40f, bCursor.Y + 3f);
                    batch.DrawString(Fonts.Arial12Bold, Hull.Name, tCursor, Color.White);
                    tCursor.Y += Fonts.Arial12Bold.LineSpacing;
                    batch.DrawString(Fonts.Arial8Bold, Localizer.GetRole(Hull.Role, EmpireManager.Player), tCursor, Color.Orange);

                    base.DrawCancel(batch);
                }
            }
        }

        public override void LoadContent()
        {
            Window              = new Rectangle(ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 250, ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - 300, 500, 600);
            loadMenu            = new Menu1(Window);
            Rectangle sub       = new Rectangle(Window.X + 20, Window.Y + 60, Window.Width - 40, Window.Height - 80);
            SaveShips           = new Submenu(sub);
            SaveShips.AddTab(Localizer.Token(198));
            ShipDesigns = new ScrollList<DesignListItem>(SaveShips);
            ShipDesigns.OnClick = OnDesignListItemClicked;
            TitlePosition       = new Vector2(Window.X + 20, Window.Y + 20);
            PlayerDesignsToggle = Add(new PlayerDesignToggleButton(new Vector2(SaveShips.Right - 44, SaveShips.Y)));
            PlayerDesignsToggle.OnClick += p =>
            {
                GameAudio.AcceptClick();
                ShowAllDesigns = !ShowAllDesigns;
                PlayerDesignsToggle.Active = ShowAllDesigns;
                ResetSL();
            };

            PopulateEntries();
            EnternamePos = TitlePosition;
            EnterNameArea.Text = Localizer.Token(199);
            ButtonSmall(sub.X + sub.Width - 88, EnternamePos.Y - 2, titleId:8, click: b =>
            {
                LoadShipToScreen();
            });

            base.LoadContent();
        }

        void OnDesignListItemClicked(DesignListItem item)
        {
            if (item.CancelHover)
            {
                if (item.Hull != null)
                {
                    ShipToDelete = item.Hull.Name;
                    var messageBox = new MessageBoxScreen(this, "Confirm Delete:");
                    messageBox.Accepted += DeleteDataAccepted;
                    ScreenManager.AddScreen(messageBox);
                }
                else if (item.Ship != null && !item.Ship.IsReadonlyDesign && !item.Ship.FromSave)
                {
                    ShipToDelete = item.Ship.Name;
                    var messageBox = new MessageBoxScreen(this, "Confirm Delete:");
                    messageBox.Accepted += DeleteAccepted;
                    ScreenManager.AddScreen(messageBox);
                }
            }
            else
            {
                if (item.Hull != null)
                {
                    EnterNameArea.Text = item.Hull.Name;
                    selectedWIP = item.Hull;
                }
                else if (item.Ship != null)
                {
                    EnterNameArea.Text = item.Ship.Name;
                }
            }
        }

        public override bool HandleInput(InputState input)
        {
            if (input.Escaped || input.RightMouseClick)
            {
                ExitScreen();
                return true;
            }

            if (ShipDesigns.HandleInput(input))
                return true;

            return base.HandleInput(input);
        }
        
        void DeleteAccepted(object sender, EventArgs e)
        {            
            GameAudio.EchoAffirmative();
            ResourceManager.ShipsDict[ShipToDelete].Deleted = true;
            ShipsToLoad.Clear();
            ShipDesigns.Reset();
            ResourceManager.DeleteShip(ShipToDelete);
            LoadContent();
        }

        void DeleteDataAccepted(object sender, EventArgs e)
        {
            GameAudio.EchoAffirmative();
            ShipsToLoad.Clear();
            ShipDesigns.Reset();
            ResourceManager.DeleteShip(ShipToDelete);
            LoadContent();
        }

        public override void Draw(SpriteBatch batch)
        {
            GameTime gameTime = GameBase.Base.GameTime;
            
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
            batch.Begin();            
            loadMenu.Draw();
            SaveShips.Draw(batch);
            ShipDesigns.Draw(batch);
            EnterNameArea.Draw(Fonts.Arial20Bold, batch, EnternamePos, gameTime, (EnterNameArea.Hover ? Color.White : new Color(255, 239, 208)));
            base.Draw(batch);
            PlayerDesignsToggle.Draw(ScreenManager);
            ToolTip.Draw(batch);
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
                    shipRoles.Add(Localizer.GetRole(Ship.Value.DesignRole, EmpireManager.Player));
                    var mh = new ModuleHeader(Localizer.GetRole(Ship.Value.DesignRole, EmpireManager.Player));
                    ShipDesigns.AddItem(new DesignListItem{ Header = mh });
                }
                catch
                {
                    Log.Warning($"Failed to load ship design {Ship.Key}");
                }
            }

            if (WIPs.Count > 0)
            {
                shipRoles.Add("WIP");
                var mh = new ModuleHeader("WIP");
                ShipDesigns.AddItem(new DesignListItem{ Header = mh });
            }

            KeyValuePair<string, Ship>[] ships = ResourceManager.ShipsDict
                .OrderBy(kv => !kv.Value.IsPlayerDesign)
                .ThenBy(kv => kv.Value.BaseHull.ShipStyle != EmpireManager.Player.data.Traits.ShipType)
                .ThenBy(kv => kv.Value.BaseHull.ShipStyle)
                .ThenByDescending(kv => kv.Value.BaseStrength)
                .ThenBy(kv => kv.Value.Name)
                .ToArray();

            foreach (DesignListItem headerItem in ShipDesigns.AllEntries.ToArray())
            {
                foreach (KeyValuePair<string, Ship> ship in ships)
                {
                    if (!ship.Value.Deleted
                        && !ship.Value.shipData.IsShipyard
                        && EmpireManager.Player.WeCanBuildThis(ship.Key)
                        && Localizer.GetRole(ship.Value.DesignRole, EmpireManager.Player) == headerItem.Header.Text
                        && (Empire.Universe?.Debug == true || !ship.Value.IsSubspaceProjector)
                        && !ResourceManager.ShipRoles[ship.Value.shipData.Role].Protected)
                    {
                        headerItem.AddSubItem(new DesignListItem{ Ship = ship.Value });
                    }
                }

                if (headerItem.Header.Text == "WIP")
                {
                    foreach (ShipData wipHull in WIPs)
                    {
                        headerItem.AddSubItem(new DesignListItem{ Hull = wipHull });
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
            ShipDesigns.Reset();
            PopulateEntries();            
        }

        public class PlayerDesignToggleButton : ToggleButton
        {
            public PlayerDesignToggleButton(Vector2 pos) : base(pos, ToggleButtonStyle.PlayerDesigns, "SelectionBox/icon_grid")
            {
                Active = true;
                WhichToolTip = 237;
            }
        }
    }
}