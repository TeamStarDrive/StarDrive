using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.Ships;

namespace Ship_Game
{
    public sealed class DesignManager : GameScreen
    {
        private readonly ShipDesignScreen screen;
        private readonly string ShipName;
        private Selector selector;
        private Submenu SaveShips;
        private Menu1 SaveMenu;
        private Rectangle Window;
        private Vector2 TitlePosition;
        private Vector2 EnternamePos;

        private readonly UITextEntry EnterNameArea = new UITextEntry();
        private UIButton Save;

        private Submenu subAllDesigns;
        private ScrollList ShipDesigns;

        public DesignManager(ShipDesignScreen screen, string txt) : base(screen)
        {
            ShipName = txt;
            this.screen = screen;
            IsPopup = true;
            TransitionOnTime = TimeSpan.FromSeconds(0.25);
            TransitionOffTime = TimeSpan.FromSeconds(0.25);
        }

        public override void Draw(SpriteBatch batch)
        {
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
            batch.Begin();
            SaveMenu.Draw();
            SaveShips.Draw(batch);
            EnterNameArea.Draw(Fonts.Arial20Bold, batch, EnternamePos, 
                StarDriveGame.Instance.GameTime, (EnterNameArea.Hover ? Color.White : new Color(255, 239, 208)));
            subAllDesigns.Draw(batch);
            ShipDesigns.Draw(batch);
            var bCursor = new Vector2(subAllDesigns.Menu.X + 20, subAllDesigns.Menu.Y + 20);
            foreach (ScrollList.Entry e in ShipDesigns.VisibleEntries)
            {
                var ship = (Ship)e.item;
                bCursor.Y = e.Y;
                batch.Draw(ship.shipData.Icon, new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);   
                var tCursor = new Vector2(bCursor.X + 40f, bCursor.Y + 3f);
                batch.DrawString(Fonts.Arial12Bold, ship.Name, tCursor, Color.White);
                tCursor.Y = tCursor.Y + Fonts.Arial12Bold.LineSpacing;
                batch.DrawString(Fonts.Arial8Bold, ship.shipData.GetRole(), tCursor, Color.Orange);
                e.DrawPlusEdit(batch);
            }
            selector?.Draw(batch);
            base.Draw(batch);
            batch.End();
        }

        private void OnSaveClicked(UIButton b)
        {
            GlobalStats.TakingInput = false;
            EnterNameArea.HandlingInput = false;
            TrySave();
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
            foreach (ScrollList.Entry e in ShipDesigns.AllEntries)
            {
                if (e.CheckHover(input))
                {
                    selector = e.CreateSelector();

                    if (input.LeftMouseClick)
                    {
                        EnterNameArea.Text = ((Ship)e.item).Name;
                        GameAudio.AcceptClick();
                    }
                }
            }

            EnterNameArea.ClickableArea = new Rectangle((int)EnternamePos.X, (int)EnternamePos.Y, 200, 30);
            if (!EnterNameArea.ClickableArea.HitTest(input.CursorPosition))
            {
                EnterNameArea.Hover = false;
            }
            else
            {
                EnterNameArea.Hover = true;
                if (input.LeftMouseClick)
                {
                    EnterNameArea.HandlingInput = true;
                }
            }
            if (!EnterNameArea.HandlingInput)
            {
                GlobalStats.TakingInput = false;
            }
            else
            {
                GlobalStats.TakingInput = true;
                EnterNameArea.HandleTextInput(ref EnterNameArea.Text, input);
                if (input.IsKeyDown(Keys.Enter))
                {
                    EnterNameArea.HandlingInput = false;
                }
            }
            return base.HandleInput(input);
        }

        public override void LoadContent()
        {
            Window = new Rectangle(ScreenWidth / 2 - 250, ScreenHeight / 2 - 300, 500, 600);
            SaveMenu = new Menu1(Window);
            var sub = new Rectangle(Window.X + 20, Window.Y + 20, Window.Width - 40, 80);
            SaveShips = new Submenu(sub);
            SaveShips.AddTab("Save Ship Design");
            TitlePosition = new Vector2(sub.X + 20, sub.Y + 45);
            var scrollList = new Rectangle(sub.X, sub.Y + 90, sub.Width, Window.Height - sub.Height - 50);
            subAllDesigns = new Submenu(scrollList);
            subAllDesigns.AddTab("All Designs");
            ShipDesigns = new ScrollList(subAllDesigns);
            foreach (KeyValuePair<string, Ship> Ship in ResourceManager.ShipsDict)
            {
                ShipDesigns.AddItem(Ship.Value);
            }
            EnternamePos = TitlePosition;
            EnterNameArea.ClickableArea = new Rectangle((int)(EnternamePos.X + Fonts.Arial20Bold.MeasureString("Design Name: ").X), (int)EnternamePos.Y - 2, 256, Fonts.Arial20Bold.LineSpacing);
            EnterNameArea.Text = ShipName;

            Save = ButtonSmall(sub.X + sub.Width - 88, EnterNameArea.ClickableArea.Y - 2, "Save", OnSaveClicked);

            base.LoadContent();
        }

        private void OverWriteAccepted(object sender, EventArgs e)
        {
            GameAudio.AffirmativeClick();
            screen?.SaveShipDesign(EnterNameArea.Text);

            Empire emp = EmpireManager.Player;
            Ship ship = ResourceManager.ShipsDict[EnterNameArea.Text];
            try
            {
                ship.BaseStrength = ship.GetStrength();
                foreach (Planet p in emp.GetPlanets())
                {
                    foreach (QueueItem qi in p.ConstructionQueue)
                    {
                        if (!qi.isShip || qi.sData.Name != EnterNameArea.Text)
                            continue;
                        qi.sData = ship.shipData;
                        qi.Cost = ship.GetCost(emp);
                    }
                }
            }
            catch (Exception x)
            {
                Log.Error(x, "Failed to set strength or rename duing ship save");
            }
            ExitScreen();
        }

        private void TrySave()
        {
            bool saveOk = true;
            bool reserved = false;
            foreach (Ship ship in ResourceManager.ShipsDict.Values)
            {
                if (EnterNameArea.Text != ship.Name)
                    continue;
                saveOk = false;
                reserved |= ship.IsReadonlyDesign;
            }

            if (reserved && !Empire.Universe.Debug)
            {
                GameAudio.NegativeClick();
                var messageBox = new MessageBoxScreen(this, $"{EnterNameArea.Text} is a reserved ship name and you cannot overwrite this design");
                ScreenManager.AddScreen(messageBox);
                return;
            }
            if (!saveOk)
            {
                var messageBox = new MessageBoxScreen(this, "Design name already exists.  Overwrite?");
                messageBox.Accepted += OverWriteAccepted;
                ScreenManager.AddScreen(messageBox);
                return;
            }
            GameAudio.AffirmativeClick();
            screen?.SaveShipDesign(EnterNameArea.Text);
            ExitScreen();
        }
    }
}