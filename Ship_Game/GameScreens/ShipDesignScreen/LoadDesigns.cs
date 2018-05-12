using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.Ships;
using Ship_Game.UI;

namespace Ship_Game.GameScreens.ShipDesignScreen
{
    public sealed class LoadDesigns : GameScreen
    {
        private Vector2 Cursor = Vector2.Zero;

        public PlayerDesignToggleButton PlayerDesignsToggle;

        private bool ShowAllDesigns = true;        

        private readonly GameScreen Screen;

        private Rectangle Window = new Rectangle();

        private Vector2 TitlePosition = new Vector2();

        private Vector2 EnternamePos = new Vector2();
         
        private UITextEntry EnterNameArea = new UITextEntry();

        //private UIButton Save;

        private UIButton Load;

        //private UIButton Options;

        //private UIButton Exit;

        private Menu1 loadMenu;

        private Submenu SaveShips;

        private ScrollList ShipDesigns;

        private MouseState currentMouse;

        private MouseState previousMouse;

        public string ShipToDelete = "";

        private Selector selector;

        private ShipData selectedWIP;

        //private bool FirstRun = true;

        private Array<UIButton> ShipsToLoad      = new Array<UIButton>();
        private readonly Texture2D Delete_Hover2 = ResourceManager.TextureDict["NewUI/icon_queue_delete_hover2"];
        private readonly Texture2D DeleteHover1  = ResourceManager.TextureDict["NewUI/icon_queue_delete_hover1"];
        private readonly Texture2D QueueDelete   = ResourceManager.TextureDict["NewUI/icon_queue_delete"];

        public LoadDesigns(Ship_Game.ShipDesignScreen screen) : base(screen)
        {
            this.Screen            = screen;
            base.IsPopup           = true;
            base.TransitionOnTime  = TimeSpan.FromSeconds(0.25);
            base.TransitionOffTime = TimeSpan.FromSeconds(0.25);
        }
        
        private void DeleteAccepted(object sender, EventArgs e)
        {            
            GameAudio.EchoAffirmative();
            ResourceManager.ShipsDict[this.ShipToDelete].Deleted = true;
            this.Buttons.Clear();
            this.ShipsToLoad.Clear();
            this.ShipDesigns.Reset();
            ResourceManager.DeleteShip(this.ShipToDelete);
            this.LoadContent();
        }

        private void DeleteDataAccepted(object sender, EventArgs e)
        {
            GameAudio.EchoAffirmative();
            this.Buttons.Clear();
            this.ShipsToLoad.Clear();
            this.ShipDesigns.Reset();
            ResourceManager.DeleteShip(this.ShipToDelete);
            this.LoadContent();
        }

