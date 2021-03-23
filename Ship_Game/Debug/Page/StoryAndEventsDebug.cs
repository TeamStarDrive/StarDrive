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

        class ExpEventItem : ScrollListItem<ExpEventItem>
        {
            readonly UILabel Story;
            readonly UILabel Outcome;
            public ExpEventItem(ExplorationEvent e, Outcome o)
            {
                Story = new UILabel($"Story: {e.Story} Event: {e.Name}");
                int idx = e.PotentialOutcomes.IndexOf(o);
                Outcome = new UILabel($"Outcome: #{idx} {o.TitleText}");
            }
            public override void PerformLayout()
            {
                Story.Pos = Pos;
                Outcome.Pos = new Vector2(Pos.X, Pos.Y + 16f);
                RequiresLayout = false;
            }
            public override void Draw(SpriteBatch batch, DrawTimes elapsed)
            {
                Story.Draw(batch, elapsed);
                Outcome.Draw(batch, elapsed);
            }
        }

        public StoryAndEventsDebug(UniverseScreen screen, DebugInfoScreen parent)
            : base(parent, DebugModes.StoryAndEvents)
        {
            DebugExpEvents = ResourceManager.EventsDict.Values.ToArray();

            Label(new Vector2(50, 150), "Click to trigger Exploration Events:");

            var eventsList = Add(new ScrollList2<ExpEventItem>(new Rectangle(50, 210, 400, 800)));
            eventsList.EnableItemEvents = true;
            eventsList.EnableItemHighlight = true;

            foreach (ExplorationEvent evt in DebugExpEvents)
            {
                for (int i = 0; i < evt.PotentialOutcomes.Count; ++i)
                {
                    Outcome outcome = evt.PotentialOutcomes[i];
                    var item = eventsList.AddItem(new ExpEventItem(evt, outcome));
                    item.OnClick = () =>
                    {
                        Planet homeworld = screen.player.GetPlanets()[0];
                        PlanetGridSquare tile = homeworld.TilesList.Find(t => t.IsTileFree(screen.player));
                        evt.DebugTriggerOutcome(homeworld, screen.player, outcome, tile);
                    };
                }
            }
        }
    }
}
