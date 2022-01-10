namespace Ship_Game.Ships
{
    public enum HangarOptions
    {
        General,

        // Anti-Ship designation
        // Hangar ship can be of any size, but will prefer attacking big ships
        AntiShip,

        // Anti-Fighter designation
        // Hangar ship can be of any size, but will prefer attacking fighter sized ships
        Interceptor,
    }
}