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
    AO,
    ThreatMatrix,
    SpatialManager,
    Input,
    Tech,
    Solar, // Sun timers, black hole data, pulsar radiation radius, InfluenceTree
    War,
    Pirates,
    Remnants,
    Agents,
    Relationship,
    FleetMulti,
    StoryAndEvents,
    Tasks,
    Particles,


    // this should always be the last valid page
    Perf,

    // special value, turns debug mode off
    Disabled,
}