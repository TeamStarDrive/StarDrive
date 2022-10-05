using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SDGraphics;
using SDUtils;
using Ship_Game.UI;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.Debug.Page;

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

    readonly ScrollList<EvtItem> ExplorationEvents;
    readonly ScrollList<EvtItem> EncounterDialogs;

    public StoryAndEventsDebug(DebugInfoScreen parent) : base(parent, DebugModes.StoryAndEvents)
    {
        ExplorationEvent[] events = ResourceManager.EventsDict.Values.ToArr();

        RectF evtR = new(50, 260, 400, 600);
        LocalizedText[] evtTabs = { "ExpEvts", "Encounters" };
        var menu = base.Add(new SubmenuScrollList<EvtItem>(evtR, evtTabs));
        menu.OnTabChange = OnTabChanged;

        ExplorationEvents = menu.List;
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
                    Planet homeworld = Player.GetPlanets()[0];
                    PlanetGridSquare tile = homeworld.TilesList.Find(t => t.IsTileFree(Player));
                    evt.DebugTriggerOutcome(homeworld, Player, outcome, tile);
                };
            }
        }

        EncounterDialogs = menu.Add(new SubmenuScrollList<EvtItem>(new(menu.Rect))).List;
        EncounterDialogs.EnableItemEvents = true;
        EncounterDialogs.EnableItemHighlight = true;

        foreach (Encounter e in ResourceManager.Encounters)
        {
            Empire faction = Universe.GetEmpireByName(e.Faction) ?? Universe.Corsairs;
            var item = EncounterDialogs.AddItem(new EvtItem(e));
            item.OnClick = () =>
            {
                EncounterPopup.Show(Screen, Player, faction, e);
            };
        }

        menu.SelectedIndex = 0;

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
            SolarSystem system = Universe.Systems.Find(s => s.InFrustum);
            if (system != null && system.PlanetList.Count > 0)
            {
                Universe.Events.CreateMeteors(system.PlanetList[0]);
            }
        }
        return base.HandleInput(input);
    }
}