using System;

namespace Microsoft.Xna.Framework
{
    public class GameComponent : IGameComponent, IUpdateable, IDisposable
    {
        public bool Enabled { get; set; } = true;
        public int UpdateOrder { get; set; }
        public Game Game { get; }
        public event EventHandler Disposed;

        public GameComponent(Game game)
        {
            Game = game;
        }

        ~GameComponent()
        {
            Dispose(false);
        }

        public virtual void Initialize()
        {
        }

        public virtual void Update(float deltaTime)
        {
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
                return;
            lock (this)
            {
                Game?.Components.Remove(this);
                if (Disposed == null)
                    return;
                Disposed(this, EventArgs.Empty);
            }
        }

    }
}
