using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Ships;

namespace Ship_Game.ShipDesignIssues
{
    public class ShipDesignIssues
    {
        public readonly ShipData Hull;
        public Array<DesignIssueDetails> CurrentDesignIssues { get; }
        public WarningLevel CurrentWarningLevel { get; private set; }

        public ShipDesignIssues(ShipData hull)
        {
            Hull = hull;
            CurrentDesignIssues = new Array<DesignIssueDetails>();
        }

        void AddDesignIssue(DesignIssueType type, WarningLevel severity)
        {
            DesignIssueDetails details = new DesignIssueDetails(type, severity);
            CurrentDesignIssues.Add(details);
            UpdateCurrentWarningLevel(details.Severity);
        }

        void UpdateCurrentWarningLevel(WarningLevel level)
        {
            if (level > CurrentWarningLevel)
                CurrentWarningLevel = level;
        }

        bool LargeCraft => Hull.HullRole == ShipData.RoleName.freighter || Hull.HullRole == ShipData.RoleName.destroyer
                           || Hull.HullRole == ShipData.RoleName.cruiser || Hull.HullRole == ShipData.RoleName.capital;


        public void Reset()
        {
            CurrentDesignIssues.Clear();
            CurrentWarningLevel = WarningLevel.None;
        }

        public void CheckIssueNoCommand(int numCommand)
        {
            if (Hull.Role != ShipData.RoleName.platform && numCommand == 0)
                AddDesignIssue(DesignIssueType.NoCommand, WarningLevel.Critical);
        }

        public void CheckIssueBackupCommand(int numCommand, int size)
        {
            if (Hull.Role != ShipData.RoleName.platform && numCommand <= 1 && size >= 500)
                AddDesignIssue(DesignIssueType.BackUpCommand, WarningLevel.Major);
        }

        public void  CheckIssueUnpoweredModules(bool unpoweredModules)
        {
            if (unpoweredModules)
                AddDesignIssue(DesignIssueType.UnpoweredModules, WarningLevel.Major);
        }

        public void CheckIssueOrdnance(float ordnanceUsed, float ordnanceRecovered, float ammoTime, int size)
        {
            if ((ordnanceUsed - ordnanceRecovered).LessOrEqual(0))
                return;  // Inf ammo

            if (ammoTime < 5)
            {
                AddDesignIssue(DesignIssueType.NoOrdnance, WarningLevel.Critical);
            }
            else
            {
                int goodAmmoTime = LargeCraft ? 50 : 25;
                if (ammoTime < goodAmmoTime)
                    AddDesignIssue(DesignIssueType.LowOrdnance, WarningLevel.Minor);
            }
        }

        public void CheckIssuePowerRecharge(float recharge)
        {
            if (recharge.Less(0))
                AddDesignIssue(DesignIssueType.NegativeRecharge, WarningLevel.Critical);
        }

        public void CheckIssueWarpDraw(float warpDraw, float ftlTime, float warpSpeed)
        {
            if (warpSpeed.AlmostZero() || warpDraw.GreaterOrEqual(0) || ftlTime > 900)
                return;

            WarningLevel severity = ftlTime < 60 ? WarningLevel.Critical : WarningLevel.Major;
            AddDesignIssue(DesignIssueType.LowWarpTime, severity);
        }

        public Color CurrentWarningColor => IssueColor(CurrentWarningLevel);

        public static Color IssueColor(WarningLevel severity)
        {
            switch (severity)
            {
                default:
                case WarningLevel.None: return Color.Green;
                case WarningLevel.Minor: return Color.Yellow;
                case WarningLevel.Major: return Color.Orange;
                case WarningLevel.Critical: return Color.Red;
            }
        }
    }

    public enum DesignIssueType
    {
        NoCommand,
        BackUpCommand,
        UnpoweredModules,
        NoOrdnance,
        LowOrdnance,
        NegativeRecharge,
        LowWarpTime
    }

    public enum WarningLevel
    {
        None,
        Minor,
        Major,
        Critical
    }

    public struct DesignIssueDetails
    {
        public readonly DesignIssueType Type;
        public readonly WarningLevel Severity;
        public readonly Color Color;
        public readonly string Title;
        public readonly string Problem;
        public readonly string Remediation;
        public readonly SubTexture Texture;

        public DesignIssueDetails(DesignIssueType issueType, WarningLevel severity) : this()
        {
            Type = issueType;
            Severity = severity;
            Color = ShipDesignIssues.IssueColor(severity);
            switch (issueType)
            {
                default:
                case DesignIssueType.NoCommand:
                    Title = new LocalizedText(2501).Text;
                    Problem = new LocalizedText(2502).Text; ;
                    Remediation = new LocalizedText(2503).Text; ;
                    Texture = ResourceManager.Texture("NewUI/IssueNoCommand");
                    break;
                case DesignIssueType.BackUpCommand:
                    Title = new LocalizedText(2504).Text;
                    Problem = new LocalizedText(2505).Text; ;
                    Remediation = new LocalizedText(2506).Text; ;
                    Texture = ResourceManager.Texture("NewUI/IssueNoCommand");
                    break;
                case DesignIssueType.UnpoweredModules:
                    Title = new LocalizedText(2507).Text;
                    Problem = new LocalizedText(2508).Text; ;
                    Remediation = new LocalizedText(2509).Text; ;
                    Texture = ResourceManager.Texture("NewUI/IssueNoCommand");
                    break;
                case DesignIssueType.NoOrdnance:
                    Title = new LocalizedText(2510).Text;
                    Problem = new LocalizedText(2511).Text; ;
                    Remediation = new LocalizedText(2512).Text; ;
                    Texture = ResourceManager.Texture("NewUI/IssueNoCommand");
                    break;
                case DesignIssueType.LowOrdnance:
                    Title = new LocalizedText(2513).Text;
                    Problem = new LocalizedText(2514).Text; ;
                    Remediation = new LocalizedText(2515).Text; ;
                    Texture = ResourceManager.Texture("NewUI/IssueNoCommand");
                    break;
                case DesignIssueType.LowWarpTime:
                    Title = new LocalizedText(2516).Text;
                    Problem = new LocalizedText(2517).Text; ;
                    Remediation = new LocalizedText(2518).Text; ;
                    Texture = ResourceManager.Texture("NewUI/IssueNoCommand");
                    break;
            }
        }
    }
}