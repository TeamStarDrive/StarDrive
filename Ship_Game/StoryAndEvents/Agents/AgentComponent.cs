using System;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;

namespace Ship_Game
{
    public sealed class AgentComponent : UIElementContainer
    {
        public Agent SelectedAgent;

        public Rectangle ComponentRect;

        public Rectangle SubRect;

        public Rectangle OpsSubRect;

        public ScrollList<AgentListItem> AgentSL;
        public ScrollList<MissionEntry> OpsSL;

        private ScreenManager ScreenManager;

        public DanButton RecruitButton;

        public EspionageScreen EspionageScreen;

        private MissionEntry Training;

        private MissionEntry Infiltrate;

        private MissionEntry Assassinate;

        private MissionEntry Sabotage;

        private MissionEntry StealTech;

        private MissionEntry StealShip;

        private MissionEntry InciteRebellion;

        private int spyLimitCount;
        private int empirePlanetSpys;

        public AgentComponent(EspionageScreen espionageScreen, Rectangle r, Rectangle operationsRect) : base(espionageScreen, r)
        {
            EspionageScreen = espionageScreen;

            ComponentRect = r;
            ScreenManager = Empire.Universe.ScreenManager;
            SubRect = new Rectangle(ComponentRect.X, ComponentRect.Y + 25, ComponentRect.Width, ComponentRect.Height - 25);
            OpsSubRect = new Rectangle(operationsRect.X + 20, ComponentRect.Y + 25, ComponentRect.Width, ComponentRect.Height - 25);

            AgentSL = new ScrollList<AgentListItem>(new Submenu(ComponentRect), 40);
            AgentSL.OnClick = OnAgentItemClicked;
            foreach (Agent agent in EmpireManager.Player.data.AgentList)
                AgentSL.AddItem(new AgentListItem{ Agent = agent });

            Rectangle c = ComponentRect;
            c.X = OpsSubRect.X;
            OpsSL = new ScrollList<MissionEntry>(new Submenu(c), 30);
            Training        = new MissionEntry(AgentMission.Training, this);
            Infiltrate      = new MissionEntry(AgentMission.Infiltrate, this);
            Assassinate     = new MissionEntry(AgentMission.Assassinate, this);
            Sabotage        = new MissionEntry(AgentMission.Sabotage, this);
            StealTech       = new MissionEntry(AgentMission.StealTech, this);
            StealShip       = new MissionEntry(AgentMission.Robbery, this);
            InciteRebellion = new MissionEntry(AgentMission.InciteRebellion, this);
            OpsSL.AddItem(Training);
            OpsSL.AddItem(Infiltrate);
            OpsSL.AddItem(Assassinate);
            OpsSL.AddItem(Sabotage);
            OpsSL.AddItem(StealTech);
            OpsSL.AddItem(StealShip);
            OpsSL.AddItem(InciteRebellion);
            RecruitButton = new DanButton(new Vector2(ComponentRect.X, ComponentRect.Y + ComponentRect.Height + 5f), Localizer.Token(2179))
            {
                Toggled = true
            };
            Checkbox(OpsSubRect.X - 10, RecruitButton.r.Y,      () => EmpireManager.Player.data.SpyMissionRepeat, "Repeat Missions", 0);
            Checkbox(OpsSubRect.X - 10, RecruitButton.r.Y + 15, () => EmpireManager.Player.data.SpyMute,          "Mute Spies",      0);
        }

        void OnAgentItemClicked(AgentListItem item)
        {
            SelectedAgent = item.Agent;
            foreach (MissionEntry mission in OpsSL.AllEntries)
                mission.UpdateMissionAvailability();
        }

        public override void Draw(SpriteBatch batch)
        {
            SubTexture money    = ResourceManager.Texture("NewUI/icon_money");
            SubTexture iconLock = ResourceManager.Texture("NewUI/icon_lock");

            batch.FillRectangle(SubRect, Color.Black);
            AgentSL.Draw(batch);

            RecruitButton.Draw(ScreenManager);
            var moneyRect = new Rectangle(RecruitButton.r.X, RecruitButton.r.Y + 30, 21, 20);
            batch.Draw(money, moneyRect, Color.White);

            var costPos = new Vector2(moneyRect.X + 25, moneyRect.Y + 10 - Fonts.Arial12Bold.LineSpacing / 2);

            int cost = ResourceManager.AgentMissionData.AgentCost + ResourceManager.AgentMissionData.TrainingCost;
            batch.DrawString(Fonts.Arial12Bold, cost.ToString(), costPos, Color.White);

            base.Draw(batch);

            var spyLimit = new Rectangle(moneyRect.X + 65, moneyRect.Y, 21, 20);
            batch.Draw(iconLock, spyLimit, Color.White);
            var spyLimitPos = new Vector2((spyLimit.X + 25), (spyLimit.Y + 10 - Fonts.Arial12.LineSpacing / 2));

            empirePlanetSpys = EmpireManager.Player.NumPlanets / 3 + 3;
            spyLimitCount = (empirePlanetSpys - EmpireManager.Player.data.AgentList.Count);
            if (empirePlanetSpys < 0) empirePlanetSpys = 0;
            batch.DrawString(Fonts.Arial12, $"For Hire : {spyLimitCount} / {empirePlanetSpys}", spyLimitPos, Color.White);

            if (SelectedAgent != null)
            {
                batch.FillRectangle(OpsSubRect, Color.Black);
                OpsSL.Draw(batch);
            }
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
                if (OpsSL.HandleInput(input))
                    return true;
            }

            if (RecruitButton.r.HitTest(input.CursorPosition))
            {
                ToolTip.CreateTooltip(Localizer.Token(2180));
            }

            if (RecruitButton.HandleInput(input))
            {
                if (EmpireManager.Player.Money < (ResourceManager.AgentMissionData.AgentCost + ResourceManager.AgentMissionData.TrainingCost) || spyLimitCount <= 0)//EmpireManager.Player.data.AgentList.Count >= EmpireManager.Player.GetPlanets().Count)
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
            foreach (MissionEntry mission in OpsSL.AllEntries)
                mission.UpdateMissionAvailability();
        }
    }
}