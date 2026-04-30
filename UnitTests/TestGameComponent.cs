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

        // MonoGame's IDrawable / IUpdateable expect change events; emitted only as no-ops here.
        public event EventHandler<EventArgs> DrawOrderChanged   = delegate { };
        public event EventHandler<EventArgs> VisibleChanged     = delegate { };
        public event EventHandler<EventArgs> EnabledChanged     = delegate { };
        public event EventHandler<EventArgs> UpdateOrderChanged = delegate { };

        // initialized by TestGameDummy.AddComponent
        public TestGameDummy Game;

        public virtual void Initialize() { }

        // MonoGame's IUpdateable.Update / IDrawable.Draw take a GameTime; we forward to the legacy
        // float-delta and parameterless overloads used throughout the test fixtures.
        public virtual void Update(GameTime gameTime)
            => Update((float)gameTime.ElapsedGameTime.TotalSeconds);
        public virtual void Update(float deltaTime) { }

        public virtual void Draw(GameTime gameTime) => Draw();
        public virtual void Draw() => Draw(Game.Batch);
        public abstract void Draw(SpriteBatch batch);
    }
}
