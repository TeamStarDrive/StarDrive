using System;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Ship_Game.Audio;
using Ship_Game.GameScreens.ShipDesign;
using Ship_Game.Ships;
using Ship_Game.UI;
using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;
using Ship_Game.Universe;

namespace Ship_Game
{
    public sealed class ShipDesignSaveScreen : GameScreen
    {
        readonly ShipDesignScreen Screen;
        UniverseState Universe => Screen.ParentUniverse.UState;
        public readonly string ShipName;
        UITextEntry EnterNameArea;
        string BaseWIPName;
        SubmenuScrollList<ShipDesignListItem> SubAllDesigns;
        ScrollList2<ShipDesignListItem> ShipDesigns;
        ShipInfoOverlayComponent ShipInfoOverlay;

        readonly bool Hulls;

        public ShipDesignSaveScreen(ShipDesignScreen screen, string shipName, bool hullDesigner = false)
            : base(screen, toPause: null)
        {
            Screen = screen;
            Rect = new Rectangle(ScreenWidth / 2 - 250, ScreenHeight / 2 - 300, 500, 600);
            BaseWIPName = shipName.Contains("_WIP") ? shipName : "";
            ShipName = shipName.Replace("/", "-").Replace("_", "-");
            IsPopup = true;
            TransitionOnTime = 0.25f;
            TransitionOffTime = 0.25f;
            Hulls = hullDesigner;
        }

        public class ShipDesignListItem : ScrollListItem<ShipDesignListItem>
        {
            public readonly string ShipName;
            public readonly IShipDesign Design;
            readonly ShipHull Hull;
            readonly bool CanBuild;
            readonly bool CanModifyDesign;
            public ShipDesignListItem(IShipDesign template, bool canBuild)
            {
                ShipName = template.Name;
                Design = template;
                CanBuild = canBuild;
                CanModifyDesign = Design.IsPlayerDesign;
            }
            public ShipDesignListItem(ShipHull hull)
            {
                ShipName = hull.VisibleName;
                Hull = hull;
                CanBuild = true;
                CanModifyDesign = true;
            }
            public override void Draw(SpriteBatch batch, DrawTimes elapsed)
            {
                string reserved = CanModifyDesign ? "" : ("(Reserved Name)");
                string role = Design?.GetRole() ?? ShipDesign.GetRole(Hull.Role);
                SubTexture icon = Design?.Icon ?? Hull?.Icon;

                batch.Draw(icon, new Rectangle((int)X, (int)Y, 48, 48));
                batch.DrawString(Fonts.Arial12Bold, ShipName, X+52, Y+4, CanBuild ? Color.White : Color.Gray);
                batch.DrawString(Fonts.Arial8Bold, $"{role} {reserved}", X+54, Y+19, CanModifyDesign ? Color.Green : Color.IndianRed);
                base.Draw(batch, elapsed);
            }
        }

