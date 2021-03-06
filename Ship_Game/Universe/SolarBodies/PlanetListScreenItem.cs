using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI;
using Ship_Game.Audio;
using Ship_Game.Commands.Goals;
using Ship_Game.Ships;
using Ship_Game.SpriteSystem;

namespace Ship_Game
{
    public sealed class PlanetListScreenItem : ScrollListItem<PlanetListScreenItem> // Moved to UI V2
    {
        public readonly Planet Planet;
        public Rectangle SysNameRect;
        public Rectangle PlanetNameRect;
        public Rectangle FertRect;
        public Rectangle RichRect;
        public Rectangle PopRect;
        public Rectangle OwnerRect;
        public Rectangle OrdersRect;
        public Rectangle DistanceRect;

        private readonly Empire Player         = EmpireManager.Player;
        private readonly Color Cream           = Colors.Cream;
        private readonly SpriteFont NormalFont = Fonts.Arial20Bold;
        private readonly SpriteFont SmallFont  = Fonts.Arial12Bold;
        private readonly SpriteFont TinyFont   = Fonts.Arial8Bold;
        private readonly Color PlanetStatColor;
        private readonly Color EmpireColor;

        private Rectangle ShipIconRect;
        private readonly UITextEntry PlanetNameEntry = new UITextEntry();
        private UIButton Colonize;
        private UIButton SendTroops;
        private UIButton RecallTroops;
        private readonly PlanetListScreen Screen;
        private readonly float Distance;
        private bool MarkedForColonization;
        public bool CanSendTroops;

        public PlanetListScreenItem(PlanetListScreen screen, Planet planet, float distance, bool canSendTroops)
        {
            Screen   = screen;
            Planet   = planet;
            Distance = distance / 1000; // Distance from nearest player colony

            PlanetStatColor = Planet.Habitable ? Color.White : Color.LightPink;
            EmpireColor     = Planet.Owner?.EmpireColor ?? new Color(255, 239, 208);
            CanSendTroops   = canSendTroops;

            foreach (Goal g in Empire.Universe.player.GetEmpireAI().Goals)
            {
                if (g.ColonizationTarget != null && g.ColonizationTarget == planet)
                    MarkedForColonization = true;
            }
        }

        public override void PerformLayout()
        {
            int x = (int)X;
            int y = (int)Y;
            int w = (int)Width;
            int h = (int)Height;
            RemoveAll();

            ButtonStyle colonizeStyle  = MarkedForColonization ? ButtonStyle.Default : ButtonStyle.BigDip;
            LocalizedText colonizeText = !MarkedForColonization ? new LocalizedText(1425) : "Cancel Colonize";
            Colonize   = Button(colonizeStyle, colonizeText, OnColonizeClicked);
            SendTroops = Button(ButtonStyle.BigDip, "Send Troops", OnSendTroopsClicked);
            SendTroops.Tooltip = new LocalizedText(1900);
            RecallTroops = Button(ButtonStyle.Medium, $"Recall Troops ({Planet.NumTroopsCanLaunchFor(Player)})", OnRecallTroopsClicked);
            RecallTroops.Tooltip = new LocalizedText(1894);

            int nextX = x;
            Rectangle NextRect(float width)
            {
                int next = nextX;
                nextX += (int)width;
                return new Rectangle(next, y, (int)width, h);
            }

            SysNameRect    = NextRect(w * 0.12f);
            PlanetNameRect = NextRect(w * 0.25f);

            DistanceRect = NextRect(100);
            FertRect     = NextRect(100);
            RichRect     = NextRect(120);
            PopRect      = NextRect(200);
            OwnerRect    = NextRect(100);
            OrdersRect   = NextRect(100);

            ShipIconRect = new Rectangle(PlanetNameRect.X + 5, PlanetNameRect.Y + 5, 50, 50);
            PlanetNameEntry.Text = Planet.Name;
            PlanetNameEntry.ClickableArea = new Rectangle(ShipIconRect.Right + 10, y, Fonts.Arial20Bold.TextWidth(Planet.Name), Fonts.Arial20Bold.LineSpacing);
            
            var btn = ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_168px");
            Colonize.Rect      = new Rectangle(OrdersRect.X + 10, OrdersRect.Y + OrdersRect.Height / 2 - btn.Height / 2, btn.Width, btn.Height);
            SendTroops.Rect    = new RectF(OrdersRect.X + Colonize.Width + 10, Colonize.Y, Colonize.Width, Colonize.Height);
            RecallTroops.Rect  = new RectF(OrdersRect.X + Colonize.Width*2 + 10, Colonize.Y, Colonize.Width, Colonize.Height);

            Colonize.Visible     = Planet.Owner == null && Planet.Habitable;
            RecallTroops.Visible = Planet.Owner != Player && Planet.NumTroopsCanLaunchFor(Player) > 0;

            UpdateButtonSendTroops();
            AddSystemName();
            AddPlanetName();
            AddPlanetTextureAndStatus();
            AddPlanetStats();
            AddHostileWarning();
            base.PerformLayout();
        }

