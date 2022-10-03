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

    static SubmenuStyle Style(ListStyle s) => s == ListStyle.Default ? SubmenuStyle.Brown : SubmenuStyle.Blue;

    public SubmenuScrollList(RectF theMenu, ScrollList2<T> list, ListStyle style = ListStyle.Default)
        : base(theMenu, Style(style))
    {
        list.Rect = ClientArea;
        List = base.Add(list);
        base.PerformLayout();
    }

    public SubmenuScrollList(RectF theMenu, ListStyle style = ListStyle.Default)
        : base(theMenu, Style(style))
    {
        List = base.Add(new ScrollList2<T>(ClientArea, style:style));
    }

    public SubmenuScrollList(in RectF theMenu, int itemHeight, ListStyle style = ListStyle.Default)
        : base(theMenu, Style(style))
    {
        List = base.Add(new ScrollList2<T>(ClientArea, itemHeight, style:style));
    }

    public SubmenuScrollList(float x, float y, float width, float height, ListStyle style = ListStyle.Default)
        : base(x, y, width, height, Style(style))
    {
        base.PerformLayout();
        List = base.Add(new ScrollList2<T>(ClientArea));
    }

    public SubmenuScrollList(LocalPos pos, Vector2 size, ListStyle style = ListStyle.Default)
        : base(pos, size, Style(style))
    {
        base.PerformLayout();
        List = base.Add(new ScrollList2<T>(ClientArea));
    }
}
