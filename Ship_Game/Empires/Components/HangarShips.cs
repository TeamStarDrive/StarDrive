namespace Ship_Game.Empires.Components
{
    /// <summary>
    /// Simple Version
    /// All carriers will use this to determine what fighters will be launched.
    /// this will be possible by role and dynamic launch type.
    /// This will mean that all carriers will use the same fighters.
    /// Advanced Version
    /// have multiple fighters that can be chosen.
    /// add new types of hangar ships.
    /// Control the way the hangar ships are chosen by situation.
    ///
    /// Entry Points:
    /// Setup in empireManager restore unserializable data.
    /// Update during ShipsWecanBuild
    /// get hangarShips during carrier launch
    /// </summary>
    public class HangarShips
    {

    }
}