using Ship_Game.AI;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using Ship_Game.Ships;
using Ship_Game.UI;
using Ship_Game.Ships.Components;
using System;
using SDUtils;

namespace Ship_Game.Debug.Page;

public class EspionageDebug : DebugPage
{
    readonly DebugEmpireSelectionSubmenu EmpireSelect;
    Empire SelEmpire => EmpireSelect.Selected;
    UIList DebugStatus;
    Array<EmpireEspionageDebug> EmpiresDebug = new();
    SelectedEmpireEspionageDebug SelectedEmpireDebug;
    const int DefaultUpdateTimer = 60;
    int UpdateTimer;


    public EspionageDebug(DebugInfoScreen parent) : base(parent, DebugModes.Agents)
    {
        EmpireSelect = base.Add(new DebugEmpireSelectionSubmenu(parent, parent.ModesTab.ClientArea.CutTop(10)));

        //DebugStatus = EmpireSelect.Add(new UIList(new LocalPos(50, 100), new Vector2(100), ListLayoutStyle.ResizeList));
        //DebugStatus.Add(new UILabel(_ => $"Us: {SelEmpire.Name}"));
        //DebugStatus.Add(new UILabel(_ => $"OurClusters: {SelThreats.OurClusters.Length}"));
        //DebugStatus.Add(new UILabel(_ => $"RivalClusters: {SelThreats.RivalClusters.Length}"));

        // we shouldn't keep track of any empty clusters, because they should be auto-pruned
        //DebugStatus.Add(new UILabel(_ => $"# of Empty OurClusters (BUG): {SelThreats.OurClusters.Count(c => c.Ships.Length == 0)}"));
        //DebugStatus.Add(new UILabel(_ => $"# of Empty RivalClusters (check TTL): {SelThreats.RivalClusters.Count(c => c.Ships.Length == 0)}"));
    }

    public override void Draw(SpriteBatch batch, DrawTimes elapsed)
    {
        if (!Visible || SelEmpire.IsDefeated)
            return;

        if (SelEmpire != SelectedEmpireDebug.Empire)
        {
            SelectedEmpireDebug = new SelectedEmpireEspionageDebug(SelEmpire);
            Empire[] empires = SelEmpire.Universe.ActiveMajorEmpires.Filter(e => e != SelEmpire);
            EmpiresDebug.Clear();
            for (int i = 0; i < empires.Length; i++)
            {
                Empire empire = empires[i];
                EmpiresDebug.Add(new EmpireEspionageDebug(empire, SelEmpire.GetEspionage(empire)));
            }
        }

        Text.SetCursor(75, 200, SelEmpire.EmpireColor);
        Text.String($"Empire: {SelectedEmpireDebug.Empire.Name}");
        if (!SelEmpire.isPlayer)
        {
            Text.String($"Personality: {SelEmpire.Personality}");
            Text.String($"Espionage Budget: {SelectedEmpireDebug.SpyBudget.String(1)}");
        }
        Text.String($"Budget Multiplier: {SelectedEmpireDebug.BudgetMultiplier}");
        Text.String($"Budget/Points PerTurn: {SelectedEmpireDebug.PointsPerTurn}");
        Text.String($"Defense Weight: {SelectedEmpireDebug.DefenseWeight}");
        Text.String($"Defense Ratio: {SelectedEmpireDebug.DefenseRatio}");

        for (int i = 0; i < EmpiresDebug.Count; i++) 
        {
            EmpireEspionageDebug empireDebug = EmpiresDebug[i];
            if (!SelEmpire.IsKnown(empireDebug.Empire) || empireDebug.Empire.IsDefeated)
                continue;

            Text.SetCursor(75 + i * 300, 400, empireDebug.Empire.EmpireColor);
            Text.String($"Infiltration Weight: {empireDebug.InfiltrationWeight}");
            Text.String($"Level: {empireDebug.Level}");
            Text.String($"Level Progress: {empireDebug.LevelProgress}/{empireDebug.NextLevelCost}");
            Text.String($"EffectiveLevel: {empireDebug.EffectiveLevel}");
            Text.String($"Progress Per Turn: {empireDebug.ProgressPerTurn}");
        }

        if (--UpdateTimer > 0)
        {
            base.Draw(batch, elapsed);
            return;
        }

        UpdateTimer = DefaultUpdateTimer;
        SelectedEmpireDebug.Update();
        float totalWeight = SelEmpire.CalcTotalEspionageWeight();
        foreach (EmpireEspionageDebug empireDebug in EmpiresDebug)
            empireDebug.Update(totalWeight);

        base.Draw(batch, elapsed);
    }

    protected struct SelectedEmpireEspionageDebug
    {
        public readonly Empire Empire;
        public float SpyBudget;
        public float BudgetMultiplier;
        public int DefenseWeight;
        public float DefenseRatio;
        public float PointsPerTurn;

        public SelectedEmpireEspionageDebug(Empire empire)
        {
            Empire = empire;
            Update();
        }

        public void Update()
        {
            SpyBudget        = Empire.AI.SpyBudget;
            BudgetMultiplier = Empire.EspionageBudgetMultiplier;
            DefenseWeight    = Empire.EspionageDefenseWeight;
            DefenseRatio     = Empire.EspionageDefenseRatio;
            PointsPerTurn    = Empire.EspionagePointsPerTurn;
        }
    }

    protected struct EmpireEspionageDebug
    { 
        public readonly Empire Empire;
        public readonly Espionage Espionage;
        public int InfiltrationWeight;
        public float LevelProgress;
        public byte Level;
        public byte EffectiveLevel;
        public int NumMoles;
        public float TotalMoneyLeeched;
        public float NextLevelCost;
        public float ProgressPerTurn;


        public EmpireEspionageDebug(Empire empire, Espionage espionage)
        {
            Empire = empire;
            Espionage = espionage;
            Update(empire.CalcTotalEspionageWeight());
        }

        public void Update(float totalWeight)
        {
            InfiltrationWeight = Espionage.ActualWeight;
            LevelProgress      = Espionage.LevelProgress;
            Level              = Espionage.Level;
            EffectiveLevel     = Espionage.EffectiveLevel;
            NumMoles           = Espionage.NumPlantedMoles;
            TotalMoneyLeeched  = Espionage.TotalMoneyLeeched;
            NextLevelCost      = Espionage.NextLevelCost;
            ProgressPerTurn    = Espionage.GetProgressToIncrease(Empire.EspionagePointsPerTurn, totalWeight);
        }
    }

}