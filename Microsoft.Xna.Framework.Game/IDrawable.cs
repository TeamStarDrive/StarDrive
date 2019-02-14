namespace Microsoft.Xna.Framework
{
    public interface IDrawable
    {
        bool Visible { get; }
        int DrawOrder { get; }
        void Draw(GameTime gameTime);
    }
}
