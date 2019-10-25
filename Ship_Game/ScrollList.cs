using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.Audio;

namespace Ship_Game
{
    public enum ListControls
    {
        All,    // show all list controls
        Cancel // only show Cancel control
    }

    public enum ListOptions
    {
        None,
        Draggable
    }

    public enum ListStyle
    {
        Default,
        Blue, // used in research screen
    }

    [DebuggerTypeProxy(typeof(ScrollListDebugView))]
    [DebuggerDisplay("Entries = {Entries.Count}  Expanded = {ExpandedEntries.Count}")]
    public class ScrollList : UIElementV2
    {
        readonly Submenu ParentMenu;
        Rectangle ScrollUp;
        Rectangle ScrollDown;
        Rectangle ScrollBarHousing;
        Rectangle ScrollBar;

        readonly int EntryHeight = 40;
        int ScrollBarHover;
        int StartDragPos;
        public bool DraggingScrollBar { get; private set; }
        float ScrollBarStartDragPos;
        float ClickTimer;
        float TimerDelay = 0.05f;
        int YOffset = 5;

        // TODO: This is set to false for compatibility reasons
        //       By default ScrollList.HandleInput will not call HandleInput and Draw on child entries
        //       If TRUE, ScrollList will automatically HandleInput
        //       and call UIElementV2.Draw on ScrollList.Entry.item
        public bool AutoManageItems = false;

        readonly int MaxVisibleEntries;
        public int FirstVisibleIndex;
        readonly Array<Entry> Entries = new Array<Entry>();
        readonly Array<Entry> ExpandedEntries = new Array<Entry>(); // Flattened entries
        readonly bool IsDraggable;
        public Entry DraggedEntry;
        Vector2 DraggedOffset;

        public ListStyle Style;
        readonly ListControls Controls = ListControls.All;

        public ScrollList(Submenu p, ListOptions options = ListOptions.None, ListStyle style = ListStyle.Default)
        {
            ParentMenu = p;
            Style = style;
            MaxVisibleEntries = (p.Rect.Height - 25) / 40;
            IsDraggable = options == ListOptions.Draggable;
            YOffset = 30;
            this.PerformLayout();
        }

        public ScrollList(Submenu p, int eHeight, ListControls controls = ListControls.All, ListStyle style = ListStyle.Default)
        {
            ParentMenu = p;
            Style = style;
            EntryHeight = eHeight;
            Controls = controls;
            MaxVisibleEntries = (p.Rect.Height - 25) / EntryHeight;
            YOffset = 30;
            this.PerformLayout();
        }

        public ScrollList(Submenu p, int eHeight, bool realRect, ListStyle style = ListStyle.Default)
        {
            ParentMenu = p;
            Style = style;
            EntryHeight = eHeight;
            MaxVisibleEntries = p.Rect.Height / EntryHeight;
            YOffset = 5;
            this.PerformLayout();
        }

        class StyleTextures
        {
            public readonly SubTexture ScrollBarArrowUp;
            public readonly SubTexture ScrollBarArrowUpHover1;
            public readonly SubTexture ScrollBarArrowDown;
            public readonly SubTexture ScrollBarArrowDownHover1;
            public readonly SubTexture ScrollBarUpDown;
            public readonly SubTexture ScrollBarUpDownHover1;
            public readonly SubTexture ScrollBarUpDownHover2;
            public readonly SubTexture ScrollBarMid;
            public readonly SubTexture ScrollBarMidHover1;
            public readonly SubTexture ScrollBarMidHover2;

            public readonly SubTexture BuildAdd;
            public readonly SubTexture BuildAddHover1;
            public readonly SubTexture BuildAddHover2;
            public readonly SubTexture BuildEdit;
            public readonly SubTexture BuildEditHover1;
            public readonly SubTexture BuildEditHover2;

            public readonly SubTexture QueueArrowUp;
            public readonly SubTexture QueueArrowUpHover1;
            public readonly SubTexture QueueArrowUpHover2;
            public readonly SubTexture QueueArrowDown;
            public readonly SubTexture QueueArrowDownHover1;
            public readonly SubTexture QueueArrowDownHover2;
            public readonly SubTexture QueueRush;
            public readonly SubTexture QueueRushHover1;
            public readonly SubTexture QueueRushHover2;
            public readonly SubTexture QueueDelete;
            public readonly SubTexture QueueDeleteHover1;
            public readonly SubTexture QueueDeleteHover2;

