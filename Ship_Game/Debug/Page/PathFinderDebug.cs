using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game.Debug.Page;

internal class PathFinderDebug : DebugPage
{
    int EmpireID = 1;

    public PathFinderDebug(DebugInfoScreen parent) : base(parent, DebugModes.PathFinder)
    {
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
        EmpireID += (increase ? 1 : -1);
        if (EmpireID > Universe.NumEmpires) EmpireID = 1;
        if (EmpireID < 1) EmpireID = Universe.NumEmpires;

        Empire e = Universe.GetEmpireById(EmpireID);
        TextColumns[0].Text = $"Empire: {e.Name}";
        TextColumns[0].Color = e.EmpireColor;
    }

    void DrawPathInfo()
    {

    }
}