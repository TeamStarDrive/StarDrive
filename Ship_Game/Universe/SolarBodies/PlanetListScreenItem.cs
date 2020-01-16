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
    public sealed class PlanetListScreenItem : ScrollListItem<PlanetListScreenItem>
    {
        public Planet Planet;
        public Rectangle SysNameRect;
        public Rectangle PlanetNameRect;
        public Rectangle FertRect;
        public Rectangle RichRect;
        public Rectangle PopRect;
        public Rectangle OwnerRect;
        public Rectangle OrdersRect;

        private readonly Empire Player         = EmpireManager.Player;
        private readonly Color Cream           = Colors.Cream;
        private readonly SpriteFont NormalFont = Fonts.Arial20Bold;
        private readonly SpriteFont SmallFont  = Fonts.Arial12Bold;
        private readonly Color PlanetStatColor;
        private readonly Color EmpireColor;

        private Rectangle ShipIconRect;
        private readonly UITextEntry PlanetNameEntry = new UITextEntry();
        private readonly UIButton Colonize;
        private readonly UIButton SendTroops;
        private readonly PlanetListScreen Screen;
        private readonly float Distance;
        private bool MarkedForColonization;

        public PlanetListScreenItem(PlanetListScreen screen, Planet planet, float distance)
        {
            Screen   = screen;
            Planet   = planet;
            Distance = distance / 1000; // Distance from nearest player colony

            PlanetStatColor = Planet.Habitable ? Color.White : Color.LightPink;
            EmpireColor     = Planet.Owner?.EmpireColor ?? new Color(255, 239, 208);

            foreach (Goal g in Empire.Universe.player.GetEmpireAI().Goals)
            {
                if (g.ColonizationTarget != null && g.ColonizationTarget == planet)
                    MarkedForColonization = true;
            }

            ButtonStyle style   = MarkedForColonization ? ButtonStyle.Default : ButtonStyle.BigDip;
            string colonizeText = !MarkedForColonization ? Localizer.Token(1425) : "Cancel Colonize";
            Colonize            = Button(style, colonizeText, OnColonizeClicked);
            SendTroops          = Button(ButtonStyle.BigDip, "Send Troops", OnSendTroopsClicked);
        }

        public override void PerformLayout()
        {
            int x = (int)X;
            int y = (int)Y;
            Rect  = new Rectangle(x, y, Rect.Width, Rect.Height);
            SysNameRect    = new Rectangle(x, y, (int)(Rect.Width * 0.12f), Rect.Height);
            PlanetNameRect = new Rectangle(x + SysNameRect.Width, y, (int)(Rect.Width * 0.25f), Rect.Height);
            FertRect     = new Rectangle(x + SysNameRect.Width + PlanetNameRect.Width, y, 100, Rect.Height);
            RichRect     = new Rectangle(x + SysNameRect.Width + PlanetNameRect.Width + FertRect.Width, y, 120, Rect.Height);
            PopRect      = new Rectangle(x + SysNameRect.Width + PlanetNameRect.Width + FertRect.Width + RichRect.Width, y, 200, Rect.Height);
            OwnerRect    = new Rectangle(x + SysNameRect.Width + PlanetNameRect.Width + FertRect.Width + RichRect.Width + PopRect.Width, y, 100, Rect.Height);
            OrdersRect   = new Rectangle(x + SysNameRect.Width + PlanetNameRect.Width + FertRect.Width + RichRect.Width + PopRect.Width + OwnerRect.Width, y, 100, Rect.Height);
            ShipIconRect = new Rectangle(PlanetNameRect.X + 5, PlanetNameRect.Y + 5, 50, 50);
            PlanetNameEntry.Text = Planet.Name;
            PlanetNameEntry.ClickableArea = new Rectangle(ShipIconRect.Right + 10, y, Fonts.Arial20Bold.TextWidth(Planet.Name), Fonts.Arial20Bold.LineSpacing);
            Colonize.Rect      = new Rectangle(OrdersRect.X + 10, OrdersRect.Y + OrdersRect.Height / 2 - ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_168px").Height / 2, ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_168px").Width, ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_168px").Height);
            SendTroops.Rect    = new Rectangle(OrdersRect.X  + Colonize.Rect.Width + 10, Colonize.Rect.Y, Colonize.Rect.Width, Colonize.Rect.Height);
            Colonize.Visible   = Planet.Owner == null && Planet.Habitable;
            SendTroops.Visible = Planet.Habitable;

            AddSystemName();
            AddPlanetName();
            AddPlanetTextureAndStatus();
            AddPlanetStats();
            AddHostileWarning();
            base.PerformLayout();
        }

        void AddSystemName()
        {
            string systemName     = Planet.ParentSystem.Name;
            SpriteFont systemFont = NormalFont.MeasureString(systemName).X <= SysNameRect.Width ? NormalFont : SmallFont;
            Vector2 sysNameCursor = new Vector2(SysNameRect.X + SysNameRect.Width / 2 - systemFont.MeasureString(systemName).X / 2f,
                                            2 + SysNameRect.Y + SysNameRect.Height / 2 - systemFont.LineSpacing / 2);
            
            Add(new UILabel(sysNameCursor, systemName, systemFont, Cream));
        }

        void AddPlanetName()
        {
            var namePos = new Vector2(X = PlanetNameEntry.ClickableArea.X, Y = PlanetNameEntry.ClickableArea.Y + 3);
            Add(new UILabel(namePos, Planet.Name, NormalFont, EmpireColor));
            // Now add Richness
            namePos.Y += NormalFont.LineSpacing - 1;
            Add(new UILabel(namePos, Planet.LocalizedRichness, SmallFont, EmpireColor));
            // And approximate distance
            DrawPlanetDistance(Distance, namePos, SmallFont);
        }

        void AddPlanetStats()
        {
            string singular;
            if (Planet.Owner != null)
                singular = Planet.Owner.data.Traits.Singular;
            else
                singular = (Planet.Habitable ? Localizer.Token(2263) : Localizer.Token(2264));

            Vector2 fertilityPos = new Vector2(FertRect.X + 35, FertRect.Y + FertRect.Height / 2 - SmallFont.LineSpacing / 2);
            Vector2 richnessPos  = new Vector2(RichRect.X + 35, RichRect.Y + RichRect.Height / 2 - SmallFont.LineSpacing / 2);
            Vector2 popPos       = new Vector2(PopRect.X + 60, PopRect.Y + PopRect.Height / 2 - SmallFont.LineSpacing / 2);
            Vector2 ownerPos     = new Vector2(OwnerRect.X + 20, OwnerRect.Y + OwnerRect.Height / 2 - SmallFont.LineSpacing / 2);

            Add(new UILabel(fertilityPos, Planet.FertilityFor(EmpireManager.Player).String(), SmallFont, PlanetStatColor));
            Add(new UILabel(richnessPos, Planet.MineralRichness.String(1), SmallFont, PlanetStatColor));
            Add(new UILabel(popPos, Planet.PopulationStringForPlayer, SmallFont, PlanetStatColor));
            Add(new UILabel(ownerPos, singular, SmallFont, EmpireColor));
        }

        void AddHostileWarning()
        {
            if (!Planet.ParentSystem.HostileForcesPresent(Player)) 
                return;
            
            string textureText = "Ground_UI/EnemyHere";
            SubTexture flash   = ResourceManager.Texture(textureText);
            var flashRect      = new Rectangle(SysNameRect.X + SysNameRect.Width - 40, SysNameRect.Y + 5, flash.Width, flash.Height);
            Add(UIPanel.FromTexture(Screen.TransientContent, flashRect, textureText, Color.White));
            // ToolTip.CreateTooltip(123);
        }

        void AddPlanetTextureAndStatus()
        {
            var planetIcon = new Rectangle(PlanetNameRect.X + 5, PlanetNameRect.Y + 5, PlanetNameRect.Height - 10, PlanetNameRect.Height - 10);
            Add(UIPanel.FromTexture(Screen.TransientContent, planetIcon, Planet.IconPath, Color.White));
            /*
            if (Planet.Owner != null)
                Add(UIPanel.FromTexture(Screen.TransientContent, planetIcon, ResourceManager.Flag(Planet.Owner)));
            */

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
            string textureText   = "UI/icon_fighting_small";
            Rectangle statusRect = new Rectangle((int)statusIcons.X - offset, (int)statusIcons.Y, 16, 16);
            Add(UIPanel.FromTexture(Screen.TransientContent, statusRect, textureText, Color.White));
            //ToolTip.CreateTooltip(119);
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
                    string textureText   = "UI/icon_spy_small";
                    Rectangle statusRect = new Rectangle((int)statusIcons.X - offset, (int)statusIcons.Y, 16, 16);
                    Add(UIPanel.FromTexture(Screen.TransientContent, statusRect, textureText, Color.White));
                    //ToolTip.CreateTooltip(120);
                    break;
                }
            }
        }

        void AddEventIcon(Vector2 statusIcons, ref int offset)
        {
            if (Planet.BuildingList.Count == 0)
                return;

            foreach (Building b in Planet.BuildingList)
            {
                if (b.EventHere && (Planet.Owner == null || !Planet.Owner.GetBDict()[b.Name]))
                {
                    offset += 20;
                    string textureText = $"Buildings/icon_{b.Icon}_48x48";
                    var statusRect     = new Rectangle((int)statusIcons.X - offset, (int)statusIcons.Y, 16, 16);
                    Add(UIPanel.FromTexture(Screen.TransientContent, statusRect, textureText, Color.White));
                    //ToolTip.CreateTooltip(Localizer.Token(b.DescriptionIndex));
                }
            }
        }

        void AddCommoditiesIcon(Vector2 statusIcons, ref int offset)
        {
            if (Planet.BuildingList.Count == 0)
                return;

            foreach (Building b in Planet.BuildingList)
            {
                if (b.IsCommodity)
                {
                    offset += 20;
                    string textureText = $"Buildings/icon_{b.Icon}_48x48";
                    var statusRect     = new Rectangle((int)statusIcons.X - offset, (int)statusIcons.Y, 16, 16);
                    Add(UIPanel.FromTexture(Screen.TransientContent, statusRect, textureText, Color.White));
                    //ToolTip.CreateTooltip(Localizer.Token(b.DescriptionIndex));
                }
            }
        }

        void AddTroopsIcon(Vector2 statusIcons, ref int offset)
        {
            int troops = Planet.CountEmpireTroops(Player);
            if (troops > 0)
            {
                offset += 20;
                string textureText = "UI/icon_troop";
                var statusRect     = new Rectangle((int)statusIcons.X - offset, (int)statusIcons.Y, 16, 16);
                Add(UIPanel.FromTexture(Screen.TransientContent, statusRect, textureText, Color.White));
                //ToolTip.CreateTooltip($"{Localizer.Token(336)}: {troops}");
            }
        }

        public /*override*/ void /*Draw*/ DontUse(SpriteBatch batch)
        {
            // Fat Bastard - I am slowly refactoring this method. Dont delete this 

            if (Planet.Habitable)  //fbedard: can send troop anywhere
            {
                int troopsInvading = 0;
                BatchRemovalCollection<Ship> ships = Screen.EmpireUI.empire.GetShips();
                for (int z = 0; z < ships.Count; z++)
                {
                    Ship ship = ships[z];
                    ShipAI ai = ship?.AI;                    
                    if (ai == null ||  ai.State == AIState.Resupply || !ship.HasOurTroops || ai.OrderQueue.IsEmpty)
                        continue;
                    if (ai.OrderQueue.Any(goal => goal.TargetPlanet != null && goal.TargetPlanet == Planet))
                        troopsInvading = ship.TroopCount;
                }

                if (troopsInvading > 0)
                {
                    SendTroops.Text = "Invading: " + troopsInvading;
                    SendTroops.Style = ButtonStyle.Default;
                }
                else
                {
                    SendTroops.Text = "Send Troops";
                    SendTroops.Style = ButtonStyle.BigDip;
                }
            }
            //fbedard : Add Send Button for your planets
            if (Planet.Owner == Empire.Universe.player)
            {
                int troopsInvading = Screen.EmpireUI.empire.GetShips()
                 .Where(troop => troop.TroopCount > 0)
                 .Where(ai => ai.AI.State != AIState.Resupply).Count(troopAI => troopAI.AI.OrderQueue.Any(goal => goal.TargetPlanet != null && goal.TargetPlanet == Planet));
                if (troopsInvading > 0)
                {
                    SendTroops.Text = "Landing: " + troopsInvading;
                    SendTroops.Style = ButtonStyle.Default;
                }
                else
                {
                    SendTroops.Text = "Send Troops";
                    SendTroops.Style = ButtonStyle.BigDip;
                }
                SendTroops.Draw(batch);
            }

            batch.DrawRectangle(Rect, Color.White);
        }

        void DrawPlanetDistance(float distance, Vector2 namePos, SpriteFont spriteFont)
        {
            DistanceDisplay distanceDisplay = new DistanceDisplay(distance);
            if (distance.Greater(0))
            {
                namePos.X += spriteFont.TextWidth(Planet.LocalizedRichness) + 4;
                namePos.Y += 2;
                Add(new UILabel(namePos, distanceDisplay.Text, Fonts.Arial10, distanceDisplay.Color));
            }
        }

        void OnSendTroopsClicked(UIButton b)
        {
            if (Screen.EmpireUI.empire.GetTroopShipForRebase(out Ship troopShip, Planet))
            {
                GameAudio.EchoAffirmative();
                troopShip.AI.OrderLandAllTroops(Planet);
            }
            else
                GameAudio.NegativeClick();
        }

        void OnColonizeClicked(UIButton b)
        {
            if (!MarkedForColonization)
            {
                GameAudio.EchoAffirmative();
                Empire.Universe.player.GetEmpireAI().Goals.Add(
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
                    case Distances.Local: Text = "(Local)"; Color = Color.Green; break;
                    case Distances.Near: Text = "(Near)"; Color = Color.YellowGreen; break;
                    case Distances.Midway: Text = "(Midway)"; Color = Color.DarkGoldenrod; break;
                    case Distances.Distant: Text = "(Distant)"; Color = Color.DarkRed; break;
                    default: Text = "(Beyond)"; Color = Color.DarkGray; break;
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
