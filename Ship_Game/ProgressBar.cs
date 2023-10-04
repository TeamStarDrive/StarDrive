using System;
using Microsoft.Xna.Framework.Graphics;
using SDUtils;
using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;
using SDGraphics;

namespace Ship_Game;

// TODO: update to UIElementV2
public sealed class ProgressBar
{
    public Rectangle pBar;
    public float Progress;
    public float Max;
    public string color = "brown";
    public bool DrawProgressText = true; // draw "50%" or "50/100"
    public bool DrawPercentage = false;
    public bool Fraction10Values = false;
    private Rectangle Left;
    private Rectangle Right;
    private Rectangle Middle;
    private Rectangle gLeft;
    private Rectangle gRight;
    private Rectangle gMiddle;
    private bool Vertical;
    private Rectangle Top;
    private Rectangle Bot;
    public ProgressBar(float x, float y, float w, float h) : this(new Rectangle((int)x, (int)y, (int)w, (int)h))
    {
    }
        
    public ProgressBar(in Rectangle r)
    {
        SetRect(r);
    }

    public ProgressBar(in Rectangle r, float max, float progress) : this(r)
    {
        Max = max;
        Progress = progress;
    }

    public void SetRect(in Rectangle r)
    {
        pBar = r;
        Left = new Rectangle(r.X, r.Y, 7, 18);
        gLeft = new Rectangle(Left.X + 3, Left.Y + 3, 4, 12);
        Right = new Rectangle(r.X + r.Width - 7, r.Y, 7, 18);
        gRight = new Rectangle(Right.X - 3, Right.Y + 3, 4, 12);
        Middle = new Rectangle(r.X + 7, r.Y, r.Width - 14, 18);
        gMiddle = new Rectangle(Middle.X, Middle.Y + 3, Middle.Width, 12);
    }

    public float Percent => Progress / Max;

    public void Draw(SpriteBatch batch)
    {
        if (Vertical)
        {
            batch.Draw(ResourceManager.Texture("NewUI/progressbar_container_top"), Top, Color.White);
            batch.Draw(ResourceManager.Texture("NewUI/progressbar_container_mid_vert"), Middle, Color.White);
            batch.Draw(ResourceManager.Texture("NewUI/progressbar_container_bot"), Bot, Color.White);
            return;
        }
        if (Max > 0f)
        {
            batch.Draw(ResourceManager.Texture($"NewUI/progressbar_grd_{color}_left"), gLeft, Color.White);
            batch.Draw(ResourceManager.Texture($"NewUI/progressbar_grd_{color}_mid"), gMiddle, Color.White);
            batch.Draw(ResourceManager.Texture($"NewUI/progressbar_grd_{color}_right"), gRight, Color.White);
            int maskX = (int)(Percent * pBar.Width + pBar.X);
            int maskW = pBar.Width - (int)(Percent * pBar.Width);
            var mask = new Rectangle(maskX, pBar.Y, maskW, 18);
            batch.FillRectangle(mask, Color.Black);
        }
        if (color != "brown")
        {
            batch.Draw(ResourceManager.Texture($"NewUI/progressbar_container_left_{color}"), Left, Color.White);
            batch.Draw(ResourceManager.Texture($"NewUI/progressbar_container_mid_{color}"), Middle, Color.White);
            batch.Draw(ResourceManager.Texture($"NewUI/progressbar_container_right_{color}"), Right, Color.White);
        }
        else
        {
            batch.Draw(ResourceManager.Texture("NewUI/progressbar_container_left"), Left, Color.White);
            batch.Draw(ResourceManager.Texture("NewUI/progressbar_container_mid"), Middle, Color.White);
            batch.Draw(ResourceManager.Texture("NewUI/progressbar_container_right"), Right, Color.White);
        }

        if (DrawProgressText)
        {
            var textPos = new Vector2(Left.X + 7, Left.Y + Left.Height / 2 - Fonts.TahomaBold9.LineSpacing / 2);
            batch.DrawString(Fonts.TahomaBold9, Fraction10Values ? Values10 : Values, textPos, Colors.Cream);
        }
    }

