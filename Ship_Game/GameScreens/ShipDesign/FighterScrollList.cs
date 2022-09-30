using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Ship_Game.AI;
using Ship_Game.GameScreens.ShipDesign;
using Ship_Game.Ships;

// ReSharper disable once CheckNamespace
namespace Ship_Game
{
    public class FighterScrollList : ScrollList2<FighterListItem>
    {
        readonly ShipDesignScreen Screen;
        Empire Player => Screen.ParentUniverse.Player;
        public ShipModule ActiveHangarModule;
        public ShipModule ActiveModule;
        public string HangarShipUIDLast = "";
        ShipInfoOverlayComponent HangarShipInfoOverlay;

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
            AddShip(ResourceManager.Ships.GetDesign(DynamicHangarOptions.DynamicLaunch.ToString()));
            AddShip(ResourceManager.Ships.GetDesign(DynamicHangarOptions.DynamicInterceptor.ToString()));
            AddShip(ResourceManager.Ships.GetDesign(DynamicHangarOptions.DynamicAntiShip.ToString()));
            foreach (string shipId in Screen.ParentUniverse.Player.ShipsWeCanBuild)
            {
                if (!ResourceManager.Ships.GetDesign(shipId, out IShipDesign hangarShip))
                    continue;
                string role = ShipDesign.GetRole(hangarShip.HullRole);
                if (!ActiveModule.PermittedHangarRoles.Contains(role))
                    continue;
                if (hangarShip.SurfaceArea > ActiveModule.MaximumHangarShipSize)
                    continue;
                AddShip(hangarShip);
            }

            HangarShipInfoOverlay = Add(new ShipInfoOverlayComponent(Screen, Screen.ParentUniverse.UState));
            OnHovered = (item) =>
            {
                IShipDesign shipToDisplay = item?.Design;
                if (ActiveHangarModule != null && shipToDisplay?.Name is "DynamicLaunch" or "DynamicInterceptor" or "DynamicAntiShip")
                {
                    ShipModule tempMod = Screen.CreateDesignModule(ActiveHangarModule.UID, ModuleOrientation.Normal, 0, "");
                    tempMod.HangarShipUID = shipToDisplay.Name;
                    tempMod.SetDynamicHangarFromShip();
                    string hangarShip = tempMod.GetHangarShipName(Player);
                    IShipDesign hs = ResourceManager.Ships.GetDesign(hangarShip, throwIfError:false);
                    if (hs != null)
                    {
                        shipToDisplay = hs;
                    }
                }

                HangarShipInfoOverlay.ShowToTopOf(Pos, shipToDisplay);
            };
        }
        
        void AddShip(IShipDesign design)
        {
            AddItem(new(design));
        }

        public override void OnItemHovered(ScrollListItemBase item)
        {
            if (item == null) // we're not hovering the scroll list, just highlight the active ship
            {
                foreach (FighterListItem e in AllEntries)
                {
                    if (ActiveModule?.HangarShipUID == e.Design.Name)
                    {
                        Highlight = new(new RectF(e.X-15, e.Y-5, e.Width+12, e.Height));
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
                ActiveModule.HangarShipUID = fighterItem.Design.Name;
                HangarShipUIDLast          = fighterItem.Name;
                ActiveModule.SetDynamicHangarFromShip();
            }

            base.OnItemClicked(item);
        }

        public override bool HandleInput(InputState input)
        {
            ShipModule activeModule = Screen.ActiveModule;
            ShipModule highlightedModule = Screen.HighlightedModule;

            activeModule ??= highlightedModule;
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
            if (HangarShipUIDLast != "" && activeModule.PermittedHangarRoles.Contains(fighter?.ShipData.GetRole()) && activeModule.MaximumHangarShipSize >= fighter?.SurfaceArea)
            {
                activeModule.HangarShipUID = HangarShipUIDLast;
            }

            ActiveHangarModule.SetDynamicHangarFromShip();
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
