using SDGraphics;

namespace Ship_Game
{
    public sealed class Camera2D
    {
        private Matrix WorldMatrix;
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

        private void UpdateTransform()
        {
            WorldMatrix = Matrix.CreateTranslation(new Vector3(-CamPos.X, -CamPos.Y, 0f))
                        * Matrix.CreateScale(new Vector3(CamZoom, CamZoom, 1f))
                        * Matrix.CreateTranslation(new Vector3(GameBase.ScreenCenter, 0f));
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
            return worldCoord.Transform(WorldMatrix);
        }

        public Vector2 GetWorldSpaceFromScreenSpace(Vector2 screenCoord)
        {
            Vector2 worldPos = screenCoord;
            worldPos -= GameBase.ScreenCenter;
            worldPos /= Zoom;
            worldPos += Pos;
            return worldPos;
        }

        public void Move(float dx, float dy)
        {
            CamPos.X += dx;
            CamPos.Y += dy;
        }

        public void MoveClamped(Vector2 pos, Vector2 min, Vector2 max)
        {
            CamPos.X += pos.X;
            CamPos.Y += pos.Y;
            CamPos = CamPos.Clamped(min.X, min.Y, max.X, max.Y);
        }

        private Vector2 CameraVelocity = Vector2.Zero;

        public void CameraDrag(InputState input)
        {
            if (input.RightMouseHeld())
            {
                if (input.StartRightHold.OutsideRadius(input.CursorPosition, 10f))
                    CameraVelocity = input.StartRightHold - input.CursorPosition;
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