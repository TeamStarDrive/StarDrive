﻿using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI;
using Ship_Game.Audio;
using Ship_Game.Commands.Goals;
using Ship_Game.Ships;
using System.Linq;
using SDGraphics;
using SDUtils;
using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;
using Ship_Game.Graphics;
using Ship_Game.Universe.SolarBodies;
using Ship_Game.Universe;
using System;

namespace Ship_Game
{
    public sealed class ExoticSystemsListScreenItem : ScrollListItem<ExoticSystemsListScreenItem> // Moved to UI V2
    {
        public readonly Planet Planet;
        public readonly SolarSystem System;
        public Rectangle SysNameRect;
        public Rectangle PlanetNameRect;
        public Rectangle OrdersRect;
        public Rectangle DistanceRect;
        public Rectangle ResourceRect;
        public Rectangle RichnessRect;
        public Rectangle OwnerRect;

        Empire Player => Universe.Player;
        readonly Color Cream = Colors.Cream;
        readonly Graphics.Font NormalFont = Fonts.Arial20Bold;
        readonly Graphics.Font SmallFont = Fonts.Arial12Bold;
        readonly Graphics.Font TinyFont = Fonts.Arial8Bold;
        readonly Color TextColor = new Color(255, 239, 208);

        Rectangle PlanetIconRect;
        Rectangle ResourceIconRect;
        readonly UITextEntry PlanetNameEntry = new UITextEntry();
        readonly UITextEntry ResourceNameEntry = new UITextEntry();
        UIButton DeployButton;
        readonly float Distance;
        bool MarkedForResearch;
        bool MarkedForMining;
        bool DysonSwarmActiveByPlayer;
        readonly UniverseState Universe;

        UILabel DeployTextInfo;
        UILabel MiningDeployedTextInfo;
        UILabel MiningInProgressTextInfo;
        UILabel Owner;
        bool IsPlanet => Planet != null;
        public bool IsStar => Planet == null;
        public bool IsForResearch => IsStar && System.IsResearchable || Planet?.IsResearchable == true;
        public bool IsForMining => !IsStar && Planet.IsMineable;
        public bool IsForDysonSwarm => IsStar && System.DysonSwarmType > 0;
        ExplorableGameObject SolarBody;

        public ExoticSystemsListScreenItem(ExplorableGameObject solarBody, float distance)
        {
            SolarBody = solarBody;
            if (solarBody is Planet planet) 
            {
                Planet   = planet;
                System   = planet.System;
                Universe = planet.Universe;
            }
            else
            {
                SolarSystem system = solarBody as SolarSystem;
                System   = system;
                Universe = system.Universe;
            }

            Distance = distance / 1000; // Distance from nearest player colony


            if (solarBody.IsResearchable && !solarBody.IsResearchStationDeployedBy(Player))
            {
                foreach (Goal g in Player.AI.Goals)
                {
                    if (g.IsResearchStationGoal(solarBody))
                    {
                        MarkedForResearch = true;
                        break;
                    }
                }
            }
            else if (Planet?.IsMineable == true && Player.AI.Goals.Any(g => g.IsMiningOpsGoal(Planet) && g.TargetShip == null))
            {
                MarkedForMining = true;
            }
            else if (Player.CanBuildDysonSwarmIn(System))
            {
                DysonSwarmActiveByPlayer = System.HasDysonSwarm && System.DysonSwarm.Owner == Player;
            }
        }

