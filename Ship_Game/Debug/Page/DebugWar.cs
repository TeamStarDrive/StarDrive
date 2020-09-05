﻿using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI.StrategyAI.WarGoals;

namespace Ship_Game.Debug.Page
{
    public class DebugWar : DebugPage
    {
        readonly UniverseScreen Screen;
        int EmpireID = 1;
        Empire EmpireAtWar;

        public DebugWar(UniverseScreen screen, DebugInfoScreen parent) : base(parent, DebugModes.RelationsWar)
        {
            Screen = screen;
            if (TextColumns.Count <= 1)
                TextColumns.Add(Label(Rect.X, Rect.Y + 300, ""));
            EmpireAtWar = EmpireManager.GetEmpireById(EmpireID);
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            if (!Visible)
                return;

            DrawWarAOs();
            base.Draw(batch, elapsed);
        }

        public override bool HandleInput(InputState input)
        {
            if (input.ArrowUp) ChangeEmpireId(true);
            else if (input.ArrowDown) ChangeEmpireId(false);
            return base.HandleInput(input);
        }

        void ChangeEmpireId(bool increase)
        {
            EmpireID = EmpireID + (increase ? 1 : -1);
            if (EmpireID > EmpireManager.NumEmpires) EmpireID = 1;
            if (EmpireID < 1) EmpireID = EmpireManager.NumEmpires;

            EmpireAtWar = EmpireManager.GetEmpireById(EmpireID);
            TextColumns[0].Text = $"Empire: {EmpireAtWar.Name}";
            TextColumns[0].Color = EmpireAtWar.EmpireColor;
        }

        public override void Update(float fixedDeltaTime)
        {
            var text = new Array<DebugTextBlock>();
            if (EmpireAtWar.data.Defeated) return;
            foreach (var kv in EmpireAtWar.AllRelations.Sorted(r=> r.Value.AtWar))
            {
                var relation = kv.Value;
                if (relation.Known && !kv.Key.isFaction && kv.Key != EmpireAtWar && !kv.Key.data.Defeated)
                {
                    text.Add(relation.DebugWar());
                }
            }
            SetTextColumns(text);
            base.Update(fixedDeltaTime);
        }

        void DrawWarAOs()
        {
            foreach(var rel in EmpireAtWar.AllRelations)
            {
                var war = rel.Value.ActiveWar;
                if (war == null ||  war.Them.isFaction) continue;
                for (int i = 0; i < war.WarTheaters.Theaters.Count; i++)
                {
                    var theater = war.WarTheaters.Theaters[i];
                    var ao      = theater.TheaterAO;
                    Screen.DrawCircleProjected(ao.Center, ao.Radius, war.Them.EmpireColor, (float) war.GetWarScoreState() * 2 + 1);
                }
            }
        }
    }
}
