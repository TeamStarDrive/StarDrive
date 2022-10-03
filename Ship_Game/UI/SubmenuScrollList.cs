using System;
using System.Collections.Generic;
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

    public SubmenuScrollList(RectF theMenu, LocalizedText title, ScrollList2<T> list, ListStyle style = ListStyle.Default)
        : base(theMenu, title, Style(style))
    {
        list.Rect = ClientArea;
        List = base.Add(list);
        base.PerformLayout();
    }

    public SubmenuScrollList(RectF menu, LocalizedText title, int itemHeight = 40, ListStyle style = ListStyle.Default)
        : base(menu, title, Style(style))
    {
        List = base.Add(new ScrollList2<T>(ClientArea, itemHeight));
    }

    public SubmenuScrollList(LocalPos pos, Vector2 size, LocalizedText title, ListStyle style = ListStyle.Default)
        : base(pos, size, title, Style(style))
    {
        base.PerformLayout();
        List = base.Add(new ScrollList2<T>(ClientArea));
    }

    public SubmenuScrollList(RectF menu, IEnumerable<LocalizedText> tabs, int itemHeight = 40, ListStyle style = ListStyle.Default)
        : base(menu, tabs, Style(style))
    {
        List = base.Add(new ScrollList2<T>(ClientArea, itemHeight));
    }

    public SubmenuScrollList(RectF menu, ListStyle style = ListStyle.Default)
        : base(menu, Style(style))
    {
        List = base.Add(new ScrollList2<T>(ClientArea, style:style));
    }

    public SubmenuScrollList(in RectF menu, int itemHeight, ListStyle style = ListStyle.Default)
        : base(menu, Style(style))
    {
        List = base.Add(new ScrollList2<T>(ClientArea, itemHeight, style:style));
    }
}