        public override void PerformLayout()
        {
            int x = (int)X;
            int y = (int)Y;
            int w = (int)Width;
            int h = (int)Height;
            RemoveAll();

            if (Planet?.IsResearchable == true || System.IsResearchable)
            {
                ButtonStyle researchStyle = MarkedForResearch ? ButtonStyle.Military : ButtonStyle.BigDip;
                LocalizedText researchText = !MarkedForResearch ? GameText.DeployResearchStation : GameText.AbortDeployent;
                DeployButton = Button(researchStyle, researchText, OnResearchClicked);
            }
            else if (Planet?.IsMineable == true)
            {
                ButtonStyle mineableStyle = MarkedForMining ? ButtonStyle.Military : ButtonStyle.Default;
                LocalizedText miningText = !MarkedForMining ? GameText.DeployMiningStation : GameText.AbortDeployent;
                DeployButton = Button(mineableStyle, miningText, OnMiningClicked);
            }
            else
            {
                ButtonStyle dysonStyle = DysonSwarmActiveByPlayer ? ButtonStyle.Military : ButtonStyle.Default;
                LocalizedText dysonText = !DysonSwarmActiveByPlayer ? GameText.BuildDysonSwarm : GameText.KillDysonSwarm;
                DeployButton = Button(dysonStyle, dysonText, OnDysonSwarmClicked);
            }

            DeployButton.Font = Fonts.TahomaBold9;
            int nextX = x;
            Rectangle NextRect(float width)
            {
                int next = nextX;
                nextX += (int)width;
                return new Rectangle(next, y, (int)width, h);
            }

            SysNameRect    = NextRect(w * 0.12f);
            PlanetNameRect = NextRect(w * 0.20f);
            DistanceRect   = NextRect(150);
            ResourceRect   = NextRect(150);
            RichnessRect   = NextRect(100);
            OwnerRect      = NextRect(100);
            OrdersRect     = NextRect(100);

            PlanetIconRect = new Rectangle(PlanetNameRect.X + 5, PlanetNameRect.Y + 5, 50, 50);
            PlanetNameEntry.Text = IsStar ? "" : Planet.Name;
            PlanetNameEntry.SetPos(PlanetIconRect.Right + 10, y);

            ResourceIconRect = new Rectangle(ResourceRect.X + 5, ResourceRect.Y + 10, 20, 20);
            ResourceNameEntry.Text = GetResourceLabel();
            ResourceNameEntry.SetPos(ResourceIconRect.Right + 10, y);

            var btn = ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_168px");
            DeployButton.Rect = new Rectangle(OrdersRect.X + 10, OrdersRect.Y + OrdersRect.Height / 2 - btn.Height / 2, btn.Width, btn.Height);

            AddSystemName();
            AddHostileWarning();
            SetResearchVisibility();
            SetMiningVisibility();
            SetDysonSwarmVisibility();
            AddTextureAndStatus();
            AddDistanceStats();
            AddPlanetName();
            AddResourceName();
            AddRichnessStat();
            AddOwner();
            base.PerformLayout();
        }

        void SetResearchVisibility()
        {
            if (!IsForResearch)
                return;

            Vector2 researchTextBox = new Vector2(DeployButton.Rect.X, DeployButton.Rect.Y + 4);
            DeployTextInfo = Add(new UILabel(researchTextBox, GameText.CannotBuildResearchStationTip2, SmallFont));
            DeployTextInfo.Color = Color.Gray;

            if (!Player.CanBuildResearchStations) 
            {
                DeployButton.Visible = false;
                return;
            }

            if (SolarBody.IsResearchStationDeployedBy(Player))
            {
                DeployButton.Visible = false;
                DeployTextInfo.Text = Localizer.Token(GameText.ResearchStationDeployed);
                DeployTextInfo.Color = Player.EmpireColor;
            }
            else
            {
                DeployButton.Visible = true;
                DeployTextInfo.Visible= false;
            }
        }

        void SetMiningVisibility()
        {
            if (!IsForMining)
                return;

            Vector2 miningTextBox = new Vector2(DeployButton.Rect.X, DeployButton.Rect.Y + 4);
            DeployTextInfo = Add(new UILabel(miningTextBox, GameText.CannotBuildMiningStationTip, SmallFont));
            DeployTextInfo.Color = Color.Gray;
            DeployButton.Visible = false;
            DeployTextInfo.Visible = true;

            
            if (Planet.Mining.Owner != null && Planet.Mining.Owner != Player)
            {
                DeployTextInfo.Text = "";
                return;
            }

            if (!Player.CanBuildMiningStations)
            {
                DeployTextInfo.Text = Localizer.Token(GameText.CannotBuildMiningStationTip2);
                return;
            }

            int numDeployed = Planet.OrbitalStations.Count(s => s.Loyalty.isPlayer && s.IsMiningStation);
            Vector2 miningDeployed = new Vector2(DeployButton.Rect.X + DeployButton.Rect.Width + 5, DeployButton.Rect.Y + 4);
            MiningDeployedTextInfo = Add(new UILabel(miningDeployed, $"Deployed: {numDeployed} ", SmallFont));
            MiningDeployedTextInfo.Color = Player.EmpireColor;
            MiningDeployedTextInfo.Visible = numDeployed > 0;

            int numInProgress = Player.AI.CountGoals(g => g.IsMiningOpsGoal(Planet) && g.TargetShip == null);
            Vector2 miningInProgress = new Vector2(MiningDeployedTextInfo.Rect.X + 
                (MiningDeployedTextInfo.Visible ? MiningDeployedTextInfo.Rect.Width + 10 : 0), DeployButton.Rect.Y + 4);
            string miningInProgressMsg = $"In Progress: {numInProgress}";
            MiningInProgressTextInfo = Add(new UILabel(miningInProgress,miningInProgressMsg, SmallFont));
            MiningInProgressTextInfo.Color = Color.Wheat;
            MiningInProgressTextInfo.Visible = numInProgress > 0;

            if (numDeployed >= Mineable.MaximumMiningStations)
            {
                DeployTextInfo.Visible = false;
                MiningDeployedTextInfo.SetRelPos(DeployButton.Rect.X, DeployButton.Rect.Y + 4);
            }
            else
            {
                DeployButton.Visible = true;
                DeployTextInfo.Visible = false;
            }
        }

