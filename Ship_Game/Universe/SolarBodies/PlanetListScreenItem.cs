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

        Rectangle ShipIconRect;
        UITextEntry ShipNameEntry = new UITextEntry();
        UIButton Colonize;
        UIButton SendTroops;
        PlanetListScreen Screen;
        bool MarkedForColonization;

        public PlanetListScreenItem(PlanetListScreen screen, Planet planet)
        {
            Screen = screen;
            Planet = planet;

            foreach (Goal g in Empire.Universe.player.GetEmpireAI().Goals)
            {
                if (g.ColonizationTarget != null && g.ColonizationTarget == planet)
                    MarkedForColonization = true;
            }

            ButtonStyle style = MarkedForColonization ? ButtonStyle.Default : ButtonStyle.BigDip;
            string colonizeText = !MarkedForColonization ? Localizer.Token(1425) : "Cancel Colonize";
            Colonize   = Button(style, colonizeText, OnColonizeClicked);
            SendTroops = Button(ButtonStyle.BigDip, "", OnSendTroopsClicked);
        }

        public override void PerformLayout()
        {
            int x = (int)X;
            int y = (int)Y;
            Rect = new Rectangle(x, y, Rect.Width, Rect.Height);
            SysNameRect = new Rectangle(x, y, (int)(Rect.Width * 0.12f), Rect.Height);
            PlanetNameRect = new Rectangle(x + SysNameRect.Width, y, (int)(Rect.Width * 0.25f), Rect.Height);
            FertRect = new Rectangle(x + SysNameRect.Width + PlanetNameRect.Width, y, 100, Rect.Height);
            RichRect = new Rectangle(x + SysNameRect.Width + PlanetNameRect.Width + FertRect.Width, y, 120, Rect.Height);
            PopRect = new Rectangle(x + SysNameRect.Width + PlanetNameRect.Width + FertRect.Width + RichRect.Width, y, 200, Rect.Height);
            OwnerRect = new Rectangle(x + SysNameRect.Width + PlanetNameRect.Width + FertRect.Width + RichRect.Width + PopRect.Width, y, 100, Rect.Height);
            OrdersRect = new Rectangle(x + SysNameRect.Width + PlanetNameRect.Width + FertRect.Width + RichRect.Width + PopRect.Width + OwnerRect.Width, y, 100, Rect.Height);
            ShipIconRect = new Rectangle(PlanetNameRect.X + 5, PlanetNameRect.Y + 5, 50, 50);
            ShipNameEntry.Text = Planet.Name;
            ShipNameEntry.ClickableArea = new Rectangle(ShipIconRect.Right + 10, y, Fonts.Arial20Bold.TextWidth(Planet.Name), Fonts.Arial20Bold.LineSpacing);
            Colonize.Rect = new Rectangle(OrdersRect.X + 10, OrdersRect.Y + OrdersRect.Height / 2 - ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_168px").Height / 2, ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_168px").Width, ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_168px").Height);
            SendTroops.Rect = new Rectangle(OrdersRect.X  + Colonize.Rect.Width + 10, Colonize.Rect.Y, Colonize.Rect.Width, Colonize.Rect.Height);
            
            base.PerformLayout();
        }

        public override void Draw(SpriteBatch batch)
        {
            base.Draw(batch);

            var textColor = new Color(118, 102, 67, 50);
            var smallHighlight = new Color(118, 102, 67, 25);

            if (ItemIndex % 2 == 0)
            {
                batch.FillRectangle(Rect, smallHighlight);
            }
            if (Planet == Screen.SelectedPlanet)
            {
                batch.FillRectangle(Rect, textColor);
            }

            string singular;
            var TextColor = Colors.Cream;
            string sysname = Planet.ParentSystem.Name;
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

            if (Planet.ParentSystem.HostileForcesPresent(EmpireManager.Player))
            {
                var flashRect = new Rectangle(SysNameRect.X + SysNameRect.Width - 40, SysNameRect.Y + 5, ResourceManager.Texture("Ground_UI/Ground_Attack").Width, ResourceManager.Texture("Ground_UI/Ground_Attack").Height);
                batch.Draw(ResourceManager.Texture("Ground_UI/EnemyHere"), flashRect, Screen.CurrentFlashColor);
                if (flashRect.HitTest(Screen.Input.CursorPosition))
                {
                    ToolTip.CreateTooltip(123);
                }
            }
            var planetIconRect = new Rectangle(PlanetNameRect.X + 5, PlanetNameRect.Y + 5, PlanetNameRect.Height - 10, PlanetNameRect.Height - 10);
            batch.Draw(Planet.PlanetTexture, planetIconRect, Color.White);
            if (Planet.Owner != null)
            {
                batch.Draw(ResourceManager.Flag(Planet.Owner), planetIconRect, Planet.Owner.EmpireColor);
            }
            int i = 0;
            var StatusIcons = new Vector2(PlanetNameRect.X + PlanetNameRect.Width, planetIconRect.Y + 10);
            if (Planet.RecentCombat)
            {
                Rectangle statusRect = new Rectangle((int)StatusIcons.X - 18, (int)StatusIcons.Y, 16, 16);
                batch.Draw(ResourceManager.Texture("UI/icon_fighting_small"), statusRect, Color.White);
                if (statusRect.HitTest(Screen.Input.CursorPosition))
                {
                    ToolTip.CreateTooltip(119);
                }
            }
            if (EmpireManager.Player.data.MoleList.Count > 0)
            {
                foreach (Mole m in EmpireManager.Player.data.MoleList)
                {
                    if (m.PlanetGuid == Planet.guid)
                    {
                        StatusIcons.X -= 20f;
                        var statusRect = new Rectangle((int) StatusIcons.X, (int) StatusIcons.Y, 16, 16);
                        batch.Draw(ResourceManager.Texture("UI/icon_spy_small"), statusRect, Color.White);
                        if (statusRect.HitTest(Screen.Input.CursorPosition))
                        {
                            ToolTip.CreateTooltip(120);
                        }
                        break;
                    }
                }
            }
            //Building lastBuilding;
            foreach (Building b in Planet.BuildingList)
            {
                if (b.EventHere && (Planet.Owner == null || !Planet.Owner.GetBDict()[b.Name]))
                {
                    StatusIcons.X -= 20f;
                    var statusRect = new Rectangle((int) StatusIcons.X, (int) StatusIcons.Y, 16, 16);
                    batch.Draw(ResourceManager.Texture($"Buildings/icon_{b.Icon}_48x48"), statusRect, Color.White);
                    i++;
                    if (statusRect.HitTest(Screen.Input.CursorPosition))
                        ToolTip.CreateTooltip(Localizer.Token(b.DescriptionIndex));
                }
            }

            foreach (Building b in Planet.BuildingList)
            {
                if (b.IsCommodity)
                {
                    StatusIcons.X -= 20f;
                    var statusRect = new Rectangle((int) StatusIcons.X, (int) StatusIcons.Y, 16, 16);
                    batch.Draw(ResourceManager.Texture($"Buildings/icon_{b.Icon}_48x48"), statusRect, Color.White);
                    i++;
                    if (statusRect.HitTest(Screen.Input.CursorPosition))
                        ToolTip.CreateTooltip(Localizer.Token(b.DescriptionIndex));
                }
            }

            int troops = Planet.TroopsHere.Count(t => t.Loyalty.isPlayer);
            if (troops > 0)
            {
                StatusIcons.X -= 20f;// (float)(18 * i);

                var statusRect = new Rectangle((int)StatusIcons.X, (int)StatusIcons.Y, 16, 16);
                batch.Draw(ResourceManager.Texture("UI/icon_troop"), statusRect, new Color(255, 255, 255, 255));//Color..White);
                if (statusRect.HitTest(Screen.Input.CursorPosition))
                {
                    ToolTip.CreateTooltip($"{Localizer.Token(336)}: {troops}");
                }
            }
            
            var rpos = new Vector2
            {
                X = ShipNameEntry.ClickableArea.X,
                Y = ShipNameEntry.ClickableArea.Y - 10
            };
            batch.DrawString(Fonts.Arial20Bold, Planet.Name, rpos, TextColor);
            rpos.Y = rpos.Y + (Fonts.Arial20Bold.LineSpacing - 3);
            Vector2 FertilityCursor = new Vector2(FertRect.X + 35, FertRect.Y + FertRect.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2);
            batch.DrawString(Fonts.Arial12Bold, Planet.FertilityFor(EmpireManager.Player).String(), FertilityCursor, (Planet.Habitable ? Color.White : Color.LightPink));
            Vector2 RichCursor = new Vector2(RichRect.X + 35, RichRect.Y + RichRect.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2);
            batch.DrawString(Fonts.Arial12Bold, Planet.MineralRichness.String(1), RichCursor, (Planet.Habitable ? Color.White : Color.LightPink));
            Vector2 PopCursor = new Vector2(PopRect.X + 60, PopRect.Y + PopRect.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2);
            batch.DrawString(Fonts.Arial12Bold, Planet.PopulationStringForPlayer, PopCursor, (Planet.Habitable ? Color.White : Color.LightPink));
            Vector2 OwnerCursor = new Vector2(OwnerRect.X + 20, OwnerRect.Y + OwnerRect.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2);
            SpriteBatch spriteBatch2 = batch;
            SpriteFont spriteFont = Fonts.Arial12Bold;
            if (Planet.Owner != null)
            {
                singular = Planet.Owner.data.Traits.Singular;
            }
            else
            {
                singular = (Planet.Habitable ? Localizer.Token(2263) : Localizer.Token(2264));
            }
            spriteBatch2.DrawString(spriteFont, singular, OwnerCursor, Planet.Owner?.EmpireColor ?? Color.Gray);
            batch.DrawString(Fonts.Arial12Bold, Planet.LocalizedRichness, rpos, TextColor);
            if (Planet.Habitable && Planet.Owner == null)
            {
                Colonize.Draw(batch);
            }

            if (Planet.Owner == null && Planet.Habitable)  //fbedard: can send troop anywhere
            {
                int troopsInvading = 0;
                BatchRemovalCollection<Ship> ships = Screen.EmpireUI.empire.GetShips();
                for (int z = 0; z < ships.Count; z++)
                {
                    Ship ship = ships[z];
                    ShipAI ai = ship?.AI;                    
                    if (ai == null ||  ai.State == AIState.Resupply || ship.TroopList.IsEmpty || ai.OrderQueue.IsEmpty) continue;
                    if (ai.OrderQueue.Any(goal => goal.TargetPlanet != null && goal.TargetPlanet == Planet))
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
            if (Planet.Owner == Empire.Universe.player)
            {
                int troopsInvading = Screen.EmpireUI.empire.GetShips()
                 .Where(troop => troop.TroopList.Count > 0)
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

            batch.DrawRectangle(Rect, textColor);
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
    }
}