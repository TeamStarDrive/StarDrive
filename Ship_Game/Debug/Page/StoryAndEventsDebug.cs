﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Input;

namespace Ship_Game.Debug.Page
{
    class StoryAndEventsDebug : DebugPage
    {
        class EvtItem : ScrollListItem<EvtItem>
        {
            readonly UILabel First;
            readonly UILabel Second;
            public EvtItem(ExplorationEvent e, Outcome o)
            {
                First = new UILabel($"{e.FileName}.xml - {e.Story} - {e.LocalizedName}");
                Second = new UILabel($"Outcome-{e.PotentialOutcomes.IndexOf(o)} {o.TitleText}");
            }
            public EvtItem(Encounter e)
            {
                First = new UILabel($"{e.FileName}.xml - {e.Name}");
                Second = new UILabel(e.DescriptionText.Substring(0, 20));
            }
            public override void PerformLayout()
            {
                First.Pos = Pos;
                Second.Pos = new Vector2(Pos.X, Pos.Y + 16f);
                RequiresLayout = false;
            }
            public override void Draw(SpriteBatch batch, DrawTimes elapsed)
            {
                First.Draw(batch, elapsed);
                Second.Draw(batch, elapsed);
            }
        }

        UniverseScreen Universe;
        Submenu Menu;
        readonly ScrollList2<EvtItem> ExplorationEvents;
        readonly ScrollList2<EvtItem> EncounterDialogs;

        public StoryAndEventsDebug(UniverseScreen screen, DebugInfoScreen parent)
            : base(parent, DebugModes.StoryAndEvents)
        {
            Universe = screen;

            ExplorationEvent[] events = ResourceManager.EventsDict.Values.ToArray();

            Menu = Add(new Submenu(50, 260, 400, 600));
            Menu.AddTab("ExpEvts");
            Menu.AddTab("Encounters");
            Menu.OnTabChange = OnTabChanged;

            ExplorationEvents = Menu.Add(new ScrollList2<EvtItem>(new Submenu(Menu.Rect)));
            ExplorationEvents.EnableItemEvents = true;
            ExplorationEvents.EnableItemHighlight = true;

            foreach (ExplorationEvent evt in events)
            {
                for (int i = 0; i < evt.PotentialOutcomes.Count; ++i)
                {
                    Outcome outcome = evt.PotentialOutcomes[i];
                    var item = ExplorationEvents.AddItem(new EvtItem(evt, outcome));
                    item.OnClick = () =>
                    {
                        Planet homeworld = screen.player.GetPlanets()[0];
                        PlanetGridSquare tile = homeworld.TilesList.Find(t => t.IsTileFree(screen.player));
                        evt.DebugTriggerOutcome(homeworld, screen.player, outcome, tile);
                    };
                }
            }

            EncounterDialogs = Menu.Add(new ScrollList2<EvtItem>(new Submenu(Menu.Rect)));
            EncounterDialogs.EnableItemEvents = true;
            EncounterDialogs.EnableItemHighlight = true;

            foreach (Encounter e in ResourceManager.Encounters)
            {
                Empire faction = EmpireManager.GetEmpireByName(e.Faction) ?? EmpireManager.Corsairs;
                var item = EncounterDialogs.AddItem(new EvtItem(e));
                item.OnClick = () =>
                {
                    EncounterPopup.Show(screen, screen.player, faction, e);
                };
            }

            Menu.SelectedIndex = 0;

            Label(Width - 200, 200, "Ctrl+M to spawn Meteors");
        }

        void OnTabChanged(int tab)
        {
            ExplorationEvents.Visible = tab == 0;
            EncounterDialogs.Visible = tab == 1;
        }

        public override bool HandleInput(InputState input)
        {
            if (input.IsCtrlKeyDown && input.KeyPressed(Keys.M))
            {
                SolarSystem system = null;
                foreach (var sys in UniverseScreen.SolarSystemList)
                {
                    if (sys.IsVisible)
                    {
                        system = sys;
                        break;
                    }
                }

                if (system != null && system.PlanetList.Count > 0)
                {
                    RandomEventManager.CreateMeteors(system.PlanetList[0]);
                }
            }
            return base.HandleInput(input);
        }
    }
}
