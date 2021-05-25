using Ship_Game.Ships;

namespace Ship_Game.AI
{
    public sealed partial class ShipAI
    {
        public struct ShipWeight
        {
            public Ship Ship;
            public float Weight;

            public ShipWeight(Ship ship, float weight = 0)
            {
                Ship = ship;
                Weight = weight;                
            }
            public ShipWeight(GameplayObject gamePlayObject, float weight = 0) : this(gamePlayObject as Ship, weight) { }            
            
            //We can just say shipWieght += 2 to add 2 the shipweight
            public static ShipWeight operator + (ShipWeight shipWeight, float weight) => new ShipWeight(shipWeight.Ship, shipWeight.Weight + weight);            
            
            //same this for a ship although... seems silly since im not "adding" a ship.
            public static ShipWeight operator +(ShipWeight shipWeight, Ship ship) => new ShipWeight(ship, shipWeight.Weight);

            //i dont know how overload the "=" operator and keep the ship. 
            public void SetWeight(float weight) => Weight = weight;
        }
    }
}