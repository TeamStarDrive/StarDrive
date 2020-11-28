namespace Microsoft.Xna.Framework
{
    public interface IUpdateable
    {
        bool Enabled { get; }
        int UpdateOrder { get; }
        void Update(float deltaTime);
    }
}
