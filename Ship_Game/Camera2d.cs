using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Ship_Game
{
    public sealed class Camera2D
    {
        private Matrix WorldMatrix;
        private bool Changed = true;

        private Vector2 CamPos = Vector2.Zero;
        private float CamRot  = 0f;
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
                CamZoom = value.Clamp(0.01f, 10f);
                Changed = true;
            }
        }

        private static Vector3 ScreenCenter => new Vector3(
            Game1.Instance.GraphicsDevice.PresentationParameters.BackBufferWidth * 0.5f,
            Game1.Instance.GraphicsDevice.PresentationParameters.BackBufferHeight * 0.5f, 0f);

        private void UpdateTransform()
        {
            WorldMatrix = Matrix.CreateTranslation(new Vector3(-CamPos.X, -CamPos.Y, 0f))
                          * Matrix.CreateScale(new Vector3(CamZoom, CamZoom, 1f))
                          * Matrix.CreateTranslation(ScreenCenter);
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

        public Vector2 GetScreenSpaceFromWorldSpace(Vector2 worldCoordinate)
		{
		    if (Changed)
		        UpdateTransform();
			Vector2.Transform(ref worldCoordinate, ref WorldMatrix, out Vector2 screenSpace);
            return screenSpace;
		}

		public void Move(Vector2 amount)
		{
			CamPos += amount;
		}
        
        public void Move(float dx, float dy)
        {
            CamPos.X += dx;
            CamPos.Y += dy;
        }

        public Vector2 GeneralCamMovement(InputState input, ScreenManager screenRes)
        {
            Vector2 adjustCam = Vector2.Zero;
            float maxX = screenRes.GraphicsDevice.PresentationParameters.BackBufferWidth;
            float maxY = screenRes.GraphicsDevice.PresentationParameters.BackBufferWidth;
            input.Repeat = true;
            if (input.CursorPosition.X <= 1 || input.Left) adjustCam.X -= 16 * (3 - this.Zoom);
            if (input.CursorPosition.X >= (maxX - 1) || input.Right) adjustCam.X += 16 * (3 - this.Zoom);
            if (input.CursorPosition.Y <= 1 || input.Up) adjustCam.Y -= 16 * (3 - this.Zoom);
            if (input.CursorPosition.Y >= (maxY - 1) || input.Down) adjustCam.Y += 16 * (3 - this.Zoom);
            input.Repeat = false;

            this.Move(adjustCam);
            return adjustCam;
        }
	}
}