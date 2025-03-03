﻿using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Ship_Game.AI;
using Ship_Game.GameScreens.ShipDesign;
using Ship_Game.Ships;
using Ship_Game.UI;

// ReSharper disable once CheckNamespace
namespace Ship_Game
{
    public class FighterScrollList : ScrollList<FighterListItem>
    {
        readonly ShipDesignScreen Screen;
        Empire Player => Screen.ParentUniverse.Player;
        public ShipModule ActiveHangarModule;
        public ShipModule ActiveModule;
        public string HangarShipUIDLast = "";
        ShipInfoOverlayComponent HangarShipInfoOverlay;

        public FighterScrollList(IClientArea rectSource, ShipDesignScreen shipDesignScreen) : base(rectSource)
        {
            Screen = shipDesignScreen;
        }

        public bool HitTest(InputState input)
        {
            return ActiveModule != null && base.HitTest(input.CursorPosition);
        }

        void Populate()
        {
            Reset();
            AddShip(ResourceManager.Ships.GetDesign(DynamicHangarOptions.DynamicLaunch.ToString()));
            AddShip(ResourceManager.Ships.GetDesign(DynamicHangarOptions.DynamicInterceptor.ToString()));
            AddShip(ResourceManager.Ships.GetDesign(DynamicHangarOptions.DynamicAntiShip.ToString()));
            foreach (IShipDesign hangarShip in Screen.ParentUniverse.Player.ShipsWeCanBuild)
            {
                string role = hangarShip.HullRole.ToString();
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
                        Highlight = new(e.Rect.Bevel(4,2), new(Color.Yellow, 25));
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

        public ShipModule GetFighterHangar()
        {
            ShipModule activeModule = Screen.ActiveModule;
            ShipModule highlightedModule = Screen.HighlightedModule;

            activeModule ??= highlightedModule;
            if (activeModule?.ModuleType == ShipModuleType.Hangar &&
                !activeModule.IsTroopBay && !activeModule.IsSupplyBay)
            {
                return activeModule;
            }
            return null;
        }

        public override bool HandleInput(InputState input)
        {
            ActiveModule = GetFighterHangar();
            if (ActiveModule != null)
            {
                SetActiveHangarModule(ActiveModule, ActiveHangarModule);
                
                // always capture input if mouse is hovering here
                return base.HandleInput(input) || HitTest(input); 
            }
            
            ActiveHangarModule = null;
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
    }
}