            public StyleTextures(string folder)
            {
                ScrollBarArrowUp         = ResourceManager.Texture(folder+"/scrollbar_arrow_up");
                ScrollBarArrowUpHover1   = ResourceManager.Texture(folder+"/scrollbar_arrow_up_hover1");
                ScrollBarArrowDown       = ResourceManager.Texture(folder+"/scrollbar_arrow_down");
                ScrollBarArrowDownHover1 = ResourceManager.Texture(folder+"/scrollbar_arrow_down_hover1");
                ScrollBarMid             = ResourceManager.Texture(folder+"/scrollbar_bar_mid");
                ScrollBarMidHover1       = ResourceManager.Texture(folder+"/scrollbar_bar_mid_hover1");
                ScrollBarMidHover2       = ResourceManager.Texture(folder+"/scrollbar_bar_mid_hover2");
                ScrollBarUpDown          = ResourceManager.Texture(folder+"/scrollbar_bar_updown");
                ScrollBarUpDownHover1    = ResourceManager.Texture(folder+"/scrollbar_bar_updown_hover1");
                ScrollBarUpDownHover2    = ResourceManager.Texture(folder+"/scrollbar_bar_updown_hover2");

                BuildAdd          = ResourceManager.Texture("NewUI/icon_build_add");
                BuildAddHover1    = ResourceManager.Texture("NewUI/icon_build_add_hover1");
                BuildAddHover2    = ResourceManager.Texture("NewUI/icon_build_add_hover2");
                BuildEdit         = ResourceManager.Texture("NewUI/icon_build_edit");
                BuildEditHover1   = ResourceManager.Texture("NewUI/icon_build_edit_hover1");
                BuildEditHover2   = ResourceManager.Texture("NewUI/icon_build_edit_hover2");

                QueueArrowUp         = ResourceManager.Texture("NewUI/icon_queue_arrow_up");
                QueueArrowUpHover1   = ResourceManager.Texture("NewUI/icon_queue_arrow_up_hover1");
                QueueArrowUpHover2   = ResourceManager.Texture("NewUI/icon_queue_arrow_up_hover2");
                QueueArrowDown       = ResourceManager.Texture("NewUI/icon_queue_arrow_down");
                QueueArrowDownHover1 = ResourceManager.Texture("NewUI/icon_queue_arrow_down_hover1");
                QueueArrowDownHover2 = ResourceManager.Texture("NewUI/icon_queue_arrow_down_hover2");
                QueueRush            = ResourceManager.Texture("NewUI/icon_queue_rushconstruction");
                QueueRushHover1      = ResourceManager.Texture("NewUI/icon_queue_rushconstruction_hover1");
                QueueRushHover2      = ResourceManager.Texture("NewUI/icon_queue_rushconstruction_hover2");
                QueueDelete          = ResourceManager.Texture("NewUI/icon_queue_delete");
                QueueDeleteHover1    = ResourceManager.Texture("NewUI/icon_queue_delete_hover1");
                QueueDeleteHover2    = ResourceManager.Texture("NewUI/icon_queue_delete_hover2");
            }
        }

        static int ContentId;
        static StyleTextures[] Styling;

        StyleTextures GetStyle()
        {
            if (Styling != null && ContentId == ResourceManager.ContentId)
                return Styling[(int)Style];

            ContentId = ResourceManager.ContentId;
            Styling = new []
            {
                new StyleTextures("NewUI"),
                new StyleTextures("ResearchMenu"),
            };
            return Styling[(int)Style];
        }

        public override void PerformLayout()
        {
            Submenu p = ParentMenu;
            StyleTextures s = GetStyle();
            int x = p.Rect.X + p.Rect.Width - 20;
            ScrollUp   = new Rectangle(x, p.Rect.Y + YOffset,            s.ScrollBarArrowUp.Width, s.ScrollBarArrowUp.Height);
            ScrollDown = new Rectangle(x, p.Rect.Y + p.Rect.Height - 14, s.ScrollBarArrowDown.Width, s.ScrollBarArrowDown.Height);
            ScrollBarHousing = new Rectangle(ScrollUp.X + 1, ScrollUp.Y + ScrollUp.Height + 3, s.ScrollBarMid.Width, ScrollDown.Y - ScrollUp.Y - ScrollUp.Height - 6);
            ScrollBar        = new Rectangle(ScrollBarHousing.X, ScrollBarHousing.Y, s.ScrollBarMid.Width, 0);
            base.PerformLayout();
        }

        void InitializeRects(Submenu p, int yOffset)
        {

        }

        public Entry AddItem(object o)
        {
            var e = new Entry(this, o, false, false);
            Entries.Add(e);
            UpdateListElements();
            return e;
        }

        public void AddItem(object o, bool plus, bool edit)
        {
            Entries.Add(new Entry(this, o, plus, edit));
            UpdateListElements();
        }

        public void SetItems<T>(IEnumerable<T> newItems) where T : class
        {
            Entries.Clear();
            ExpandedEntries.Clear();
            foreach (T item in newItems)
                Entries.Add(new Entry(this, item, false, false));
            UpdateListElements();
        }

