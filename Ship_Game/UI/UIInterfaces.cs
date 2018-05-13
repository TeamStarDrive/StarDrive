namespace Ship_Game
{
    public interface IInputHandler
    {
        // @return TRUE if input was handled by the UI Control
        bool HandleInput(InputState input);
    }
}
