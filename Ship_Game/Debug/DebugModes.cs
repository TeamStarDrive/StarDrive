namespace Ship_Game.Debug;

public enum DebugModes
{
    Normal,
    Empire,
    Targeting,
    PathFinder,
    DefenseCo,
    Trade,
    Planets,
    ThreatMatrix,
    SpatialManager,
    Input,
    Tech,
    Influence, // InfluenceTree
    Solar, // Sun timers, black hole data, pulsar radiation radius
    War,
    Pirates,
    Remnants,
    Agents,
    Relationship,
    FleetMulti,
    StoryAndEvents,
    Tasks,
    Particles,
    SpaceRoads,
    Goals,
    MiningOps,


    // this should always be the last valid page
    Perf,

    // special value, turns debug mode off
    Disabled,
}