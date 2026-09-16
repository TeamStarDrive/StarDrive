using Ship_Game.Fleets;
using SDGraphics;
using SDUtils;
using Vector2 = SDGraphics.Vector2;
using Matrix = SDGraphics.Matrix;

namespace Ship_Game
{
    public sealed partial class FleetDesignScreen
    {
        public override void Update(float fixedDeltaTime)
        {
            CamPos.X = CamPos.X.SmoothStep(DesiredCamPos.X, 0.2f);
            CamPos.Y = CamPos.Y.SmoothStep(DesiredCamPos.Y, 0.2f);
            CamPos.Z = CamPos.Z.SmoothStep(DesiredCamPos.Z, 0.2f);

            Matrix cameraMatrix = Matrices.CreateLookAtDown(CamPos.X, CamPos.Y, -CamPos.Z);
            SetViewMatrix(cameraMatrix);

            UpdateClickableSquads();
            SelectedFleet.AssembleFleet(SelectedFleet.FinalPosition, SelectedFleet.FinalDirection, true);
            base.Update(fixedDeltaTime);
        }

        void UpdateClickableSquads()
        {
            ClickableSquads.Clear();

            foreach (Fleet.Squad squad in AllSquads)
            {
                Vector2 pos = ProjectToScreenPos(new(squad.Offset, 0));
                ClickableSquads.Add(new()
                {
                    Rect = RectF.FromCenter(pos, 32, 32),
                    Squad = squad
                });
            }
        }
    }
}