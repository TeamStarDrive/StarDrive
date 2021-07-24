using System;

namespace Ship_Game.Ships.Components
{
    public class AlertStatus
    {
        [Flags]
        public enum Status
        {
            None,
            Decreasing,
            Increasing,
            Stagnant,
            Low,
            Moderate,
            High
        }

        public enum CrewStatus
        {
            None,
            FlatFooted,
            Unprepared,
            Preparing,
            Prepared,
            Exceptional
        }

        float AlertTime;
        float AlertLevelTime;
        Ship Owner;

        const float InCombatTime    = 10;
        const float BadGuysNearTime = 5;
        
        CrewStatus Crew;
        Status Progress;

        public bool ReadyForNonCombatDuties => (Crew <= CrewStatus.Unprepared && Progress.HasFlag(Status.Low)) ||
                                               Progress.HasFlag(Status.Moderate);
        public bool ReadyForCombatDuties => Crew >= CrewStatus.Prepared;

        public AlertStatus(Ship owner)
        {
            Owner = owner;
        }

        public void Update(FixedSimTime timeStep)
        {
            AlertTime -= timeStep.FixedTime;
            if (Owner.InCombat)
            {
                AlertTime = InCombatTime;
                Progress  = IncreaseAlertLevelTime(timeStep) & Status.High;
            }
            else if (Owner.AI.BadGuysNear)
            {
                AlertTime       = Math.Max(AlertTime, BadGuysNearTime);
                Status progress = IncreaseAlertLevelTime(timeStep);
                Status level    = Progress.HasFlag(Status.Stagnant) & AlertTime <= 5 ? Status.Moderate : Status.High;
                Progress        = progress & level;
            }
            else
            {
                Progress  = DecreaseAlertLevelTime(timeStep) & Status.Low;
                AlertTime = Math.Max(AlertTime, 0);
            }
        }

        Status IncreaseAlertLevelTime(FixedSimTime timeStep)
        {
            AlertLevelTime += timeStep.FixedTime * Math.Max(1, Owner.Level);
            Status progress = Status.Stagnant;
            if (AlertLevelTime > 1)
            {
                switch (Crew)
                {
                    case CrewStatus.None:
                    case CrewStatus.FlatFooted:
                    case CrewStatus.Unprepared:
                    case CrewStatus.Preparing:
                        Crew++;
                        progress = Status.Increasing;
                        break;
                    case CrewStatus.Prepared:
                        if (Owner.Level > 1)
                        {
                            Crew++;
                            progress = Status.Increasing;
                        }
                        break;
                    case CrewStatus.Exceptional:
                        break;
                }
                AlertLevelTime = 0;
            }

            return progress;
        }

        Status DecreaseAlertLevelTime(FixedSimTime timeStep)
        {
            AlertLevelTime -= timeStep.FixedTime * Math.Max(1, Owner.Level);
            Status progress = Status.Stagnant;
            if (AlertLevelTime <= 0)
            {
                switch (Crew)
                {
                    case CrewStatus.None:
                    case CrewStatus.FlatFooted:
                    case CrewStatus.Unprepared:
                        if (Owner.Level < 1)
                        {
                            Crew--;
                            progress = Status.Decreasing;
                        }
                        break;
                    case CrewStatus.Preparing:
                    case CrewStatus.Prepared:
                    case CrewStatus.Exceptional:
                        Crew--;
                        progress = Status.Decreasing;
                        break;
                }
                AlertLevelTime = 1;
            }

            return progress;
        }
    }
}