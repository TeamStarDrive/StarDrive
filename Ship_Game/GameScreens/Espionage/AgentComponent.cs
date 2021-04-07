using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;

namespace Ship_Game.GameScreens.Espionage
{
    public sealed class AgentComponent : UIElementContainer
    {
        public Agent SelectedAgent;

        public Rectangle ComponentRect;

        public Rectangle SubRect;

        public Rectangle OpsSubRect;

        public ScrollList2<AgentListItem> AgentSL;
        
        public ScrollList2<MissionListItem> OpsSL;

        private ScreenManager ScreenManager;

        public DanButton RecruitButton;

        public EspionageScreen EspionageScreen;

        private MissionListItem Training;

        private MissionListItem Infiltrate;

        private MissionListItem Assassinate;

        private MissionListItem Sabotage;

        private MissionListItem StealTech;

        private MissionListItem StealShip;

        private MissionListItem InciteRebellion;

        private int AvailableSpies;
        private int SpyLimit;

        public AgentComponent(EspionageScreen espionageScreen, Rectangle r, Rectangle operationsRect) : base(r)
        {
            EspionageScreen = espionageScreen;

            ComponentRect = r;
            ScreenManager = Empire.Universe.ScreenManager;
            SubRect = new Rectangle(ComponentRect.X, ComponentRect.Y + 25, ComponentRect.Width, ComponentRect.Height - 25);
            OpsSubRect = new Rectangle(operationsRect.X + 20, ComponentRect.Y + 25, ComponentRect.Width, ComponentRect.Height - 25);
            AgentSL = new ScrollList2<AgentListItem>(new Submenu(ComponentRect), 40);
            AgentSL.OnClick = OnAgentItemClicked;
            foreach (Agent agent in EmpireManager.Player.data.AgentList)
                AgentSL.AddItem(new AgentListItem { Agent = agent });
            Add(AgentSL);



            Rectangle c = ComponentRect;
            c.X = OpsSubRect.X;
            OpsSL = new ScrollList2<MissionListItem>(new Submenu(c), 30);
            Add(OpsSL);
            Training        = new MissionListItem(AgentMission.Training, this);
            Infiltrate      = new MissionListItem(AgentMission.Infiltrate, this);
            Assassinate     = new MissionListItem(AgentMission.Assassinate, this);
            Sabotage        = new MissionListItem(AgentMission.Sabotage, this);
            StealTech       = new MissionListItem(AgentMission.StealTech, this);
            StealShip       = new MissionListItem(AgentMission.Robbery, this);
            InciteRebellion = new MissionListItem(AgentMission.InciteRebellion, this);
            OpsSL.AddItem(Training);
            OpsSL.AddItem(Infiltrate);
            OpsSL.AddItem(Assassinate);
            OpsSL.AddItem(Sabotage);
            OpsSL.AddItem(StealTech);
            OpsSL.AddItem(StealShip);
            OpsSL.AddItem(InciteRebellion);
            RecruitButton = new DanButton(new Vector2(ComponentRect.X, ComponentRect.Y + ComponentRect.Height + 5f), Localizer.Token(GameText.TrainNew))
            {
                Toggled = true
            };
            Checkbox(OpsSubRect.X - 10, RecruitButton.r.Y,      () => EmpireManager.Player.data.SpyMissionRepeat, "Repeat Missions", 0);
            Checkbox(OpsSubRect.X - 10, RecruitButton.r.Y + 15, () => EmpireManager.Player.data.SpyMute,          "Mute Spies",      0);
            //PerformLayout();
        }

