using SDGraphics;
using Ship_Game.AI;
using Ship_Game.Data.Serialization;
using Ship_Game.ExtensionMethods;
using Ship_Game.Graphics.Particles;
using Vector2 = SDGraphics.Vector2;
using Vector3 = SDGraphics.Vector3;

namespace Ship_Game.Ships
{
    [StarDataType]
    public class LaunchShip
    {
        [StarData] readonly Ship Owner;
        [StarData] public float Scale;
        [StarData] float RotationDegX = 75;
        [StarData] float RotationRadZ;
        [StarData] float StartingScale;
        [StarData] readonly float SecondsToZeroX;
        [StarData] readonly float SecondsHalfScale;
        [StarData] readonly float TotalDuration;
        const int DistanceMovementUp = 1000;
        const int MinSecondsToHalfScale = 2;
        const int MaxSecondsToHalfScale = 10;


        [StarDataConstructor]
        public LaunchShip(Ship owner, bool dynamicRotationZ)
        {
            Owner = owner;
            SecondsHalfScale = (DistanceMovementUp / owner.MaxSTLSpeed.LowerBound(100)).Clamped(MinSecondsToHalfScale, MaxSecondsToHalfScale);
            SecondsToZeroX = RotationDegX / owner.RotationRadsPerSecond.ToDegrees().LowerBound(5);
            TotalDuration = SecondsHalfScale + SecondsToZeroX;
            RotationRadZ = (45f * ((int)(owner.Universe.StarDate*10) % 10 % 8)).ToRadians();
            int lala = (int)(owner.Universe.StarDate * 10);
            int lala2 = lala % 10;
            int lala3 = lala2 % 8;
        }

        public void Update(bool visibleToPlayer, FixedSimTime timeStep)
        {
            Owner.AI.IgnoreCombat = true;
            Owner.YRotation = 0;
            Owner.Rotation = RotationRadZ;
            Scale += timeStep.FixedTime / TotalDuration;
            if (Scale >= 0.5f)
                RotationDegX = 75 - ((Scale - 0.5f)/0.5f * 75);

            if (Scale >= 1 && !Owner.IsConstructor && !Owner.IsSupplyShuttle)
                Owner.AI.IgnoreCombat = false;

            if (!visibleToPlayer)
                return;

            var SO = Owner.GetSO();
            if (Owner.GetSO() != null)
            {
                SO.World = Matrix.CreateTranslation(new Vector3(Owner.ShipData.BaseHull.MeshOffset, 0f))
                             * Matrix.CreateScale(Scale)
                             * Matrix.CreateRotationY(Owner.YRotation)
                             * Matrix.CreateRotationX(RotationDegX.ToRadians())
                             * Matrix.CreateRotationZ(Owner.Rotation)
                             * Matrix.CreateTranslation(new Vector3(Owner.Position, 0f));
                SO.UpdateAnimation(timeStep.FixedTime);

                //UpdateThrusters(timeStep);
            }
            else // auto-create scene objects if possible
            {
                Owner.Universe.Screen?.QueueSceneObjectCreation(Owner);
            }
        }

        public void Complete()
        {
            Owner.AI.IgnoreCombat= false;
        }
    }

    public enum LaunchPlan
    {
        Planet,
        Hanger,
        Shipyard
    }

}