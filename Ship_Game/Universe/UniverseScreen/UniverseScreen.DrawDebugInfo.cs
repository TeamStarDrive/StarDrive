using System;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    public partial class UniverseScreen
    {
        // empire perf indicators
        public readonly AggregatePerfTimer PreEmpirePerf   = new();
        public readonly AggregatePerfTimer EmpireInfluPerf = new();
                        
        public readonly AggregatePerfTimer ResetBordersPerf  = new();
        public readonly AggregatePerfTimer ScanFromPlanetsPerf = new();
        public readonly AggregatePerfTimer ThreatMatrixPerf  = new();

        public readonly AggregatePerfTimer EmpireUpdatePerf = new();
        public readonly AggregatePerfTimer EmpireMiscPerf   = new();
        public readonly AggregatePerfTimer PostEmpirePerf   = new();

        public readonly AggregatePerfTimer TurnTimePerf = new();
        public readonly AggregatePerfTimer ProcessSimTurnsPerf = new();
        
        public readonly AggregatePerfTimer DrawGroupTotalPerf = new();
        int ActualDrawFPS => DrawGroupTotalPerf.MeasuredSamples;

        public readonly AggregatePerfTimer RenderGroupTotalPerf = new();
        public readonly AggregatePerfTimer BeginSunburnPerf = new();
        public readonly AggregatePerfTimer BackdropPerf = new();
        public readonly AggregatePerfTimer SunburnDrawPerf = new();
        public readonly AggregatePerfTimer DrawPlanetsPerf = new();
        public readonly AggregatePerfTimer DrawShieldsPerf = new();
        public readonly AggregatePerfTimer DrawParticles = new();
        public readonly AggregatePerfTimer DrawExplosionsPerf = new();
        public readonly AggregatePerfTimer EndSunburnPerf = new();
        
        public readonly AggregatePerfTimer OverlaysGroupTotalPerf = new();
        public readonly AggregatePerfTimer DrawFogInfluence = new();
        public readonly AggregatePerfTimer DrawBorders = new();
        public readonly AggregatePerfTimer DrawFogOfWar = new();
        public readonly AggregatePerfTimer DrawOverFog = new();

        public readonly AggregatePerfTimer IconsGroupTotalPerf = new();
        public readonly AggregatePerfTimer DrawProj = new();
        public readonly AggregatePerfTimer DrawShips = new();
        public readonly AggregatePerfTimer DrawIcons = new();
        public readonly AggregatePerfTimer DrawUI = new();
    }
}
