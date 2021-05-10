using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game.GameScreens.Sandbox
{
    public struct PredictedLine
    {
        public Vector2 Start;
        public Vector2 End;
    }

    public class PredictionDebugPlatform : Ship
    {
        public PredictionDebugPlatform(string shipName, Empire owner, Vector2 position)
            : base(shipName, owner, position)
        {
            VanityName = "Prediction Debugger " + shipName;
        }

        readonly Array<PredictedLine> PredictResults = new Array<PredictedLine>();
        public IReadOnlyList<PredictedLine> Predictions => PredictResults;

        int NumHitsScored;
        int NumShotsFired;
        float ShotTimer;

        public float AccuracyPercent => NumShotsFired == 0 ? 0 : NumHitsScored / (float)NumShotsFired;
        public bool CanFire { get; set; } = false;

        public override void Update(FixedSimTime timeStep)
        {
            ApplyAllRepair(1000f*timeStep.FixedTime, 1, true); // +1000HP/s
            AddPower(25f);

            ShotTimer += timeStep.FixedTime;
            if (ShotTimer > 5f) // cull shots to have a fresh accuracy
            {
                ShotTimer = 0f;
                NumHitsScored /= 2;
                NumShotsFired /= 2;
            }

            if (Weapons.IsEmpty)
            {
                Log.Warning($"PredictionDebug ship {ShipName} has no weapons!");
            }
            else if (CanFire)
            {
                GameplayObject[] nearby = UniverseScreen.Spatial.FindNearby(GameObjectType.Ship,
                                                                                 this, 4000f, maxResults:64);
                nearby.SortByDistance(Center);

                var nearbyShips = new Array<Ship>(nearby.Cast<Ship>());
                var noProjectiles = new Array<Projectile>();

                foreach (Weapon weapon in Weapons)
                {
                    weapon.Module.FieldOfFire = RadMath.TwoPI/3f;
                    weapon.fireDelay = 0.5f;
                    weapon.BaseRange = 4000f;
                    PredictResults.Clear();

                    foreach (Ship ship in nearbyShips)
                    {
                        if (weapon.isBeam)
                        {
                            weapon.UpdateAndFireAtTarget(null, noProjectiles, nearbyShips);
                        }
                        else
                        {
                            Vector2 pip = weapon.ProjectedImpactPointNoError(ship);
                            PredictResults.Add(new PredictedLine{ Start = weapon.Origin, End = pip });
                            if (weapon.ManualFireTowardsPos(pip))
                                NumShotsFired++;
                        }
                    }
                }
            }
            else
            {
                foreach (Weapon weapon in Weapons)
                {
                    weapon.BaseRange = 10;
                    weapon.DamageAmount = 0.5f;
                    weapon.CooldownTimer = 1f;
                }
            }
            base.Update(timeStep);
        }

        public override void OnDamageInflicted(ShipModule victim, float damage)
        {
            NumHitsScored++;
        }
    }
}