        bool RemoveSub(Entry e)
        {
            foreach (Entry entry in Entries)
                if (entry.RemoveSub(e)) return true;
            return false;
        }

        public void Remove(Entry e)
        {
            if (!RemoveSub(e))
                Entries.Remove(e);

            if (ExpandedEntries.Remove(e))
                UpdateListElements();
        }

        bool RemoveSubItem(Predicate<Entry> predicate)
        {
            foreach (Entry entry in Entries)
                if (entry.RemoveFirstSubIf(predicate)) return true;
            return false;
        }

        public void RemoveItem(object o)
        {
            bool ItemPredicate(Entry e) => e.item == o;

            if (!RemoveSubItem(ItemPredicate))
                Entries.RemoveFirst(ItemPredicate);

            if (ExpandedEntries.RemoveFirst(ItemPredicate))
                UpdateListElements();
        }

        public void RemoveFirstIf<T>(Func<T, bool> predicate) where T : class
        {
            bool ItemPredicate(Entry e) => e.item is T item && predicate(item);

            if (!RemoveSubItem(ItemPredicate))
                Entries.RemoveFirst(ItemPredicate);

            if (ExpandedEntries.RemoveFirst(ItemPredicate))
                UpdateListElements();
        }

        public void RemoveFirst()
        {
            if (Entries.Count <= 0)
                return;
            Entries.RemoveAt(0);
            ExpandedEntries.RemoveAt(0);
            UpdateListElements();
        }

        public Entry EntryAt(int index) => Entries[index];

        public T FirstItem<T>() where T : class
        {
            return (T)ExpandedEntries[0].item;
        }

        public T ItemAt<T>(int index) where T : class
        {
            return (T)ExpandedEntries[index].item;
        }

        public T ItemAtTop<T>() where T : class
        {
            return (T)ExpandedEntries[FirstVisibleIndex].item;
        }

        public int IndexOf<T>(Func<T, bool> predicate) where T : class
        {
            for (int i = 0; i < Entries.Count; ++i)
            {
                Entry e = Entries[i];
                if (e.item is T item && predicate(item))
                    return i;
            }
            return -1;
        }

        public void Sort<T, TValue>(Func<T, TValue> predicate) where T : class
        {
            Entry[] sorted = Entries.OrderBy(e => predicate(e.item as T)).ToArray();
            Entries.Clear();
            Entries.AddRange(sorted);
            UpdateListElements();
        }

        public void SortDescending<T, TValue>(Func<T, TValue> predicate) where T : class
        {
            Entry[] sorted = Entries.OrderByDescending(e => predicate(e.item as T)).ToArray();
            Entries.Clear();
            Entries.AddRange(sorted);
            UpdateListElements();
        }

        public IReadOnlyList<Entry> AllEntries         => Entries;
        public IReadOnlyList<Entry> AllExpandedEntries => ExpandedEntries;
        public int NumEntries         => Entries.Count;
        public int NumExpandedEntries => ExpandedEntries.Count;

        // @note Optimized for speed
        Entry[] CopyVisibleEntries(Array<Entry> entries)
        {
            int start = FirstVisibleIndex;
            int end = Math.Min(entries.Count, FirstVisibleIndex + MaxVisibleEntries);
            int count = end - start;

            Entry[] items = count <= 0 ? Empty<Entry>.Array : new Entry[count];
            for (int i = 0; i < items.Length; ++i)
                items[i] = entries[start++];
            return items;
        }

        public Entry[] VisibleEntries         => CopyVisibleEntries(Entries);
        public Entry[] VisibleExpandedEntries => CopyVisibleEntries(ExpandedEntries);


        static Array<T> CopyAllItemsOfType<T>(Array<Entry> entries)
        {
            var items = new Array<T>();
            for (int i = 0; i < entries.Count; ++i)
                if (entries[i].item is T item)
                    items.Add(item);
            return items;
        }

        public Array<T> AllItems<T>()         => CopyAllItemsOfType<T>(Entries);
        public Array<T> AllExpandedItems<T>() => CopyAllItemsOfType<T>(ExpandedEntries);


        public void Reset()
        {
            Entries.Clear();
            ExpandedEntries.Clear();
            FirstVisibleIndex = 0;
            UpdateListElements();
        }