        protected override void Destroy()
        {
            ShipDesigns?.Dispose(ref ShipDesigns);
            base.Destroy();
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            GameTime gameTime = Game1.Instance.GameTime;
            
            ScreenManager.FadeBackBufferToBlack(base.TransitionAlpha * 2 / 3);
            ScreenManager.SpriteBatch.Begin();            
            loadMenu.Draw();
            SaveShips.Draw();
            ShipDesigns.Draw(base.ScreenManager.SpriteBatch);
            EnterNameArea.Draw(Fonts.Arial20Bold, base.ScreenManager.SpriteBatch, this.EnternamePos, gameTime, (this.EnterNameArea.Hover ? Color.White : new Color(255, 239, 208)));
            Vector2 bCursor = new Vector2((float)(this.SaveShips.Menu.X + 20), (float)(this.SaveShips.Menu.Y + 20));
            
            for (int i = this.ShipDesigns.indexAtTop; i < this.ShipDesigns.Copied.Count && i < this.ShipDesigns.indexAtTop + this.ShipDesigns.entriesToDisplay; i++)
            {
                bCursor = new Vector2((float)(this.SaveShips.Menu.X + 20), (float)(this.SaveShips.Menu.Y + 20));
                ScrollList.Entry e = this.ShipDesigns.Copied[i];
                if (e.item == null)
                    continue;
                bCursor.Y = (float)e.clickRect.Y;
                if (e.item is ModuleHeader header)
                {
                    header.Draw(base.ScreenManager, bCursor);
                }
                else if (e.item is Ship ship)
                {
                    bCursor.X = bCursor.X + 15f;
                    try
                    {
                        base.ScreenManager.SpriteBatch.Draw(ship.shipData.Icon, new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);
                    }
                    catch(KeyNotFoundException error)
                    {
                        error.Data.Add("key= ", e.item);
                    }
                    Vector2 tCursor = new Vector2(bCursor.X + 40f, bCursor.Y + 3f);
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, ship.Name, tCursor, Color.White);
                    tCursor.Y = tCursor.Y + Fonts.Arial12Bold.LineSpacing;
                    var role = Localizer.GetRole(ship.shipData.HullRole, EmpireManager.Player);
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, role, tCursor, Color.Orange);
                    tCursor.X = tCursor.X + Fonts.Arial8Bold.MeasureString(role).X + 8;
                    ship.GetTechScore(out int[] scores);
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, $"" +
                                                                                $"Off: {scores[2]} Def: {scores[0]} Pwr: {Math.Max(scores[1], scores[3])}", tCursor, Color.Orange);
                    if (e.clickRectHover == 1 && !ship.IsReadonlyDesign && !ship.FromSave)
                    {
                        ScreenManager.SpriteBatch.Draw(DeleteHover1, e.cancel, Color.White);
                        if (e.cancel.HitTest(new Vector2((float)Mouse.GetState().X, (float)Mouse.GetState().Y)))
                        {
                            ScreenManager.SpriteBatch.Draw(Delete_Hover2, e.cancel, Color.White);
                            ToolTip.CreateTooltip(78);
                        }
                    }
                    else if (!ship.IsReadonlyDesign && !ship.FromSave)
                    {
                        base.ScreenManager.SpriteBatch.Draw(QueueDelete, e.cancel, Color.White);
                    }
                }
                else if(e.item is ShipData shipData)
                {
                    bCursor.X = bCursor.X + 15f;                    
                    base.ScreenManager.SpriteBatch.Draw(ResourceManager.HullsDict[shipData.Hull].Icon, new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);
                    Vector2 tCursor = new Vector2(bCursor.X + 40f, bCursor.Y + 3f);
                    base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, shipData.Name, tCursor, Color.White);
                    tCursor.Y = tCursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, Localizer.GetRole(shipData.Role, EmpireManager.Player), tCursor, Color.Orange);
                    if (e.clickRectHover != 1)
                    {
                        base.ScreenManager.SpriteBatch.Draw(QueueDelete, e.cancel, Color.White);
                    }
                    else
                    {
                        base.ScreenManager.SpriteBatch.Draw(DeleteHover1, e.cancel, Color.White);
                        if (e.cancel.HitTest(new Vector2((float)Mouse.GetState().X, (float)Mouse.GetState().Y)))
                        {
                            base.ScreenManager.SpriteBatch.Draw(Delete_Hover2, e.cancel, Color.White);
                            ToolTip.CreateTooltip(78);
                        }
                    }
                }
            }
            selector?.Draw(ScreenManager.SpriteBatch);
            foreach (UIButton b in this.Buttons)
            {
                b.Draw(base.ScreenManager.SpriteBatch);
            }
            this.PlayerDesignsToggle.Draw(base.ScreenManager);
            ToolTip.Draw(spriteBatch);
            base.ScreenManager.SpriteBatch.End();
        }

        public override bool HandleInput(InputState input)
        {
            this.currentMouse = input.MouseCurr;
            Vector2 MousePos = new Vector2((float)this.currentMouse.X, (float)this.currentMouse.Y);
            this.ShipDesigns.HandleInput(input);
            if (input.Escaped || input.RightMouseClick)
            {
                this.ExitScreen();
                return true;
            }
            foreach (UIButton b in this.Buttons)
            {
                if (!b.Rect.HitTest(MousePos))
                {
                    b.State = UIButton.PressState.Default;
                }
                else
                {
                    b.State = UIButton.PressState.Hover;
                    if (this.currentMouse.LeftButton == ButtonState.Pressed && this.previousMouse.LeftButton == ButtonState.Pressed)
                    {
                        b.State = UIButton.PressState.Pressed;
                    }
                    if (this.currentMouse.LeftButton != ButtonState.Released || this.previousMouse.LeftButton != ButtonState.Pressed)
                    {
                        continue;
                    }
                    string launches = b.Launches;
                    if (launches == null || !(launches == "Load"))
                    {
                        continue;
                    }
                    this.LoadShipToScreen();
                }
            }
            this.selector = null;
            for (int i = this.ShipDesigns.indexAtTop; i < this.ShipDesigns.Copied.Count && i < this.ShipDesigns.indexAtTop + this.ShipDesigns.entriesToDisplay; i++)
            {
                ScrollList.Entry e = this.ShipDesigns.Copied[i];
                if (e.item is ModuleHeader)
                {
                    (e.item as ModuleHeader).HandleInput(input, e);
                }
                else if (e.item is ShipData)
                {
                    if (!e.clickRect.HitTest(MousePos))
                    {
                        e.clickRectHover = 0;
                    }
                    else
                    {
                        if (e.cancel.HitTest(MousePos) && input.InGameSelect)
                        {
                            this.ShipToDelete = (e.item as ShipData).Name;
                            MessageBoxScreen messageBox = new MessageBoxScreen(this, "Confirm Delete:");
                            messageBox.Accepted += new EventHandler<EventArgs>(this.DeleteDataAccepted);
                            base.ScreenManager.AddScreen(messageBox);
                        }
                        this.selector = new Selector(e.clickRect);
                        if (e.clickRectHover == 0)
                        {
                            GameAudio.PlaySfxAsync("sd_ui_mouseover");
                        }
                        e.clickRectHover = 1;
                        if (input.MouseCurr.LeftButton == ButtonState.Pressed && input.MousePrev.LeftButton == ButtonState.Released)
                        {
                            this.EnterNameArea.Text = (e.item as ShipData).Name;
                            this.selectedWIP = e.item as ShipData;
                            GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                        }
                    }
                }
                else if (!e.clickRect.HitTest(MousePos))
                {
                    e.clickRectHover = 0;
                }
                else
                {
                    if (e.cancel.HitTest(MousePos) && !(e.item as Ship).IsReadonlyDesign && !(e.item as Ship).FromSave && input.InGameSelect)
                    {
                        this.ShipToDelete = (e.item as Ship).Name;
                        MessageBoxScreen messageBox = new MessageBoxScreen(this, "Confirm Delete:");
                        messageBox.Accepted += DeleteAccepted;
                        base.ScreenManager.AddScreen(messageBox);
                    }
                    this.selector = new Selector(e.clickRect);
                    if (e.clickRectHover == 0)
                    {
                        GameAudio.PlaySfxAsync("sd_ui_mouseover");
                    }
                    e.clickRectHover = 1;
                    if (input.MouseCurr.LeftButton == ButtonState.Pressed && input.MousePrev.LeftButton == ButtonState.Released)
                    {
                        this.EnterNameArea.Text = (e.item as Ship).Name;
                        GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                    }
                }
            }
            if (this.PlayerDesignsToggle.HandleInput(input))
            {
                GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                this.ShowAllDesigns = !this.ShowAllDesigns;
                this.PlayerDesignsToggle.Active = this.ShowAllDesigns;
                this.ResetSL();
            }
            //if (this.PlayerDesignsToggle.Rect.HitTest(input.CursorPosition))
            //{
            //    ToolTip.CreateTooltip(Localizer.Token(2225));
            //}
            this.previousMouse = input.MousePrev;
            return base.HandleInput(input);
        }

        public override void LoadContent()
        {
            Window              = new Rectangle(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 250, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - 300, 500, 600);
            loadMenu            = new Menu1(this.Window);
            Rectangle sub       = new Rectangle(this.Window.X + 20, this.Window.Y + 60, this.Window.Width - 40, this.Window.Height - 80);
            SaveShips           = new Submenu(sub);
            SaveShips.AddTab(Localizer.Token(198));
            ShipDesigns         = new ScrollList(this.SaveShips);
            Vector2 Cursor      = new Vector2((float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 84), (float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - 100));
            TitlePosition       = new Vector2((float)(this.Window.X + 20), (float)(this.Window.Y + 20));
            string path         = Dir.ApplicationData;
            Rectangle gridRect  = new Rectangle(this.SaveShips.Menu.X + this.SaveShips.Menu.Width - 44, this.SaveShips.Menu.Y, 29, 20);
            PlayerDesignsToggle = new PlayerDesignToggleButton(gridRect);
            
            PopulateEntries(path);
            EnternamePos = TitlePosition;
            EnterNameArea.Text = Localizer.Token(199);
            Load = ButtonSmall(sub.X + sub.Width - 88, EnternamePos.Y - 2, "Load", titleId:8);

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
            if (this.Screen != null)
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
                        this.ShipDesigns.AddItem(mh);
                    }
                    catch
                    {
                        Log.Error($"Failed to load ship design {Ship.Key}");
                    }
                }
                if (WIPs.Count > 0)
                {
                    ShipRoles.Add("WIP");
                    ModuleHeader mh = new ModuleHeader("WIP");
                    this.ShipDesigns.AddItem(mh);
                }
                foreach (ScrollList.Entry e in ShipDesigns.Entries)
                {
                    foreach (KeyValuePair<string, Ship> Ship in ResourceManager.ShipsDict
                        .OrderBy(x => true)
                        .ThenBy(player => !player.Value.IsPlayerDesign)
                        .ThenBy(empire => empire.Value.BaseHull.ShipStyle != EmpireManager.Player.data.Traits.ShipType)
                        .ThenBy(empire => empire.Value.BaseHull.ShipStyle)
                        .ThenByDescending(tech => tech.Value.GetTechScore(out int[] _))
                        .ThenBy(name => name.Value.Name))
                    {
                        if (Ship.Value.Deleted 
                            || Ship.Value.shipData.IsShipyard
                            || !EmpireManager.Player.WeCanBuildThis(Ship.Key)
                            || Localizer.GetRole(Ship.Value.DesignRole, EmpireManager.Player) != (e.item as ModuleHeader).Text
                            || (Empire.Universe?.Debug != true && Ship.Value.Name == "Subspace Projector")
                            || ResourceManager.ShipRoles[Ship.Value.shipData.Role].Protected)
                        {
                            continue;
                        }
                        if (Ship.Value.IsReadonlyDesign || Ship.Value.FromSave)
                        {
                            e.AddItem(Ship.Value);
                        }
                        else
                        {
                            e.AddItemWithCancel(Ship.Value);
                        }
                    }
                    if ((e.item as ModuleHeader).Text != "WIP")
                    {
                        continue;
                    }
                    foreach (ShipData data in WIPs)
                    {
                        e.AddItemWithCancel(data);
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
            string[] strArrays = text.Split(new char[] { ' ' });
            for (int i = 0; i < (int)strArrays.Length; i++)
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
            this.ShipDesigns.Entries.Clear();
            this.ShipDesigns.Copied.Clear();
            string path = Dir.ApplicationData;
            PopulateEntries(path);            
        }

        public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
        }

        public class PlayerDesignToggleButton : ToggleButton
        {
            private const string ActiveTexture   = "SelectionBox/PlayerDesignsPressed";
            private const string InactiveTexture = "SelectionBox/PlayerDesignsActive"; //"SelectionBox/button_grid_inactive";
            private const string HoverTexture    =  "SelectionBox/button_grid_hover";
            private const string PressedTexture  = "SelectionBox/button_grid_pressed";
            private const string IconTexture     = "SelectionBox/icon_grid";

            public PlayerDesignToggleButton(Rectangle gridRect) : base(gridRect, ActiveTexture, InactiveTexture, HoverTexture, PressedTexture, IconTexture)
            {
                Active = true;
                WhichToolTip = 237;
            }
        }
    }
}