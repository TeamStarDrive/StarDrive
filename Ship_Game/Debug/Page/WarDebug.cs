using Microsoft.Xna.Framework.Graphics;
using SDUtils;
using Ship_Game.Gameplay;

namespace Ship_Game.Debug.Page
{
    public class WarDebug : DebugPage
    {
        int EmpireID = 1;
        Empire EmpireAtWar;

        public WarDebug(DebugInfoScreen parent) : base(parent, DebugModes.War)
        {
            if (TextColumns.Count <= 1)
                TextColumns.Add(Label(Rect.X, Rect.Y + 300, ""));
            EmpireAtWar = Universe.GetEmpireById(EmpireID);
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            if (!Visible)
                return;

            //DrawWarAOs();
            base.Draw(batch, elapsed);
        }

        public override bool HandleInput(InputState input)
        {
            if      (input.ArrowUp)   ChangeEmpireId(true);
            else if (input.ArrowDown) ChangeEmpireId(false);

            return base.HandleInput(input);
        }

        void ChangeEmpireId(bool increase)
        {
            do
            {
                EmpireID += (increase ? 1 : -1);
                if (EmpireID > Universe.NumEmpires) EmpireID = 1;
                if (EmpireID < 1) EmpireID = Universe.NumEmpires;
                EmpireAtWar = Universe.GetEmpireById(EmpireID);
            }
            while (EmpireAtWar.data.Defeated);
            TextColumns[0].Text = $"Empire: {EmpireAtWar.Name}";
            TextColumns[0].Color = EmpireAtWar.EmpireColor;
        }

        public override void Update(float fixedDeltaTime)
        {
            var text = new Array<DebugTextBlock>();
            if (EmpireAtWar.data.Defeated)
                return;
            
            var column = new DebugTextBlock();
            column.AddLine($"{EmpireID} {EmpireAtWar.Name}", EmpireAtWar.EmpireColor);
            text.Add(column);

            foreach (Relationship rel in EmpireAtWar.AllRelations.Sorted(r => r.AtWar))
            {
                if (rel.Known && !rel.Them.IsFaction && rel.Them != EmpireAtWar && !rel.Them.data.Defeated)
                    text.Add(rel.DebugWar(EmpireAtWar));
            }

            SetTextColumns(text);
            base.Update(fixedDeltaTime);
        }
    }
}
