using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship_Game.UI
{
    /// <summary>
    /// Presents a convenient "Key""Value" SplitElement container
    /// 
    /// Example usage:
    /// var keyvals = Add(new UIKeyValueLabel(GameText.DesignCompletion, "100%"));
    /// keyvals.ValueText = "50%";
    /// </summary>
    public class UIKeyValueLabel : SplitElement
    {
        public UILabel Key;
        public UILabel Value;

        LocalizedText RawKey;
        string SeparatorString = ": ";

        Func<float> GetValue;
        Func<float, Color> GetColor;
        float CurrentValue;
        bool IsPercent = false;

        public UIKeyValueLabel(in LocalizedText keyText, in LocalizedText valueText,
                               Color? valueColor = null, float split = 0f)
            : base(new UILabel(keyText.Concat(": ")),
                   new UILabel(valueText, valueColor ?? Color.White))
        {
            Key = (UILabel)First;
            Value = (UILabel)Second;
            Split = split;
            RawKey = keyText;
        }

        public LocalizedText KeyText
        {
            get => RawKey;
            set
            {
                RawKey = value;
                Key.Text = value.Concat(SeparatorString);
            }
        }

        public LocalizedText ValueText
        {
            get => Value.Text;
            set => Value.Text = value;
        }

        public Color ValueColor
        {
            get => Value.Color;
            set => Value.Color = value;
        }

        public string Separator
        {
            get => SeparatorString;
            set
            {
                SeparatorString = value;
                KeyText = RawKey;
            }
        }

        public Func<float> DynamicPercent
        {
            set
            {
                GetValue = value;
                CurrentValue = float.NaN;
                IsPercent = true;
            }
        }

        public Func<float> DynamicValue
        {
            set
            {
                GetValue = value;
                CurrentValue = float.NaN;
                IsPercent = false;
            }
        }

        public Func<float, Color> DynamicColor
        {
            set
            {
                GetColor = value;
                CurrentValue = float.NaN;
            }
        }

        public override void Update(float fixedDeltaTime)
        {
            if (GetValue != null)
            {
                float value = GetValue();
                if (CurrentValue != value)
                {
                    CurrentValue = value;
                    if (GetColor != null)
                        Value.Color = GetColor(value);
                    ValueText = IsPercent ? CurrentValue.ToString("P0") : CurrentValue.GetNumberString();
                }
            }
            base.Update(fixedDeltaTime);
        }
    }
}
