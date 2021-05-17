using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;
using Ship_Game.GameScreens.ShipDesign;
using Ship_Game.Ships;

namespace Ship_Game
{
    public sealed class ShipDesignSaveScreen : GameScreen
    {
        readonly ShipDesignScreen Screen;
        readonly string ShipName;
        UITextEntry EnterNameArea;

        Submenu subAllDesigns;
        ScrollList2<ShipDesignListItem> ShipDesigns;
        ShipInfoOverlayComponent ShipInfoOverlay;

        readonly bool Hulls;

        public ShipDesignSaveScreen(ShipDesignScreen screen, string shipName, bool hullDesigner = false) : base(screen)
        {
            Rect = new Rectangle(ScreenWidth / 2 - 250, ScreenHeight / 2 - 300, 500, 600);
            ShipName = shipName;
            Screen = screen;
            IsPopup = true;
            TransitionOnTime = 0.25f;
            TransitionOffTime = 0.25f;
            Hulls = hullDesigner;
        }

        public class ShipDesignListItem : ScrollListItem<ShipDesignListItem>
        {
            public Ship Ship;
            public readonly string ShipName;
            readonly ShipData Hull;
            readonly bool CanBuild;
            readonly bool CanModifyDesign;
            public ShipDesignListItem(Ship template, bool canBuild)
            {
                Ship = template;
                ShipName = template.Name;
                Hull = template.shipData;
                CanBuild = canBuild;
                CanModifyDesign = template.IsPlayerDesign;
            }
            public ShipDesignListItem(ShipData hull)
            {
                ShipName = hull.Name;
                Hull = hull;
                CanBuild = true;
                CanModifyDesign = true;
            }
            public override void Draw(SpriteBatch batch, DrawTimes elapsed)
            {
                string reserved = CanModifyDesign ? "" : ("(Reserved Name)");
                batch.Draw(Hull.Icon, new Rectangle((int)X, (int)Y, 48, 48));
                batch.DrawString(Fonts.Arial12Bold, ShipName, X+52, Y+4, CanBuild ? Color.White : Color.Gray);
                batch.DrawString(Fonts.Arial8Bold, $"{Hull.GetRole()} {reserved}", X+54, Y+19, CanModifyDesign ? Color.Green : Color.IndianRed);
                base.Draw(batch, elapsed);
            }
        }

        public override void LoadContent()
        {
            Submenu background = Add(new Submenu(Rect.X + 20, Rect.Y + 20, Rect.Width - 40, 80));
            background.Background = new Menu1(Rect);
            background.AddTab(Hulls ? GameText.SaveHullDesign : GameText.SaveShipDesign);

            subAllDesigns = new Submenu(background.X, background.Y + 90, background.Width,
                                        Rect.Height - background.Height - 50);
            subAllDesigns.AddTab(GameText.SimilarDesignNames);

            EnterNameArea = Add(new UITextEntry(background.Pos + new Vector2(20, 40), GameText.DesignName));
            EnterNameArea.Text = ShipName;
            EnterNameArea.Color = Colors.Cream;
            EnterNameArea.OnTextChanged = (text) => PopulateDesigns(text);

            ShipDesigns = Add(new ScrollList2<ShipDesignListItem>(subAllDesigns));
            ShipDesigns.EnableItemHighlight = true;
            ShipDesigns.OnClick = OnShipDesignItemClicked;

            PopulateDesigns(ShipName);
            ButtonSmall(background.Right - 88, EnterNameArea.Y - 2, GameText.Save, OnSaveClicked);

            ShipInfoOverlay = Add(new ShipInfoOverlayComponent(this));
            ShipDesigns.OnHovered = (item) =>
            {
                if (EmpireManager.Player.ShipsWeCanBuild.Contains(item?.Ship?.Name))
                    ShipInfoOverlay.ShowToLeftOf(item?.Pos ?? Vector2.Zero, item?.Ship);
                else
                    ShipInfoOverlay.Hide();
            };

            base.LoadContent();
        }

        void PopulateDesigns(string shipNameMatch)
        {
            string filter = shipNameMatch.ToLower();
            if (Hulls)
            {
                ShipData[] hulls = ResourceManager.Hulls
                    .Filter(h => h.Name.ToLower().Contains(filter));

                ShipDesigns.SetItems(hulls.Select(h => new ShipDesignListItem(h)));
            }
            else
            {
                Ship[] shipList = ResourceManager.GetShipTemplates()
                    .Filter(s => !s.Deleted && s.Name.ToLower().Contains(filter));

                ShipDesigns.SetItems(shipList.Select(s => 
                    new ShipDesignListItem(s, EmpireManager.Player.ShipsWeCanBuild.Contains(s.Name))));
            }
        }

        void OnShipDesignItemClicked(ShipDesignListItem item)
        {
            EnterNameArea.Text = item.ShipName;
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
            EnterNameArea.StopInput();
            TrySave();
        }

        void OverWriteAccepted()
        {
            GameAudio.AffirmativeClick();

            string shipOrHullName = EnterNameArea.Text;

            if (Hulls)
            {
                Screen?.SaveHullDesign(shipOrHullName);

                if (!ResourceManager.Hull(shipOrHullName, out ShipData hull))
                {
                    Log.Error($"Failed to get Hull Template after Save: {shipOrHullName}");
                    return;
                }
            }
            else
            {
                Screen?.SaveShipDesign(shipOrHullName);

                if (!ResourceManager.GetShipTemplate(shipOrHullName, out Ship ship))
                {
                    Log.Error($"Failed to get Ship Template after Save: {shipOrHullName}");
                    return;
                }

                Empire emp = EmpireManager.Player;
                try
                {
                    ship.BaseStrength = ship.GetStrength();
                    foreach (Planet p in emp.GetPlanets())
                    {
                        foreach (QueueItem qi in p.ConstructionQueue)
                        {
                            if (qi.isShip && qi.sData.Name == shipOrHullName)
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
            }

            ExitScreen();
        }

        void TrySave()
        {
            string shipOrHullName = EnterNameArea.Text;

            if (shipOrHullName.IsEmpty())
            {
                string what = Hulls ? "hull" : "design";
                ScreenManager.AddScreen(new MessageBoxScreen(this, $"Please enter a name for your {what}", MessageBoxButtons.Ok));
                GameAudio.NegativeClick();
                return;
            }

            bool exists = false;

            if (Hulls)
            {
                foreach (ShipData hull in ResourceManager.Hulls)
                {
                    if (shipOrHullName == hull.Name)
                        exists = true;
                }
            }
            else
            {
                bool reserved = false;
                foreach (Ship ship in ResourceManager.GetShipTemplates())
                {
                    if (shipOrHullName == ship.Name)
                    {
                        exists = true;
                        reserved |= ship.IsReadonlyDesign;
                    }
                }

                if (reserved && !Empire.Universe.Debug)
                {
                    GameAudio.NegativeClick();
                    ScreenManager.AddScreen(new MessageBoxScreen(this, $"{shipOrHullName} is a reserved ship name and you cannot overwrite this design"));
                    return;
                }
            }

            if (exists)
            {
                GameAudio.NegativeClick();
                string alreadyExists = Hulls ? $"Hull named '{shipOrHullName}' already exists. Overwrite?"
                                             : $"Design named '{shipOrHullName}' already exists. Overwrite?";
                ScreenManager.AddScreen(new MessageBoxScreen(this, alreadyExists)
                {
                    Accepted = OverWriteAccepted
                });
            }
            else
            {
                OverWriteAccepted();
            }
        }

        public override bool HandleInput(InputState input)
        {
            return base.HandleInput(input);
        }
    }
}