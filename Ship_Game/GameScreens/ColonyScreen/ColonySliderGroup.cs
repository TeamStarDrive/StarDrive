﻿using System;
using Microsoft.Xna.Framework;

namespace Ship_Game
{
    sealed class ColonySliderGroup : UIElementContainer
    {
        readonly ColonySlider[] Sliders = new ColonySlider[3];
        ColonySlider Food, Prod, Res;
        Planet P;

        public bool DrawIcons; // draw resource icons or not?
        public Action OnSlidersChanged;

        public ColonySliderGroup(Planet p, in Rectangle housing, bool drawIcons)
            : base(housing)
        {
            P = p;
            DrawIcons = drawIcons;
            PerformLayout();
        }

        public override void PerformLayout()
        {
            if (Food == null)
            {
                for (int i = 0; i < 3; ++i)
                {
                    Sliders[i] = Add(new ColonySlider((ColonyResType)i, P, DrawIcons)
                    {
                        OnSliderChange = OnSliderChanged
                    });
                }
                Food = Sliders[(int)ColonyResType.Food];
                Prod = Sliders[(int)ColonyResType.Prod];
                Res  = Sliders[(int)ColonyResType.Res];
                SetPlanet(P);
            }

            
            int spacingY = (int)(0.25f * Height);
            for (int i = 0; i < 3; ++i)
            {
                Sliders[i].Pos = new Vector2(X, Y + spacingY*(i+1));
                Sliders[i].Width = Width;
            }

            base.PerformLayout();
        }

        public void SetPlanet(Planet p)
        {
            P = p;
            foreach (ColonySlider s in Sliders)
                s.P = p;
            if (p != null)
            {
                Food.IsDisabled = p.IsCybernetic;
                // Force lock if cybernetic, otherwise keep the lock the same as user setting
                if (p.IsCybernetic)
                    Food.LockedByUser = true;
            }
        }

        // solve 3-way slider change
        void OnSliderChanged(ColonySlider a, float difference)
        {
            ColonySlider b = Sliders.Find(s => s != a && !s.LockedByUser); // always unlocked
            ColonySlider c = Sliders.Find(s => s != a && s != b);    // maybe locked

            if (c.LockedByUser) // only one is locked, eaaasy and perfect accuracy
            {
                a.Value += difference.Clamped(-a.Value, b.Value);
                b.Resource.AutoBalanceWorkers(a.Value + c.Value);
            }
            else // all 3 unlocked
            {
                float move = difference.Clamped(-a.Value, b.Value + c.Value);
                a.Value += move;

                void ApplyDelta(ColonySlider s, float delta)
                {
                    float value = s.Value + delta;
                    if      (value < 0f) { a.Value += value;    value = 0f; }
                    else if (value > 1f) { a.Value += value-1f; value = 1f; }
                    s.Value = value;
                }
                ApplyDelta(b, -move/2);
                ApplyDelta(c, -move/2);

                // @note There is always a tiny chance for a float error
                c.Resource.AutoBalanceWorkers(a.Value + b.Value);
            }

            float sum = Sliders.Sum(s => s.Value);
            if (!sum.AlmostEqual(1f))
                Log.Warning($"ColonySlider bad sum {sum} ==> F:{Food.Value} P:{Prod.Value} R:{Res.Value}");

            OnSlidersChanged?.Invoke();
        }

        public override bool HandleInput(InputState input)
        {
            if (P == null)
            {
                Log.Error("ColonySliderGroup Planet not initialized!");
                return false;
            }

            int numLocked = Sliders.Count(s => s.LockedByUser);
            foreach (ColonySlider s in Sliders)
            {
                s.CanDrag = !s.LockedByUser && numLocked <= 1 && P.colonyType == Planet.ColonyType.Colony;
            }

            Prod.IsCrippled = P.CrippledTurns > 0;
            Prod.IsInvasion = P.RecentCombat;

            // prioritize currently dragging slider for input events
            ColonySlider dragged = Sliders.Find(s => s.IsDragging);
            if (dragged != null)
            {
                return dragged.HandleInput(input);
            }
            return base.HandleInput(input);
        }
    }
}
