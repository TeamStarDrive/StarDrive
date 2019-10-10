using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI;
using Ship_Game.Audio;
using Ship_Game.Commands.Goals;
using Ship_Game.Ships;

namespace Ship_Game
{
    public sealed class PlanetListScreenEntry : UIElementContainer
    {
        public Planet planet;

        public Rectangle TotalEntrySize;

        public Rectangle SysNameRect;

        public Rectangle PlanetNameRect;

        public Rectangle FertRect;

        public Rectangle RichRect;

        public Rectangle PopRect;

        public Rectangle OwnerRect;

        public Rectangle OrdersRect;

        private Rectangle ShipIconRect;

        private UITextEntry ShipNameEntry = new UITextEntry();

        private UIButton Colonize;
        private UIButton SendTroops;

        public PlanetListScreen screen;

        private bool MarkedForColonization;

        //private string Status_Text;

        public PlanetListScreenEntry(Planet p, int x, int y, int width, int height, PlanetListScreen caller) : base(caller, new Rectangle(x, y, width, height))
        {
            screen = caller;
            planet = p;
            TotalEntrySize = new Rectangle(x, y, width - 60, height);
            SysNameRect = new Rectangle(x, y, (int)(TotalEntrySize.Width * 0.12f), height);
            PlanetNameRect = new Rectangle(x + SysNameRect.Width, y, (int)(TotalEntrySize.Width * 0.25f), height);
            FertRect = new Rectangle(x + SysNameRect.Width + PlanetNameRect.Width, y, 100, height);
            RichRect = new Rectangle(x + SysNameRect.Width + PlanetNameRect.Width + FertRect.Width, y, 120, height);
            PopRect = new Rectangle(x + SysNameRect.Width + PlanetNameRect.Width + FertRect.Width + RichRect.Width, y, 200, height);
            OwnerRect = new Rectangle(x + SysNameRect.Width + PlanetNameRect.Width + FertRect.Width + RichRect.Width + PopRect.Width, y, 100, height);
            OrdersRect = new Rectangle(x + SysNameRect.Width + PlanetNameRect.Width + FertRect.Width + RichRect.Width + PopRect.Width + OwnerRect.Width, y, 100, height);
            //this.Status_Text = "";
            ShipIconRect = new Rectangle(PlanetNameRect.X + 5, PlanetNameRect.Y + 5, 50, 50);
            string shipName = planet.Name;
            ShipNameEntry.ClickableArea = new Rectangle(ShipIconRect.X + ShipIconRect.Width + 10, 2 + SysNameRect.Y + SysNameRect.Height / 2 - Fonts.Arial20Bold.LineSpacing / 2, (int)Fonts.Arial20Bold.MeasureString(shipName).X, Fonts.Arial20Bold.LineSpacing);
            ShipNameEntry.Text = shipName;

            foreach (Goal g in Empire.Universe.player.GetEmpireAI().Goals)
            {
                if (g.ColonizationTarget == null || g.ColonizationTarget != p)
                {
                    continue;
                }
                MarkedForColonization = true;
            }

            ButtonStyle style = MarkedForColonization ? ButtonStyle.Default : ButtonStyle.BigDip;
            string colonizeText = !MarkedForColonization ? Localizer.Token(1425) : "Cancel Colonize";
            Colonize = Button(style, 0f, 0f, colonizeText, OnColonizeClicked);
            Colonize.SetAbsPos(OrdersRect.X + 10, OrdersRect.Y + OrdersRect.Height - Colonize.Size.Y);
            SendTroops = Button(ButtonStyle.BigDip, OrdersRect.X + Colonize.Rect.Width + 10, 0f, "", OnSendTroopsClicked);
        }

