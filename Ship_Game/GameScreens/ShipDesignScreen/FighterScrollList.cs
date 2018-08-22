
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI;
using Ship_Game.Ships;

// ReSharper disable once CheckNamespace
namespace Ship_Game
{
    public class FighterScrollList : ScrollList
    {
        private readonly GameScreen Screen;
        private Selector SelectionBox;
        private readonly InputState Input;
        public bool ResetOnNextDraw = true;
        public Rectangle Choosefighterrect;
        public ShipModule ActiveHangarModule;
        public ShipModule ActiveModule;
        public string HangarShipUIDLast = "";
        private readonly Submenu FighterSubMenu;

        public FighterScrollList(Submenu fighterList, GameScreen shipDesignScreen) : base(fighterList, 40)
        {
            Screen = shipDesignScreen;
            Input = Screen.Input;
            FighterSubMenu = fighterList;
        }

        public bool HitTest(InputState input)
        {
            return   FighterSubMenu.Menu.HitTest(input.CursorPosition) && ActiveModule != null ;
        }

        private void Populate()
        {
            Reset();
            AddItem(ResourceManager.GetShipTemplate(DynamicHangarType.DynamicLaunch.ToString()));
            AddItem(ResourceManager.GetShipTemplate(DynamicHangarType.DynamicFighter.ToString()));
            AddItem(ResourceManager.GetShipTemplate(DynamicHangarType.DynamicBomber.ToString()));
            foreach (string shipname in EmpireManager.Player.ShipsWeCanBuild)
            {
                if (!ResourceManager.ShipsDict.TryGetValue(shipname, out Ship fighter)) continue;
                string role = ShipData.GetRole(fighter.DesignRole);
                if (!ActiveModule.PermittedHangarRoles.Contains(role)) continue;
                if (fighter.Size > ActiveModule.MaximumHangarShipSize) continue;

                AddItem(ResourceManager.ShipsDict[shipname]);
            }         
        }

        public bool HandleInput(InputState input, ShipModule activeModule, ShipModule highlightedModule)
        {
            base.HandleInput(input);
            activeModule = activeModule ?? highlightedModule;
            if (activeModule?.ModuleType == ShipModuleType.Hangar && !activeModule.IsTroopBay
                && !activeModule.IsSupplyBay)
            {
                ActiveModule = activeModule;
                SetActiveHangarModule(activeModule, ActiveHangarModule);
                
                foreach (Entry e in VisibleExpandedEntries)
                {
                    if (!(e.item is Ship ship)) continue;
                    if (FighterSubMenu.Menu.HitTest(Screen.Input.CursorPosition))
                    {
                        if (!e.CheckHover(input))
                            continue;
                        SelectionBox = e.CreateSelector();
                        if (!input.InGameSelect)
                            continue;
                        ActiveModule.hangarShipUID = ship.Name;
                        HangarShipUIDLast = ship.Name;
                        GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                    }
                    else if (ActiveModule.hangarShipUID == ship.Name)
                    {
                        SelectionBox = e.CreateSelector();
                    }
                }
                return true;
            }
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
            if (HangarShipUIDLast != "" && activeModule.PermittedHangarRoles.Contains(fighter?.shipData.GetRole()) && activeModule.MaximumHangarShipSize >= fighter?.Size)
            {
                activeModule.hangarShipUID = HangarShipUIDLast;
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (ActiveHangarModule == null)
                return;

            Screen.DrawRectangle(FighterSubMenu.Menu, Color.TransparentWhite, Color.Black);  
            
            var bCursor = new Vector2(FighterSubMenu.Menu.X + 15, (FighterSubMenu.Menu.Y + 25));
            foreach (Entry e in VisibleEntries)
            {
                if (!(e.item is Ship ship))
                    continue;
                bCursor.Y = e.Y;
                spriteBatch.Draw(ship.shipData.Icon, new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);
                var tCursor = new Vector2(bCursor.X + 40f, bCursor.Y + 3f);
                //Color color = ShipBuilder.IsDynamicHangar(ship.Name) ? Color.Gold : Color.White;
                Color color = ShipBuilder.GetHangarTextColor(ship.Name);
                spriteBatch.DrawString(Fonts.Arial12Bold, (!string.IsNullOrEmpty(ship.VanityName) ? ship.VanityName : ship.Name), tCursor, color);
                tCursor.Y += Fonts.Arial12Bold.LineSpacing;
            }
            SelectionBox?.Draw(spriteBatch);
            FighterSubMenu.Draw();
            base.Draw(spriteBatch);
        }
    }
}
