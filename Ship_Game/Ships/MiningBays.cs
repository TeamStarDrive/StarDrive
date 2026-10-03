using SDGraphics;
using SDUtils;
using Color = Microsoft.Xna.Framework.Color;

namespace Ship_Game.Ships
{
    public struct LightPillar
    {
        public Vector2 Position;
        public float TopZ;
        public float BottomZ;
        public float HalfWidth;
        public Color Color;
    }

    public class MiningBays
    {
        public const float PillarAbove = 1.2f;
        public const float PillarBelow = 0.6f;
        public const float DimmestPillar = 0.35f;
        public const float FlickerOnSeconds = 2f;
        public const float FlickerOffSeconds = 2f;
        static readonly (float At, float Level)[] FlickerOn =
        {
            (0f, 0f), (0.07f, 1f), (0.11f, 0f), (0.28f, 0.6f), (0.31f, 0f), (0.47f, 1f), (0.51f, 0f),
            (0.62f, 0.4f), (0.66f, 0f), (0.77f, 1f), (0.81f, 0.3f), (0.87f, 1f), (0.91f, 0.5f), (1f, 1f)
        };
        static readonly (float At, float Level)[] FlickerOff =
        {
            (0f, 1f), (0.15f, 0.2f), (0.2f, 1f), (0.35f, 0f), (0.4f, 0.8f), (0.52f, 0f),
            (0.65f, 0.5f), (0.7f, 0f), (0.9f, 0.25f), (1f, 0f)
        };

        Ship Owner;
        readonly ShipModule[] AllMiningBays;
        readonly ParticleEmitter[] SmokeEmitters;
        bool EmittersStarted;
        bool RefiningKnown;
        float RefiningChangedAt = float.NegativeInfinity;
        float LitOutput;
        public byte RefiningOutput { get; private set; } // 0-100

        public MiningBays(Ship ship, ShipModule[] slots)
        {
            Owner = ship;
            AllMiningBays = slots.Filter(module => module.IsMiningBay);
            SmokeEmitters = new ParticleEmitter[AllMiningBays.Length];
        }

        public void Dispose()
        {
            Owner = null;
        }

        public void ProcessMiningBays(float rawResourcesStored, Planet planet)
        {
            if (Owner == null
                || !HasOrdnanceToLaunch()
                || rawResourcesStored >= Owner.MiningStationCargoSpaceMax && RefiningOutput == 0)
            {
                return;
            }

            float resourcesNeeded = RefiningOutput > 0 ? Owner.MiningStationCargoSpaceMax 
                                                       : Owner.MiningStationCargoSpaceMax - rawResourcesStored;

            if (TryGetCandidateBayAndReturnExcessMiners(resourcesNeeded, out ShipModule candidateBay)
                && CreateMiningShip(candidateBay, out Ship miningShip)) 
            {
                candidateBay.HangarTimer = candidateBay.HangarTimerConstant;
                miningShip.AI.OrderMinePlanet(planet);
                return;
            }
        }

        bool TryGetCandidateBayAndReturnExcessMiners(float resourcesNeeded, out ShipModule candidateBay)
        {
            candidateBay = null;
            float miningShipsCapacityAlreadyMining = 0;
            foreach (ShipModule miningBay in AllMiningBays)
            {
                if (miningBay.Active)
                {
                    if (miningBay.TryGetHangarShipActive(out Ship activeMiningShip))
                    {
                        miningShipsCapacityAlreadyMining += activeMiningShip.CargoSpaceMax;
                        if (activeMiningShip.CargoSpaceUsed > resourcesNeeded && activeMiningShip.AI.State == Ship_Game.AI.AIState.Mining)
                        {
                            activeMiningShip.InitLaunch(LaunchPlan.MinerReturn, activeMiningShip.RotationDegrees);
                            activeMiningShip.AI.OrderReturnToHangarDeferred();
                            continue;
                        }

                        if (miningShipsCapacityAlreadyMining >= resourcesNeeded)
                            return false;
                    }
                    else if (candidateBay == null
                        && miningShipsCapacityAlreadyMining < resourcesNeeded  
                        && miningBay.HangarTimer <= 0)
                    {
                        candidateBay = miningBay;
                    }
                }
            }

            return candidateBay != null;
        }
            