        void DrawScrollBar(SpriteBatch batch)
        {
            StyleTextures s = GetStyle();
            int updownsize = (ScrollBar.Height - s.ScrollBarMid.Height) / 2;
            var up  = new Rectangle(ScrollBar.X, ScrollBar.Y, ScrollBar.Width, updownsize);
            var mid = new Rectangle(ScrollBar.X, ScrollBar.Y + updownsize, ScrollBar.Width, s.ScrollBarMid.Height);
            var bot = new Rectangle(ScrollBar.X, mid.Y + mid.Height, ScrollBar.Width, updownsize);
            if (ScrollBarHover == 0)
            {
                batch.Draw(s.ScrollBarUpDown, up, Color.White);
                batch.Draw(s.ScrollBarMid, mid, Color.White);
                batch.Draw(s.ScrollBarUpDown, bot, Color.White);
            }
            else if (ScrollBarHover == 1)
            {
                batch.Draw(s.ScrollBarUpDownHover1, up, Color.White);
                batch.Draw(s.ScrollBarMidHover1, mid, Color.White);
                batch.Draw(s.ScrollBarUpDownHover1, bot, Color.White);
            }
            else if (ScrollBarHover == 2)
            {
                batch.Draw(s.ScrollBarUpDownHover2, up, Color.White);
                batch.Draw(s.ScrollBarMidHover2, mid, Color.White);
                batch.Draw(s.ScrollBarUpDownHover2, bot, Color.White);
            }
            Vector2 mousepos = Mouse.GetState().Pos();
            batch.Draw(ScrollUp.HitTest(mousepos) ? s.ScrollBarArrowUpHover1 : s.ScrollBarArrowUp, ScrollUp, Color.White);
            batch.Draw(ScrollDown.HitTest(mousepos) ? s.ScrollBarArrowDownHover1 : s.ScrollBarArrowDown, ScrollDown, Color.White);
        }

        public override void Draw(SpriteBatch batch)
        {
            if (ExpandedEntries.Count > MaxVisibleEntries)
                DrawScrollBar(batch);

            if (AutoManageItems)
                DrawChildElements(batch);

            // @todo Why the hell do we need to know the exact type of item??
            if (DraggedEntry?.item is QueueItem queueItem)
            {
                Vector2 bCursor = Mouse.GetState().Pos() + DraggedOffset;
                var tCursor = new Vector2(bCursor.X + 40f, bCursor.Y);
                var r = new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30);
                var pbRect = new Rectangle((int)tCursor.X, (int)tCursor.Y + Fonts.Arial12Bold.LineSpacing, 150, 18);
                var pb = new ProgressBar(pbRect, queueItem.Cost, queueItem.ProductionSpent);

                if (queueItem.isBuilding)
                {
                    batch.Draw(ResourceManager.Texture($"Buildings/icon_{queueItem.Building.Icon}_48x48"), r, Color.White);
                    batch.DrawString(Fonts.Arial12Bold, queueItem.Building.Name, tCursor, Color.White);
                    pb.Draw(batch);
                }
                else if (queueItem.isShip)
                {
                    batch.Draw(queueItem.sData.Icon, r, Color.White);
                    batch.DrawString(Fonts.Arial12Bold, queueItem.sData.Name, tCursor, Color.White);
                    pb.Draw(batch);
                }
                else if (queueItem.isTroop)
                {
                    Troop template = ResourceManager.GetTroopTemplate(queueItem.TroopType);
                    template.Draw(batch, r);
                    batch.DrawString(Fonts.Arial12Bold, queueItem.TroopType, tCursor, Color.White);
                    pb.Draw(batch);
                }
            }
        }

        public void DrawDraggedEntry(SpriteBatch batch)
        {
            if (DraggedEntry != null)
                batch.FillRectangle(DraggedEntry.Rect, new Color(0, 0, 0, 150));
        }

        float PercentViewed   => MaxVisibleEntries / (float)ExpandedEntries.Count;
        float StartingPercent => FirstVisibleIndex / (float)ExpandedEntries.Count;

        void UpdateScrollBar()
        {
            StyleTextures s = GetStyle();
            int scrollStart = (int)(StartingPercent * ScrollBarHousing.Height);
            int scrollEnd = (int)(ScrollBarHousing.Height * PercentViewed);
            int width = s.ScrollBarMid.Width;

            ScrollBar = new Rectangle(ScrollBarHousing.X, ScrollBarHousing.Y + scrollStart, width, scrollEnd);
        }

        bool HandleScrollUpDownButtons(InputState input)
        {
            if (!input.InGameSelect)
                return false;
            if (ScrollUp.HitTest(input.CursorPosition))
            {
                if (FirstVisibleIndex > 0)
                    --FirstVisibleIndex;
                UpdateScrollBar();
                return true;
            }
            if (ScrollDown.HitTest(input.CursorPosition))
            {
                if (FirstVisibleIndex + MaxVisibleEntries < ExpandedEntries.Count)
                    ++FirstVisibleIndex;
                UpdateScrollBar();
                return true;
            }
            return false;
        }

