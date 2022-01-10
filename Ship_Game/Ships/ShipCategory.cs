namespace Ship_Game.Ships
{
    public enum ShipCategory
    {
        Unclassified,
        Civilian,
        Recon,
        // controls the percentage of internal damage before a ship will retreat
        // conservative means if a ship suffers 20% internal damage, it retreats
        Conservative,
        Neutral,
        Reckless,
        Kamikaze
    }
}