        public override bool HandleInput(InputState input)
        {
            if (SendTroops.HitTest(input.CursorPosition) && input.RightMouseClick)
            {
                OnSendTroopsRightClick();
                return true;
            }

            return base.HandleInput(input);
        }

        void AddSystemName()
        {
            string systemName     = Planet.ParentSystem.Name;
            SpriteFont systemFont = NormalFont.MeasureString(systemName).X <= SysNameRect.Width ? NormalFont : SmallFont;
            var sysNameCursor = new Vector2(SysNameRect.X + SysNameRect.Width / 2 - systemFont.MeasureString(systemName).X / 2f,
                                        2 + SysNameRect.Y + SysNameRect.Height / 2 - systemFont.LineSpacing / 2);
            
            Label(sysNameCursor, systemName, systemFont, Cream);
        }

        void AddPlanetName()
        {
            var namePos = new Vector2(PlanetNameEntry.ClickableArea.X, PlanetNameEntry.ClickableArea.Y + 3);
            Label(namePos, Planet.Name, NormalFont, EmpireColor);
            // Now add Richness
            namePos.Y += NormalFont.LineSpacing;
            string richness = Planet.LocalizedRichness;
            Label(namePos, richness, SmallFont, EmpireColor);

            float fertEnvMultiplier = EmpireManager.Player.PlayerEnvModifier(Planet.Category);
            if (!fertEnvMultiplier.AlmostEqual(1))
            {
                Color fertEnvColor       = fertEnvMultiplier.Less(1) ? Color.Pink : Color.LightGreen;
                string multiplierString  = $" (x {fertEnvMultiplier.String(2)})";
                var fertEnvMultiplierPos = new Vector2(namePos.X + SmallFont.MeasureString(richness).X + 5, namePos.Y + 2);
                Label(fertEnvMultiplierPos, multiplierString, TinyFont, fertEnvColor);
            }
        }

        void AddPlanetStats()
        {
            LocalizedText singular;
            if (Planet.Owner != null)
                singular = Planet.Owner.data.Traits.Singular;
            else
                singular = (Planet.Habitable ? 2263 : 2264);

            var distancePos  = new Vector2(DistanceRect.X + 35, DistanceRect.Y + DistanceRect.Height / 2 - SmallFont.LineSpacing / 2);
            var fertilityPos = new Vector2(FertRect.X + 35, FertRect.Y + FertRect.Height / 2 - SmallFont.LineSpacing / 2);
            var richnessPos  = new Vector2(RichRect.X + 35, RichRect.Y + RichRect.Height / 2 - SmallFont.LineSpacing / 2);
            var popPos       = new Vector2(PopRect.X + 60, PopRect.Y + PopRect.Height / 2 - SmallFont.LineSpacing / 2);
            var ownerPos     = new Vector2(OwnerRect.X + 20, OwnerRect.Y + OwnerRect.Height / 2 - SmallFont.LineSpacing / 2);

            DrawPlanetDistance(Distance, distancePos, SmallFont);
            Label(fertilityPos, Planet.FertilityFor(EmpireManager.Player).String(), SmallFont, PlanetStatColor);
            Label(richnessPos, Planet.MineralRichness.String(1), SmallFont, PlanetStatColor);
            Label(popPos, Planet.PopulationStringForPlayer, SmallFont, PlanetStatColor);
            Label(ownerPos, singular, SmallFont, EmpireColor);
        }

        void AddHostileWarning()
        {
            if (!Planet.ParentSystem.HostileForcesPresent(Player)) 
                return;
            
            SubTexture flash = ResourceManager.Texture("Ground_UI/EnemyHere");
            UIPanel enemyHere = Panel(SysNameRect.X + SysNameRect.Width - 40, SysNameRect.Y + 5, flash);
            enemyHere.Tooltip = GameTips.EnemyHere;
        }

        void AddPlanetTextureAndStatus()
        {
            var planetIcon = new Rectangle(PlanetNameRect.X + 5, PlanetNameRect.Y + 5, PlanetNameRect.Height - 10, PlanetNameRect.Height - 10);
            Add( new UIPanel(planetIcon, ResourceManager.Texture(Planet.IconPath))
            {
                Tooltip = GameTips.PlanetPanel
            });

            if (Planet.Owner != null)
                Panel(planetIcon, EmpireColor, ResourceManager.Flag(Planet.Owner));

            AddPlanetStatusIcons(planetIcon);
        }