        bool HasOrdnanceToLaunch()
        {
            if (AllMiningBays.Length == 0)
                return false;

            ShipModule miningBay = AllMiningBays[0];
            miningBay.HangarShipUID = Owner.Loyalty.GetMiningShipName();
            Ship miningShipTemplate = ResourceManager.GetShipTemplate(miningBay.HangarShipUID);
            return miningShipTemplate.ShipOrdLaunchCost < Owner.Ordinance;
        }

        bool CreateMiningShip(ShipModule hangar, out Ship miningShip)
        {
            miningShip = Ship.CreateShipFromHangar(Owner.Universe, hangar, Owner.Loyalty, Owner.Position, Owner);
            if (miningShip != null)
                Owner.OnShipLaunched(miningShip, hangar);

            return miningShip != null;
        }

        public void UpdateMiningVisuals(FixedSimTime timeStep)
        {
            if (RefiningOutput == 0)
                return;
            
            for (int i = 0; i < AllMiningBays.Length; i++)
            {
                EmittersStarted = true;
                ShipModule miningBay = AllMiningBays[i];
                if (miningBay.Active && miningBay.Powered)
                {
                    if (SmokeEmitters[i] == null)
                        SmokeEmitters[i] = Owner.Universe.Screen.Particles.SmokePlume.NewEmitter(39f, miningBay.Position, 0.75f);
                    else
                        SmokeEmitters[i].Update(timeStep.FixedTime, miningBay.Position.ToVec3(-50));
                }
            }
        }

        public void DestroyEmmiters()
        {
            if (!EmittersStarted)
                return;

            for (int i = 0; i < AllMiningBays.Length; i++)
                SmokeEmitters[i] = null;

            EmittersStarted = false;
        }

        public void UpdateIsRefining(float ratio)
        {
            byte output = (byte)(ratio * 100);
            if (RefiningKnown && output > 0 != RefiningOutput > 0)
                RefiningChangedAt = Owner?.Universe.Screen?.CurrentSimTime ?? 0f;

            RefiningKnown = true;
            RefiningOutput = output;
            if (output > 0)
                LitOutput = output / 100f;
        }

        public float PillarBrightness(float simTime)
        {
            float since = simTime - RefiningChangedAt;
            float level = RefiningOutput > 0 ? Flicker(FlickerOn, since / FlickerOnSeconds)
                                             : Flicker(FlickerOff, since / FlickerOffSeconds);
            return level * (DimmestPillar + (1f - DimmestPillar) * LitOutput);
        }

        static float Flicker((float At, float Level)[] steps, float since)
        {
            float level = steps[0].Level;
            for (int i = 1; i < steps.Length && since >= steps[i].At; ++i)
                level = steps[i].Level;
            return level;
        }

        public void AddLightPillars(float simTime, Array<LightPillar> pillars)
        {
            Ship owner = Owner;
            float brightness = PillarBrightness(simTime);
            if (owner == null || brightness <= 0f)
                return;

            Color empire = owner.Loyalty.EmpireColor;
            var color = new Color(empire.R, empire.G, empire.B, (byte)(brightness * 255));
            foreach (ShipModule bay in AllMiningBays)
            {
                if (bay.Active && bay.Powered)
                {
                    pillars.Add(new LightPillar
                    {
                        Position = bay.Position,
                        TopZ = -owner.Radius * PillarAbove,
                        BottomZ = owner.Radius * PillarBelow,
                        HalfWidth = bay.Radius * 0.25f,
                        Color = color,
                    });
                }
            }
        }
    }
}
