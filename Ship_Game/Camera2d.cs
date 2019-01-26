using Microsoft.Xna.Framework;

namespace Ship_Game
{
    public sealed class Camera2D
    {
        private Matrix WorldMatrix;
        private Matrix InverseWorld;
        private bool Changed = true;

        private Vector2 CamPos = Vector2.Zero;
        private float CamRot;
        private float CamZoom = 1f;

        public Vector2 Pos
        {
            get => CamPos;
            set
            {
                CamPos = value;
                Changed = true;
            }
        }
        public float Rotation
        {
            get => CamRot;
            set
            {
                CamRot = value;
                Changed = true;
            }
        }

        public float Zoom
        {
            get => CamZoom;
            set
            {
                CamZoom = value.Clamped(0.01f, 10f);
                Changed = true;
            }
        }

        private static Vector3 ScreenCenter => new Vector3(
            StarDriveGame.Instance.GraphicsDevice.PresentationParameters.BackBufferWidth * 0.5f,
            StarDriveGame.Instance.GraphicsDevice.PresentationParameters.BackBufferHeight * 0.5f, 0f);

        private void UpdateTransform()
        {
            WorldMatrix = Matrix.CreateTranslation(new Vector3(-CamPos.X, -CamPos.Y, 0f))
                        * Matrix.CreateScale(new Vector3(CamZoom, CamZoom, 1f))
                        * Matrix.CreateTranslation(ScreenCenter);
            Matrix.Invert(ref WorldMatrix, out InverseWorld);
        }

        public Matrix Transform
        {
            get
            {
                if (Changed)
                    UpdateTransform();
                return WorldMatrix;
            }
        }

        public Vector2 GetScreenSpaceFromWorldSpace(Vector2 worldCoord)
        {
            if (Changed)
                UpdateTransform();
            Vector2.Transform(ref worldCoord, ref WorldMatrix, out Vector2 screenCoord);
            return screenCoord;
        }

        public Vector2 GetWorldSpaceFromScreenSpace(Vector2 screenCoord)
        {
            Vector2 worldPos = screenCoord;
            worldPos -= StarDriveGame.Instance.ScreenCenter;
            worldPos /= Zoom;
            worldPos += Pos;
            return worldPos;
        }

        public void Move(float dx, float dy)
        {
            CamPos.X += dx;
            CamPos.Y += dy;            
        }

        public Vector2 WASDCamMovement(InputState input, GameScreen screen, float limit)
        {
            Vector2 adjustCam = Vector2.Zero;
            Vector2 c = screen.ScreenCenter;
            if (input.WASDLeft  && CamPos.X - c.X  > -limit) adjustCam.X -= GlobalStats.CameraPanSpeed * (5 - Zoom);
            if (input.WASDRight && CamPos.X - c.X  <  limit) adjustCam.X += GlobalStats.CameraPanSpeed * (5 - Zoom);
            if (input.WASDUp    && CamPos.Y - c.Y > -limit)  adjustCam.Y -= GlobalStats.CameraPanSpeed * (5 - Zoom);
            if (input.WASDDown  && CamPos.Y - c.Y <  limit)  adjustCam.Y += GlobalStats.CameraPanSpeed * (5 - Zoom);
            
            CamPos += adjustCam; 
            return CamPos - c;
        }

        private Vector2 CameraVelocity = Vector2.Zero;

        public void CameraDrag(InputState input)
        {            
            if (input.RightMouseHeld())
            {
                if (input.StartRighthold.OutsideRadius(input.CursorPosition, 10f))
                    CameraVelocity = input.StartRighthold - input.CursorPosition;
                else
                    CameraVelocity = Vector2.Zero;
            }
            else if (!input.LeftMouseHeld())
            {
                CameraVelocity = Vector2.Zero;
            }

            if (CameraVelocity.Length() > 150f)
                CameraVelocity = CameraVelocity.Normalized() * 150f;

            CamPos += CameraVelocity; 
        }
    }
}