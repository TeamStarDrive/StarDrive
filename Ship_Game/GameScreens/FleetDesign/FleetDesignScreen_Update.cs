using Ship_Game.AI;
using Ship_Game.Fleets;
using SDGraphics;
using SDUtils;
using Vector2 = SDGraphics.Vector2;
using Vector3 = SDGraphics.Vector3;
using Matrix = SDGraphics.Matrix;
using Rectangle = SDGraphics.Rectangle;

namespace Ship_Game
{
    public sealed partial class FleetDesignScreen
    {
        public override void Update(float fixedDeltaTime)
        {
            CamPos.X += CamVelocity.X;
            CamPos.Y += CamVelocity.Y;
            CamPos.Z = Microsoft.Xna.Framework.MathHelper.SmoothStep(CamPos.Z, DesiredCamHeight, 0.2f);

            var camPos = new Vector3(-CamPos.X, CamPos.Y, CamPos.Z);
            var lookAt = new Vector3(-CamPos.X, CamPos.Y, 0f);
            SetViewMatrix(Matrix.CreateRotationY(180f.ToRadians())
                        * Matrix.CreateLookAt(camPos, lookAt, Vector3.Down));

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
                    Vector3 pScreenSpace = new Vector3(Viewport.Project(new Vector3(squad.Offset, 0f), Projection, View, Matrix.Identity));
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