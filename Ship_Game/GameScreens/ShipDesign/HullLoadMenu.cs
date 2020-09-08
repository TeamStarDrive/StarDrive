using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game.GameScreens.ShipDesign
{
    public class HullsListMenu : UIElementContainer
    {
        ShipToolScreen Screen;
        public ShipData Changeto { get; private set; }
        public ShipData ActiveHull { get; private set; }
        ScrollList2<HullListItem> HullSL;

        public Action<ShipData> OnHullChange;

        public HullsListMenu(ShipToolScreen screen)
        {
            Screen = screen;
            var background = new Submenu(Screen.ScreenWidth - 285, 100, 280, 400);
            background.Background = new Selector(background.Rect.CutTop(25), new Color(0, 0, 0, 210)); // black background
            background.AddTab(Localizer.Token(107));
            HullSL = Add(new ScrollList2<HullListItem>(background));
            HullSL.EnableItemHighlight = true;
            HullSL.OnClick = (item) => ChangeHull(item.Hull);

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
                HullListItem item = HullSL.AddItem(new HullListItem(cat));
                foreach (ShipData hull in ResourceManager.Hulls)
                {
                    if (item.HeaderText == Localizer.GetRole(hull.Role, hull.ShipStyle))
                        item.AddSubItem(new HullListItem(hull));
                }
            }
        }

        class HullListItem : ScrollListItem<HullListItem>
        {
            public ShipData Hull;
            public HullListItem(string headerText) : base(headerText) {}
            public HullListItem(ShipData hull) { Hull = hull; }
            public override void Draw(SpriteBatch batch, DrawTimes elapsed)
            {
                base.Draw(batch, elapsed);
                if (Hull != null)
                {
                    float iconSize = Height;
                    batch.Draw(Hull.Icon, new Vector2(X, Y), new Vector2(iconSize));
                    batch.DrawString(Fonts.Arial12Bold, Hull.Name, X+iconSize+4, Y+4);
                    batch.DrawString(Fonts.Arial8Bold, Localizer.GetRole(Hull.HullRole, Hull.ShipStyle), X+iconSize+4, Y+18, Color.Orange);
                }
            }
        }

        void ChangeHull(ShipData hull)
        {
            if (hull == null)
                return;

            ActiveHull = new ShipData
            {
                Animated          = hull.Animated,
                CombatState       = hull.CombatState,
                Hull              = hull.Hull,
                IconPath          = hull.ActualIconPath,
                ModelPath         = hull.HullModel,
                Name              = hull.Name,
                Role              = hull.Role,
                ShipStyle         = hull.ShipStyle,
                ThrusterList      = hull.ThrusterList,
                ShipCategory      = hull.ShipCategory,
                HangarDesignation = hull.HangarDesignation,
                CarrierShip       = hull.CarrierShip,
                BaseHull          = hull.BaseHull
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

            OnHullChange?.Invoke(ActiveHull);
        }
    }
}