        void SetDysonSwarmVisibility()
        {
            if (!IsForDysonSwarm)
                return;

            if (System.HasDysonSwarm && System.DysonSwarm.Owner != Player
                || Player.data.Traits.DysonSwarmType < System.DysonSwarmType
                || !System.HasPlanetsOwnedBy(Player))
            {
                DeployButton.Visible = false;
            }
        }

        public override bool HandleInput(InputState input)
        {
            return base.HandleInput(input);
        }

        void AddSystemName()
        {
            string systemName = System.Name;
            Graphics.Font systemFont = NormalFont.MeasureString(systemName).X <= SysNameRect.Width ? NormalFont : SmallFont;
            var sysNameCursor = new Vector2(SysNameRect.X + SysNameRect.Width / 2 - systemFont.MeasureString(systemName).X / 2f,
                                        2 + SysNameRect.Y + SysNameRect.Height / 2 - systemFont.LineSpacing / 2);

            Label(sysNameCursor, systemName, systemFont, Cream);
        }

        void AddPlanetName()
        {
            if (IsStar)
                return;

            var namePos = new Vector2(PlanetNameEntry.X, PlanetNameEntry.Y + 3);
            Label(namePos, Planet.Name, NormalFont, TextColor);
            // Now add Richness
            namePos.Y += NormalFont.LineSpacing;
            string richness = Planet.LocalizedRichness;
            Label(namePos, richness, SmallFont, TextColor);
        }

        void AddDistanceStats()
        {
            var distancePos = new Vector2(DistanceRect.X + 45, DistanceRect.Y + DistanceRect.Height / 2 - SmallFont.LineSpacing / 2);
            DrawDistance(Distance, distancePos, SmallFont);
        }

        void AddResourceName()
        {
            bool researchable = IsForResearch;
            bool mineable = IsForMining;
            var namePos = new Vector2(ResourceRect.X + 30, ResourceRect.Y + ResourceRect.Height / 2 - SmallFont.LineSpacing / 2);
            Color labelColor = researchable ? Color.CornflowerBlue
                                            : mineable ? Color.White 
                                                       : Color.Gold; // Dyson Swarm
            var resourceName = Label(namePos, GetResourceLabel(), SmallFont, labelColor);

            resourceName.Tooltip = researchable ? new LocalizedText(GameText.ResearchPointsAreAddedInto) 
                                                : mineable ? Planet.Mining.ResourceDescription
                                                           : "";

            Panel(ResourceIconRect, IsForDysonSwarm ? Color.Yellow : Color.White, researchable 
                ? ResourceManager.Texture("NewUI/icon_science") 
                : mineable ? Planet.Mining.ExoticResourceIcon
                           : ResourceManager.Texture("NewUI/icon_projection"));
        }

        string GetResourceLabel()
        {
            return IsForResearch ? "Research"
                                 : IsForMining ? Planet.Mining.TranslatedResourceName.Text
                                               : $"{Localizer.Token(GameText.DysonSwarm)} {System.DysonSwarmType}";
        }

        void AddRichnessStat()
        {
            string richness = IsStar || Planet.IsResearchable ? "" : Planet.Mining.Richness.String(0);
            var sysNameCursor = new Vector2(RichnessRect.X + 30, RichnessRect.Y + RichnessRect.Height / 2 - SmallFont.LineSpacing / 2);

            Label(sysNameCursor, richness, SmallFont, Cream);
        }

