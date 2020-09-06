
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI;
using Ship_Game.Ships;

// ReSharper disable once CheckNamespace
namespace Ship_Game
{
    public class FighterScrollList : ScrollList2<FighterListItem>
    {
        readonly ShipDesignScreen Screen;
        public ShipModule ActiveHangarModule;
        public ShipModule ActiveModule;
        public string HangarShipUIDLast = "";

        public FighterScrollList(Submenu fighterList, ShipDesignScreen shipDesignScreen) : base(fighterList, 40)
        {
            Screen = shipDesignScreen;
        }

        public bool HitTest(InputState input)
        {
            return ActiveModule != null && Rect.HitTest(input.CursorPosition);
        }

        void Populate()
        {
            Reset();
            AddShip(ResourceManager.GetShipTemplate(DynamicHangarOptions.DynamicLaunch.ToString()));
            AddShip(ResourceManager.GetShipTemplate(DynamicHangarOptions.DynamicInterceptor.ToString()));
            AddShip(ResourceManager.GetShipTemplate(DynamicHangarOptions.DynamicAntiShip.ToString()));
            foreach (string shipId in EmpireManager.Player.ShipsWeCanBuild)
            {
                if (!ResourceManager.GetShipTemplate(shipId, out Ship hangarShip))
                    continue;
                string role = ShipData.GetRole(hangarShip.shipData.HullRole);
                if (!ActiveModule.PermittedHangarRoles.Contains(role))
                    continue;
                if (hangarShip.SurfaceArea > ActiveModule.MaximumHangarShipSize)
                    continue;
                AddShip(ResourceManager.ShipsDict[shipId]);
            }
        }
        
        void AddShip(Ship ship)
        {
            AddItem(new FighterListItem(ship));
        }

        public override void OnItemHovered(ScrollListItemBase item)
        {
            if (item == null) // we're not hovering the scroll list, just highlight the active ship
            {
                foreach (FighterListItem e in AllEntries)
                {
                    if (ActiveModule?.hangarShipUID == e.Ship.Name)
                    {
                        Highlight = new Selector(new RectF(e.X-15, e.Y-5, e.Width+12, e.Height));
                    }
                }
            }
            base.OnItemHovered(item);
        }

        public override void OnItemClicked(ScrollListItemBase item)
        {
            if (ActiveModule != null)
            {
                var fighterItem            = (FighterListItem)item;
                ActiveModule.hangarShipUID = fighterItem.Ship.Name;
                HangarShipUIDLast          = fighterItem.Name;
            }

            base.OnItemClicked(item);
        }

        public override bool HandleInput(InputState input)
        {
            ShipModule activeModule = Screen.ActiveModule;
            ShipModule highlightedModule = Screen.HighlightedModule;

            activeModule = activeModule ?? highlightedModule;
            if (activeModule?.ModuleType == ShipModuleType.Hangar 
                && !activeModule.IsTroopBay 
                && !activeModule.IsSupplyBay)
            {
                ActiveModule = activeModule;
                SetActiveHangarModule(activeModule, ActiveHangarModule);
                
                base.HandleInput(input);
                return HitTest(input);
            }
            
            base.HandleInput(input);
            ActiveHangarModule = null;
            ActiveModule = null;
            HangarShipUIDLast = "";
            return false;
        }

        bool HangarNotSelected(ShipModule activeModule, ShipModule activeHangarModule)
            => activeHangarModule == activeModule || activeModule.ModuleType != ShipModuleType.Hangar;

        public void SetActiveHangarModule(ShipModule activeModule, ShipModule activeHangarModule)
        {
            if (HangarNotSelected(activeModule, activeHangarModule))
                return;

            ActiveHangarModule = activeModule;
            Populate();

            Ship fighter = ResourceManager.GetShipTemplate(HangarShipUIDLast, false);
            if (HangarShipUIDLast != "" && activeModule.PermittedHangarRoles.Contains(fighter?.shipData.GetRole()) && activeModule.MaximumHangarShipSize >= fighter?.SurfaceArea)
            {
                activeModule.hangarShipUID = HangarShipUIDLast;
            }
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            if (ActiveHangarModule == null)
                return;

            Screen.DrawRectangle(Rect, Color.TransparentWhite, Color.Black);
            base.Draw(batch, elapsed);
        }
    }
}
