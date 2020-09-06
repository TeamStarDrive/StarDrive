using Microsoft.Xna.Framework;
using Ship_Game.AI;
using Ship_Game.Fleets;

namespace Ship_Game
{
    public sealed partial class FleetDesignScreen
    {
        public override void Update(float fixedDeltaTime)
        {
            CamPos.X += CamVelocity.X;
            CamPos.Y += CamVelocity.Y;
            CamPos.Z = MathHelper.SmoothStep(CamPos.Z, DesiredCamHeight, 0.2f);

            View = Matrix.CreateRotationY(180f.ToRadians())
                   * Matrix.CreateLookAt(new Vector3(-CamPos.X, CamPos.Y, CamPos.Z),
                       new Vector3(-CamPos.X, CamPos.Y, 0f), Vector3.Down);

            if (SelectedFleet != null)
            {
                UpdateClickableSquads();
                SelectedFleet.AssembleFleet2(SelectedFleet.FinalPosition, SelectedFleet.FinalDirection);
            }
            base.Update(fixedDeltaTime);
        }

        void UpdateClickableSquads()
        {
            ClickableSquads.Clear();
            foreach (Array<Fleet.Squad> flank in SelectedFleet.AllFlanks)
            {
                foreach (Fleet.Squad squad in flank)
                {
                    Vector3 pScreenSpace = Viewport.Project(new Vector3(squad.Offset, 0f), Projection, View, Matrix.Identity);
                    var pPos = new Vector2(pScreenSpace.X, pScreenSpace.Y);
                    var cs = new ClickableSquad
                    {
                        ScreenPos = pPos,
                        Squad = squad
                    };
                    ClickableSquads.Add(cs);
                }
            }
        }
    }
}