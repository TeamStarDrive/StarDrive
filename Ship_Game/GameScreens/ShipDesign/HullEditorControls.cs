using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;
using SDGraphics;
using SDGraphics.Input;
using SDUtils;
using Ship_Game.Audio;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;
using Vector3 = SDGraphics.Vector3;
using Point = SDGraphics.Point;

namespace Ship_Game.GameScreens.ShipDesign
{
    internal class HullEditorControls : UIElementContainer
    {
        readonly ShipDesignScreen S;
        readonly UILabel Title;
        readonly UIList StatLabels;
        readonly UIList EditList;
        readonly UIList ThrusterList;
        readonly FloatSlider MeshOffsetY;
        UIButton AddThrusterBtn;

        Restrictions LastRestriction = Restrictions.IO;
        Point LastEditedPos;

        bool IsPainting;
        bool PaintLeftButton;
        Point LastPaintedCell; // relative to GridCenter, which shifts when the grid grows

        enum SlotOp { Edit, AddDelete }
        SlotOp Op = SlotOp.Edit;

        bool IsEditing => S.HullEditMode;

        int HoveredThrusterIdx = -1;
        bool IsEditingThruster;

        public HullEditorControls(ShipDesignScreen screen, Vector2 pos)
            : base(pos, new Vector2(200, 400))
        {
            S = screen;
            var toggleEdit = Button(ButtonStyle.Low100, "Hull Editor", b => ToggleHullEditMode());
            if (!GlobalStats.Defaults.EnableHullEditor)
                toggleEdit.Tooltip = "Allows you to edit slots in hulls. This is disabled by default since changing hull will " +
                    "invalidate all related AI designs, making the game easier. Enable it in Globals.yaml file if you know what" +
                    "you are doing.";
            toggleEdit.SetLocalPos(0,0);
            Title = LabelRel("EDITING HULL", Fonts.Arial14Bold, 120, 0);

            StatLabels = Add(new UIList(ListLayoutStyle.ResizeList));
            StatLabels.SetLocalPos(0, 20);
            StatLabels.Padding = new Vector2(2, 8);
            AddLabel(StatLabels, () => $"GridPos [{S.GridPosUnderCursor.X},{S.GridPosUnderCursor.Y}] slot: {S.SlotUnderCursor}");
            AddLabel(StatLabels, () => $"MeshOffset {S.CurrentHull?.MeshOffset}");
            AddLabel(StatLabels, () => $"GridCenter {S.CurrentHull?.GridCenter}");

            EditList = Add(new UIList(ListLayoutStyle.ResizeList));
            EditList.SetLocalPos(0, 100);
            EditList.Padding = new Vector2(2, 8);

            MeshOffsetY = EditList.Add(new FloatSlider(SliderStyle.Decimal, new Vector2(200, 30),
                                           "MeshOffset.Y", -200, +200, 0));
            MeshOffsetY.Step = 2f;

            var btnEdit = EditList.Button(ButtonStyle.Medium, "EDIT Slot IO", b =>
            {
                Op = SlotOp.Edit;
                Title.DynamicText = (l) => $"EDITING SLOTS {LastRestriction}";
            });
            btnEdit.DynamicText = () => $"EDIT Slot {LastRestriction}";

            var btnAdd = EditList.Button(ButtonStyle.Medium, "ADD/DEL Slot", b =>
            {
                Op = SlotOp.AddDelete;
                Title.Text = "ADDING/DELETING SLOTS";
            });

            btnEdit.Tooltip = "Left Click on a slot to EDIT Restriction forward, Right Click to EDIT Restriction backward. " +
                              "Hold and drag to paint the current Restriction";
            btnAdd.Tooltip = "Left Click on empty space to ADD a new slot, Right Click on existing slot to DELETE it. " +
                             "Hold and drag to keep adding or deleting";

            ThrusterList = Add(new UIList(ListLayoutStyle.ResizeList));
            ThrusterList.SetLocalPos(0, 300);
            ThrusterList.Padding = new Vector2(2, 8);

            SetHullEditVisibility(IsEditing);
        }

