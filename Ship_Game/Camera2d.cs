using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Ship_Game
{
	public class Camera2d
	{
		protected float _zoom;

		public Matrix _transform;

		public Vector2 _pos;

		protected float _rotation;

		public Vector2 Pos
		{
			get
			{
				return this._pos;
			}
			set
			{
				this._pos = value;
			}
		}

		public float Rotation
		{
			get
			{
				return this._rotation;
			}
			set
			{
				this._rotation = value;
			}
		}

		public float Zoom
		{
			get
			{
				return this._zoom;
			}
			set
			{
				this._zoom = value;
				if (this._zoom < 0.01f)
				{
					this._zoom = 0.01f;
				}
				if (this._zoom >= 10f)
				{
					this._zoom = 10f;
				}
			}
		}

		public Camera2d()
		{
			this._zoom = 1f;
			this._rotation = 0f;
			this._pos = Vector2.Zero;
		}

		public Matrix get_transformation(GraphicsDevice graphicsDevice)
		{
			this._transform = (Matrix.CreateTranslation(new Vector3(-this._pos.X, -this._pos.Y, 0f)) * Matrix.CreateScale(new Vector3(this.Zoom, this.Zoom, 1f))) * Matrix.CreateTranslation(new Vector3((float)Game1.Instance.GraphicsDevice.PresentationParameters.BackBufferWidth * 0.5f, (float)Game1.Instance.GraphicsDevice.PresentationParameters.BackBufferHeight * 0.5f, 0f));
			return this._transform;
		}

		public Vector2 GetScreenSpaceFromWorldSpace(Vector2 worldCoordinate)
		{
			Matrix transform = (Matrix.CreateTranslation(new Vector3(-this._pos.X, -this._pos.Y, 0f)) * Matrix.CreateScale(new Vector3(this.Zoom, this.Zoom, 1f))) * Matrix.CreateTranslation(new Vector3((float)Game1.Instance.GraphicsDevice.PresentationParameters.BackBufferWidth * 0.5f, (float)Game1.Instance.GraphicsDevice.PresentationParameters.BackBufferHeight * 0.5f, 0f));
			Matrix matrix = Matrix.CreateRotationY(3.14159274f) * Matrix.CreateRotationX(3.14159274f);
			return Vector2.Transform(worldCoordinate, transform);
		}

		public void Move(Vector2 amount)
		{
			Camera2d camera2d = this;
			camera2d._pos = camera2d._pos + amount;
		}
	}
}