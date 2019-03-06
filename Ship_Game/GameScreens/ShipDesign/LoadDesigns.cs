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

        private bool ShowAllDesigns = true;        

        private readonly Ship_Game.ShipDesignScreen Screen;
        private Rectangle Window;
        private Vector2 TitlePosition;

        private Vector2 EnternamePos;
        private UITextEntry EnterNameArea = new UITextEntry();

        private Menu1 loadMenu;
        private Submenu SaveShips;
        private ScrollList ShipDesigns;

        public string ShipToDelete = "";

        private Selector selector;
        private ShipData selectedWIP;

        private Array<UIButton> ShipsToLoad = new Array<UIButton>();

        public LoadDesigns(Ship_Game.ShipDesignScreen screen) : base(screen)
        {
            Screen            = screen;
            IsPopup           = true;
            TransitionOnTime  = 0.25f;
            TransitionOffTime = 0.25f;
        }
        
        private void DeleteAccepted(object sender, EventArgs e)
        {            
            GameAudio.EchoAffirmative();
            ResourceManager.ShipsDict[ShipToDelete].Deleted = true;
            ShipsToLoad.Clear();
            ShipDesigns.Reset();
            ResourceManager.DeleteShip(ShipToDelete);
            LoadContent();
        }

        private void DeleteDataAccepted(object sender, EventArgs e)
        {
            GameAudio.EchoAffirmative();
            ShipsToLoad.Clear();
            ShipDesigns.Reset();
            ResourceManager.DeleteShip(ShipToDelete);
            LoadContent();
        }

        public override void Draw(SpriteBatch batch)
        {
            GameTime gameTime = StarDriveGame.Instance.GameTime;
            
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
            batch.Begin();            
            loadMenu.Draw();
            SaveShips.Draw(batch);
            ShipDesigns.Draw(batch);
            EnterNameArea.Draw(Fonts.Arial20Bold, batch, EnternamePos, gameTime, (EnterNameArea.Hover ? Color.White : new Color(255, 239, 208)));
            
            foreach (ScrollList.Entry e in ShipDesigns.VisibleExpandedEntries)
            {
                var bCursor = new Vector2(SaveShips.Menu.X + 20, SaveShips.Menu.Y + 20);
                if (e.item == null)
                    continue;
                bCursor.Y = e.Y;
                if (e.item is ModuleHeader header)
                {
                    header.Draw(ScreenManager, bCursor);
                }
                else if (e.item is Ship ship)
                {
                    bCursor.X = bCursor.X + 15f;
                    batch.Draw(ship.shipData.Icon, new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);

                    var tCursor = new Vector2(bCursor.X + 40f, bCursor.Y + 3f);
                    batch.DrawString(Fonts.Arial12Bold, ship.Name, tCursor, Color.White);
                    tCursor.Y = tCursor.Y + Fonts.Arial12Bold.LineSpacing;
                    var role = ship.BaseHull.Name;
                    batch.DrawString(Fonts.Arial8Bold, role, tCursor, Color.DarkGray);
                    tCursor.X = tCursor.X + Fonts.Arial8Bold.MeasureString(role).X + 8;
                    batch.DrawString(Fonts.Arial8Bold, $"Base Strength: {ship.BaseStrength.String(0)}", tCursor, Color.Orange);
                    
                    if (!ship.IsReadonlyDesign && !ship.FromSave)
                    {
                        e.DrawCancel(batch, Input);
                    }
                }
                else if (e.item is ShipData shipData)
                {
                    bCursor.X = bCursor.X + 15f;                    
                    batch.Draw(shipData.Icon, new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);
                    var tCursor = new Vector2(bCursor.X + 40f, bCursor.Y + 3f);
                    batch.DrawString(Fonts.Arial12Bold, shipData.Name, tCursor, Color.White);
                    tCursor.Y += Fonts.Arial12Bold.LineSpacing;
                    batch.DrawString(Fonts.Arial8Bold, Localizer.GetRole(shipData.Role, EmpireManager.Player), tCursor, Color.Orange);

                    e.DrawCancel(batch, Input);
                }
            }
            selector?.Draw(batch);
            base.Draw(batch);
            PlayerDesignsToggle.Draw(ScreenManager);
            ToolTip.Draw(batch);
            batch.End();
        }

        public override bool HandleInput(InputState input)
        {
            ShipDesigns.HandleInput(input);
            if (input.Escaped || input.RightMouseClick)
            {
                ExitScreen();
                return true;
            }
            selector = null;
            foreach (ScrollList.Entry e in ShipDesigns.VisibleExpandedEntries)
            {
                if (e.item is ModuleHeader header)
                {
                    if (header.HandleInput(input, e))
                        break;
                }
                else if (e.item is ShipData data)
                {
                    if (e.CheckHover(MousePos))
                    {
                        if (e.WasCancelHovered(input) && input.InGameSelect)
                        {
                            ShipToDelete = data.Name;
                            var messageBox = new MessageBoxScreen(this, "Confirm Delete:");
                            messageBox.Accepted += DeleteDataAccepted;
                            ScreenManager.AddScreen(messageBox);
                        }
                        selector = e.CreateSelector();

                        if (input.LeftMouseClick)
                        {
                            EnterNameArea.Text = data.Name;
                            selectedWIP = data;
                            GameAudio.AcceptClick();
                        }
                    }
                }
                else if (e.CheckHover(MousePos))
                {
                    if (e.item is Ship ship)
                    {
                        if (e.WasCancelHovered(input) && !ship.IsReadonlyDesign && !ship.FromSave && input.InGameSelect)
                        {
                            ShipToDelete = ship.Name;
                            var messageBox = new MessageBoxScreen(this, "Confirm Delete:");
                            messageBox.Accepted += DeleteAccepted;
                            ScreenManager.AddScreen(messageBox);
                        }
                        selector = e.CreateSelector();

                        if (input.LeftMouseClick)
                        {
                            EnterNameArea.Text = ship.Name;
                            GameAudio.AcceptClick();
                        }
                    }
                }
            }
            return base.HandleInput(input);
        }

        public override void LoadContent()
        {
            Window              = new Rectangle(ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 250, ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - 300, 500, 600);
            loadMenu            = new Menu1(Window);
            Rectangle sub       = new Rectangle(Window.X + 20, Window.Y + 60, Window.Width - 40, Window.Height - 80);
            SaveShips           = new Submenu(sub);
            SaveShips.AddTab(Localizer.Token(198));
            ShipDesigns         = new ScrollList(SaveShips);
            TitlePosition       = new Vector2(Window.X + 20, Window.Y + 20);
            PlayerDesignsToggle = Add(new PlayerDesignToggleButton(new Vector2(SaveShips.Menu.X + SaveShips.Menu.Width - 44, SaveShips.Menu.Y)));
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

        private void PopulateEntries()
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

            if (Screen != null)
            {
                var shipRoles = new Array<string>();

                foreach (KeyValuePair<string, Ship> Ship in ResourceManager.ShipsDict)
                {
                    //added by gremlin HIDING ERRORS
                    try
                    {
                        if (!ShowAllDesigns && !ResourceManager.ShipsDict[Ship.Key].IsPlayerDesign) continue;

                        if (!EmpireManager.Player.WeCanBuildThis(Ship.Key) || shipRoles.Contains(Localizer.GetRole(
                                Ship.Value.DesignRole
                                , EmpireManager.Player)) || Empire.Universe?.Debug != true && ResourceManager.ShipRoles[Ship.Value.shipData.Role].Protected)
                        {
                            Log.Info($"Ship Design exluded by filter {Ship.Key}");
                            continue;
                        }
                        shipRoles.Add(Localizer.GetRole(Ship.Value.DesignRole, EmpireManager.Player));
                        ModuleHeader mh = new ModuleHeader(Localizer.GetRole(Ship.Value.DesignRole, EmpireManager.Player));
                        ShipDesigns.AddItem(mh);
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
                    ShipDesigns.AddItem(mh);
                }

                KeyValuePair<string, Ship>[] ships = ResourceManager.ShipsDict
                    .OrderBy(kv => !kv.Value.IsPlayerDesign)
                    .ThenBy(kv => kv.Value.BaseHull.ShipStyle != EmpireManager.Player.data.Traits.ShipType)
                    .ThenBy(kv => kv.Value.BaseHull.ShipStyle)
                    .ThenByDescending(kv => kv.Value.BaseStrength)
                    .ThenBy(kv => kv.Value.Name)
                    .ToArray();

                foreach (ScrollList.Entry e in ShipDesigns.AllEntries)
                {
                    foreach (KeyValuePair<string, Ship> ship in ships)
                    {
                        if (ship.Value.Deleted 
                            || ship.Value.shipData.IsShipyard
                            || !EmpireManager.Player.WeCanBuildThis(ship.Key)
                            || Localizer.GetRole(ship.Value.DesignRole, EmpireManager.Player) != (e.item as ModuleHeader).Text
                            || (Empire.Universe?.Debug != true && ship.Value.Name == "Subspace Projector")
                            || ResourceManager.ShipRoles[ship.Value.shipData.Role].Protected)
                        {
                            continue;
                        }
                        if (ship.Value.IsReadonlyDesign || ship.Value.FromSave)
                        {
                            e.AddSubItem(ship.Value);
                        }
                        else
                        {
                            e.AddSubItem(ship.Value);
                        }
                    }
                    if ((e.item as ModuleHeader).Text != "WIP")
                    {
                        continue;
                    }
                    foreach (ShipData data in WIPs)
                    {
                        e.AddSubItem(data);
                    }
                }
            }
        }

        private void LoadShipToScreen()
        {
            Ship loadedShip = ResourceManager.GetShipTemplate(EnterNameArea.Text, false);
            loadedShip?.shipData.UpdateBaseHull();
            Screen.ChangeHull(loadedShip?.shipData ?? selectedWIP);                
            ExitScreen();
        }

        private void ResetSL()
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