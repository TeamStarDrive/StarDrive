using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;
using Ship_Game.Ships;

namespace Ship_Game
{
    public sealed class DesignManager : GameScreen
    {
        readonly ShipDesignScreen Screen;
        readonly string ShipName;
        UITextEntry EnterNameArea;

        Submenu subAllDesigns;
        ScrollList2<ShipDesignListItem> ShipDesigns;
        ShipInfoOverlayComponent ShipInfoOverlay;

        public DesignManager(ShipDesignScreen screen, string txt) : base(screen)
        {
            Rect = new Rectangle(ScreenWidth / 2 - 250, ScreenHeight / 2 - 300, 500, 600);
            ShipName = txt;
            Screen = screen;
            IsPopup = true;
            TransitionOnTime = 0.25f;
            TransitionOffTime = 0.25f;
        }

        public class ShipDesignListItem : ScrollListItem<ShipDesignListItem>
        {
            public Ship Ship;
            private readonly bool CanBuild;
            public ShipDesignListItem(Ship template, bool canBuild)
            {
                Ship     = template;
                CanBuild = canBuild;
            }
            public override void Draw(SpriteBatch batch, DrawTimes elapsed)
            {
                string reserved = Ship.IsPlayerDesign ? "" : ("(Reserved Name)");
                batch.Draw(Ship.shipData.Icon, new Rectangle((int)X, (int)Y, 48, 48));
                batch.DrawString(Fonts.Arial12Bold, Ship.Name, X+52, Y+4, CanBuild ? Color.White : Color.Gray);
                batch.DrawString(Fonts.Arial8Bold, $"{Ship.shipData.GetRole()} {reserved}", X+54, Y+19, Ship.IsPlayerDesign ? Color.Green : Color.IndianRed);
                base.Draw(batch, elapsed);
            }
        }

        public override void LoadContent()
        {
            Submenu background = Add(new Submenu(Rect.X + 20, Rect.Y + 20, Rect.Width - 40, 80));
            background.Background = new Menu1(Rect);
            background.AddTab("Save Ship Design");

            subAllDesigns = new Submenu(background.X, background.Y + 90, background.Width,
                                        Rect.Height - background.Height - 50);
            subAllDesigns.AddTab("Similar Design Names");

            EnterNameArea = Add(new UITextEntry(background.Pos + new Vector2(20, 40), "Design Name: "));
            EnterNameArea.Text = ShipName;
            EnterNameArea.Color = Colors.Cream;

            ShipDesigns = Add(new ScrollList2<ShipDesignListItem>(subAllDesigns));
            ShipDesigns.EnableItemHighlight = true;
            ShipDesigns.OnClick = OnShipDesignItemClicked;

            PopulateDesigns(ShipName);
            ButtonSmall(background.Right - 88, EnterNameArea.Y - 2, "Save", OnSaveClicked);

            ShipInfoOverlay = Add(new ShipInfoOverlayComponent(this));
            ShipDesigns.OnHovered = (item) =>
            {
                if (EmpireManager.Player.ShipsWeCanBuild.Contains(item?.Ship.Name))
                    ShipInfoOverlay.ShowToLeftOf(item?.Pos ?? Vector2.Zero, item?.Ship);
                else
                    ShipInfoOverlay.Hide();
            };

            base.LoadContent();
        }

        bool CheckExistingDesignNames(string shipName, out Ship[] shipList)
        {
            shipList = ResourceManager.ShipsDict.Values.Filter(s =>  !s.Deleted && s.Name.ToLower().Contains(shipName.ToLower()));
            return shipList != null;
        }

        void PopulateDesigns(string newText)
        {
            if (CheckExistingDesignNames(newText, out Ship[] shipList))
                ShipDesigns.SetItems(shipList.Select(s => 
                    new ShipDesignListItem(s, EmpireManager.Player.ShipsWeCanBuild.Contains(s.Name))));
        }

        void OnShipDesignItemClicked(ShipDesignListItem item)
        {
            EnterNameArea.Text = item.Ship.Name;
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
            batch.Begin();
            base.Draw(batch, elapsed);
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
            Screen?.SaveShipDesign(EnterNameArea.Text);

            Empire emp = EmpireManager.Player;
            Ship ship = ResourceManager.ShipsDict[EnterNameArea.Text];
            try
            {
                ship.BaseStrength = ship.GetStrength();
                foreach (Planet p in emp.GetPlanets())
                {
                    foreach (QueueItem qi in p.ConstructionQueue)
                    {
                        if (qi.isShip && qi.sData.Name == EnterNameArea.Text)
                        {
                            qi.sData = ship.shipData;
                            qi.Cost = ship.GetCost(emp);
                        }
                    }
                }
            }
            catch (Exception x)
            {
                Log.Error(x, "Failed to set strength or rename during ship save");
            }
            ExitScreen();
        }

        void TrySave()
        {
            bool saveOk = true;
            bool reserved = false;
            foreach (Ship ship in ResourceManager.ShipsDict.Values)
            {
                if (EnterNameArea.Text == ship.Name)
                {
                    saveOk = false;
                    reserved |= ship.IsReadonlyDesign;
                }
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
            Screen?.SaveShipDesign(EnterNameArea.Text);
            ExitScreen();
        }

        public override bool HandleInput(InputState input)
        {
            string name = "";
            if (EnterNameArea.HandlingInput && (input.IsBackSpace || input.IsEnterOrEscape)
                || EnterNameArea.HandleTextInput(ref name, input) && name.NotEmpty())
            {
                PopulateDesigns($"{EnterNameArea.Text}{name}");
            }

            return base.HandleInput(input);
        }
    }
}