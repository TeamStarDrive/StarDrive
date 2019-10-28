using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.Audio;
using Ship_Game.Ships;

namespace Ship_Game
{
    public sealed class DesignManager : GameScreen
    {
        readonly ShipDesignScreen screen;
        readonly string ShipName;
        Submenu SaveShips;
        Menu1 SaveMenu;
        Rectangle Window;
        Vector2 TitlePosition;
        Vector2 EnternamePos;

        readonly UITextEntry EnterNameArea = new UITextEntry();
        UIButton Save;

        Submenu subAllDesigns;
        ScrollList<ShipDesignListItem> ShipDesigns;

        public DesignManager(ShipDesignScreen screen, string txt) : base(screen)
        {
            ShipName = txt;
            this.screen = screen;
            IsPopup = true;
            TransitionOnTime = 0.25f;
            TransitionOffTime = 0.25f;
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

            ShipDesigns = new ScrollList<ShipDesignListItem>(subAllDesigns);
            foreach (KeyValuePair<string, Ship> Ship in ResourceManager.ShipsDict)
            {
                ShipDesigns.AddItem(new ShipDesignListItem(Ship.Value));
            }
            ShipDesigns.OnClick = OnShipDesignItemClicked;

            EnternamePos = TitlePosition;
            EnterNameArea.ClickableArea = new Rectangle((int)(EnternamePos.X + Fonts.Arial20Bold.MeasureString("Design Name: ").X), (int)EnternamePos.Y - 2, 256, Fonts.Arial20Bold.LineSpacing);
            EnterNameArea.Text = ShipName;

            Save = ButtonSmall(sub.X + sub.Width - 88, EnterNameArea.ClickableArea.Y - 2, "Save", OnSaveClicked);
            base.LoadContent();
        }

        void OnShipDesignItemClicked(ShipDesignListItem item)
        {
            EnterNameArea.Text = item.Ship.Name;
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


        public override void Draw(SpriteBatch batch)
        {
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
            batch.Begin();
            SaveMenu.Draw(batch);
            SaveShips.Draw(batch);
            EnterNameArea.Draw(batch, Fonts.Arial20Bold, EnternamePos, (EnterNameArea.Hover ? Color.White : Colors.Cream));
            subAllDesigns.Draw(batch);
            ShipDesigns.Draw(batch);
            base.Draw(batch);
            batch.End();
        }

        void OnSaveClicked(UIButton b)
        {
            GlobalStats.TakingInput = false;
            EnterNameArea.HandlingInput = false;
            TrySave();
        }

        void OverWriteAccepted()
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

        void TrySave()
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
                ScreenManager.AddScreen(new MessageBoxScreen(this, $"{EnterNameArea.Text} is a reserved ship name and you cannot overwrite this design"));
                return;
            }
            if (!saveOk)
            {
                ScreenManager.AddScreen(new MessageBoxScreen(this, "Design name already exists.  Overwrite?")
                {
                    Accepted = OverWriteAccepted
                });
                return;
            }
            GameAudio.AffirmativeClick();
            screen?.SaveShipDesign(EnterNameArea.Text);
            ExitScreen();
        }
    }
}