        void AddOwner()
        {
            if (IsForDysonSwarm)
            {
                string owner = System.HasDysonSwarm ? System.DysonSwarm.Owner.data.Traits.Singular : "None";
                var ownerNameCursor = new Vector2(OwnerRect.X + 25, OwnerRect.Y + OwnerRect.Height / 2 - SmallFont.LineSpacing / 2);
                Owner = Label(ownerNameCursor, owner, SmallFont, owner == "None" ? Cream : System.DysonSwarm.Owner.EmpireColor);
            }
            else if (Planet?.IsMineable == true)
            {
                string owner = Planet.Mining.HasOpsOwner ? Planet.Mining.Owner.data.Traits.Singular : "None";
                var ownerNameCursor = new Vector2(OwnerRect.X + 25, OwnerRect.Y + OwnerRect.Height / 2 - SmallFont.LineSpacing / 2);
                Owner = Label(ownerNameCursor, owner, SmallFont, owner == "None" ? Cream : Planet.Mining.Owner.EmpireColor);
            }
        }

        void AddHostileWarning()
        {
            if (Player.KnownEnemyStrengthIn(System) > 0)
            {
                SubTexture flash = ResourceManager.Texture("Ground_UI/EnemyHere");
                UIPanel enemyHere = Panel(SysNameRect.X + SysNameRect.Width - 40, SysNameRect.Y + 5, flash);
                enemyHere.Tooltip = GameText.IndicatesThatHostileForcesWere;
            }
        }

        void AddTextureAndStatus()
        {
            var icon = new Rectangle(PlanetNameRect.X + 5, PlanetNameRect.Y + 5, PlanetNameRect.Height - 10, PlanetNameRect.Height - 10);
            Add(new UIPanel(icon, ResourceManager.Texture(IsStar ? System.Sun.IconPath : Planet.IconPath))
            {
                Tooltip = GameText.PlanetTypeAndRichnessThe
            });

        }

        void DrawDistance(float distance, Vector2 namePos, Graphics.Font spriteFont)
        {
            DistanceDisplay distanceDisplay = new DistanceDisplay(distance);
            if (distance > 0)
                Label(namePos, distanceDisplay.Text, spriteFont, distanceDisplay.Color);
        }

        void OnResearchClicked(UIButton b)
        {
            GameAudio.EchoAffirmative();
            if (!MarkedForResearch)
            {
                if (IsStar)
                    Player.AI.AddGoalAndEvaluate(new ProcessResearchStation(Player, System, System.SelectStarResearchStationPos()));
                else
                    Player.AI.AddGoalAndEvaluate(new ProcessResearchStation(Player, Planet));

                DeployButton.Text = GameText.AbortDeployent;
                DeployButton.Style = ButtonStyle.Military;
                MarkedForResearch = true;
            }
            else
            {
                Player.AI.CancelResearchStation(Planet);
                DeployButton.Text = GameText.DeployResearchStation;
                DeployButton.Style = ButtonStyle.BigDip;
            }
        }

        void OnMiningClicked(UIButton b)
        {
            if (!MarkedForMining) 
            { 
                Player.AI.AddGoalAndEvaluate(new MiningOps(Player, Planet));
                if (!Planet.Mining.CanAddMiningStationFor(Player))
                {
                    DeployButton.Text = GameText.AbortDeployent;
                    DeployButton.Style = ButtonStyle.Military;
                    MarkedForMining = true;
                }
            }
            else
            {
                Player.AI.CancelMiningStation(Planet);
                if (Planet.Mining.CanAddMiningStationFor(Player))
                {
                    DeployButton.Text = GameText.DeployMiningStation;
                    DeployButton.Style = ButtonStyle.Default;
                    MarkedForMining = false;
                }
            }

            SetMiningVisibility();
        }

        void OnDysonSwarmClicked(UIButton b)
        {
            if (System.HasDysonSwarm)
            {
                System.KillDysonSwarm();
                DeployButton.Text = GameText.BuildDysonSwarm;
                DeployButton.Style = ButtonStyle.Default;
                DysonSwarmActiveByPlayer = false;
                Owner.Text = "None";
                Owner.Color = Cream;
            }
            else
            {
                System.ActivateDysonSwarm(Player);
                DeployButton.Text = GameText.KillDysonSwarm;
                DeployButton.Style = ButtonStyle.Military;
                DysonSwarmActiveByPlayer = true;
                Owner.Text = Player.data.Traits.Singular;
                Owner.Color = Player.EmpireColor;
            }
        }
    }
}