        bool HandleScrollDragInput(InputState input)
        {
            ScrollBarHover = 0;
            if (!ScrollBarHousing.HitTest(input.CursorPosition))
                return false;

            if (!ScrollBar.HitTest(input.CursorPosition))
                return false;

            ScrollBarHover = 1;
            if (!input.LeftMouseClick)
                return false;

            ScrollBarHover = 2;
            StartDragPos = (int)input.CursorPosition.Y;
            ScrollBarStartDragPos = ScrollBar.Y;
            DraggingScrollBar = true;
            return true;
        }

        bool HandleScrollBarDragging(InputState input)
        {
            if (!input.LeftMouseDown || !DraggingScrollBar)
                return false;

            float difference = input.CursorPosition.Y - StartDragPos;
            if (Math.Abs(difference) > 0f)
            {
                ScrollBar.Y = (int)(ScrollBarStartDragPos + difference);
                if (ScrollBar.Y < ScrollBarHousing.Y)
                {
                    ScrollBar.Y = ScrollBarHousing.Y;
                }
                else if (ScrollBar.Y + ScrollBar.Height > ScrollBarHousing.Y + ScrollBarHousing.Height)
                {
                    ScrollBar.Y = ScrollBarHousing.Y + ScrollBarHousing.Height - ScrollBar.Height;
                }
            }

            float mousePosAsPct = (input.CursorPosition.Y - ScrollBarHousing.Y) / ScrollBarHousing.Height;
            mousePosAsPct = mousePosAsPct.Clamped(0f, 1f);
            FirstVisibleIndex = (int)(ExpandedEntries.Count * mousePosAsPct);
            if (FirstVisibleIndex + MaxVisibleEntries >= ExpandedEntries.Count)
            {
                FirstVisibleIndex = ExpandedEntries.Count - MaxVisibleEntries;
            }
            return true;
        }

        bool HandleScrollBar(InputState input)
        {
            bool hit = HandleScrollUpDownButtons(input);
            hit |= HandleScrollDragInput(input);
            hit |= HandleScrollBarDragging(input);

            if (DraggingScrollBar && input.LeftMouseUp)
                DraggingScrollBar = false;
            return hit;
        }

        bool HandleMouseScrollUpDown(InputState input)
        {
            if (!ParentMenu.HitTest(input.CursorPosition))
                return false;
            if (input.ScrollIn)
            {
                if (FirstVisibleIndex > 0)
                    --FirstVisibleIndex;
                UpdateScrollBar();
                return true;
            }
            if (input.ScrollOut)
            {
                if (FirstVisibleIndex + MaxVisibleEntries < ExpandedEntries.Count)
                    ++FirstVisibleIndex;
                UpdateScrollBar();
                return true;
            }
            return false;
        }

        void HandleDraggable(InputState input)
        {
            if (IsDraggable && DraggedEntry == null)
            {
                if (input.LeftMouseUp)
                {
                    ClickTimer = 0f;
                }
                else
                {
                    ClickTimer += 0.0166666675f;
                    if (ClickTimer > TimerDelay)
                    {
                        for (int i = FirstVisibleIndex; i < ExpandedEntries.Count && i < FirstVisibleIndex + MaxVisibleEntries; i++)
                        {
                            Entry e = ExpandedEntries[i];
                            if (!e.CheckHover(input))
                                continue;
                            DraggedEntry = e;
                            DraggedOffset = e.TopLeft - input.CursorPosition;
                            break;
                        }
                    }
                }
            }
            if (input.LeftMouseUp)
            {
                ClickTimer = 0f;
                DraggedEntry = null;
            }
        }

        void HandleElementDragging(InputState input, Func<int, int, bool> onDragElement)
        {
            if (DraggedEntry == null || !input.LeftMouseDown)
                return;

            int dragged = Entries.FirstIndexOf(e => e.Rect == DraggedEntry.Rect);

            for (int i = FirstVisibleIndex; i < Entries.Count && i < FirstVisibleIndex + MaxVisibleEntries; i++)
            {
                if (Entries[i].CheckHover(input) && dragged != -1 && onDragElement(i, dragged))
                    break;
            }
        }

        public override bool HandleInput(InputState input)
        {
            bool hit = HandleScrollBar(input);
            hit |= HandleMouseScrollUpDown(input);
            HandleDraggable(input);

            HandleElementDragging(input, (newIndex, dragged) =>
            {
                if (newIndex < dragged)
                {
                    Entry toReplace = Entries[newIndex];
                    Entries[newIndex] = Entries[dragged];
                    Entries[newIndex].Rect = toReplace.Rect;
                    Entries[dragged] = toReplace;
                    Entries[dragged].Rect = DraggedEntry.Rect;
                    DraggedEntry = Entries[newIndex];
                    return true;
                }
                return false;
            });
            UpdateListElements();
            if (!hit && AutoManageItems)
                return HandleInputChildElements(input);
            return hit;
        }

