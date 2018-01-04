
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;

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

        public new void Dispose()
        {
            SelectionBox?.RemoveFromParent();
            SelectionBox = null;
            base.Dispose();
        }

        private void Populate()
        {            
            Entries.Clear();
            Copied.Clear();
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
                
                for (int index = indexAtTop;
                    index < Copied.Count
                    && index < indexAtTop + entriesToDisplay;
                    ++index)
                {
                    Entry entry = Copied[index];
                    if (!(entry.item is Ship ship)) continue;
                    if (FighterSubMenu.Menu.HitTest(Screen.Input.CursorPosition))
                    {
                        if (!entry.clickRect.HitTest(input.CursorPosition)) continue;
                        entry.clickRectHover = 1;
                        SelectionBox = new Selector(entry.clickRect);
                        if (!input.InGameSelect ) continue;

                        ActiveModule.hangarShipUID = ship.Name;
                        HangarShipUIDLast = ship.Name;
                        GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                    }
                    else if (ActiveModule.hangarShipUID == ship.Name)
                    {
                        SelectionBox = new Selector(entry.clickRect);

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
            if (HangarNotSelected(activeModule, activeHangarModule)) return;

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

            if (ActiveHangarModule == null) return;
            Screen.DrawRectangle(FighterSubMenu.Menu, Color.TransparentWhite, Color.Black);  
            
            Vector2 bCursor = new Vector2(FighterSubMenu.Menu.X + 15, (float)(FighterSubMenu.Menu.Y + 25));
            for (int i = indexAtTop; i < Entries.Count && i < indexAtTop + entriesToDisplay; i++)
            {
                Entry e = Entries[i];
                Ship ship = e.item as Ship;
                if (ship == null) continue;

                bCursor.Y = e.clickRect.Y;
                spriteBatch.Draw(ResourceManager.Texture(ResourceManager.HullsDict[ship.GetShipData().Hull].IconPath), new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);
                Vector2 tCursor = new Vector2(bCursor.X + 40f, bCursor.Y + 3f);
                spriteBatch.DrawString(Fonts.Arial12Bold, (!string.IsNullOrEmpty(ship.VanityName) ? ship.VanityName : ship.Name), tCursor, Color.White);
                tCursor.Y = tCursor.Y + Fonts.Arial12Bold.LineSpacing;
            }
            SelectionBox?.Draw(spriteBatch);

            FighterSubMenu.Draw();


            base.Draw(spriteBatch);

        }
    }
}
