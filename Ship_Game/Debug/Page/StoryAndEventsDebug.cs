using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship_Game.Debug.Page
{
    class StoryAndEventsDebug : DebugPage
    {
        ExplorationEvent[] DebugExpEvents;

        class EvtItem : ScrollListItem<EvtItem>
        {
            readonly UILabel First;
            readonly UILabel Second;
            public EvtItem(ExplorationEvent e, Outcome o)
            {
                First = new UILabel($"Story: {e.Story} Event: {e.Name}");
                int idx = e.PotentialOutcomes.IndexOf(o);
                Second = new UILabel($"Outcome: #{idx} {o.TitleText}");
            }
            public EvtItem(Encounter e)
            {
                First = new UILabel($"Faction: {e.Faction} Encounter: {e.Name}");
                Second = new UILabel(e.DescriptionText);
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

        Submenu Menu;
        ScrollList2<EvtItem> ExplorationEvents;
        ScrollList2<EvtItem> EncounterDialogs;

        public StoryAndEventsDebug(UniverseScreen screen, DebugInfoScreen parent)
            : base(parent, DebugModes.StoryAndEvents)
        {
            DebugExpEvents = ResourceManager.EventsDict.Values.ToArray();

            Menu = Add(new Submenu(50, 200, 400, 800));
            Menu.AddTab("ExpEvts");
            Menu.AddTab("Encounters");
            Menu.OnTabChange = OnTabChanged;

            ExplorationEvents = Menu.Add(new ScrollList2<EvtItem>(new Submenu(Menu.Rect)));
            ExplorationEvents.EnableItemEvents = true;
            ExplorationEvents.EnableItemHighlight = true;

            foreach (ExplorationEvent evt in DebugExpEvents)
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
                foreach (Message m in e.MessageList)
                {
                    Empire faction = EmpireManager.GetEmpireByName(e.Faction) ?? EmpireManager.Corsairs;
                    var item = EncounterDialogs.AddItem(new EvtItem(e));
                    item.OnClick = () =>
                    {
                        EncounterPopup.Show(screen, screen.player, faction, e);
                    };
                }
            }

            Menu.SelectedIndex = 0;
        }

        private void OnTabChanged(int tab)
        {
            ExplorationEvents.Visible = tab == 0;
            EncounterDialogs.Visible = tab == 1;
        }
    }
}
