using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.Audio;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game.GameScreens.ShipDesign
{
    public class HullsListMenu
    {
        public ShipData Changeto { get; private set; }
        public ShipData ActiveHull { get; private set; }
        ScrollList<HullListItem> HullSL;
        Submenu HullSelectionSub;
        Rectangle HullSelectionRect;

        public int ScreenWidth => GameBase.ScreenWidth;
        public int ScreenHeight => GameBase.ScreenHeight;
        public Vector2 ScreenArea => GameBase.ScreenSize;
        public Vector2 ScreenCenter => GameBase.ScreenCenter;

        class HullListItem : ScrollList<HullListItem>.Entry
        {
            public ModuleHeader Header;
            public ShipData Hull;

            public override void Draw(SpriteBatch batch)
            {
                if (Header != null)
                {
                    Header.Pos = Pos;
                    Header.Draw(batch);
                }
                else if (Hull != null)
                {
                    var bCursor = new Vector2(X, Y);
                    batch.Draw(Hull.Icon, new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);

                    var tCursor = new Vector2(bCursor.X + 40f, bCursor.Y + 3f);
                    batch.DrawString(Fonts.Arial12Bold, Hull.Name, tCursor, Color.White);

                    tCursor.Y += Fonts.Arial12Bold.LineSpacing;
                    batch.DrawString(Fonts.Arial8Bold, Localizer.GetRole(Hull.HullRole, Hull.ShipStyle), tCursor, Color.Orange);
                }
            }
        }
        
        public void LoadContent()
        {
            HullSelectionRect = new Rectangle(ScreenWidth - 285, 100, 280, 400);
            HullSelectionSub = new Submenu(HullSelectionRect);
            HullSelectionSub.AddTab(Localizer.Token(107));
            HullSL = new ScrollList<HullListItem>(HullSelectionSub);
            HullSL.OnClick = OnHullListItemClicked;

            var categories = new Array<string>();
            foreach (ShipData hull in ResourceManager.Hulls)
            {
                string cat = Localizer.GetRole(hull.Role, hull.ShipStyle);
                if (!categories.Contains(cat))
                    categories.Add(cat);
            }

            categories.Sort();
            foreach (string cat in categories)
            {
                HullListItem item = HullSL.AddItem(new HullListItem{ Header = new ModuleHeader(cat, 240) });
                foreach (ShipData hull in ResourceManager.Hulls)
                {
                    if (item.Header.Text == Localizer.GetRole(hull.Role, hull.ShipStyle))
                        item.AddSubItem(new HullListItem{ Hull = hull });
                }
            }
        }

        void OnHullListItemClicked(HullListItem item)
        {
            ChangeHull(item.Hull);
        }

        public bool HandleInput(InputState input)
        {
            return HullSL.HandleInput(input);
        }

        void ChangeHull(ShipData hull)
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

        public void Draw(SpriteBatch batch)
        {
            Rectangle r = HullSelectionSub.Rect;
            r.Y += 25;
            r.Height -= 25;
            var sel = new Selector(r, new Color(0, 0, 0, 210));
            sel.Draw(batch);
            HullSL.Draw(batch);
            HullSelectionSub.Draw(batch);
        }

    }
}
