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

        public UIKeyValueLabel(in LocalizedText keyText, in LocalizedText valueText,
                               Color? valueColor = null, float split = 0f)
            : base(new UILabel(keyText.Concat(": ")),
                   new UILabel(valueText, valueColor ?? Color.White))
        {
            Key = (UILabel)First;
            Value = (UILabel)Second;
            Split = split;
        }

        public LocalizedText KeyText
        {
            get => Key.Text;
            set => Key.Text = value.Concat(": ");
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
    }
}