        public void Initialize(ShipHull hull)
        {
            MeshOffsetY.AbsoluteValue = hull.MeshOffset.Y;
            MeshOffsetY.OnChange = (s) =>
            {
                hull.MeshOffset.Y = (float)Math.Round(s.AbsoluteValue);
                S.UpdateHullWorldPos();
            };

            ThrusterList.RemoveAll();
            AddThrusterBtn = ThrusterList.AddButton("Add Thruster", OnAddThrusterClicked);
            AddThrusterBtn.Visible = IsEditing;

            for (int i = 0; i < hull.Thrusters.Length; ++i)
            {
                int tIndex = i;
                AddLabel(ThrusterList, () =>
                {
                    if (tIndex >= S.CurrentHull.Thrusters.Length)
                        return "Thruster Deleted"; // this thruster was deleted, Initialize() will be called again
                    var t = S.CurrentHull.Thrusters[tIndex];
                    return $"Thruster X:{t.Position.X} Y:{t.Position.Y} Z:{t.Position.Z} Scale:{t.Scale}";
                });
            }
        }

        void AddLabel(UIList owner, Func<string> dynamicText)
        {
            var label = owner.Add(new UILabel(Fonts.Arial12Bold));
            label.DynamicText = l => dynamicText();
            label.Color = Color.Yellow;
        }

        void ToggleHullEditMode()
        {
            if (GlobalStats.Defaults.EnableHullEditor)
            {
                S.HullEditMode = !S.HullEditMode;
                SetHullEditVisibility(IsEditing);
                S.ChangeHull(S.CurrentHull, zoomToHull: false);
            }
            else
            {
                GameAudio.NegativeClick();
            }
        }

        void SetHullEditVisibility(bool visible)
        {
            EditList.Visible = visible;
            Title.Visible = visible;
            if (AddThrusterBtn != null)
                AddThrusterBtn.Visible = visible;
        }

        public override bool HandleInput(InputState input)
        {
            if (base.HandleInput(input))
                return true; // make sure button captures are done first

            if (IsEditing)
            {
                if (!IsEditingThruster)
                    HoveredThrusterIdx = GetThrusterIdUnderCursor();

                if (HoveredThrusterIdx != -1 && input.LeftMouseClick)
                {
                    IsEditingThruster = true;
                    GameAudio.DesignSoftBeep();
                }

                if (IsEditingThruster)
                {
                    ModifyThruster(input, HoveredThrusterIdx);
                    return true;
                }

                if (input.LeftMouseClick || input.RightMouseClick)
                {
                    (SlotStruct clicked, Point pos) = S.GetSlotUnderCursor();
                    IsPainting = Op == SlotOp.AddDelete || clicked != null;
                    PaintLeftButton = input.LeftMouseClick;
                    LastPaintedCell = pos.Sub(S.CurrentHull.GridCenter);
                    if (ModifyHull(input, pos))
                    {
                        GameAudio.DesignSoftBeep();
                        return true;
                    }
                    else
                    {
                        GameAudio.NegativeClick();
                    }
                }
                else if (IsPainting && (PaintLeftButton ? input.LeftMouseDown : input.RightMouseDown))
                {
                    (_, Point pos) = S.GetSlotUnderCursor();
                    ShipHull painted = Op == SlotOp.AddDelete
                        ? PaintSlots(S.CurrentHull, ref LastPaintedCell, pos, PaintLeftButton, S.IsSymmetricDesignMode)
                        : PaintRestriction(S.CurrentHull, ref LastPaintedCell, pos, LastRestriction, S.IsSymmetricDesignMode);
                    if (painted != null)
                    {
                        S.ChangeHull(painted, zoomToHull:false);
                        GameAudio.DesignSoftBeep();
                    }
                    return true;
                }
                else
                {
                    IsPainting = false;
                }
            }
            return false;
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            if (IsEditing)
            {
                if (!IsEditingThruster)
                {
                    (SlotStruct slot, Point pos) = S.GetSlotUnderCursor();
                    DrawSlotCursor(slot, pos);
                    if (S.IsSymmetricDesignMode)
                    {
                        Point mirrorPos = S.CurrentHull.MirroredSlotPos(pos);
                        DrawSlotCursor(S.ModuleGrid.Get(mirrorPos), mirrorPos);
                    }
                }

                for (int i = 0; i < S.CurrentHull.Thrusters.Length; ++i)
                {
                    var t = GetThruster(i);
                    Vector2 tPos = t.WorldPos2D;
                    Color tColor = i == HoveredThrusterIdx ? Color.Red : Color.Orange;
                    S.DrawCircleProjected(t.WorldPos2D, t.WorldRadius, Color.Orange, thickness:2);
                    if (i == HoveredThrusterIdx)
                        S.DrawCrossHairProjected(tPos, t.WorldRadius*2, tColor);
                }
            }

            base.Draw(batch, elapsed);
        }

