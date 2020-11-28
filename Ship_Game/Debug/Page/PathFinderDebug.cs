using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game.Debug.Page
{
    internal class PathFinderDebug : DebugPage
    {
        readonly UniverseScreen Screen;
        int EmpireID = 1;

        public PathFinderDebug(UniverseScreen screen, DebugInfoScreen parent) : base(parent, DebugModes.PathFinder)
        {
            Screen = screen;
            if (TextColumns.Count <= 1)
                TextColumns.Add(Label(Rect.X, Rect.Y + 300, ""));
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            if (!Visible)
                return;

            DrawPathInfo();
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
            EmpireID = EmpireID + (increase ? 1 : -1);
            if (EmpireID > EmpireManager.NumEmpires) EmpireID = 1;
            if (EmpireID < 1) EmpireID = EmpireManager.NumEmpires;

            Empire e = EmpireManager.GetEmpireById(EmpireID);
            TextColumns[0].Text = $"Empire: {e.Name}";
            TextColumns[0].Color = e.EmpireColor;
        }

        void DrawPathInfo()
        {

        }
    }
}

