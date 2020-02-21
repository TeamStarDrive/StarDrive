using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game.GameScreens.ShipDesign.DesignIssues
{
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

        public DesignIssueDetails(DesignIssueType issueType)
        {
            Type = issueType;
            switch (issueType)
            {
                default:
                case DesignIssueType.NoCommand:
                    Severity    = WarningLevel.Critical;
                    Color       = Color.Red;
                    Title       = new LocalizedText(2501).Text;
                    Problem     = new LocalizedText(2502).Text; ;
                    Remediation = new LocalizedText(2503).Text; ;
                    Texture     = ResourceManager.Texture("NewUI/IssueNoCommand");
                    break;
                case DesignIssueType.BackUpCommand:
                    Severity    = WarningLevel.Major;
                    Color       = Color.Orange;
                    Title       = new LocalizedText(2504).Text;
                    Problem     = new LocalizedText(2505).Text; ;
                    Remediation = new LocalizedText(2506).Text; ;
                    Texture     = ResourceManager.Texture("NewUI/IssueNoCommand");
                    break;
                case DesignIssueType.UnpoweredModules:
                    Severity    = WarningLevel.Major;
                    Color       = Color.Orange;
                    Title       = new LocalizedText(2507).Text;
                    Problem     = new LocalizedText(2508).Text; ;
                    Remediation = new LocalizedText(2509).Text; ;
                    Texture     = ResourceManager.Texture("NewUI/IssueNoCommand");
                    break;
                case DesignIssueType.NoOrdnance:
                    Severity    = WarningLevel.Critical;
                    Color       = Color.Red;
                    Title       = new LocalizedText(2510).Text;
                    Problem     = new LocalizedText(2511).Text; ;
                    Remediation = new LocalizedText(2512).Text; ;
                    Texture     = ResourceManager.Texture("NewUI/IssueNoCommand");
                    break;
                case DesignIssueType.LowOrdnance:
                    Severity    = WarningLevel.Minor;
                    Color       = Color.Yellow;
                    Title       = new LocalizedText(2513).Text;
                    Problem     = new LocalizedText(2514).Text; ;
                    Remediation = new LocalizedText(2515).Text; ;
                    Texture     = ResourceManager.Texture("NewUI/IssueNoCommand");
                    break;
            }
        }
    }

    public enum DesignIssueType
    {
        NoCommand,
        BackUpCommand,
        UnpoweredModules,
        NoOrdnance,
        LowOrdnance
    }
}