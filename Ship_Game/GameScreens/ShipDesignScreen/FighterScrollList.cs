using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;

// ReSharper disable once CheckNamespace
namespace Ship_Game
{
    public class FighterScrollList : ScrollList
    {
        private readonly ShipDesignScreen Screen;
        private Selector SelectionBox;
        private readonly InputState Input;
        public bool ResetOnNextDraw = true;
        public Rectangle Choosefighterrect;
        public ShipModule ActiveHangarModule;
        public ShipModule ActiveModule;
        public string HangarShipUIDLast = "Undefined";
        private Submenu FighterSubMennu;

        public FighterScrollList(Submenu fighterList, ShipDesignScreen shipDesignScreen) : base(fighterList, 40)
        {
            Screen = shipDesignScreen;
            Input = Screen.Input;
            FighterSubMennu = fighterList;
        }

        private void DestroySelectionBox()
        {
            SelectionBox?.RemoveFromParent();
            SelectionBox = null;
        }

        private void Populate()
        {            
            Entries.Clear();
            Copied.Clear();
            foreach (string shipname in EmpireManager.Player.ShipsWeCanBuild)
            {
                if (!ResourceManager.ShipsDict.TryGetValue(shipname, out Ship fighter)) continue;
                if (!ActiveModule.PermittedHangarRoles.Contains(ShipData.GetRole(fighter.DesignRole))) continue;
                if (fighter.Size >= ActiveModule.MaximumHangarShipSize) continue;

                AddItem(Ship_Game.ResourceManager.ShipsDict[shipname]);
            }         
        }
        private void UpdateHangarOptions(ShipModule mod)
        {
            if (ActiveHangarModule != mod && mod.ModuleType == ShipModuleType.Hangar)
            {
                Populate();
            }
        }

        public bool HandleInput(InputState input, ShipModule activeModule, ShipModule highlightedModule )
        {
         
            //if (HangarShipUIDLast != "Undefined")
            //{
            //    Populate();
            //    ActiveModule.hangarShipUID = HangarShipUIDLast;
            //}
            //else if (Entries.Count > 0 && Entries[0].item is Ship ship)
            //{
            //    activeModule.hangarShipUID = ship.Name;
            //}

            if (activeModule != null)
            {
                if (activeModule.ModuleType == ShipModuleType.Hangar && !activeModule.IsTroopBay
                    && !activeModule.IsSupplyBay)
                {
                    ActiveModule = activeModule;
                    SetActiveHangarModule(activeModule, ActiveHangarModule);
                    //UpdateHangarOptions(activeModule);
                    HandleInput(input);
                    for (int index = indexAtTop;
                        index < Copied.Count
                        && index < indexAtTop + entriesToDisplay;
                        ++index)
                    {
                        Entry entry = Copied[index];
                        if (entry.clickRect.HitTest(input.CursorPosition))
                        {
                            SelectionBox = new Selector(entry.clickRect);
                            entry.clickRectHover = 1;
                            SelectionBox = new Selector(entry.clickRect);
                            if (!input.InGameSelect) continue;

                            ActiveModule.hangarShipUID = (entry.item as Ship).Name;
                            HangarShipUIDLast = (entry.item as Ship).Name;
                            GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                            return true;
                        }
                    }
                }
            }
            else if (highlightedModule != null && highlightedModule.ModuleType == ShipModuleType.Hangar
                     && (!highlightedModule.IsTroopBay && !highlightedModule.IsSupplyBay))
            {
                HandleInput(input);
                for (int index = indexAtTop;
                    index < Copied.Count
                    && index < indexAtTop + entriesToDisplay;
                    ++index)
                {
                    Entry entry = Copied[index];
                    if (!entry.clickRect.HitTest(input.CursorPosition)) continue;
                    SelectionBox = new Selector(entry.clickRect);
                    entry.clickRectHover = 1;
                    SelectionBox = new Selector(entry.clickRect);
                    if (!input.InGameSelect) continue;
                    highlightedModule.hangarShipUID = (entry.item as Ship).Name;
                    HangarShipUIDLast = (entry.item as Ship).Name;
                    GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                    return true;
                }
            }

            return base.HandleInput(input);

            
        }

        public void SetActiveHangarModule(ShipModule activeModule, ShipModule activeHangarModule)
        {
            if (activeHangarModule == activeModule || activeModule.ModuleType != ShipModuleType.Hangar) return;

            ActiveHangarModule = activeModule;
            Populate();

            Ship fighter = Ship_Game.ResourceManager.GetShipTemplate(HangarShipUIDLast, false);
            if (HangarShipUIDLast != "Undefined" && activeModule.PermittedHangarRoles.Contains(fighter?.shipData.GetRole()) && activeModule.MaximumHangarShipSize >= fighter?.Size)
            {
                activeModule.hangarShipUID = HangarShipUIDLast;
            }
            else if (Entries.Count > 0)
            {
                activeModule.hangarShipUID = (Entries[0].item as Ship).Name;
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {

            Vector2 bCursor = new Vector2(FighterSubMennu.Menu.X + 15, (float)(FighterSubMennu.Menu.Y + 25));
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
            FighterSubMennu.Draw();


            base.Draw(spriteBatch);

        }
    }
}
