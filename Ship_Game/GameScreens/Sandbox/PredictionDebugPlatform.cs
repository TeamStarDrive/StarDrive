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
        float ShotTimer = 0f;

        public float AccuracyPercent => NumHitsScored / (float)NumShotsFired;

        public override void Update(float elapsedTime)
        {
            ApplyAllRepair(1000f*elapsedTime, 1, true); // +1000HP/s

            ShotTimer += elapsedTime;
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
            else
            {
                GameplayObject[] nearby = UniverseScreen.SpaceManager.FindNearby(this, 4000f, GameObjectType.Ship);
                Weapon weapon = Weapons[0];
                weapon.Module.FieldOfFire = 360f;
                weapon.fireDelay = 0.5f;
                weapon.Range = 4000f;
                weapon.DamageAmount = 0.5f;
                PredictResults.Clear();
                foreach (GameplayObject o in nearby)
                {
                    if (weapon.ProjectedImpactPointNoError(o, out Vector2 pip))
                    {
                        PredictResults.Add(new PredictedLine{ Start = weapon.Origin, End = pip });
                        if (weapon.MouseFireAtTarget(pip))
                        {
                            NumShotsFired++;
                        }
                    }
                }

                for (int i = 1; i < Weapons.Count; ++i)
                {
                    Weapon disable = Weapons[i];
                    disable.Range = 10;
                    disable.DamageAmount = 0.5f;
                    disable.CooldownTimer = 10f;
                }
            }
            base.Update(elapsedTime);
        }

        public override void OnDamageInflicted(ShipModule victim, float damage)
        {
            NumHitsScored++;
        }
    }
}
