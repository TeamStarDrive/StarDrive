using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.Audio;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game.GameScreens.ShipDesign
{
    public class Hulls
    {
        private ScrollList HullSL;
        public ShipData Changeto { get; private set; }
        public DesignModuleGrid ModuleGrid { get; private set; }
        public ShipData ActiveHull { get; private set; }
        private Submenu HullSelectionSub;
        private Rectangle HullSelectionRect;

        public int ScreenWidth => GameBase.ScreenWidth;
        public int ScreenHeight => GameBase.ScreenHeight;
        public Vector2 ScreenArea => GameBase.ScreenSize;
        public Vector2 ScreenCenter => GameBase.ScreenCenter;
        public bool HandleShipHullListSelection(InputState input)
        {
            HullSL.HandleInput(input);
            foreach (ScrollList.Entry e in HullSL.VisibleExpandedEntries)
            {
                if (e.item is ModuleHeader moduleHeader)
                {
                    if (moduleHeader.HandleInput(input, e))
                        return true;
                }
                else if (e.CheckHover(input))
                {
                    if (!input.InGameSelect)
                        continue;
                    GameAudio.AcceptClick();

                    ChangeHull(e.item as ShipData);
                    return true;
                }
            }
            return false;
        }
        public void ChangeHull(ShipData hull)
        {
            if (hull == null) return;
            ActiveHull = new ShipData
            {
                Animated = hull.Animated,
                CombatState = hull.CombatState,
                Hull = hull.Hull,
                IconPath = hull.ActualIconPath,
                ModelPath = hull.HullModel,
                Name = hull.Name,
                Role = hull.Role,
                ShipStyle = hull.ShipStyle,
                ThrusterList = hull.ThrusterList,
                ShipCategory = hull.ShipCategory,
                HangarDesignation = hull.HangarDesignation,
                ShieldsBehavior = hull.ShieldsBehavior,
                CarrierShip = hull.CarrierShip,
                BaseHull = hull.BaseHull
            };
            ActiveHull.UpdateBaseHull();

            ActiveHull.ModuleSlots = new ModuleSlotData[hull.ModuleSlots.Length];
            for (int i = 0; i < hull.ModuleSlots.Length; ++i)
            {
                ModuleSlotData hullSlot = hull.ModuleSlots[i];
                var data = new ModuleSlotData
                {
                    Position = hullSlot.Position,
                    Restrictions = hullSlot.Restrictions,
                    Facing = hullSlot.Facing,
                    InstalledModuleUID = hullSlot.InstalledModuleUID,
                    Orientation = hullSlot.Orientation,
                    SlotOptions = hullSlot.SlotOptions
                };
                ActiveHull.ModuleSlots[i] = data;
            }
        }

        public void DrawHullSelection(SpriteBatch batch, ScreenManager screenManager)
        {
            Rectangle r = HullSelectionSub.Rect;
            r.Y += 25;
            r.Height -= 25;
            var sel = new Selector(r, new Color(0, 0, 0, 210));
            sel.Draw(batch);
            HullSL.Draw(batch);
            Vector2 mousePos = Mouse.GetState().Pos();
            HullSelectionSub.Draw(batch);

            foreach (ScrollList.Entry e in HullSL.VisibleExpandedEntries)
            {
                var bCursor = new Vector2(HullSelectionSub.X + 10, e.Y);
                if (e.item is ModuleHeader header)
                {
                    header.Draw(screenManager, bCursor);
                }
                else if (e.item is ShipData ship)
                {
                    bCursor.X += 10f;
                    batch.Draw(ship.Icon, new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);
                    var tCursor = new Vector2(bCursor.X + 40f, bCursor.Y + 3f);
                    batch.DrawString(Fonts.Arial12Bold, ship.Name, tCursor, Color.White);
                    tCursor.Y += Fonts.Arial12Bold.LineSpacing;
                    batch.DrawString(Fonts.Arial8Bold, Localizer.GetRole(ship.HullRole, ship.ShipStyle), tCursor, Color.Orange);

                    e.CheckHover(mousePos);
                }
            }
        }

        public void LoadContent()
        {
            HullSelectionRect = new Rectangle(ScreenWidth - 285, 100, 280, 400);
            HullSelectionSub = new Submenu(HullSelectionRect);
            HullSelectionSub.AddTab(Localizer.Token(107));
            HullSL = new ScrollList(HullSelectionSub);
            var categories = new Array<string>();
            foreach (ShipData hull in ResourceManager.Hulls)
            {
                string cat = Localizer.GetRole(hull.Role, hull.ShipStyle);
                if (!categories.Contains(cat))
                    categories.Add(cat);
            }

            categories.Sort();
            foreach (string cat in categories) HullSL.AddItem(new ModuleHeader(cat, 240));

            foreach (ScrollList.Entry e in HullSL.AllEntries)
            {
                foreach (ShipData hull in ResourceManager.Hulls)
                {
                    if (((ModuleHeader)e.item).Text == Localizer.GetRole(hull.Role, hull.ShipStyle))
                        e.AddSubItem(hull);
                }
            }
        }
    }
}