        void DrawSlotCursor(SlotStruct slot, Point pos)
        {
            bool hasSlot = slot == null;
            Color c = hasSlot ? Color.Green : Color.Red;
            if (Op == SlotOp.Edit)
                c = !hasSlot ? Color.Green : Color.Red;

            Vector2 worldPos = S.ModuleGrid.GridPosToWorld(pos);
            S.DrawRectangleProjected(new RectF(worldPos, new Vector2(16)), c);
        }

        ref ShipHull.ThrusterZone GetThruster(int index)
        {
            return ref S.CurrentHull.Thrusters[index];
        }

        int GetThrusterIdUnderCursor()
        {
            Vector2 cursorWorld = S.CursorWorldPosition2D;
            for (int i = 0; i < S.CurrentHull.Thrusters.Length; ++i)
            {
                var t = GetThruster(i);
                if (cursorWorld.InRadius(t.WorldPos2D, t.WorldRadius))
                    return i;
            }
            return -1;
        }

        void OnAddThrusterClicked(UIButton btn)
        {
            ShipHull hull = S.CurrentHull;
            float z = hull.Thrusters.Length != 0 ? hull.Thrusters[hull.Thrusters.Length-1].Position.Z : 0;
            float scale = hull.Thrusters.Length != 0 ? hull.Thrusters[hull.Thrusters.Length-1].Scale : 32;
            hull.AddThruster(new Vector3(0, 0, z), scale);
            S.DesignedShip.InitializeThrusters(hull);
            Initialize(hull);
        }

        void ModifyThruster(InputState input, int thrusterId)
        {
            if (input.LeftMouseReleased || thrusterId == -1)
            {
                IsEditingThruster = false;
            }
            else if (input.KeyPressed(Keys.Delete))
            {
                S.CurrentHull.RemoveThruster(thrusterId);
                S.DesignedShip.InitializeThrusters(S.CurrentHull);

                Initialize(S.CurrentHull);
                IsEditingThruster = false;
            }
            else if (input.LeftMouseDown)
            {
                ref var tz = ref GetThruster(thrusterId);
                Thruster thruster = S.DesignedShip.ThrusterList[thrusterId];

                if (input.IsShiftKeyDown)
                {
                    Vector2 worldSize = S.UnprojectToWorldPosition(input.EndLeftHold)
                                      - S.UnprojectToWorldPosition(input.StartLeftHold);
                    float size = Math.Max(worldSize.Length(), 1.0f);
                    tz.SetWorldScale(size);
                }
                else // set position
                {
                    tz.SetWorldPos2D(S.CursorWorldPosition2D);
                }

                thruster.LocalPos = tz.Position;
                thruster.Scale    = tz.Scale;
                thruster.UpdatePosition(S.DesignedShip.Position, 0, S.DesignedShip.Direction3D);
            }
        }

        internal static ShipHull PaintSlots(ShipHull hull, ref Point lastPaintedCell, Point pos, bool add, bool mirror)
            => PaintLine(hull, ref lastPaintedCell, pos, (h, slots, cell) => AddOrDelete(h, slots, cell, add, mirror));

        internal static ShipHull PaintRestriction(ShipHull hull, ref Point lastPaintedCell, Point pos, Restrictions r, bool mirror)
            => PaintLine(hull, ref lastPaintedCell, pos, (h, slots, cell) => SetRestriction(h, slots, cell, r, mirror));

