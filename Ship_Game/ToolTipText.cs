using System;

namespace Ship_Game
{
    public struct ToolTipText
    {
        public int Id; // Tooltip ID
        public string Text; // custom text

        public static readonly ToolTipText None = new ToolTipText();
        public bool IsValid => Id > 0 || Text.NotEmpty();

        public static implicit operator ToolTipText(int id)
        {
            return new ToolTipText{ Id = id };
        }

        // Allows button.Tooltip = GameTips.AttackRunsOrder;
        public static implicit operator ToolTipText(GameTips gameTip)
        {
            return new ToolTipText{ Id = (int)gameTip };
        }

        // Allows button.Tooltip = GameText.LandAllTroopsListedIn;
        public static implicit operator ToolTipText(GameText gameText)
        {
            return new ToolTipText{ Id = (int)gameText };
        }

        public static implicit operator ToolTipText(string text)
        {
            return new ToolTipText{ Text = text };
        }

        public static implicit operator ToolTipText(in LocalizedText text)
        {
            switch (text.Method)
            {
                case LocalizationMethod.Id:      return new ToolTipText{ Id   = text.Id     };
                case LocalizationMethod.RawText: return new ToolTipText{ Text = text.String };
                case LocalizationMethod.Parse:   return new ToolTipText{ Text = text.Text   };
            }
            return None;
        }

        // This is purely for debugging purposes
        public override string ToString()
        {
            if (Id > 0)
            {
                ToolTip tooltip = ToolTip.GetToolTip(Id);
                if (tooltip != null)
                {
                    return "TIP/"+(GameTips)Id+"/: \""+Localizer.Token(tooltip.TextId)+"\"";
                }
                if (Text.IsEmpty()) // try to recover.. somehow
                {
                    return "ID/"+(GameText)Id+"/: \""+Localizer.Token(Id)+"\"";
                }
            }
            return "TEXT: \""+Text+"\"";
        }

        // intentional lazy lookup for tooltips
        public string LocalizedText
        {
            get
            {
                if (Id > 0)
                {
                    ToolTip tooltip = ToolTip.GetToolTip(Id);
                    if (tooltip != null)
                    {
                        return Localizer.Token(tooltip.TextId);
                    }
                    if (Text.IsEmpty()) // try to recover.. somehow
                    {
                        return Localizer.Token(Id);
                    }
                }
                return Text;
            }
        }
    }
}