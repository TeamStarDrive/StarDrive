using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Ship_Game
{
    public enum ListControls
    {
        All,    // show all list controls
        Cancel, // only show Cancel control
    }

    public class ScrollList
    {
        private readonly Submenu Parent;
        public Rectangle ScrollUp;
        public Rectangle ScrollDown;
        public Rectangle ScrollBarHousing;
        public Rectangle ScrollBar;

        private readonly int EntryHeight = 40;
        private int ScrollBarHover;
        private int StartDragPos;
        private bool DraggingScrollBar;
        private float ScrollBarStartDragPos;
        private float ClickTimer;
        private float TimerDelay = 0.05f;

        public int entriesToDisplay;
        public int indexAtTop;
        private readonly Array<Entry> Entries = new Array<Entry>();
        private readonly Array<Entry> ExpandedEntries = new Array<Entry>(); // Flattened entries
        public bool IsDraggable;
        public Entry DraggedEntry;
        private Vector2 DraggedOffset;

        // Added by EVWeb to not waste space when a list won't use certain buttons
        private readonly ListControls Controls = ListControls.All;
        private readonly Texture2D ArrowUpIcon  = ResourceManager.Texture("NewUI/icon_queue_arrow_up");
        private readonly Texture2D BuildAddIcon = ResourceManager.Texture("NewUI/icon_build_add");

        private readonly Texture2D ScrollBarArrowUp   = ResourceManager.Texture("NewUI/scrollbar_arrow_up");
        private readonly Texture2D ScrollBarArrorDown = ResourceManager.Texture("NewUI/scrollbar_arrow_down");
        private readonly Texture2D ScrollBarMidMarker = ResourceManager.Texture("NewUI/scrollbar_bar_mid");
        

        public ScrollList(Submenu p)
        {
            Parent = p;
            entriesToDisplay = (p.Menu.Height - 25) / 40;            
            InitializeRects(p, 30);
        }

        public ScrollList(Submenu p, int eHeight, ListControls controls = ListControls.All)
        {
            Parent = p;
            EntryHeight = eHeight;
            Controls = controls;
            entriesToDisplay = (p.Menu.Height - 25) / EntryHeight;
            InitializeRects(p, 30);
        }

        public ScrollList(Submenu p, int eHeight, bool realRect)
        {
            Parent = p;
            EntryHeight = eHeight;
            entriesToDisplay = p.Menu.Height / EntryHeight;
            InitializeRects(p, 5);
        }

        private void InitializeRects(Submenu p, int yOffset)
        {
            int x = p.Menu.X + p.Menu.Width - 20;
            ScrollUp   = new Rectangle(x, p.Menu.Y + yOffset,            ScrollBarArrowUp.Width, ScrollBarArrowUp.Height);
            ScrollDown = new Rectangle(x, p.Menu.Y + p.Menu.Height - 14, ScrollBarArrorDown.Width, ScrollBarArrorDown.Height);
            ScrollBarHousing = new Rectangle(ScrollUp.X + 1, ScrollUp.Y + ScrollUp.Height + 3, ScrollBarMidMarker.Width, ScrollDown.Y - ScrollUp.Y - ScrollUp.Height - 6);
            ScrollBar        = new Rectangle(ScrollBarHousing.X, ScrollBarHousing.Y, ScrollBarMidMarker.Width, 0);
        }

        public Entry AddItem(object o)
        {
            var e = new Entry(this, o, false, false);
            Entries.Add(e);
            Update();
            return e;
        }

        public void AddItem(object o, bool plus, bool edit)
        {
            Entries.Add(new Entry(this, o, plus, edit));
            Update();
        }

        public void AddQItem(object o)
        {
            Entries.Add(new Entry(this, o, false, false)
            {
                QItem = 1
            });
            Update();
        }

        public void Remove(Entry e)
        {
            foreach (Entry entry in Entries)
                if (entry.SubEntries.Count > 0 && entry.SubEntries.Remove(e))
                    break;

            Entries.Remove(e);
            bool changed = ExpandedEntries.Remove(e);
            if (changed) Update();
        }

        public void RemoveItem(object o)
        {
            bool ItemPredicate(Entry e) => e.item == o;

            Entries.RemoveFirstIf(ItemPredicate);
            bool changed = ExpandedEntries.RemoveFirstIf(ItemPredicate);
            if (changed) Update();
        }

        public void RemoveFirstIf<T>(Func<T, bool> predicate) where T : class
        {
            bool ItemPredicate(Entry e) => e.item is T item && predicate(item);

            foreach (Entry entry in Entries)
                if (entry.SubEntries.Count > 0)
                    entry.SubEntries.RemoveFirstIf(ItemPredicate);

            Entries.RemoveFirstIf(ItemPredicate);
            bool changed = ExpandedEntries.RemoveFirstIf(ItemPredicate);
            if (changed) Update();
        }

        public void RemoveFirst()
        {
            if (Entries.Count > 0)
            {
                Entries.RemoveAt(0);
                ExpandedEntries.RemoveAt(0);
                Update();
            }
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
            return (T)ExpandedEntries[indexAtTop].item;
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
            Update();
        }

        public void SortDescending<T, TValue>(Func<T, TValue> predicate) where T : class
        {
            Entry[] sorted = Entries.OrderByDescending(e => predicate(e.item as T)).ToArray();
            Entries.Clear();
            Entries.AddRange(sorted);
            Update();
        }

        public IReadOnlyList<Entry> AllEntries => Entries;
        public IReadOnlyList<Entry> AllExpandedEntries => ExpandedEntries;
        public int NumEntries => Entries.Count;
        public int NumExpandedEntries => ExpandedEntries.Count;

        public IEnumerable<Entry> VisibleEntries
        {
            get
            {
                for (int i = indexAtTop; i < Entries.Count && i < indexAtTop + entriesToDisplay; ++i)
                    yield return Entries[i];
            }
        }

        public IEnumerable<Entry> VisibleExpandedEntries
        {
            get
            {
                for (int i = indexAtTop; i < ExpandedEntries.Count && i < indexAtTop + entriesToDisplay; ++i)
                    yield return ExpandedEntries[i];
            }
        }

        public IEnumerable<T> VisibleItems<T>() where T : class
        {
            for (int i = indexAtTop; i < Entries.Count && i < indexAtTop + entriesToDisplay; ++i)
                if (Entries[i].item is T item)
                    yield return item;
        }

        public IEnumerable<T> VisibleExpandedItems<T>() where T : class
        {
            for (int i = indexAtTop; i < ExpandedEntries.Count && i < indexAtTop + entriesToDisplay; ++i)
                if (ExpandedEntries[i].item is T item)
                    yield return item;
        }

        public IEnumerable<T> AllItems<T>() where T : class
        {
            for (int i = 0; i < Entries.Count; ++i)
                if (Entries[i].item is T item)
                    yield return item;
        }

        public IEnumerable<T> AllExpandedItems<T>() where T : class
        {
            for (int i = 0; i < ExpandedEntries.Count; ++i)
                if (ExpandedEntries[i].item is T item)
                    yield return item;
        }

        public void Reset()
        {
            Entries.Clear();
            ExpandedEntries.Clear();
            indexAtTop = 0;
        }

        private void DrawScrollBar(SpriteBatch spriteBatch)
        {
            int updownsize = (ScrollBar.Height - ScrollBarMidMarker.Height) / 2;
            var up  = new Rectangle(ScrollBar.X, ScrollBar.Y, ScrollBar.Width, updownsize);
            var mid = new Rectangle(ScrollBar.X, ScrollBar.Y + updownsize, ScrollBar.Width, ScrollBarMidMarker.Height);
            var bot = new Rectangle(ScrollBar.X, mid.Y + mid.Height, ScrollBar.Width, updownsize);
            if (ScrollBarHover == 0)
            {
                spriteBatch.Draw(ResourceManager.Texture("NewUI/scrollbar_bar_updown"), up, Color.White);
                spriteBatch.Draw(ScrollBarMidMarker, mid, Color.White);
                spriteBatch.Draw(ResourceManager.Texture("NewUI/scrollbar_bar_updown"), bot, Color.White);
            }
            else if (ScrollBarHover == 1)
            {
                spriteBatch.Draw(ResourceManager.Texture("NewUI/scrollbar_bar_updown_hover1"), up, Color.White);
                spriteBatch.Draw(ResourceManager.Texture("NewUI/scrollbar_bar_mid_hover1"), mid, Color.White);
                spriteBatch.Draw(ResourceManager.Texture("NewUI/scrollbar_bar_updown_hover1"), bot, Color.White);
            }
            else if (ScrollBarHover == 2)
            {
                spriteBatch.Draw(ResourceManager.Texture("NewUI/scrollbar_bar_updown_hover2"), up, Color.White);
                spriteBatch.Draw(ResourceManager.Texture("NewUI/scrollbar_bar_mid_hover2"), mid, Color.White);
                spriteBatch.Draw(ResourceManager.Texture("NewUI/scrollbar_bar_updown_hover2"), bot, Color.White);
            }
            Vector2 mousepos = Mouse.GetState().Pos();
            spriteBatch.Draw(ScrollUp.HitTest(mousepos) ? ResourceManager.Texture("NewUI/scrollbar_arrow_up_hover1") : ScrollBarArrowUp, ScrollUp, Color.White);
            spriteBatch.Draw(ScrollDown.HitTest(mousepos) ? ResourceManager.Texture("NewUI/scrollbar_arrow_down_hover1") : ScrollBarArrorDown, ScrollDown, Color.White);
        }

        private void DrawScrollBarBlue(SpriteBatch spriteBatch)
        {
            int updownsize = (ScrollBar.Height - ResourceManager.Texture("ResearchMenu/scrollbar_bar_mid").Height) / 2;
            var up  = new Rectangle(ScrollBar.X, ScrollBar.Y, ScrollBar.Width, updownsize);
            var mid = new Rectangle(ScrollBar.X, ScrollBar.Y + updownsize, ScrollBar.Width, ResourceManager.Texture("ResearchMenu/scrollbar_bar_mid").Height);
            var bot = new Rectangle(ScrollBar.X, mid.Y + mid.Height, ScrollBar.Width, updownsize);
            if (ScrollBarHover == 0)
            {
                spriteBatch.Draw(ResourceManager.Texture("ResearchMenu/scrollbar_bar_updown"), up, Color.White);
                spriteBatch.Draw(ResourceManager.Texture("ResearchMenu/scrollbar_bar_mid"), mid, Color.White);
                spriteBatch.Draw(ResourceManager.Texture("ResearchMenu/scrollbar_bar_updown"), bot, Color.White);
            }
            else if (ScrollBarHover == 1)
            {
                spriteBatch.Draw(ResourceManager.Texture("ResearchMenu/scrollbar_bar_updown_hover1"), up, Color.White);
                spriteBatch.Draw(ResourceManager.Texture("ResearchMenu/scrollbar_bar_mid_hover1"), mid, Color.White);
                spriteBatch.Draw(ResourceManager.Texture("ResearchMenu/scrollbar_bar_updown_hover1"), bot, Color.White);
            }
            else if (ScrollBarHover == 2)
            {
                spriteBatch.Draw(ResourceManager.Texture("ResearchMenu/scrollbar_bar_updown_hover2"), up, Color.White);
                spriteBatch.Draw(ResourceManager.Texture("ResearchMenu/scrollbar_bar_mid_hover2"), mid, Color.White);
                spriteBatch.Draw(ResourceManager.Texture("ResearchMenu/scrollbar_bar_updown_hover2"), bot, Color.White);
            }
            spriteBatch.Draw(ResourceManager.Texture("ResearchMenu/scrollbar_arrow_up"), ScrollUp, Color.White);
            spriteBatch.Draw(ResourceManager.Texture("ResearchMenu/scrollbar_arrow_down"), ScrollDown, Color.White);
        }

        public virtual void Draw(SpriteBatch spriteBatch)
        {
            if (ExpandedEntries.Count > entriesToDisplay)
                DrawScrollBar(spriteBatch);

            // @todo Why the hell do we need to know the exact type of item??
            if (DraggedEntry?.item is QueueItem queueItem)
            {
                Vector2 bCursor = Mouse.GetState().Pos() + DraggedOffset;
                var tCursor = new Vector2(bCursor.X + 40f, bCursor.Y);
                var r = new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30);
                var pbRect = new Rectangle((int)tCursor.X, (int)tCursor.Y + Fonts.Arial12Bold.LineSpacing, 150, 18);
                var pb = new ProgressBar(pbRect)
                {
                    Max = queueItem.Cost,
                    Progress = queueItem.productionTowards
                };

                if (queueItem.isBuilding)
                {
                    spriteBatch.Draw(ResourceManager.Texture($"Buildings/icon_{queueItem.Building.Icon}_48x48"), r, Color.White);
                    spriteBatch.DrawString(Fonts.Arial12Bold, queueItem.Building.Name, tCursor, Color.White);
                    pb.Draw(spriteBatch);
                }
                else if (queueItem.isShip)
                {
                    spriteBatch.Draw(ResourceManager.HullsDict[queueItem.sData.Hull].Icon, r, Color.White);
                    spriteBatch.DrawString(Fonts.Arial12Bold, queueItem.sData.Name, tCursor, Color.White);
                    pb.Draw(spriteBatch);
                }
                else if (queueItem.isTroop)
                {
                    Troop template = ResourceManager.GetTroopTemplate(queueItem.troopType);
                    template.Draw(spriteBatch, r);
                    spriteBatch.DrawString(Fonts.Arial12Bold, queueItem.troopType, tCursor, Color.White);
                    pb.Draw(spriteBatch);
                }
            }
        }

        private float PercentViewed => entriesToDisplay / (float)ExpandedEntries.Count;
        private float StartingPercent => indexAtTop / (float)ExpandedEntries.Count;

        private void UpdateScrollBar(bool blue = false)
        {
            int scrollStart = (int)(StartingPercent * ScrollBarHousing.Height);
            int scrollEnd = (int)(ScrollBarHousing.Height * PercentViewed);
            int width = blue ? ResourceManager.Texture("ResearchMenu/scrollbar_bar_mid").Width : ScrollBarMidMarker.Width;

            ScrollBar = new Rectangle(ScrollBarHousing.X, ScrollBarHousing.Y + scrollStart, width, scrollEnd);
        }

        public void DrawBlue(SpriteBatch spriteBatch)
        {
            if (ExpandedEntries.Count > entriesToDisplay)
            {
                UpdateScrollBar(blue: true);
                DrawScrollBarBlue(spriteBatch);
            }
        }

        private bool HandleScrollUpDownButtons(InputState input)
        {
            if (!input.InGameSelect)
                return false;
            if (ScrollUp.HitTest(input.CursorPosition))
            {
                if (indexAtTop > 0)
                    --indexAtTop;
                UpdateScrollBar();
                return true;
            }
            if (ScrollDown.HitTest(input.CursorPosition))
            {
                if (indexAtTop + entriesToDisplay < ExpandedEntries.Count)
                    ++indexAtTop;
                UpdateScrollBar();
                return true;
            }
            return false;
        }

        private bool HandleScrollDragInput(InputState input)
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

        private bool HandleScrollBarDragging(InputState input)
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
            indexAtTop = (int)(ExpandedEntries.Count * mousePosAsPct);
            if (indexAtTop + entriesToDisplay >= ExpandedEntries.Count)
            {
                indexAtTop = ExpandedEntries.Count - entriesToDisplay;
            }
            return true;
        }

        private bool HandleScrollBar(InputState input)
        {
            bool hit = HandleScrollUpDownButtons(input);
            hit |= HandleScrollDragInput(input);
            hit |= HandleScrollBarDragging(input);
            
            if (DraggingScrollBar && input.LeftMouseUp)
                DraggingScrollBar = false;
            return hit;
        }

        private bool HandleParentMenuOpen(InputState input)
        {
            if (!Parent.Menu.HitTest(input.CursorPosition))
                return false;
            if (input.ScrollIn)
            {
                if (indexAtTop > 0)
                    --indexAtTop;
                UpdateScrollBar();
                return true;
            }
            if (input.ScrollOut)
            {
                if (indexAtTop + entriesToDisplay < ExpandedEntries.Count)
                    ++indexAtTop;
                UpdateScrollBar();
                return true;
            }
            return false;
        }

        private void HandleDraggable(InputState input)
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
                        for (int i = indexAtTop; i < ExpandedEntries.Count && i < indexAtTop + entriesToDisplay; i++)
                        {
                            Entry e = ExpandedEntries[i];
                            if (!e.clickRect.HitTest(input.CursorPosition))
                                continue;
                            DraggedEntry = e;
                            DraggedOffset = new Vector2(e.clickRect.X, e.clickRect.Y) - input.CursorPosition;
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

        private void HandleElementDragging(InputState input, Func<int, int, bool> onDragElement)
        {
            if (DraggedEntry == null || !input.LeftMouseDown)
                return;

            int dragged = 0;
            // WTF?
            for (int i = indexAtTop; i < Entries.Count && i < indexAtTop + entriesToDisplay; i++)
            {
                Entry e = Entries[i];
                if (e.clickRect == DraggedEntry.clickRect)
                {
                    dragged = i;
                    break;
                }
            }
            for (int i = indexAtTop; i < Entries.Count && i < indexAtTop + entriesToDisplay; i++)
            {
                Entry e = Entries[i];
                if (!e.clickRect.HitTest(input.CursorPosition))
                    continue;
                if (onDragElement(i, dragged))
                    break;
            }
        }

        public virtual bool HandleInput(InputState input)
        {
            bool hit = HandleScrollBar(input);
            hit |= HandleParentMenuOpen(input);
            HandleDraggable(input);

            HandleElementDragging(input, (newIndex, dragged) =>
            {
                if (newIndex < dragged)
                {
                    Entry toReplace = Entries[newIndex];
                    Entries[newIndex] = Entries[dragged];
                    Entries[newIndex].clickRect = toReplace.clickRect;
                    Entries[dragged] = toReplace;
                    Entries[dragged].clickRect = DraggedEntry.clickRect;
                    DraggedEntry = Entries[newIndex];
                    return true;
                }
                return false;
            });
            Update();
            return hit;
        }

        public bool HandleInput(InputState input, Planet p)
        {
            bool hit = HandleScrollBar(input);
            hit |= HandleParentMenuOpen(input);
            HandleDraggable(input);

            HandleElementDragging(input, (newIndex, dragged) =>
            {
                if (newIndex < dragged)
                {
                    Entry toReplace = Entries[newIndex];
                    Entries[newIndex] = Entries[dragged];
                    Entries[newIndex].clickRect = toReplace.clickRect;
                    p.ConstructionQueue[newIndex] = p.ConstructionQueue[dragged];
                    Entries[dragged] = toReplace;
                    Entries[dragged].clickRect = DraggedEntry.clickRect;
                    p.ConstructionQueue[dragged] = toReplace.item as QueueItem;
                    DraggedEntry = Entries[newIndex];
                    return true;
                }
                if (newIndex > dragged)
                {
                    Entry toRemove = Entries[dragged];
                    for (int j = dragged + 1; j <= newIndex; j++)
                    {
                        Entries[j].clickRect = Entries[j - 1].clickRect;
                        Entries[j - 1] = Entries[j];
                        p.ConstructionQueue[j - 1] = p.ConstructionQueue[j];
                    }
                    toRemove.clickRect = Entries[newIndex].clickRect;
                    Entries[newIndex] = toRemove;
                    p.ConstructionQueue[newIndex] = toRemove.item as QueueItem;
                    DraggedEntry = Entries[newIndex];
                }
                return false;
            });
            Update();
            return hit;
        }

        private void UpdateClickRect(Entry e, Point topLeft, int j)
        {
            var r = new Rectangle(topLeft.X + 20, topLeft.Y + 35 + j * EntryHeight, Parent.Menu.Width - 40, EntryHeight);
            int right = r.X + r.Width;
            int iconY = r.Y + 15;
            e.clickRect = r;
            if (e.Plus) e.addRect  = new Rectangle(right - 30, iconY - BuildAddIcon.Height / 2, BuildAddIcon.Width, BuildAddIcon.Height);
            if (e.Edit) e.editRect = new Rectangle(right - 60, iconY - BuildAddIcon.Height / 2, BuildAddIcon.Width, BuildAddIcon.Height);
            
            int offset = 0;
            bool all = Controls == ListControls.All;
            if (all) e.up     = new Rectangle(right - (offset += 30), iconY - ArrowUpIcon.Height / 2, ArrowUpIcon.Width, ArrowUpIcon.Height);
            if (all) e.down   = new Rectangle(right - (offset += 30), iconY - ArrowUpIcon.Height / 2, ArrowUpIcon.Width, ArrowUpIcon.Height);
            if (all) e.apply  = new Rectangle(right - (offset += 30), iconY - ArrowUpIcon.Height / 2, ArrowUpIcon.Width, ArrowUpIcon.Height);
            e.cancel = new Rectangle(right - (offset += 30), iconY - ArrowUpIcon.Height / 2, ArrowUpIcon.Width, ArrowUpIcon.Height);
        }

        public void TransitionUpdate(Rectangle r)
        {
            ScrollUp = new Rectangle(r.X + r.Width - 20, r.Y + 30, ScrollBarArrowUp.Width, ScrollBarArrowUp.Height);
            ScrollDown = new Rectangle(r.X + r.Width - 20, r.Y + r.Height - 14, ScrollBarArrorDown.Width, ScrollBarArrorDown.Height);
            ScrollBarHousing = new Rectangle(ScrollUp.X + 1, ScrollUp.Y + ScrollUp.Height + 3, ScrollBarMidMarker.Width, ScrollDown.Y - ScrollUp.Y - ScrollUp.Height - 6);
            int j = 0;
            for (int i = 0; i < Entries.Count; i++)
            {
                Entry e = Entries[i];
                if (i < indexAtTop || i > indexAtTop + entriesToDisplay - 1)
                {
                    e.SetUnclickable();
                }
                else
                {
                    UpdateClickRect(e, r.Pos(), j++);
                }
            }
        }

        public void Update()
        {
            ExpandedEntries.Clear();

            foreach (Entry e in Entries)
            {
                ExpandedEntries.Add(e);
                if (!e.Expanded)
                    continue;
                foreach (Entry sub in e.SubEntries)
                {
                    ExpandedEntries.Add(sub);
                    foreach (Entry subsub in sub.SubEntries)
                        ExpandedEntries.Add(subsub);
                }
            }
            
            int j = 0;
            for (int i = 0; i < ExpandedEntries.Count; i++)
            {
                Entry e = ExpandedEntries[i];
                if (i >= indexAtTop)
                {
                    UpdateClickRect(e, Parent.Menu.Pos(), j++);
                }
                else
                {
                    e.SetUnclickable();
                }
            }
            ScrollBar.Height = (int)(ScrollBarHousing.Height * PercentViewed);
            if (indexAtTop < 0)
                indexAtTop = 0;
        }

        public class Entry
        {
            // entries with subitems can be expanded or collapsed via category title
            public bool Expanded { get; private set; }
            public Rectangle clickRect;
            public object item;
            public readonly bool Plus;
            public readonly bool Edit;
            private bool PlusHover;
            private bool EditHover;

            public Rectangle editRect;
            public Rectangle addRect;
            private readonly ScrollList List;

            public bool Hovered { get; private set; }
            public int QItem;

            public Rectangle up;
            public Rectangle down;
            public Rectangle cancel;
            public Rectangle apply;


            // moved this here for consistency
            public Array<Entry> SubEntries = new Array<Entry>();

            public override string ToString() => $"Y:{clickRect.Y} | {item}";

            public Entry(ScrollList list, object item, bool plus, bool edit)
            {
                List = list;
                this.item = item;
                Plus = plus;
                Edit = edit;
            }

            public void AddItem(object o, bool addAndEdit = false)
            {
                SubEntries.Add(new Entry(List, o, addAndEdit, addAndEdit));
            }

            public bool WasClicked(InputState input)
            {
                return input.LeftMouseClick && clickRect.HitTest(input.CursorPosition);
            }

            public void SetUnclickable()
            {
                clickRect = new Rectangle(-500, -500, 0, 0);
            }

            public void Expand(bool expanded)
            {
                if (Expanded == expanded)
                    return;
                Expanded = expanded;
                if (!expanded)
                {
                    List.indexAtTop = List.indexAtTop - SubEntries.Count;
                    if (List.indexAtTop < 0)
                        List.indexAtTop = 0;
                }
                List.Update();
            }

            public bool CheckHover(MouseState mouse) => CheckHover(mouse.Pos());
            public bool CheckHover(InputState input) => CheckHover(input.CursorPosition);
            public bool CheckHover(Vector2 mousePos)
            {
                bool wasHovered = Hovered;
                Hovered = clickRect.HitTest(mousePos);

                if (!wasHovered && Hovered)
                    GameAudio.PlaySfxAsync("sd_ui_mouseover");

                return Hovered;
            } 
            public bool CheckPlus(InputState input) => (PlusHover = addRect.HitTest(input.CursorPosition));
            public bool CheckEdit(InputState input) => (EditHover = editRect.HitTest(input.CursorPosition));

            public void DrawPlusEdit(SpriteBatch spriteBatch)
            {
                DrawPlus(spriteBatch);
                DrawEdit(spriteBatch);
            }

            public void DrawPlus(SpriteBatch spriteBatch)
            {
                if (Plus)
                {
                    string plus = PlusHover ? "NewUI/icon_build_add_hover2"
                           : Hovered ? "NewUI/icon_build_add_hover1"
                                            : "NewUI/icon_build_add";
                    spriteBatch.Draw(ResourceManager.Texture(plus), addRect, Color.White);
                }
            }

            public void DrawEdit(SpriteBatch spriteBatch)
            {
                if (Edit)
                {
                    string edit = EditHover ? "NewUI/icon_build_edit_hover2"
                           : Hovered ? "NewUI/icon_build_edit_hover1"
                                            : "NewUI/icon_build_edit";
                    spriteBatch.Draw(ResourceManager.Texture(edit), editRect, Color.White);
                }
            }
        }
    }
}