    public void DrawGrayed(SpriteBatch batch)
    {
        if (Vertical)
        {
            batch.Draw(ResourceManager.Texture("NewUI/progressbar_container_top"), Top, Color.DarkGray);
            batch.Draw(ResourceManager.Texture("NewUI/progressbar_container_mid_vert"), Middle, Color.DarkGray);
            batch.Draw(ResourceManager.Texture("NewUI/progressbar_container_bot"), Bot, Color.DarkGray);
            return;
        }
        if (Max > 0f)
        {
            batch.Draw(ResourceManager.Texture($"NewUI/progressbar_grd_{color}_left"), gLeft, Color.DarkGray);
            batch.Draw(ResourceManager.Texture($"NewUI/progressbar_grd_{color}_mid"), gMiddle, Color.DarkGray);
            batch.Draw(ResourceManager.Texture($"NewUI/progressbar_grd_{color}_right"), gRight, Color.DarkGray);
            int maskX = (int)(Progress / Max * pBar.Width + pBar.X);
            int maskW = pBar.Width - (int)(Progress / Max * pBar.Width);
            var mask = new Rectangle(maskX, pBar.Y, maskW, 18);
            batch.FillRectangle(mask, Color.Black);
        }
        if (color != "brown")
        {
            batch.Draw(ResourceManager.Texture($"NewUI/progressbar_container_left_{color}"), Left, Color.DarkGray);
            batch.Draw(ResourceManager.Texture($"NewUI/progressbar_container_mid_{color}"), Middle, Color.DarkGray);
            batch.Draw(ResourceManager.Texture($"NewUI/progressbar_container_right_{color}"), Right, Color.DarkGray);
        }
        else
        {
            batch.Draw(ResourceManager.Texture("NewUI/progressbar_container_left"), Left, Color.DarkGray);
            batch.Draw(ResourceManager.Texture("NewUI/progressbar_container_mid"), Middle, Color.DarkGray);
            batch.Draw(ResourceManager.Texture("NewUI/progressbar_container_right"), Right, Color.DarkGray);
        }
        var textPos = new Vector2(Left.X + 7, Left.Y + Left.Height / 2 - Fonts.TahomaBold9.LineSpacing / 2);
        batch.DrawString(Fonts.TahomaBold9, Fraction10Values ? Values10 : Values, textPos, Color.DarkGray);
    }

    string Values10 => DrawPercentage ? $"{Progress.String(1)}%" : $"{Progress.String(1)}/{Max.String(1)}";
    string Values    => DrawPercentage ? $"{(int)Progress}%" : $"{(int)Progress}/{(int)Max}";
}

// HACK: wrapper for ProgressBar
public class ProgressBarElement : UIElementContainer
{
    public ProgressBar ProgressBar;
    public new UILabel Label;
    public string LabelPrefix;
    public bool UsePercent = true; // show progress as a percentage

    public int Percent => Max == 0f ? 0 : GetPercent(Progress, Max);
    public float Progress => ProgressBar.Progress;
        
    public float Max
    {
        get => ProgressBar.Max;
        set => ProgressBar.Max = value;
    }

    public ProgressBarElement(in RectF rect, float max = 100f, float progress = 0f) : base(rect)
    {
        ProgressBar = new(rect, max, progress);
        ProgressBar.DrawProgressText = false; // UIElementV2 uses UILabel instead
    }

    public void SetProgress(float progress)
    {
        ProgressBar.Progress = progress;
    }

    public void SetProgress(int progress)
    {
        ProgressBar.Progress = progress;
    }

    public static int GetPercent(float current, float max)
    {
        return (int)Math.Round((current*100f) / max);
    }

    public void EnableProgressLabel(string labelPrefix = "", Graphics.Font font = null)
    {
        font ??= Label?.Font ?? Fonts.TahomaBold9;

        LabelPrefix = labelPrefix;
        Label ??= Add(new UILabel(labelPrefix, font));
        Label.Font = font;
        Label.TextAlign = TextAlign.HorizontalCenter;
        Label.AxisAlign = Align.Center;
        Label.SetRelPos(0f, 0f);
        UpdateLabelText();
    }

    public override void PerformLayout()
    {
        base.PerformLayout();
        ProgressBar.SetRect(Rect);
    }

    void UpdateLabelText()
    {
        string prefix = LabelPrefix.NotEmpty() ? $"{LabelPrefix}: " : "";
        Label.Text = UsePercent
            ? $"{prefix}{Percent}%"
            : $"{prefix}{(int)Progress}/{(int)Max}";
    }

    public override void Update(float fixedDeltaTime)
    {
        if (Label != null)
        {
            UpdateLabelText();
        }
        base.Update(fixedDeltaTime);
    }

    public override void Draw(SpriteBatch batch, DrawTimes elapsed)
    {
        ProgressBar.Draw(batch);
        base.Draw(batch, elapsed);
    }
}