        void OnAgentItemClicked(AgentListItem item)
        {
            SelectedAgent = item.Agent;
            foreach (MissionListItem mission in OpsSL.AllEntries)
                mission.UpdateMissionAvailability();
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            SubTexture money    = ResourceManager.Texture("NewUI/icon_money");
            SubTexture iconLock = ResourceManager.Texture("NewUI/icon_lock");

            batch.FillRectangle(SubRect, Color.Black);

            
            RecruitButton.Draw(ScreenManager);
            var moneyRect = new Rectangle(RecruitButton.r.X, RecruitButton.r.Y + 30, 21, 20);
            batch.Draw(money, moneyRect, Color.White);

            var costPos = new Vector2(moneyRect.X + 25, moneyRect.Y + 10 - Fonts.Arial12Bold.LineSpacing / 2);

            int cost = ResourceManager.AgentMissionData.AgentCost + ResourceManager.AgentMissionData.TrainingCost;
            batch.DrawString(Fonts.Arial12Bold, cost.ToString(), costPos, Color.White);

            base.Draw(batch, elapsed);

            var spyLimit = new Rectangle(moneyRect.X + 65, moneyRect.Y, 21, 20);
            batch.Draw(iconLock, spyLimit, Color.White);
            var spyLimitPos = new Vector2((spyLimit.X + 25), (spyLimit.Y + 10 - Fonts.Arial12.LineSpacing / 2));

            SpyLimit = EmpireManager.Player.GetEmpireAI().EmpireSpyLimit;
            AvailableSpies = SpyLimit - EmpireManager.Player.data.AgentList.Count;
            if (SpyLimit < 0) SpyLimit = 0;
            batch.DrawString(Fonts.Arial12, $"For Hire : {AvailableSpies} / {SpyLimit}", spyLimitPos, Color.White);

            if (SelectedAgent != null)
            {
                batch.FillRectangle(OpsSubRect, Color.Black);
                OpsSL.Draw(batch, elapsed);
            }
            base.Draw(batch, elapsed);
        }

        public static string GetName(string[] tokens)
        {
            var firstNames = new Array<string>();
            var lastNames = new Array<string>();
            foreach (string t in tokens)
            {
                if (t.Split(' ').Length != 1)
                {
                    lastNames.Add(t);
                }
                else
                {
                    firstNames.Add(t);
                    lastNames.Add(t);
                }
            }

            string first = RandomMath.RandItem(firstNames);
            string last  = RandomMath.RandItem(lastNames);
            return $"{first} {last}";
        }


        string[] LoadNames()
        {
            string playerNames = $"Content/NameGenerators/spynames_{EmpireManager.Player.data.Traits.ShipType}.txt";
            string names = File.Exists(playerNames)
                ? File.ReadAllText(playerNames)
                : File.ReadAllText("Content/NameGenerators/spynames_Humans.txt");
            return names.Split(',');
        }

        //added by gremlin deveksmod Spy Handleinput
        public override bool HandleInput(InputState input)
        {
            if (AgentSL.HandleInput(input))
                return true;

            if (SelectedAgent != null)
            {
                foreach (MissionListItem mission in OpsSL.AllEntries)
                    mission.UpdateMissionAvailability();

                if (OpsSL.HandleInput(input))
                    return true;
            }

            if (RecruitButton.r.HitTest(input.CursorPosition))
            {
                ToolTip.CreateTooltip(Localizer.Token(GameText.RecruitANewAgentTo));
            }

            if (RecruitButton.HandleInput(input))
            {
                if (EmpireManager.Player.Money < (ResourceManager.AgentMissionData.AgentCost + ResourceManager.AgentMissionData.TrainingCost) 
                    || AvailableSpies <= 0)
                {
                    GameAudio.NegativeClick();
                }
                else
                {
                    EmpireManager.Player.AddMoney(-ResourceManager.AgentMissionData.AgentCost);
                    var agent = new Agent
                    {
                        Name = GetName(LoadNames()),
                        Age = RandomMath.RandomBetween(20, 30)
                    };

                    // Added new agent information
                    int randomPlanetIndex = RandomMath.InRange(EmpireManager.Player.GetPlanets().Count);
                    agent.HomePlanet = EmpireManager.Player.GetPlanets()[randomPlanetIndex].Name;
                    EmpireManager.Player.data.AgentList.Add(agent);
                    AgentSL.AddItem(new AgentListItem{ Agent = agent });
                    agent.AssignMission(AgentMission.Training, EmpireManager.Player, "");
                }
            }

            return base.HandleInput(input);
        }

        public void Reinitialize()
        {
            if (SelectedAgent == null)
                return;
            foreach (MissionListItem mission in OpsSL.AllEntries)
                mission.UpdateMissionAvailability();
        }
    }
}
