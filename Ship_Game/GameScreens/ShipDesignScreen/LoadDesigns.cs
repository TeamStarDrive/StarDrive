using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Ships;

namespace Ship_Game.GameScreens.ShipDesignScreen
{
    public sealed class LoadDesigns : GameScreen
    {
        private Vector2 Cursor = Vector2.Zero;

        public PlayerDesignToggleButton PlayerDesignsToggle;

        private bool ShowAllDesigns = true;        

        private readonly GameScreen Screen;

        private Rectangle Window;

        private Vector2 TitlePosition;

        private Vector2 EnternamePos;
         
        private UITextEntry EnterNameArea = new UITextEntry();

        //private UIButton Save;

        private UIButton Load;

        //private UIButton Options;

        //private UIButton Exit;

        private Menu1 loadMenu;

        private Submenu SaveShips;

        private ScrollList ShipDesigns;

        public string ShipToDelete = "";

        private Selector selector;

        private ShipData selectedWIP;

        //private bool FirstRun = true;

        private Array<UIButton> ShipsToLoad      = new Array<UIButton>();
        private readonly Texture2D Delete_Hover2 = ResourceManager.Texture("NewUI/icon_queue_delete_hover2");
        private readonly Texture2D DeleteHover1  = ResourceManager.Texture("NewUI/icon_queue_delete_hover1");
        private readonly Texture2D QueueDelete   = ResourceManager.Texture("NewUI/icon_queue_delete");

        public LoadDesigns(Ship_Game.ShipDesignScreen screen) : base(screen)
        {
            Screen            = screen;
            IsPopup           = true;
            TransitionOnTime  = TimeSpan.FromSeconds(0.25);
            TransitionOffTime = TimeSpan.FromSeconds(0.25);
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
            GameTime gameTime = Game1.Instance.GameTime;
            
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
            batch.Begin();            
            loadMenu.Draw();
            SaveShips.Draw();
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
                    var role = Localizer.GetRole(ship.shipData.HullRole, EmpireManager.Player);
                    batch.DrawString(Fonts.Arial8Bold, role, tCursor, Color.Orange);
                    tCursor.X = tCursor.X + Fonts.Arial8Bold.MeasureString(role).X + 8;
                    ship.GetTechScore(out int[] scores);
                    batch.DrawString(Fonts.Arial8Bold, $"Off: {scores[2]} Def: {scores[0]} Pwr: {Math.Max(scores[1], scores[3])}", tCursor, Color.Orange);
                    
                    if (!ship.IsReadonlyDesign && !ship.FromSave)
                    {
                        e.DrawCancel(batch, Input);
                    }
                }
                else if (e.item is ShipData shipData)
                {
                    bCursor.X = bCursor.X + 15f;                    
                    batch.Draw(ResourceManager.HullsDict[shipData.Hull].Icon, new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);
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
                            GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
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
                            GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
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
            string path         = Dir.ApplicationData;
            PlayerDesignsToggle = Add(new PlayerDesignToggleButton(new Vector2(SaveShips.Menu.X + SaveShips.Menu.Width - 44, SaveShips.Menu.Y)));
            PlayerDesignsToggle.OnClick += p =>
            {
                GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                ShowAllDesigns = !ShowAllDesigns;
                PlayerDesignsToggle.Active = ShowAllDesigns;
                ResetSL();
            };

            PopulateEntries(path);
            EnternamePos = TitlePosition;
            EnterNameArea.Text = Localizer.Token(199);
            Load = ButtonSmall(sub.X + sub.Width - 88, EnternamePos.Y - 2, titleId:8, click: b =>
            {
                LoadShipToScreen();
            });

            base.LoadContent();
        }

        private void PopulateEntries(string path)
        {
            Array<ShipData> WIPs = new Array<ShipData>();
            foreach (FileInfo info in Dir.GetFiles(path + "/StarDrive/WIP"))
            {
                ShipData newShipData = ShipData.Parse(info);
                var empire = EmpireManager.Player;
                if (empire.IsHullUnlocked(newShipData.Hull))
                {
                    WIPs.Add(newShipData);
                }
            }
            Array<string> ShipRoles = new Array<string>();
            if (Screen != null)
            {
                foreach (KeyValuePair<string, Ship> Ship in ResourceManager.ShipsDict)
                {
                    //added by gremlin HIDING ERRORS
                    try
                    {
                        if (!ShowAllDesigns && !ResourceManager.ShipsDict[Ship.Key].IsPlayerDesign) continue;

                        if (!EmpireManager.Player.WeCanBuildThis(Ship.Key) || ShipRoles.Contains(Localizer.GetRole(
                                Ship.Value.DesignRole
                                , EmpireManager.Player)) || Empire.Universe?.Debug != true && ResourceManager.ShipRoles[Ship.Value.shipData.Role].Protected)
                        {
                            Log.Info($"Ship Design exluded by filter {Ship.Key}");
                            continue;
                        }
                        ShipRoles.Add(Localizer.GetRole(Ship.Value.DesignRole, EmpireManager.Player));
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
                    ShipRoles.Add("WIP");
                    var mh = new ModuleHeader("WIP");
                    ShipDesigns.AddItem(mh);
                }

                KeyValuePair<string, Ship>[] ships = ResourceManager.ShipsDict
                    .OrderBy(kv => !kv.Value.IsPlayerDesign)
                    .ThenBy(kv => kv.Value.BaseHull.ShipStyle != EmpireManager.Player.data.Traits.ShipType)
                    .ThenBy(kv => kv.Value.BaseHull.ShipStyle)
                    .ThenByDescending(kv => kv.Value.GetTechScore(out int[] _))
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
            if (Screen is Ship_Game.ShipDesignScreen shipDesignScreen)                            
                shipDesignScreen.ChangeHull(loadedShip?.shipData ?? selectedWIP);                
            
            ExitScreen();
        }

        private string parseText(string text, float Width)
        {
            string line = string.Empty;
            string returnString = string.Empty;
            string[] strArrays = text.Split(' ');
            for (int i = 0; i < strArrays.Length; i++)
            {
                string word = strArrays[i];
                if (Fonts.Arial12Bold.MeasureString(string.Concat(line, word)).Length() > Width)
                {
                    returnString = string.Concat(returnString, line, '\n');
                    line = string.Empty;
                }
                line = string.Concat(line, word, ' ');
            }
            return string.Concat(returnString, line);
        }

        private void ResetSL()
        {
            ShipDesigns.Reset();
            string path = Dir.ApplicationData;
            PopulateEntries(path);            
        }

        public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
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