        public bool HandleInput(InputState input, Planet p)
        {
            bool hit = HandleScrollBar(input);
            hit |= HandleMouseScrollUpDown(input);
            HandleDraggable(input);

            HandleElementDragging(input, (newIndex, dragged) =>
            {
                if (newIndex < dragged)
                {
                    Entry toReplace = Entries[newIndex];
                    Entries[newIndex] = Entries[dragged];
                    Entries[newIndex].Rect = toReplace.Rect;
                    p.ConstructionQueue[newIndex] = p.ConstructionQueue[dragged];
                    Entries[dragged] = toReplace;
                    Entries[dragged].Rect = DraggedEntry.Rect;
                    p.ConstructionQueue[dragged] = toReplace.item as QueueItem;
                    DraggedEntry = Entries[newIndex];
                    return true;
                }
                if (newIndex > dragged)
                {
                    Entry toRemove = Entries[dragged];
                    for (int j = dragged + 1; j <= newIndex; j++)
                    {
                        Entries[j].Rect = Entries[j - 1].Rect;
                        Entries[j - 1] = Entries[j];
                        p.ConstructionQueue[j - 1] = p.ConstructionQueue[j];
                    }
                    toRemove.Rect = Entries[newIndex].Rect;
                    Entries[newIndex] = toRemove;
                    p.ConstructionQueue[newIndex] = toRemove.item as QueueItem;
                    DraggedEntry = Entries[newIndex];
                }
                return false;
            });
            UpdateListElements();
            if (!hit && AutoManageItems)
                return HandleInputChildElements(input);
            return hit;
        }

        public void TransitionUpdate(Rectangle r)
        {
            StyleTextures s = GetStyle();
            ScrollUp = new Rectangle(r.X + r.Width - 20, r.Y + 30, s.ScrollBarArrowUp.Width, s.ScrollBarArrowUp.Height);
            ScrollDown = new Rectangle(r.X + r.Width - 20, r.Y + r.Height - 14, s.ScrollBarArrowDown.Width, s.ScrollBarArrowDown.Height);
            ScrollBarHousing = new Rectangle(ScrollUp.X + 1, ScrollUp.Y + ScrollUp.Height + 3, s.ScrollBarMid.Width, ScrollDown.Y - ScrollUp.Y - ScrollUp.Height - 6);
            int j = 0;
            int end = Math.Min(Entries.Count, FirstVisibleIndex + MaxVisibleEntries);
            for (int i = 0; i < Entries.Count; i++)
            {
                Entry e = Entries[i];
                if (i >= FirstVisibleIndex && i < end)
                    e.UpdateClickRect(r.Pos(), j++);
                else
                    e.SetUnclickable();
            }
        }

        public void UpdateListElements()
        {
            ExpandedEntries.Clear();
            foreach (Entry e in Entries)
                e.GetFlattenedVisibleExpandedEntries(ExpandedEntries);

            FirstVisibleIndex = FirstVisibleIndex.Clamped(0,
                Math.Max(0, ExpandedEntries.Count - MaxVisibleEntries));

            int j = 0;
            for (int i = 0; i < ExpandedEntries.Count; i++)
            {
                Entry e = ExpandedEntries[i];
                if (i >= FirstVisibleIndex)
                    e.UpdateClickRect(ParentMenu.Rect.Pos(), j++);
                else
                    e.SetUnclickable();
            }

            UpdateScrollBar();
        }

        
        bool HandleInputChildElements(InputState input)
        {
            for (int i = FirstVisibleIndex; i < ExpandedEntries.Count && i < FirstVisibleIndex + MaxVisibleEntries; i++)
            {
                if (ExpandedEntries[i].TryGet(out UIElementV2 element))
                    if (element.HandleInput(input))
                        return true;
            }
            return false;
        }

        
        void DrawChildElements(SpriteBatch batch)
        {
            for (int i = FirstVisibleIndex; i < ExpandedEntries.Count && i < FirstVisibleIndex + MaxVisibleEntries; i++)
            {
                if (ExpandedEntries[i].TryGet(out UIElementV2 element))
                    element.Draw(batch);
            }
        }


        public class Entry
        {
            // entries with subitems can be expanded or collapsed via category title
            public bool Expanded { get; private set; }

            // true if item is currently being hovered over with mouse cursor
            public bool Hovered { get; private set; }

            // true if item is visible, false if it isn't visible and doesn't participate in layout
            public bool Visible = true;

            public Rectangle Rect;
            public object item;