        public override void LoadContent()
        {
            Submenu background = Add(new Submenu(Rect.X + 20, Rect.Y + 20, Rect.Width - 40, 80));
            background.SetBackground(new Menu1(Rect));
            background.AddTab(Hulls ? GameText.SaveHullDesign : GameText.SaveShipDesign);

            RectF subAllDesignsR = new(background.X, background.Y + 90, background.Width, Rect.Height - background.Height - 50);
            SubAllDesigns = Add(new SubmenuScrollList<ShipDesignListItem>(subAllDesignsR));
            SubAllDesigns.AddTab(GameText.SimilarDesignNames);

            EnterNameArea = Add(new UITextEntry(background.Pos + new Vector2(20, 40), GameText.DesignName));
            EnterNameArea.Text = ShipName;
            EnterNameArea.Color = Colors.Cream;
            EnterNameArea.OnTextChanged = PopulateDesigns;

            ShipDesigns = SubAllDesigns.List;
            ShipDesigns.EnableItemHighlight = true;
            ShipDesigns.OnClick = OnShipDesignItemClicked;

            PopulateDesigns(ShipName);
            ButtonSmall(background.Right - 88, EnterNameArea.Y - 2, GameText.Save, OnSaveClicked);

            ShipInfoOverlay = Add(new ShipInfoOverlayComponent(this, Universe));
            ShipDesigns.OnHovered = (item) =>
            {
                if (item != null && (Screen.EnableDebugFeatures || Screen.Player.ShipsWeCanBuild.Contains(item.ShipName)))
                    ShipInfoOverlay.ShowToLeftOf(item?.Pos ?? Vector2.Zero, item?.Design);
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
                ShipHull[] hulls = ResourceManager.Hulls
                    .Filter(h => h.VisibleName.ToLower().Contains(filter));

                ShipDesigns.SetItems(hulls.Select(h => new ShipDesignListItem(h)));
            }
            else
            {
                IShipDesign[] shipList = ResourceManager.Ships.Designs
                    .Filter(s => !s.Deleted && s.Name.ToLower().Contains(filter));

                ShipDesigns.SetItems(shipList.Select(s => 
                    new ShipDesignListItem(s, Screen.Player.ShipsWeCanBuild.Contains(s.Name))));
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

        void OverWriteAccepted(string shipOrHullName, FileInfo overwriteProtected)
        {
            GameAudio.AffirmativeClick();

            if (Hulls)
            {
                ShipHull saved = Screen.SaveHullDesign(shipOrHullName, overwriteProtected);

                if (!ResourceManager.Hull(saved.HullName, out ShipHull _))
                {
                    Log.Error($"Failed to get Hull Template after Save: {saved.HullName}");
                    return;
                }
            }
            else
            {
                Screen.SaveShipDesign(shipOrHullName, overwriteProtected);
                if (BaseWIPName.NotEmpty())
                    ShipDesignWIP.RemoveRelatedWiPs(Universe, BaseWIPName);
                if (!ResourceManager.GetShipTemplate(shipOrHullName, out Ship ship))
                {
                    Log.Error($"Failed to get Ship Template after Save: {shipOrHullName}");
                    return;
                }

                UpdateConstructionQueue(ship, shipOrHullName);
            }

            ExitScreen();
        }

        void UpdateConstructionQueue(Ship ship, string shipOrHullName)
        {
            Empire emp = Screen.Player;
            try
            {
                foreach (Planet p in emp.GetPlanets())
                {
                    foreach (QueueItem qi in p.ConstructionQueue)
                    {
                        if (qi.isShip && qi.ShipData.Name == shipOrHullName)
                        {
                            qi.ShipData = ship.ShipData;
                            qi.Cost = ship.ShipData.GetCost(emp);
                        }
                    }
                }
            }
            catch (Exception x)
            {
                Log.Error(x, "Failed to set strength or rename during ship save");
            }
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
            bool reserved = false;
            FileInfo source = null;

            if (Hulls)
            {
                ShipHull hull = ResourceManager.Hulls.FirstOrDefault(h => h.VisibleName == shipOrHullName);
                exists = hull != null;
                source = hull?.Source;
            }
            else
            {
                IShipDesign ship = ResourceManager.ShipDesigns.FirstOrDefault(s => s.Name == shipOrHullName);
                exists = ship != null;
                source = ship?.Source;
                reserved = ship?.IsReadonlyDesign == true;

                if (reserved && !Screen.EnableDebugFeatures)
                {
                    GameAudio.NegativeClick();
                    ScreenManager.AddScreen(new MessageBoxScreen(this, $"{shipOrHullName} is a reserved ship name and you cannot overwrite this design"));
                    return;
                }

                // Note - UState.Ships is not thread safe, but the game is paused in this screen
                if (Universe.Ships.Any(s => s.Name == shipOrHullName))
                {
                    GameAudio.NegativeClick();
                    ScreenManager.AddScreen(new MessageBoxScreen(this, $"{shipOrHullName} currently exist the universe." +
                                                                       " You cannot overwrite a design with this name.",
                                                                       MessageBoxButtons.Ok));
                    return;
                }

                if (HelperFunctions.DesignInQueue(Screen, shipOrHullName, out string playerPlanets))
                {
                    GameAudio.NegativeClick();
                    if (playerPlanets.NotEmpty())
                    {
                        ScreenManager.AddScreen(new MessageBoxScreen
                            (this, $"{shipOrHullName} currently exist the your planets' build queue." +
                                   $" You cannot overwrite this design name.\n Related planets: {playerPlanets}.",
                                   MessageBoxButtons.Ok));
                    }
                    else
                    {
                        ScreenManager.AddScreen(new MessageBoxScreen
                            (this, $"{shipOrHullName} currently exist the universe (maybe by another empire). " +
                                   "You cannot overwrite this design name.", MessageBoxButtons.Ok));
                    }

                    return;
                }
            }

            if (exists)
            {
                GameAudio.NegativeClick();
                string alreadyExists = Hulls ? $"Hull named '{shipOrHullName}' already exists. Overwrite?"
                                             : $"Design named '{shipOrHullName}' already exists. Overwrite?";
                if (reserved)
                    alreadyExists = $"Reserved Design named '{shipOrHullName}' already exists. Overwrite at '{source.RelPath()}'?";

                ScreenManager.AddScreen(new MessageBoxScreen(this, alreadyExists)
                {
                    Accepted = () => OverWriteAccepted(shipOrHullName, reserved ? source : null)
                });;
            }
            else
            {
                OverWriteAccepted(shipOrHullName, null);
            }
        }

        public override bool HandleInput(InputState input)
        {
            return base.HandleInput(input);
        }
    }
}