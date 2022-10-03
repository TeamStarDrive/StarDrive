using System;
using SDGraphics;

namespace Ship_Game.UI;

/// <summary>
/// A variation of Submenu which always contains
/// a ScrollList in its ClientArea 
/// </summary>
public class SubmenuScrollList<T> : Submenu where T : ScrollListItem<T>
{
    public ScrollList2<T> List;

    public SubmenuScrollList(RectF theMenu, ListStyle style = ListStyle.Default)
        : base(theMenu, style == ListStyle.Default ? SubmenuStyle.Brown : SubmenuStyle.Blue)
    {
        List = base.Add(new ScrollList2<T>(ClientArea, style:style));
    }

    public SubmenuScrollList(in RectF theMenu, int itemHeight, ListStyle style = ListStyle.Default)
        : base(theMenu, style == ListStyle.Default ? SubmenuStyle.Brown : SubmenuStyle.Blue)
    {
        List = base.Add(new ScrollList2<T>(ClientArea, itemHeight, style:style));
    }

    public SubmenuScrollList(float x, float y, float width, float height, ListStyle style = ListStyle.Default)
        : base(x, y, width, height, style == ListStyle.Default ? SubmenuStyle.Brown : SubmenuStyle.Blue)
    {
        base.PerformLayout();
        List = base.Add(new ScrollList2<T>(ClientArea));
    }
    public SubmenuScrollList(LocalPos pos, Vector2 size, ListStyle style = ListStyle.Default)
        : base(pos, size, style == ListStyle.Default ? SubmenuStyle.Brown : SubmenuStyle.Blue)
    {
        base.PerformLayout();
        List = base.Add(new ScrollList2<T>(ClientArea));
    }
}