        public override void Draw(SpriteBatch batch)
        {
            string singular;
            var TextColor = new Color(255, 239, 208);
            string sysname = planet.ParentSystem.Name;
            if (Fonts.Arial20Bold.MeasureString(sysname).X <= SysNameRect.Width)
            {
                var SysNameCursor = new Vector2(SysNameRect.X + SysNameRect.Width / 2 - Fonts.Arial20Bold.MeasureString(sysname).X / 2f, 2 + SysNameRect.Y + SysNameRect.Height / 2 - Fonts.Arial20Bold.LineSpacing / 2);
                batch.DrawString(Fonts.Arial20Bold, sysname, SysNameCursor, TextColor);
            }
            else
            {
                var SysNameCursor = new Vector2(SysNameRect.X + SysNameRect.Width / 2 - Fonts.Arial12Bold.MeasureString(sysname).X / 2f, 2 + SysNameRect.Y + SysNameRect.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2);
                batch.DrawString(Fonts.Arial12Bold, sysname, SysNameCursor, TextColor);
            }

            if (planet.ParentSystem.HostileForcesPresent(EmpireManager.Player))
            {
                var flashRect = new Rectangle(SysNameRect.X + SysNameRect.Width - 40, SysNameRect.Y + 5, ResourceManager.Texture("Ground_UI/Ground_Attack").Width, ResourceManager.Texture("Ground_UI/Ground_Attack").Height);
                batch.Draw(ResourceManager.Texture("Ground_UI/EnemyHere"), flashRect, screen.CurrentFlashColor);
                if (flashRect.HitTest(screen.Input.CursorPosition))
                {
                    ToolTip.CreateTooltip(123);
                }
            }
            var planetIconRect = new Rectangle(PlanetNameRect.X + 5, PlanetNameRect.Y + 5, PlanetNameRect.Height - 10, PlanetNameRect.Height - 10);
            batch.Draw(planet.PlanetTexture, planetIconRect, Color.White);
            if (planet.Owner != null)
            {
                batch.Draw(ResourceManager.Flag(planet.Owner), planetIconRect, planet.Owner.EmpireColor);
            }
            int i = 0;
            var StatusIcons = new Vector2(PlanetNameRect.X + PlanetNameRect.Width, planetIconRect.Y + 10);
            if (planet.RecentCombat)
            {
                Rectangle statusRect = new Rectangle((int)StatusIcons.X - 18, (int)StatusIcons.Y, 16, 16);
                batch.Draw(ResourceManager.Texture("UI/icon_fighting_small"), statusRect, Color.White);
                if (statusRect.HitTest(screen.Input.CursorPosition))
                {
                    ToolTip.CreateTooltip(119);
                }
            }
            if (EmpireManager.Player.data.MoleList.Count > 0)
            {
                foreach (Mole m in EmpireManager.Player.data.MoleList)
                {
                    if (m.PlanetGuid == planet.guid)
                    {
                        StatusIcons.X -= 20f;
                        var statusRect = new Rectangle((int) StatusIcons.X, (int) StatusIcons.Y, 16, 16);
                        batch.Draw(ResourceManager.Texture("UI/icon_spy_small"), statusRect, Color.White);
                        if (statusRect.HitTest(screen.Input.CursorPosition))
                        {
                            ToolTip.CreateTooltip(120);
                        }
                        break;
                    }
                }
            }
            //Building lastBuilding;
            foreach (Building b in planet.BuildingList)
            {
                if (b.EventHere && (planet.Owner == null || !planet.Owner.GetBDict()[b.Name]))
                {
                    StatusIcons.X -= 20f;
                    var statusRect = new Rectangle((int) StatusIcons.X, (int) StatusIcons.Y, 16, 16);
                    batch.Draw(ResourceManager.Texture($"Buildings/icon_{b.Icon}_48x48"), statusRect, Color.White);
                    i++;
                    if (statusRect.HitTest(screen.Input.CursorPosition))
                        ToolTip.CreateTooltip(Localizer.Token(b.DescriptionIndex));
                }
            }

            foreach (Building b in planet.BuildingList)
            {
                if (b.IsCommodity)
                {
                    StatusIcons.X -= 20f;
                    var statusRect = new Rectangle((int) StatusIcons.X, (int) StatusIcons.Y, 16, 16);
                    batch.Draw(ResourceManager.Texture($"Buildings/icon_{b.Icon}_48x48"), statusRect, Color.White);
                    i++;
                    if (statusRect.HitTest(screen.Input.CursorPosition))
                        ToolTip.CreateTooltip(Localizer.Token(b.DescriptionIndex));
                }
            }

            int troops = planet.TroopsHere.Count(t => t.Loyalty.isPlayer);
            if (troops > 0)
            {
                StatusIcons.X -= 20f;// (float)(18 * i);

                var statusRect = new Rectangle((int)StatusIcons.X, (int)StatusIcons.Y, 16, 16);
                batch.Draw(ResourceManager.Texture("UI/icon_troop"), statusRect, new Color(255, 255, 255, 255));//Color..White);
                if (statusRect.HitTest(screen.Input.CursorPosition))
                {
                    ToolTip.CreateTooltip($"{Localizer.Token(336)}: {troops}");
                }
            }
            
            var rpos = new Vector2
            {
                X = ShipNameEntry.ClickableArea.X,
                Y = ShipNameEntry.ClickableArea.Y - 10
            };
            batch.DrawString(Fonts.Arial20Bold, planet.Name, rpos, TextColor);
            rpos.Y = rpos.Y + (Fonts.Arial20Bold.LineSpacing - 3);
            Vector2 FertilityCursor = new Vector2(FertRect.X + 35, FertRect.Y + FertRect.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2);
            batch.DrawString(Fonts.Arial12Bold, planet.FertilityFor(EmpireManager.Player).String(), FertilityCursor, (planet.Habitable ? Color.White : Color.LightPink));
            Vector2 RichCursor = new Vector2(RichRect.X + 35, RichRect.Y + RichRect.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2);
            batch.DrawString(Fonts.Arial12Bold, planet.MineralRichness.String(1), RichCursor, (planet.Habitable ? Color.White : Color.LightPink));
            Vector2 PopCursor = new Vector2(PopRect.X + 60, PopRect.Y + PopRect.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2);
            batch.DrawString(Fonts.Arial12Bold, planet.PopulationString, PopCursor, (planet.Habitable ? Color.White : Color.LightPink));
            Vector2 OwnerCursor = new Vector2(OwnerRect.X + 20, OwnerRect.Y + OwnerRect.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2);
            SpriteBatch spriteBatch2 = batch;
            SpriteFont spriteFont = Fonts.Arial12Bold;
            if (planet.Owner != null)
            {
                singular = planet.Owner.data.Traits.Singular;
            }
            else
            {
                singular = (planet.Habitable ? Localizer.Token(2263) : Localizer.Token(2264));
            }
            spriteBatch2.DrawString(spriteFont, singular, OwnerCursor, planet.Owner?.EmpireColor ?? Color.Gray);
            batch.DrawString(Fonts.Arial12Bold, planet.LocalizedRichness, rpos, TextColor);
            if (planet.Habitable && planet.Owner == null)
            {
                Colonize.Draw(batch);
            }

            if (planet.Owner == null && planet.Habitable)  //fbedard: can send troop anywhere
            {
                int troopsInvading = 0;
                BatchRemovalCollection<Ship> ships = screen.EmpireUI.empire.GetShips();
                for (int z = 0; z < ships.Count; z++)
                {
                    Ship ship = ships[z];
                    ShipAI ai = ship?.AI;                    
                    if (ai == null ||  ai.State == AIState.Resupply || ship.TroopList.IsEmpty || ai.OrderQueue.IsEmpty) continue;
                    if (ai.OrderQueue.Any(goal => goal.TargetPlanet != null && goal.TargetPlanet == planet))
                        troopsInvading = ship.TroopList.Count;
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
                SendTroops.Draw(batch);
            }
            //fbedard : Add Send Button for your planets
            if (planet.Owner == Empire.Universe.player)
            {
                int troopsInvading = screen.EmpireUI.empire.GetShips()
                 .Where(troop => troop.TroopList.Count > 0)
                 .Where(ai => ai.AI.State != AIState.Resupply).Count(troopAI => troopAI.AI.OrderQueue.Any(goal => goal.TargetPlanet != null && goal.TargetPlanet == planet));
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
        }

        private void OnSendTroopsClicked(UIButton b)
        {
            if (screen.EmpireUI.empire.GetTroopShipForRebase(out Ship troopShip, planet))
            {
                GameAudio.EchoAffirmative();
                troopShip.AI.OrderLandAllTroops(planet);
            }
            else
                GameAudio.NegativeClick();
        }

        private void OnColonizeClicked(UIButton b)
        {
            if (!MarkedForColonization)
            {
                GameAudio.EchoAffirmative();
                Empire.Universe.player.GetEmpireAI().Goals.Add(
                    new MarkForColonization(planet, Empire.Universe.player));
                Colonize.Text = "Cancel Colonize";
                Colonize.Style = ButtonStyle.Default;
                MarkedForColonization = true;
                return;
            }

            // @todo this is so hacky
            foreach (Goal g in Empire.Universe.player.GetEmpireAI().Goals)
            {
                if (g.ColonizationTarget == null || g.ColonizationTarget != planet)
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

        public void SetNewPos(int x, int y)
        {
            TotalEntrySize = new Rectangle(x, y, TotalEntrySize.Width, TotalEntrySize.Height);
            SysNameRect = new Rectangle(x, y, (int)(TotalEntrySize.Width * 0.12f), TotalEntrySize.Height);
            PlanetNameRect = new Rectangle(x + SysNameRect.Width, y, (int)(TotalEntrySize.Width * 0.25f), TotalEntrySize.Height);
            FertRect = new Rectangle(x + SysNameRect.Width + PlanetNameRect.Width, y, 100, TotalEntrySize.Height);
            RichRect = new Rectangle(x + SysNameRect.Width + PlanetNameRect.Width + FertRect.Width, y, 120, TotalEntrySize.Height);
            PopRect = new Rectangle(x + SysNameRect.Width + PlanetNameRect.Width + FertRect.Width + RichRect.Width, y, 200, TotalEntrySize.Height);
            OwnerRect = new Rectangle(x + SysNameRect.Width + PlanetNameRect.Width + FertRect.Width + RichRect.Width + PopRect.Width, y, 100, TotalEntrySize.Height);
            OrdersRect = new Rectangle(x + SysNameRect.Width + PlanetNameRect.Width + FertRect.Width + RichRect.Width + PopRect.Width + OwnerRect.Width, y, 100, TotalEntrySize.Height);
            ShipIconRect = new Rectangle(PlanetNameRect.X + 5, PlanetNameRect.Y + 5, 50, 50);
            string shipName = planet.Name;
            ShipNameEntry.ClickableArea = new Rectangle(ShipIconRect.X + ShipIconRect.Width + 10, 2 + SysNameRect.Y + SysNameRect.Height / 2 - Fonts.Arial20Bold.LineSpacing / 2, (int)Fonts.Arial20Bold.MeasureString(shipName).X, Fonts.Arial20Bold.LineSpacing);
            Colonize.Rect = new Rectangle(OrdersRect.X + 10, OrdersRect.Y + OrdersRect.Height / 2 - ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_168px").Height / 2, ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_168px").Width, ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_168px").Height);
            SendTroops.Rect = new Rectangle(OrdersRect.X  + Colonize.Rect.Width + 10, Colonize.Rect.Y, Colonize.Rect.Width, Colonize.Rect.Height);
        }
    }
}