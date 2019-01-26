using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game.Debug.Page
{
    internal class PathDebug : DebugPage
    {
        readonly UniverseScreen Screen;
        int EmpireID = 1;

        public PathDebug(UniverseScreen screen, DebugInfoScreen parent) : base(parent, DebugModes.Pathing)
        {
            Screen = screen;
            if (DebugText.Count <= 1)
                DebugText.Add(Label(Rect.X, Rect.Y + 300, ""));
        }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);
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
            DebugText[0].Text = $"Empire: {e.Name}";
            DebugText[0].Color = e.EmpireColor;
        }

        void DrawPathInfo()
        {
            Empire e = EmpireManager.GetEmpireById(EmpireID);
            int width = e.grid.GetLength(0);
            int height = e.grid.GetLength(1);
            for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                byte weight = e.grid[x, y];
                Color color = weight == 80 ? Color.Black : e.EmpireColor;
                var translated = new Vector2((x - e.granularity) * Screen.PathMapReducer, 
                                             (y - e.granularity) * Screen.PathMapReducer);
                Screen.DrawCircleProjected(translated, Screen.PathMapReducer * 0.5f, weight + 5, color);
            }
        }
    }
}