            // Plus and Edit buttons in ColonyScreen build list
            readonly bool Plus;
            readonly bool Edit;
            bool PlusHover;
            bool EditHover;
            Rectangle PlusRect;
            Rectangle EditRect;

            readonly ScrollList List;

            Rectangle Up;
            Rectangle Down;
            Rectangle Cancel;
            Rectangle Apply;

            readonly Array<Entry> SubEntries = new Array<Entry>();

            public override string ToString() => $"Y:{Rect.Y} | {item}";

            public Entry(ScrollList list, object item, bool plus, bool edit)
            {
                List = list;
                this.item = item;
                Plus = plus;
                Edit = edit;
            }

            public T Get<T>() => (T)item;
            public bool Is<T>() => item is T;
            public T As<T>() where T : class => item as T;
            public bool TryGet<T>(out T outItem)
            {
                if (item is T theItem)
                { outItem = theItem; return true; }
                outItem = default(T);
                return false;
            }

            public Selector CreateSelector() => new Selector(Rect);
            public Vector2 TopLeft => new Vector2(Rect.X, Rect.Y);
            public int X => Rect.X;
            public int Y => Rect.Y;
            public int W => Rect.Width;
            public int H => Rect.Height;
            public int Right => Rect.X + Rect.Width;
            public int CenterX => Rect.X + Rect.Width / 2;
            public int CenterY => Rect.Y + Rect.Height / 2;

            public void AddSubItem(object o, bool addAndEdit = false)
            {
                SubEntries.Add(new Entry(List, o, addAndEdit, addAndEdit));
            }

            public bool RemoveSub(Entry e)
            {
                if (SubEntries.IsEmpty)
                    return false;

                foreach (Entry sub in SubEntries)
                    if (sub.RemoveSub(e)) return true;

                return SubEntries.Remove(e);
            }

            public bool RemoveFirstSubIf(Predicate<Entry> predicate)
            {
                if (SubEntries.IsEmpty)
                    return false;

                foreach (Entry sub in SubEntries)
                    if (sub.RemoveFirstSubIf(predicate)) return true;

                return SubEntries.RemoveFirst(predicate);
            }

            public void GetFlattenedVisibleExpandedEntries(Array<Entry> entries)
            {
                if (Visible)
                {
                    entries.Add(this);
                    if (Expanded)
                        foreach (Entry sub in SubEntries)
                            sub.GetFlattenedVisibleExpandedEntries(entries);
                }
            }

            public void Expand(bool expanded)
            {
                if (Expanded == expanded)
                    return;
                Expanded = expanded;
                if (!expanded)
                {
                    List.FirstVisibleIndex -= SubEntries.Count;
                    if (List.FirstVisibleIndex < 0)
                        List.FirstVisibleIndex = 0;
                }
                List.UpdateListElements();
            }

            public bool WasClicked(InputState input)
            {
                return input.LeftMouseClick && CheckHover(input);
            }

            public void SetUnclickable()
            {
                Rect = new Rectangle(-500, -500, 0, 0);
            }

            public bool WasPlusHovered(InputState input)   => PlusRect.HitTest(input.CursorPosition);
            public bool WasUpHovered(InputState input)     => Up.HitTest(input.CursorPosition);
            public bool WasDownHovered(InputState input)   => Down.HitTest(input.CursorPosition);
            public bool WasCancelHovered(InputState input) => Cancel.HitTest(input.CursorPosition);
            public bool WasApplyHovered(InputState input)  => Apply.HitTest(input.CursorPosition);

            public bool CheckHover(InputState input) => CheckHover(input.CursorPosition);
            public bool CheckHover(Vector2 mousePos)
            {
                bool wasHovered = Hovered;
                Hovered = Rect.HitTest(mousePos);

                if (!wasHovered && Hovered)
                    GameAudio.ButtonMouseOver();

                return Hovered;
            }

            public bool CheckHoverNoSound(Vector2 mousePos)
            {
                Hovered = Rect.HitTest(mousePos);
                return Hovered;
            }

            public bool CheckPlus(InputState input) => (PlusHover = PlusRect.HitTest(input.CursorPosition));
            public bool CheckEdit(InputState input) => (EditHover = EditRect.HitTest(input.CursorPosition));

            public void DrawPlusEdit(SpriteBatch spriteBatch)
            {
                DrawPlus(spriteBatch);
                DrawEdit(spriteBatch);
            }

            public void DrawPlus(SpriteBatch spriteBatch)
            {
                if (Plus)
                {
                    StyleTextures s = List.GetStyle();
                    SubTexture plus = PlusHover ? s.BuildAddHover2
                                      : Hovered ? s.BuildAddHover1
                                                : s.BuildAdd;
                    spriteBatch.Draw(plus, PlusRect, Color.White);
                }
            }