        void AddPlanetStatusIcons(Rectangle planetIcon)
        {
            var statusIcons = new Vector2(PlanetNameRect.X + PlanetNameRect.Width, planetIcon.Y + 10);
            int offset      = 0;

            AddRecentCombat(statusIcons, ref offset);
            AddMoleIcons(statusIcons, ref offset);
            AddEventIcon(statusIcons, ref offset);
            AddCommoditiesIcon(statusIcons, ref offset);
            AddTroopsIcon(statusIcons, ref offset);
        }

        void AddRecentCombat(Vector2 statusIcons, ref int offset)
        {
            if (!Planet.RecentCombat) 
                return;

            offset += 18;
            var statusRect = new Rectangle((int)statusIcons.X - offset, (int)statusIcons.Y, 16, 16);
            UIPanel status = Panel(statusRect, ResourceManager.Texture("UI/icon_fighting_small"));
            status.Tooltip = GameTips.GroundCom;
        }

        void AddMoleIcons(Vector2 statusIcons, ref int offset) // Haha, moles..
        {
            if (EmpireManager.Player.data.MoleList.Count <= 0) 
                return;

            foreach (Mole m in EmpireManager.Player.data.MoleList)
            {
                if (m.PlanetGuid == Planet.guid)
                {
                    offset += 20;
                    var spyRect = new Rectangle((int)statusIcons.X - offset, (int)statusIcons.Y, 16, 16);
                    UIPanel spy = Panel(spyRect, ResourceManager.Texture("UI/icon_spy_small"));
                    spy.Tooltip = GameTips.Spy;
                    break;
                }
            }
        }

        void AddBuildingIcon(Building b, Vector2 statusIcons, ref int offset)
        {
            offset += 20;
            var buildingRect = new Rectangle((int)statusIcons.X - offset, (int)statusIcons.Y, 16, 16);
            UIPanel building = Panel(buildingRect, ResourceManager.Texture($"Buildings/icon_{b.Icon}_48x48"));
            building.Tooltip = b.DescriptionText;
        }

        void AddEventIcon(Vector2 statusIcons, ref int offset)
        {
            if (Planet.BuildingList.Count == 0)
                return;

            foreach (Building b in Planet.BuildingList)
            {
                if (b.EventHere && (Planet.Owner == null || !Planet.Owner.GetBDict()[b.Name]))
                {
                    AddBuildingIcon(b, statusIcons, ref offset);
                }
            }
        }

        void AddCommoditiesIcon(Vector2 statusIcons, ref int offset)
        {
            if (Planet.BuildingList.Count == 0)
                return;

            foreach (Building b in Planet.BuildingList)
            {
                if (b.IsCommodity || b.IsVolcano)
                {
                    AddBuildingIcon(b, statusIcons, ref offset);
                }
            }
        }

        void AddTroopsIcon(Vector2 statusIcons, ref int offset)
        {
            int troops = Planet.CountEmpireTroops(Player);
            if (troops > 0)
            {
                offset += 20;
                var troopRect = new Rectangle((int)statusIcons.X - offset, (int)statusIcons.Y, 16, 16);
                UIPanel troop = Panel(troopRect, ResourceManager.Texture("UI/icon_troop"));
                troop.Tooltip = LocalizedText.Parse($"{{Troops}}: {troops}");
            }
        }

        void UpdateButtonSendTroops()
        {
            if (TryGetIncomingTroops(out int troopsInvading, out _))
            {
                ButtonStyle style  = Planet.Owner == Player || Planet.Owner == null ? ButtonStyle.Default : ButtonStyle.Military;
                string text        = "Invading:";

                if (Planet.Owner == Player)    text = "Rebasing:";
                else if (Planet.Owner == null) text = "Landing:";

                SendTroops.Text = $"{text} {troopsInvading}";
                SendTroops.Style = style;
            }
            else
            {
                SendTroops.Text    = "Send Troops";
                SendTroops.Visible = Planet.Habitable && CanSendTroops && !Player.IsNAPactWith(Planet.Owner);
                SendTroops.Style   = Planet.Owner == Player || Planet.Owner == null ? ButtonStyle.Default : ButtonStyle.BigDip;
            }
        }