        static ShipHull PaintLine(ShipHull hull, ref Point lastPaintedCell, Point pos,
                                  Func<ShipHull, Array<HullSlot>, Point, bool> paintCell)
        {
            Point from = lastPaintedCell.Add(hull.GridCenter);
            if (pos == from)
                return null;

            ShipHull newHull = hull.GetClone();
            var slots = new Array<HullSlot>(newHull.HullSlots);
            int steps = Math.Max(Math.Abs(pos.X - from.X), Math.Abs(pos.Y - from.Y));
            bool changed = false;
            for (int i = 1; i <= steps; ++i)
            {
                var cell = new Point(from.X + (pos.X - from.X) * i / steps, from.Y + (pos.Y - from.Y) * i / steps);
                changed |= paintCell(newHull, slots, cell);
            }

            lastPaintedCell = pos.Sub(hull.GridCenter);
            if (!changed)
                return null;

            newHull.SetHullSlots(slots);
            return newHull;
        }

        static bool AddOrDelete(ShipHull hull, Array<HullSlot> slots, Point pos, bool add, bool mirror)
        {
            if (!AddOrDeleteAt(slots, pos, add))
                return false;
            if (mirror)
                AddOrDeleteAt(slots, hull.MirroredSlotPos(pos), add);
            return true;
        }

        static bool AddOrDeleteAt(Array<HullSlot> slots, Point pos, bool add)
        {
            for (int i = 0; i < slots.Count; ++i)
            {
                if (slots[i].Pos == pos)
                {
                    if (add)
                        return false;
                    slots.RemoveAt(i);
                    return true;
                }
            }

            if (!add)
                return false;
            slots.Add(new HullSlot(pos.X, pos.Y, Restrictions.IO));
            return true;
        }

        static bool SetRestriction(ShipHull hull, Array<HullSlot> slots, Point pos, Restrictions r, bool mirror)
        {
            bool changed = SetRestrictionAt(slots, pos, r);
            if (mirror)
                changed |= SetRestrictionAt(slots, hull.MirroredSlotPos(pos), r);
            return changed;
        }

        static bool SetRestrictionAt(Array<HullSlot> slots, Point pos, Restrictions r)
        {
            for (int i = 0; i < slots.Count; ++i)
            {
                if (slots[i].Pos == pos)
                {
                    if (slots[i].R == r)
                        return false;
                    slots[i] = new HullSlot(pos.X, pos.Y, r);
                    return true;
                }
            }
            return false;
        }

        bool ModifyHull(InputState input, Point pos)
        {
            ShipHull newHull = S.CurrentHull.GetClone();
            HullSlot slot = newHull.FindSlot(pos);
            var slots = new Array<HullSlot>(newHull.HullSlots);
            Point mirrorPos = newHull.MirroredSlotPos(pos);
            HullSlot mirrorSlot = S.IsSymmetricDesignMode ? newHull.FindSlot(mirrorPos) : null;

            switch (Op)
            {
                case SlotOp.AddDelete:
                {
                    if (!AddOrDelete(newHull, slots, pos, add: input.LeftMouseClick, S.IsSymmetricDesignMode))
                        return false;
                    newHull.SetHullSlots(slots);
                    break;
                }
                case SlotOp.Edit:
                {
                    if (slot != null)
                    {
                        if (LastEditedPos == pos)
                            LastRestriction = slot.R.IncrementWithWrap(input.LeftMouseClick ? +1 : -1);
                        LastEditedPos = pos;

                        slots.Remove(slot);
                        slots.Add(new HullSlot(slot.Pos.X, slot.Pos.Y, LastRestriction));
                        if (mirrorSlot != null)
                        {
                            slots.Remove(mirrorSlot);
                            slots.Add(new HullSlot(mirrorPos.X, mirrorPos.Y, LastRestriction));
                        }
                        newHull.SetHullSlots(slots);
                    }
                    else
                    {
                        // when Left/Right clicking on an empty pos, change LastRestriction
                        LastRestriction = LastRestriction.IncrementWithWrap(input.LeftMouseClick ? +1 : -1);
                        return true;
                    }
                    break;
                }
            }

            S.ChangeHull(newHull, zoomToHull:false);
            return true;
        }
    }
}
