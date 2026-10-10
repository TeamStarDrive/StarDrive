namespace Ship_Game.Graphics;

// A fluorescent tube's light level while it flickers, as steps over a 0..1 span of the flicker's time
public static class FluorescentLight
{
    public static readonly (float At, float Level)[] TurnOn =
    {
        (0f, 0f), (0.07f, 1f), (0.11f, 0f), (0.28f, 0.6f), (0.31f, 0f), (0.47f, 1f), (0.51f, 0f),
        (0.62f, 0.4f), (0.66f, 0f), (0.77f, 1f), (0.81f, 0.3f), (0.87f, 1f), (0.91f, 0.5f), (1f, 1f)
    };

    public static readonly (float At, float Level)[] TurnOff =
    {
        (0f, 1f), (0.15f, 0.2f), (0.2f, 1f), (0.35f, 0f), (0.4f, 0.8f), (0.52f, 0f),
        (0.65f, 0.5f), (0.7f, 0f), (0.9f, 0.25f), (1f, 0f)
    };

    public static float Level((float At, float Level)[] steps, float fraction)
    {
        float level = steps[0].Level;
        for (int i = 1; i < steps.Length && fraction >= steps[i].At; ++i)
            level = steps[i].Level;
        return level;
    }

    // a tube that never manages to start: TurnOn over and over, `seconds` each time
    public static float Stutter(float time, float seconds)
    {
        return Level(TurnOn, time / seconds % 1f);
    }
}
