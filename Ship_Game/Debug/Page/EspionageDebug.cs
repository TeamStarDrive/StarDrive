using Ship_Game.AI;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using Ship_Game.Ships;
using Ship_Game.UI;
using Ship_Game.Ships.Components;
using System;
using SDUtils;
using System.Windows.Forms.VisualStyles;

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
    }

    public override void Draw(SpriteBatch batch, DrawTimes elapsed)
    {
        if (!Visible || SelEmpire.IsDefeated)
            return;

        if (SelectedEmpireDebug == null || SelEmpire != SelectedEmpireDebug.Empire)
        {
            SelectedEmpireDebug = new SelectedEmpireEspionageDebug(SelEmpire);
            Empire[] empires = SelEmpire.Universe.ActiveMajorEmpires.Filter(e => e != SelEmpire);
            EmpiresDebug.Clear();
            for (int i = 0; i < empires.Length; i++)
            {
                Empire empire = empires[i];
                EmpiresDebug.Add(new EmpireEspionageDebug(SelEmpire,empire));
            }
        }

        Text.SetCursor(75, 200, SelEmpire.EmpireColor);
        Text.String($"Empire: {SelectedEmpireDebug.Empire.Name}");
        if (!SelEmpire.isPlayer)
        {
            Text.String($"Personality:           {SelEmpire.Personality}");
            Text.String($"Espionage Budget:      {SelectedEmpireDebug.SpyBudget.String(1)}");
        }
        Text.String($"Budget Multiplier:           {SelectedEmpireDebug.BudgetMultiplier}");
        Text.String($"Budget/Points PerTurn:  {SelectedEmpireDebug.PointsPerTurn}");
        Text.String($"Defense Weight:             {SelectedEmpireDebug.DefenseWeight}");
        Text.String($"Defense Ratio:                {SelectedEmpireDebug.DefenseRatio}");

        for (int i = 0; i < EmpiresDebug.Count; i++) 
        {
            EmpireEspionageDebug empireDebug = EmpiresDebug[i];
            if (!SelEmpire.IsKnown(empireDebug.Empire) || empireDebug.Empire.IsDefeated)
                continue;

            Text.SetCursor(75 + (i - (i > 4 ? 5 : 0)) * 300, 350 * (i > 4 ? 2 : 1), empireDebug.Empire.EmpireColor);
            Text.String($"-----------------------------------------------");
            Text.String($"Empire: {empireDebug.Empire.Name}");
            Text.String($"{empireDebug.Empire.Personality}");
            Text.String($"Infiltration Weight:        {empireDebug.InfiltrationWeight}/{empireDebug.TotalWeight}");
            Text.String($"Level:                            {empireDebug.Level}");
            Text.String($"LimitLevel:                    {empireDebug.LimitLevel}");
            Text.String($"EffectiveLevel:              {empireDebug.EffectiveLevel}");
            Text.String($"Level Progress:             {empireDebug.LevelProgress.String(2)}/{empireDebug.NextLevelCost}");
            Text.String($"Progress Per Turn:        {empireDebug.ProgressPerTurn.String(2)}");
            Text.String($"Number of Moles:          {empireDebug.NumMoles}");
            Text.NewLine();
            foreach (OpsTurns operation in empireDebug.Operations)
                Text.String($"({operation.Level}) {operation.Type}: {operation.TurnsLeft}");
            
            Text.String($"-----------------------------------------------");
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

    protected class SelectedEmpireEspionageDebug
    {
        public readonly Empire Empire;
        public float SpyBudget { get; private set; }
        public float BudgetMultiplier { get; private set; }
        public int DefenseWeight { get; private set; }
        public float DefenseRatio { get; private set; }
        public float PointsPerTurn { get; private set; }

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

    protected class EmpireEspionageDebug
    {
        public readonly Empire Empire;
        readonly Empire SelEmpire;
        readonly Espionage Espionage;
        public int InfiltrationWeight { get; private set; }
        public float LevelProgress { get; private set; }
        public byte Level { get; private set; }
        public byte EffectiveLevel { get; private set; }

        public byte LimitLevel { get; private set; }
        public int NumMoles { get; private set; }
        public float TotalMoneyLeeched { get; private set; }
        public float NextLevelCost { get; private set; }
        public float ProgressPerTurn { get; private set; }
        public float TotalWeight { get; private set; }

        public Array<OpsTurns> Operations { get; private set; } = new();


        public EmpireEspionageDebug(Empire selEmpire, Empire empire)
        {
            SelEmpire = selEmpire;
            Empire = empire;
            Espionage = selEmpire.GetEspionage(empire);
            Update(selEmpire.CalcTotalEspionageWeight());
        }

        public void Update(float totalWeight)
        {
            InfiltrationWeight = Espionage.ActualWeight;
            LevelProgress      = Espionage.LevelProgress;
            Level              = Espionage.Level;
            EffectiveLevel     = Espionage.EffectiveLevel;
            LimitLevel         = Espionage.LimitLevel;
            NumMoles           = Espionage.NumPlantedMoles;
            TotalMoneyLeeched  = Espionage.TotalMoneyLeeched;
            NextLevelCost      = Espionage.NextLevelCost;
            ProgressPerTurn    = Espionage.GetProgressToIncrease(SelEmpire.EspionagePointsPerTurn, totalWeight);
            TotalWeight        = totalWeight;

            Operations.Clear();
            foreach (InfiltrationOpsType type in (InfiltrationOpsType[])Enum.GetValues(typeof(InfiltrationOpsType)))
                Operations.Add(new OpsTurns(type, Espionage));
        }
    }
    protected struct OpsTurns
    {
        public readonly InfiltrationOpsType Type;
        public readonly string TurnsLeft;
        public readonly byte Level;

        public OpsTurns(InfiltrationOpsType type, Espionage espionage)
            {
            Type = type;
            TurnsLeft = espionage.IsOperationActive(type) ? espionage.RemainingTurnsForOps(type) : "Inactive";
            Level = Espionage.GetOpsLevel(type);
        }
    }

}