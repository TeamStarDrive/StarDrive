using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.Audio;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

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

        Restrictions LastRestriction = Restrictions.IO;
        Point LastEditedPos;

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

            btnEdit.Tooltip = "Left Click on a slot to EDIT Restriction forward, Right Click to EDIT Restriction backward";
            btnAdd.Tooltip = "Left Click on empty space to ADD a new slot, Right Click on existing slot to DELETE it";

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
            ThrusterList.AddButton("Add Thruster", (btn) =>
            {
                float scale = hull.Thrusters.Length == 0 ? hull.Thrusters[hull.Thrusters.Length-1].Scale : 32;
                hull.AddThruster(new Vector3(256, 256, 0), scale);
                S.DesignedShip.InitializeThrusters(hull);
                Initialize(hull);
            });

            for (int i = 0; i < hull.Thrusters.Length; ++i)
            {
                int tIndex = i;
                AddLabel(ThrusterList, () =>
                {
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
            S.HullEditMode = !S.HullEditMode;
            SetHullEditVisibility(IsEditing);
            S.ChangeHull(S.CurrentHull, zoomToHull:false);
        }

        void SetHullEditVisibility(bool visible)
        {
            EditList.Visible = visible;
            Title.Visible = visible;
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
                    (SlotStruct slot, Point pos) = S.GetSlotUnderCursor();
                    if (ModifyHull(input, slot, pos))
                    {
                        GameAudio.DesignSoftBeep();
                        return true;
                    }
                    else
                    {
                        GameAudio.NegativeClick();
                    }
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
                    bool hasSlot = slot == null;
                    Color c = hasSlot ? Color.Green : Color.Red;
                    if (Op == SlotOp.Edit)
                        c = !hasSlot ? Color.Green : Color.Red;

                    Vector2 worldPos = S.ModuleGrid.GridPosToWorld(pos);
                    S.DrawRectangleProjected(new RectF(worldPos, new Vector2(16)), c);
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

        bool ModifyHull(InputState input, SlotStruct ss, Point pos)
        {
            ShipHull newHull = S.CurrentHull.GetClone();
            HullSlot slot = newHull.FindSlot(pos);
            var slots = new Array<HullSlot>(newHull.HullSlots);

            switch (Op)
            {
                case SlotOp.AddDelete:
                {
                    if (ss == null && input.LeftMouseClick)
                    {
                        slots.Add(new HullSlot(pos.X, pos.Y, Restrictions.IO));
                        newHull.SetHullSlots(slots);
                    }
                    else if (ss != null && input.RightMouseClick)
                    {
                        slots.Remove(slot);
                        newHull.SetHullSlots(slots);
                    }
                    else
                    {
                        return false;
                    }
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