        bool TryGetIncomingTroops(out int incomingTroops, out Array<Ship> incomingTroopShips)
        {
            incomingTroopShips = new Array<Ship>();
            incomingTroops      = 0;
            BatchRemovalCollection<Ship> ships = Player.GetShips();
            for (int i = 0; i < ships.Count; i++)
            {
                Ship ship = ships[i];
                ShipAI ai = ship?.AI;
                if (ai == null || ai.State == AIState.Resupply || !ship.HasOurTroops || ai.OrderQueue.IsEmpty)
                    continue;

                if (ai.OrderQueue.Any(goal => goal.TargetPlanet != null
                                              && goal.TargetPlanet == Planet
                                              && (goal.Plan == ShipAI.Plan.LandTroop || goal.Plan == ShipAI.Plan.Rebase)))
                {
                    incomingTroopShips.AddUnique(ship);
                    incomingTroops += ship.TroopCount;
                }
            }

            return incomingTroopShips.Count > 0;
        }

        public void SetCanSendTroops(bool value)
        {
            CanSendTroops = value;
        }

        void DrawPlanetDistance(float distance, Vector2 namePos, SpriteFont spriteFont)
        {
            DistanceDisplay distanceDisplay = new DistanceDisplay(distance);
            if (distance.Greater(0))
            {
                Label(namePos, distanceDisplay.Text, spriteFont, distanceDisplay.Color);
            }
        }

        void OnSendTroopsClicked(UIButton b)
        {
            if (Player.GetTroopShipForRebase(out Ship troopShip, Planet))
            {
                GameAudio.EchoAffirmative();
                troopShip.AI.OrderLandAllTroops(Planet);
                Screen.RefreshSendTroopButtonsVisibility();
                UpdateButtonSendTroops();
            }
            else
                GameAudio.NegativeClick();
        }

        void OnSendTroopsRightClick()
        {
            if (!TryGetIncomingTroops(out _, out Array<Ship> incomingTroopShips))
                return;

            Ship ship = incomingTroopShips.Last();
            ship.AI.OrderRebaseToNearest();
            UpdateButtonSendTroops();
        }

        void OnRecallTroopsClicked(UIButton b)
        {
            bool troopLaunched = false;
            for (int i = Planet.TroopsHere.Count-1; i >= 0; i--)
            {
                Troop t = Planet.TroopsHere[i];
                if (t.Loyalty != Player)
                    continue;

                Ship troopShip = t.Launch();
                if (troopShip != null)
                {
                    troopLaunched = true;
                    troopShip.AI.OrderRebaseToNearest();
                }
            }

            if (troopLaunched)
            {
                GameAudio.EchoAffirmative();
                PerformLayout();
            }
            else
            {
                GameAudio.NegativeClick();
            }
        }

        void OnColonizeClicked(UIButton b)
        {
            if (!MarkedForColonization)
            {
                GameAudio.EchoAffirmative();
                Player.GetEmpireAI().Goals.Add(
                    new MarkForColonization(Planet, Empire.Universe.player));

                Colonize.Text = "Cancel Colonize";
                Colonize.Style = ButtonStyle.Default;
                MarkedForColonization = true;
                return;
            }

            // @todo this is so hacky
            foreach (Goal g in Empire.Universe.player.GetEmpireAI().Goals)
            {
                if (g.ColonizationTarget == null || g.ColonizationTarget != Planet)
                {
                    continue;
                }
                GameAudio.EchoAffirmative();
                g.FinishedShip?.AI.OrderOrbitNearest(true);
                Empire.Universe.player.GetEmpireAI().Goals.QueuePendingRemoval(g);
                MarkedForColonization = false;
                Colonize.Text = "Colonize";
                Colonize.Style = ButtonStyle.BigDip;
                break;
            }
            Empire.Universe.player.GetEmpireAI().Goals.ApplyPendingRemovals();
        }

        struct DistanceDisplay
        {
            public readonly string Text;
            public readonly Color Color;
            private Distances PlanetDistance;

            public DistanceDisplay(float distance) : this()
            {
                DeterminePlanetDistanceCategory(distance);
                switch (PlanetDistance)
                {
                    case Distances.Local:   Text  = "Local";   Color = Color.Green; break;
                    case Distances.Near:    Text  = "Near";    Color = Color.YellowGreen; break;
                    case Distances.Midway:  Text  = "Midway";  Color = Color.DarkGoldenrod; break;
                    case Distances.Distant: Text  = "Distant"; Color = Color.DarkRed; break;
                    default:                Text  = "Beyond";  Color = Color.DarkGray; break;
                }
            }

            void DeterminePlanetDistanceCategory(float distance)
            {
                if (distance.LessOrEqual(140)) PlanetDistance = Distances.Local;
                else if (distance.LessOrEqual(1200)) PlanetDistance = Distances.Near;
                else if (distance.LessOrEqual(3000)) PlanetDistance = Distances.Midway;
                else if (distance.LessOrEqual(6000)) PlanetDistance = Distances.Distant;
                else PlanetDistance = Distances.Beyond;
            }

            enum Distances
            {
                Local,
                Near,
                Midway,
                Distant,
                Beyond
            }
        }
    }
}
