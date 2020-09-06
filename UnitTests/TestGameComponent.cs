using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace UnitTests
{
    public abstract class TestGameComponent : IGameComponent, IDrawable, IUpdateable
    {
        public bool Visible     { get; } = true;
        public bool Enabled     { get; } = true;
        public int  DrawOrder   { get; } = 0;
        public int  UpdateOrder { get; } = 0;

        // initialized by TestGameDummy.AddComponent
        public TestGameDummy Game;

        public virtual void Initialize()
        {
        }
        public virtual void Update(float deltaTime)
        {
        }
        public virtual void Draw(float deltaTime)
        {
            Draw(Game.Batch);
        }
        public abstract void Draw(SpriteBatch batch);
    }
}