            public void DrawEdit(SpriteBatch spriteBatch)
            {
                if (Edit)
                {
                    StyleTextures s = List.GetStyle();
                    SubTexture edit = EditHover ? s.BuildEditHover2
                                      : Hovered ? s.BuildEditHover1
                                                : s.BuildEdit;
                    spriteBatch.Draw(edit, EditRect, Color.White);
                }
            }

            public void DrawUpDownApplyCancel(SpriteBatch batch, InputState input)
            {
                StyleTextures s = List.GetStyle();
                if (!Hovered)
                {
                    batch.Draw(s.QueueArrowUp, Up, Color.White);
                    batch.Draw(s.QueueArrowDown, Down, Color.White);
                    batch.Draw(s.QueueRush, Apply, Color.White);
                    batch.Draw(s.QueueDelete, Cancel, Color.White);
                    return;
                }

                batch.Draw(s.QueueArrowUpHover1, Up, Color.White);
                batch.Draw(s.QueueArrowDownHover1, Down, Color.White);
                batch.Draw(s.QueueRushHover1, Apply, Color.White);
                batch.Draw(s.QueueDeleteHover1, Cancel, Color.White);

                Vector2 pos = input.CursorPosition;
                if (Up.HitTest(pos))
                {
                    batch.Draw(s.QueueArrowUpHover2, Up, Color.White);
                }
                if (Down.HitTest(pos))
                {
                    batch.Draw(s.QueueArrowDownHover2, Down, Color.White);
                }
                if (Apply.HitTest(pos))
                {
                    batch.Draw(s.QueueRushHover2, Apply, Color.White);
                    ToolTip.CreateTooltip(50);
                }
                if (Cancel.HitTest(pos))
                {
                    batch.Draw(s.QueueDeleteHover2, Cancel, Color.White);
                    ToolTip.CreateTooltip(53);
                }
            }

            public void DrawCancel(SpriteBatch batch, InputState input, string toolTipText = null)
            {
                StyleTextures s = List.GetStyle();
                if (Hovered)
                {
                    batch.Draw(s.QueueDeleteHover1, Cancel, Color.White);
                    if (WasCancelHovered(input))
                    {
                        batch.Draw(s.QueueDeleteHover2, Cancel, Color.White);
                        if (toolTipText.NotEmpty())
                            ToolTip.CreateTooltip(toolTipText);
                        else
                            ToolTip.CreateTooltip(78);
                    }
                }
                else
                {
                    batch.Draw(s.QueueDelete, Cancel, Color.White);
                }
            }

            public void UpdateClickRect(Point topLeft, int j)
            {
                int height = List.EntryHeight;
                var r = new Rectangle(topLeft.X + 20, topLeft.Y + 35 + j * height, List.ParentMenu.Rect.Width - 40, height);
                int right = r.X + r.Width;
                int iconY = r.Y + 15;
                Rect = r;
                if (List.AutoManageItems && item is UIElementV2 element)
                {
                    element.Rect = r;
                    element.PerformLayout();
                }

                StyleTextures s = List.GetStyle();
                SubTexture addIcon = s.BuildAdd;
                SubTexture upIcon  = s.QueueArrowUp;

                if (Plus) PlusRect  = new Rectangle(right - 30, iconY - addIcon.Height / 2, addIcon.Width, addIcon.Height);
                if (Edit) EditRect = new Rectangle(right - 60, iconY - addIcon.Height / 2, addIcon.Width, addIcon.Height);

                int offset = 0;
                bool all = List.Controls == ListControls.All;
                if (all) Up    = new Rectangle(right - (offset += 30), iconY - upIcon.Height / 2, upIcon.Width, upIcon.Height);
                if (all) Down  = new Rectangle(right - (offset += 30), iconY - upIcon.Height / 2, upIcon.Width, upIcon.Height);
                if (all) Apply = new Rectangle(right - (offset += 30), iconY - upIcon.Height / 2, upIcon.Width, upIcon.Height);
                        Cancel = new Rectangle(right - (offset +  30), iconY - upIcon.Height / 2, upIcon.Width, upIcon.Height);
            }
        }
    }

    internal sealed class ScrollListDebugView
    {
        readonly ScrollList List;

        // ReSharper disable once UnusedMember.Global
        public ScrollListDebugView(ScrollList list)
        {
            List = list;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public ScrollList.Entry[] Items
        {
            get
            {
                IReadOnlyList<ScrollList.Entry> allEntries = List.AllEntries;
                var items = new ScrollList.Entry[allEntries.Count];
                for (int i = 0; i < items.Length; ++i)
                    items[i] = allEntries[i];
                return items;
            }
        }
    }
}