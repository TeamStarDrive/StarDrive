namespace Ship_Game.AI
{
    // WARNING: Enums are serialized as integers, so don't change the order
    //          because it will break savegames. Add new entries to the end.
    //          At some point we will add stable enum mapping.
    public enum AIState
    {
        // using explicit integers to support deleting entries
        DoNothing = 0,
        Combat = 1,
        HoldPosition = 2,
        AwaitingOrders = 3,
        AttackTarget = 4,
        Escort = 5,
        SystemTrader = 6,
        AttackRunner = 7,
        Orbit = 8,
        PatrolSystem = 9,
        Flee = 10,
        Colonize = 11,
        MoveTo = 12,
        PirateRaiderCarrier = 13,
        Explore = 14,
        SystemDefender = 15,
        Resupply = 17,
        Rebase = 18,
        RebaseToShip = 19,
        Bombard = 20,
        Boarding = 21,
        ReturnToHangar = 22,
        MineAsteroids = 23,
        Ferrying = 24,
        Refit = 25,
        Intercept = 26,
        FormationMoveTo = 27,
        AssaultPlanet = 28,
        Exterminate = 29,
        Scuttle = 30,
        Scrap = 31,
        ResupplyEscort = 32,
        ReturnHome = 33,
        SupplyReturnHome = 34,
        Research = 35,
        Mining = 36
    }
}