using Microsoft.Xna.Framework.Graphics;

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

        public override void Draw(SpriteBatch batch)
        {
            if (!Visible)
                return;

            DrawPathInfo();
            base.Draw(batch);
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

        public override void Update(float deltaTime)
        {
            var text = new Array<DebugTextBlock>();

            foreach (var kv in EmpireAtWar.AllRelations)
            {
                var relation = kv.Value;
                if (relation.Known && !kv.Key.isFaction && kv.Key != EmpireAtWar)
                {
                    text.Add(relation.DebugWar());
                }
            }
            SetTextColumns(text);
            base.Update(deltaTime);
        }

        void DrawPathInfo()
        {

        }
    }
}
