using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.Gameplay;

// ReSharper disable once CheckNamespace
namespace Ship_Game
{
    public sealed partial class ShipDesignScreen
    {
        public override void Draw(SpriteBatch spriteBatch)
        {
            GameTime gameTime = Game1.Instance.GameTime;
            ScreenManager.BeginFrameRendering(gameTime, ref View, ref Projection);

            Empire.Universe.bg.Draw(Empire.Universe, Empire.Universe.starfield);
            ScreenManager.RenderSceneObjects();

            ScreenManager.SpriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate,
                SaveStateMode.None, Camera.Transform);
            if (ToggleOverlay)
            {
                foreach (SlotStruct slot in this.Slots)
                {
                    if (slot.Module != null)
                    {
                        ScreenManager.SpriteBatch.Draw(
                            ResourceManager.Texture("Modules/tile_concreteglass_1x1")
                            , new Rectangle(slot.PQ.enclosingRect.X, slot.PQ.enclosingRect.Y
                                , 16 * slot.Module.XSIZE, 16 * slot.Module.YSIZE), Color.Gray);
                    }
                    else
                    {
                        if (this.ActiveModule != null)
                        {
                            Texture2D item = ResourceManager.Texture("Modules/tile_concreteglass_1x1");
                            Color activeColor = slot.ShowValid ? Color.LightGreen : Color.Red;

                            spriteBatch.Draw(item, slot.PQ.enclosingRect, activeColor);
                            if (slot.Powered)
                            {
                                ScreenManager.SpriteBatch.Draw(
                                    ResourceManager.Texture("Modules/tile_concreteglass_1x1")
                                    , slot.PQ.enclosingRect, new Color(255, 255, 0, 150));
                            }
                        }
                        else if (slot.Powered)
                        {
                            ScreenManager.SpriteBatch.Draw(
                                ResourceManager.Texture("Modules/tile_concreteglass_1x1")
                                , slot.PQ.enclosingRect, Color.Yellow);
                        }
                        else
                        {
                            SpriteBatch spriteBatch1 = ScreenManager.SpriteBatch;
                            Texture2D texture2D = ResourceManager.Texture("Modules/tile_concreteglass_1x1");
                            Rectangle rectangle1 = slot.PQ.enclosingRect;
                            Color unpoweredColored;
                            if (slot.ShowValid)
                            {
                                unpoweredColored = Color.LightGreen;
                            }
                            else
                            {
                                unpoweredColored = (slot.ShowValid ? Color.White : Color.Red);
                            }
                            spriteBatch1.Draw(texture2D, rectangle1, unpoweredColored);
                        }
                    }
                    if (slot.Module != null)
                        continue;
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, string.Concat(" ", slot.Restrictions)
                        , new Vector2(slot.PQ.enclosingRect.X, slot.PQ.enclosingRect.Y)
                        , Color.Navy, 0f, Vector2.Zero, 0.4f, SpriteEffects.None, 1f);
                }
                foreach (SlotStruct slot in this.Slots)
                {
                    if (slot.ModuleUID == null || slot.Tex == null)
                    {
                        continue;
                    }
                    if (slot.State != ActiveModuleState.Normal)
                    {
                        Rectangle r = new Rectangle(
                            slot.PQ.enclosingRect.X,
                            slot.PQ.enclosingRect.Y,
                            16 * slot.Module.XSIZE,
                            16 * slot.Module.YSIZE);

                        // @todo Simplify this
                        switch (slot.State)
                        {
                            case ActiveModuleState.Left:
                            {
                                int h = slot.Module.YSIZE * 16;
                                int w = slot.Module.XSIZE * 16;
                                r.Width = h; // swap width & height
                                r.Height = w;
                                r.Y += h;
                                ScreenManager.SpriteBatch.Draw(slot.Tex, r, null, Color.White, -1.57079637f,
                                    Vector2.Zero
                                    , SpriteEffects.None, 1f);
                                break;
                            }
                            case ActiveModuleState.Right:
                            {
                                int w = slot.Module.YSIZE * 16;
                                int h = slot.Module.XSIZE * 16;
                                r.Width = w;
                                r.Height = h;
                                r.X += h;
                                ScreenManager.SpriteBatch.Draw(slot.Tex, r, null, Color.White, 1.57079637f, Vector2.Zero
                                    , SpriteEffects.None, 1f);
                                break;
                            }
                            case ActiveModuleState.Rear:
                            {
                                ScreenManager.SpriteBatch.Draw(slot.Tex, r, null, Color.White, 0f, Vector2.Zero
                                    , SpriteEffects.FlipVertically, 1f);
                                break;
                            }
                        }
                    }
                    else if (slot.Module.XSIZE <= 1 && slot.Module.YSIZE <= 1)
                    {
                        if (slot.Module.ModuleType != ShipModuleType.PowerConduit)
                        {
                            ScreenManager.SpriteBatch.Draw(slot.Tex, slot.PQ.enclosingRect, Color.White);
                        }
                        else
                        {
                            string graphic = GetConduitGraphic(slot);
                            var conduitTex = ResourceManager.Texture("Conduits/" + graphic);
                            ScreenManager.SpriteBatch.Draw(conduitTex, slot.PQ.enclosingRect, Color.White);
                            if (slot.Module.Powered)
                            {
                                var poweredTex = ResourceManager.Texture("Conduits/" + graphic + "_power");
                                ScreenManager.SpriteBatch.Draw(poweredTex, slot.PQ.enclosingRect, Color.White);
                            }
                        }
                    }
                    else if (slot.SlotReference.Position.X <= 256f)
                    {
                        ScreenManager.SpriteBatch.Draw(slot.Tex, new Rectangle(slot.PQ.enclosingRect.X,
                            slot.PQ.enclosingRect.Y
                            , 16 * slot.Module.XSIZE, 16 * slot.Module.YSIZE), Color.White);
                    }
                    else
                    {
                        ScreenManager.SpriteBatch.Draw(slot.Tex, new Rectangle(slot.PQ.enclosingRect.X,
                                slot.PQ.enclosingRect.Y
                                , 16 * slot.Module.XSIZE, 16 * slot.Module.YSIZE), null, Color.White, 0f, Vector2.Zero
                            , SpriteEffects.FlipHorizontally, 1f);
                    }
                    if (slot.Module != HoveredModule)
                    {
                        continue;
                    }
                    ScreenManager.SpriteBatch.DrawRectangle(new Rectangle(slot.PQ.enclosingRect.X,
                        slot.PQ.enclosingRect.Y
                        , 16 * slot.Module.XSIZE, 16 * slot.Module.YSIZE), Color.White, 2f);
                }
                foreach (SlotStruct slot in this.Slots)
                {
                    if (slot.ModuleUID == null || slot.Tex == null ||
                        slot.Module != this.HighlightedModule && !this.ShowAllArcs)
                    {
                        continue;
                    }
                    if (slot.Module.shield_power_max > 0f)
                    {
                        Vector2 center = new Vector2(slot.PQ.enclosingRect.X + 16 * slot.Module.XSIZE / 2
                            , slot.PQ.enclosingRect.Y + 16 * slot.Module.YSIZE / 2);
                        DrawCircle(center, slot.Module.shield_radius, 50, Color.LightGreen);
                    }


                    // @todo Use this to fix the 'original' code below :)))
                    var arcTexture = Empire.Universe.GetArcTexture(slot.Module.FieldOfFire);

                    void DrawArc(Color drawcolor)
                    {
                        var center = new Vector2(slot.PQ.enclosingRect.X + 16 * slot.Module.XSIZE / 2
                            , slot.PQ.enclosingRect.Y + 16 * slot.Module.YSIZE / 2);
                        var origin = new Vector2(250f, 250f);

                        var toDraw = new Rectangle((int)center.X, (int)center.Y, 500, 500);
                        ScreenManager.SpriteBatch.Draw(arcTexture, toDraw, null, drawcolor
                            , slot.Module.Facing.ToRadians(), origin, SpriteEffects.None, 1f);

                    }

                    Weapon w = slot.Module.InstalledWeapon;
                    if (w == null)
                        continue;
                    if      (w.Tag_Cannon && !w.Tag_Energy)   DrawArc(new Color(255, 255, 0, 255));                    
                    else if (w.Tag_Railgun || w.Tag_Subspace) DrawArc(new Color(255, 0, 255, 255));                    
                    else if (w.Tag_Cannon)                    DrawArc(new Color(0, 255, 0, 255));                    
                    else if (!w.isBeam)                       DrawArc(new Color(255, 0, 0, 255));                    
                    else                                      DrawArc(new Color(0, 0, 255, 255));                
                }

                foreach (SlotStruct ss in this.Slots)
                {
                    if (ss.Module == null)
                    {
                        continue;
                    }
                    Vector2 Center = new Vector2(ss.PQ.X + 16 * ss.Module.XSIZE / 2,
                        ss.PQ.Y + 16 * ss.Module.YSIZE / 2);
                    Vector2 lightOrigin = new Vector2(8f, 8f);
                    if (ss.Module.PowerDraw <= 0f || ss.Module.Powered ||
                        ss.Module.ModuleType == ShipModuleType.PowerConduit)
                    {
                        continue;
                    }
                    Rectangle? nullable8 = null;
                    ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/lightningBolt"],
                        Center, nullable8, Color.White, 0f, lightOrigin, 1f, SpriteEffects.None, 1f);
                }
            }
            ScreenManager.SpriteBatch.End();
            ScreenManager.SpriteBatch.Begin();
            
            Vector2 mousePos = Input.CursorPosition;
            if (this.ActiveModule != null &&// !this.ActiveModSubMenu.Menu.HitTest(mousePos) &&
                !this.ModSel.Menu.HitTest(mousePos) 
                
                && (!this.Choosefighterrect.HitTest(mousePos) ||
                                                        this.ActiveModule.ModuleType != ShipModuleType.Hangar ||
                                                        this.ActiveModule.IsSupplyBay || this.ActiveModule.IsTroopBay))
            {
                ShipModule moduleTemplate = ResourceManager.GetModuleTemplate(ActiveModule.UID);

                Rectangle r = new Rectangle((int)Input.CursorPosition.X, (int)Input.CursorPosition.Y,
                    (int) ((float) (16 * this.ActiveModule.XSIZE) * this.Camera.Zoom),
                    (int) ((float) (16 * this.ActiveModule.YSIZE) * this.Camera.Zoom));
                switch (this.ActiveModState)
                {
                    case ActiveModuleState.Normal:
                    {
                        ScreenManager.SpriteBatch.Draw(
                            ResourceManager.TextureDict[moduleTemplate.IconTexturePath], r, Color.White);
                        break;
                    }
                    case ActiveModuleState.Left:
                    {
                        r.Y = r.Y + (int) ((16 * moduleTemplate.XSIZE) * Camera.Zoom);
                        int h = r.Height;
                        int w = r.Width;
                        r.Width = h;
                        r.Height = w;
                        ScreenManager.SpriteBatch.Draw(
                            ResourceManager.TextureDict[moduleTemplate.IconTexturePath], r, null, Color.White,
                            -1.57079637f, Vector2.Zero, SpriteEffects.None, 1f);
                        break;
                    }
                    case ActiveModuleState.Right:
                    {
                        r.X = r.X + (int) ((16 * moduleTemplate.YSIZE) * Camera.Zoom);
                        int h = r.Height;
                        int w = r.Width;
                        r.Width = h;
                        r.Height = w;
                        ScreenManager.SpriteBatch.Draw(
                            ResourceManager.TextureDict[moduleTemplate.IconTexturePath], r, null, Color.White,
                            1.57079637f, Vector2.Zero, SpriteEffects.None, 1f);
                        break;
                    }
                    case ActiveModuleState.Rear:
                    {
                        ScreenManager.SpriteBatch.Draw(
                            ResourceManager.TextureDict[moduleTemplate.IconTexturePath], r, null, Color.White,
                            0f, Vector2.Zero, SpriteEffects.FlipVertically, 1f);
                        break;
                    }
                }
                if (this.ActiveModule.shield_power_max > 0f)
                {
                    Vector2 center = new Vector2(Input.CursorPosition.X, Input.CursorPosition.Y) +
                                     new Vector2(moduleTemplate.XSIZE * 16 / 2f,
                                         moduleTemplate.YSIZE * 16 / 2f);
                    DrawCircle(center, this.ActiveModule.shield_radius * this.Camera.Zoom, 50, Color.LightGreen);
                }
            }
            this.DrawUI(gameTime);
            selector?.Draw(ScreenManager.SpriteBatch);
            ArcsButton.DrawWithShadowCaps(ScreenManager);
            if (Debug)
            {
                float width2 = ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2f;

                var pos = new Vector2(width2 - Fonts.Arial20Bold.MeasureString("Debug").X / 2, 120f);
                HelperFunctions.DrawDropShadowText(ScreenManager, "Debug", pos, Fonts.Arial20Bold);
                pos = new Vector2(width2 - Fonts.Arial20Bold.MeasureString(Operation.ToString()).X / 2, 140f);
                HelperFunctions.DrawDropShadowText(ScreenManager, Operation.ToString(), pos, Fonts.Arial20Bold);
#if SHIPYARD
                string ratios = $"I: {TotalI}       O: {TotalO}      E: {TotalE}      IO: {TotalIO}      " +
                                $"IE: {TotalIE}      OE: {TotalOE}      IOE: {TotalIOE}";
                pos = new Vector2(width2 - Fonts.Arial20Bold.MeasureString(Ratios).X / 2, 180f);
                HelperFunctions.DrawDropShadowText(base.ScreenManager, Ratios, pos, Fonts.Arial20Bold);
#endif
            }
            Close.Draw(ScreenManager);
            ScreenManager.SpriteBatch.End();
            ScreenManager.EndFrameRendering();
        }



        

        private void DrawHullSelection()
        {
            Rectangle r = this.HullSelectionSub.Menu;
            r.Y = r.Y + 25;
            r.Height = r.Height - 25;
            Selector sel = new Selector(r, new Color(0, 0, 0, 210));
            sel.Draw(ScreenManager.SpriteBatch);
            this.HullSL.Draw(ScreenManager.SpriteBatch);
            float x = (float) Mouse.GetState().X;
            MouseState state = Mouse.GetState();
            Vector2 MousePos = new Vector2(x, (float) state.Y);
            this.HullSelectionSub.Draw();
            Vector2 bCursor = new Vector2((float) (this.HullSelectionSub.Menu.X + 10),
                (float) (this.HullSelectionSub.Menu.Y + 45));
            for (int i = this.HullSL.indexAtTop;
                i < this.HullSL.Copied.Count && i < this.HullSL.indexAtTop + this.HullSL.entriesToDisplay;
                i++)
            {
                bCursor = new Vector2((float) (this.HullSelectionSub.Menu.X + 10),
                    (float) (this.HullSelectionSub.Menu.Y + 45));
                ScrollList.Entry e = this.HullSL.Copied[i];
                bCursor.Y = (float) e.clickRect.Y;
                if (e.item is ModuleHeader)
                {
                    (e.item as ModuleHeader).Draw(ScreenManager, bCursor);
                }
                else if (e.item is ShipData)
                {
                    bCursor.X = bCursor.X + 10f;
                    ScreenManager.SpriteBatch.Draw(
                        ResourceManager.TextureDict[(e.item as ShipData).IconPath],
                        new Rectangle((int) bCursor.X, (int) bCursor.Y, 29, 30), Color.White);
                    Vector2 tCursor = new Vector2(bCursor.X + 40f, bCursor.Y + 3f);
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, (e.item as ShipData).Name, tCursor,
                        Color.White);
                    tCursor.Y = tCursor.Y + (float) Fonts.Arial12Bold.LineSpacing;
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold,
                        Localizer.GetRole((e.item as ShipData).Role, EmpireManager.Player), tCursor, Color.Orange);
                    if (e.clickRect.HitTest(MousePos))
                    {
                        if (e.clickRectHover == 0)
                        {
                            GameAudio.PlaySfxAsync("sd_ui_mouseover");
                        }
                        e.clickRectHover = 1;
                    }
                }
            }
        }



        private void DrawModuleSelection()
        {
            Rectangle r = this.ModSel.Menu;
            r.Y = r.Y + 25;
            r.Height = r.Height - 25;
            Selector sel = new Selector(r, new Color(0, 0, 0, 210));
            sel.Draw(ScreenManager.SpriteBatch);
            this.ModSel.Draw(ScreenManager.SpriteBatch);
            //this.WeaponSl.Draw(ScreenManager.SpriteBatch);
        }

        private void DrawRequirement(ref Vector2 Cursor, string words, bool met)
        {
            float amount = 165f;
            if (GlobalStats.IsGermanFrenchOrPolish)
            {
                amount = amount + 35f;
            }
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, words, Cursor,
                (met ? Color.LightGreen : Color.LightPink));
            string stats = (met ? "OK" : "X");
            Cursor.X = Cursor.X + (amount - Fonts.Arial12Bold.MeasureString(stats).X);
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, stats, Cursor,
                (met ? Color.LightGreen : Color.LightPink));
            Cursor.X = Cursor.X - (amount - Fonts.Arial12Bold.MeasureString(stats).X);
        }

        private void DrawShipInfoPanel()
        {
            float HitPoints = 0f;
            float Mass = 0f;
            float PowerDraw = 0f;
            float PowerCapacity = 0f;
            float OrdnanceCap = 0f;
            float PowerFlow = 0f;
            float ShieldPower = 0f;
            float Thrust = 0f;
            float AfterThrust = 0f;
            float CargoSpace = 0f;
            int TroopCount = 0;
            float Size = 0f;
            float Cost = 0f;
            float WarpThrust = 0f;
            float TurnThrust = 0f;
            float WarpableMass = 0f;
            float WarpDraw = 0f;
            float FTLCount = 0f;
            float FTLSpeed = 0f;
            float RepairRate = 0f;
            float sensorRange = 0f;
            float sensorBonus = 0f;
            float BeamLongestDuration = 0f;
            float OrdnanceUsed = 0f;
            float OrdnanceRecoverd = 0f;
            float WeaponPowerNeeded = 0f;
            float Upkeep = 0f;
            float FTLSpoolTimer = 0f;
            float EMPResist = 0f;
            bool bEnergyWeapons = false;
            float Off = 0f;
            float Def = 0;
            float strength = 0;
            float targets = 0;
            int fixedtargets = 0;
            float TotalECM = 0f;

            // bonuses are only available in mods
            ResourceManager.HullBonuses.TryGetValue(ActiveHull.Hull, out HullBonus bonus);

            foreach (SlotStruct slot in this.Slots)
            {
                Size = Size + 1f;
                if (slot.Module == null)
                {
                    continue;
                }
                HitPoints = HitPoints + (slot.Module.Health +
                                         EmpireManager.Player.data.Traits.ModHpModifier * slot.Module.Health);
                if (slot.Module.Mass < 0f && slot.Powered)
                {
                    if (slot.Module.ModuleType == ShipModuleType.Armor)
                    {
                        Mass += slot.Module.Mass * EmpireManager.Player.data.ArmourMassModifier;
                    }
                    else
                        Mass += slot.Module.Mass;
                }
                else if (slot.Module.Mass > 0f)
                {
                    if (slot.Module.ModuleType == ShipModuleType.Armor)
                    {
                        Mass += slot.Module.Mass * EmpireManager.Player.data.ArmourMassModifier;
                    }
                    else
                        Mass += slot.Module.Mass;
                }
                TroopCount += slot.Module.TroopCapacity;
                PowerCapacity += slot.Module.PowerStoreMax +
                                 slot.Module.PowerStoreMax * EmpireManager.Player.data.FuelCellModifier;
                OrdnanceCap = OrdnanceCap + (float) slot.Module.OrdinanceCapacity;
                PowerFlow += slot.Module.PowerFlowMax +
                             slot.Module.PowerFlowMax * EmpireManager.Player.data.PowerFlowMod;
                if (slot.Module.Powered)
                {
                    EMPResist += slot.Module.EMP_Protection;
                    WarpableMass = WarpableMass + slot.Module.WarpMassCapacity;
                    PowerDraw = PowerDraw + slot.Module.PowerDraw;
                    WarpDraw = WarpDraw + slot.Module.PowerDrawAtWarp;
                    if (slot.Module.ECM > TotalECM)
                        TotalECM = slot.Module.ECM;
                    if (slot.Module.InstalledWeapon != null && slot.Module.InstalledWeapon.PowerRequiredToFire > 0)
                        bEnergyWeapons = true;
                    if (slot.Module.InstalledWeapon != null && slot.Module.InstalledWeapon.BeamPowerCostPerSecond > 0)
                        bEnergyWeapons = true;
                    if (slot.Module.FTLSpeed > 0f)
                    {
                        FTLCount = FTLCount + 1f;
                        FTLSpeed = FTLSpeed + slot.Module.FTLSpeed;
                    }
                    if (slot.Module.FTLSpoolTime * EmpireManager.Player.data.SpoolTimeModifier > FTLSpoolTimer)
                    {
                        FTLSpoolTimer = slot.Module.FTLSpoolTime * EmpireManager.Player.data.SpoolTimeModifier;
                    }
                    ShieldPower += slot.Module.shield_power_max +
                                   EmpireManager.Player.data.ShieldPowerMod * slot.Module.shield_power_max;
                    Thrust = Thrust + slot.Module.thrust;
                    WarpThrust = WarpThrust + slot.Module.WarpThrust;
                    TurnThrust = TurnThrust + slot.Module.TurnThrust;

                    RepairRate += ((slot.Module.BonusRepairRate + slot.Module.BonusRepairRate *
                                    EmpireManager.Player.data.Traits.RepairMod) * (1f + bonus?.RepairBonus ?? 0));
                    OrdnanceRecoverd += slot.Module.OrdnanceAddedPerSecond;
                    if (slot.Module.SensorRange > sensorRange)
                    {
                        sensorRange = slot.Module.SensorRange;
                    }
                    if (slot.Module.SensorBonus > sensorBonus)
                        sensorBonus = slot.Module.SensorBonus;

                    //added by gremlin collect weapon stats                  
                    if (slot.Module.isWeapon || slot.Module.BombType != null)
                    {
                        Weapon weapon;
                        if (slot.Module.BombType == null)
                            weapon = slot.Module.InstalledWeapon;
                        else
                            weapon = ResourceManager.WeaponsDict[slot.Module.BombType];
                        OrdnanceUsed += weapon.OrdinanceRequiredToFire / weapon.fireDelay * weapon.SalvoCount;
                        WeaponPowerNeeded += weapon.PowerRequiredToFire / weapon.fireDelay * weapon.SalvoCount;
                        if (weapon.isBeam)
                            WeaponPowerNeeded += weapon.BeamPowerCostPerSecond * weapon.BeamDuration / weapon.fireDelay;
                        if (BeamLongestDuration < weapon.BeamDuration)
                            BeamLongestDuration = weapon.BeamDuration;
                    }
                    //end
                    if (slot.Module.FixedTracking > fixedtargets)
                        fixedtargets = slot.Module.FixedTracking;

                    targets += slot.Module.TargetTracking;
                }
                Cost = Cost + slot.Module.Cost * UniverseScreen.GamePaceStatic;
                CargoSpace = CargoSpace + slot.Module.Cargo_Capacity;
            }

            targets += fixedtargets;

            Mass = Mass + (float) (ActiveHull.ModuleSlots.Length / 2);
            Mass = Mass * EmpireManager.Player.data.MassModifier;
            if (Mass < (float) (ActiveHull.ModuleSlots.Length / 2))
            {
                Mass = (float) (ActiveHull.ModuleSlots.Length / 2);
            }
            float Speed = 0f;
            float WarpSpeed = WarpThrust / (Mass + 0.1f);
            //Added by McShooterz: hull bonus speed
            WarpSpeed *= EmpireManager.Player.data.FTLModifier * (1f + bonus?.SpeedBonus ?? 0);
            float single = WarpSpeed / 1000f;
            string WarpString = string.Concat(single.ToString("#.0"), "k");
            float Turn = 0f;
            if (Mass > 0f)
            {
                Speed = Thrust / Mass;
                Turn = TurnThrust / Mass / 700f;
            }
            float AfterSpeed = AfterThrust / (Mass + 0.1f);
            AfterSpeed = AfterSpeed * EmpireManager.Player.data.SubLightModifier;
            Turn = (float) MathHelper.ToDegrees(Turn);
            Vector2 Cursor = new Vector2((float) (this.StatsSub.Menu.X + 10), (float) (this.ShipStats.Menu.Y + 33));

            if (bonus != null) //Added by McShooterz: Draw Hull Bonuses
            {
                Vector2 LCursor = new Vector2(this.HullSelectionRect.X - 145, HullSelectionRect.Y + 31);
                if (bonus.ArmoredBonus != 0 || bonus.ShieldBonus != 0 || bonus.SensorBonus != 0 ||
                    bonus.SpeedBonus != 0 || bonus.CargoBonus != 0 || bonus.DamageBonus != 0 ||
                    bonus.FireRateBonus != 0 || bonus.RepairBonus != 0 || bonus.CostBonus != 0)
                {
                    ScreenManager.SpriteBatch.DrawString(Fonts.Verdana14Bold, Localizer.Token(6015), LCursor,
                        Color.Orange);
                    LCursor.Y = LCursor.Y + (float) (Fonts.Verdana14Bold.LineSpacing + 2);
                }
                if (bonus.ArmoredBonus != 0)
                {
                    this.DrawHullBonus(ref LCursor, Localizer.Token(6016), bonus.ArmoredBonus);
                    LCursor.Y = LCursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 2);
                }
                if (bonus.ShieldBonus != 0)
                {
                    this.DrawHullBonus(ref LCursor, "Shield Strength", bonus.ShieldBonus);
                    LCursor.Y = LCursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 2);
                }
                if (bonus.SensorBonus != 0)
                {
                    this.DrawHullBonus(ref LCursor, Localizer.Token(6017), bonus.SensorBonus);
                    LCursor.Y = LCursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 2);
                }
                if (bonus.SpeedBonus != 0)
                {
                    this.DrawHullBonus(ref LCursor, Localizer.Token(6018), bonus.SpeedBonus);
                    LCursor.Y = LCursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 2);
                }
                if (bonus.CargoBonus != 0)
                {
                    this.DrawHullBonus(ref LCursor, Localizer.Token(6019), bonus.CargoBonus);
                    LCursor.Y = LCursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 2);
                }
                if (bonus.DamageBonus != 0)
                {
                    this.DrawHullBonus(ref LCursor, "Weapon Damage", bonus.DamageBonus);
                    LCursor.Y = LCursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 2);
                }
                if (bonus.FireRateBonus != 0)
                {
                    this.DrawHullBonus(ref LCursor, Localizer.Token(6020), bonus.FireRateBonus);
                    LCursor.Y = LCursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 2);
                }
                if (bonus.RepairBonus != 0)
                {
                    this.DrawHullBonus(ref LCursor, Localizer.Token(6013), bonus.RepairBonus);
                    LCursor.Y = LCursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 2);
                }
                if (bonus.CostBonus != 0)
                {
                    this.DrawHullBonus(ref LCursor, Localizer.Token(6021), bonus.CostBonus);
                    LCursor.Y = LCursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 10);
                }
            }
            //Added by McShooterz: hull bonus starting cost
            DrawStat(ref Cursor, Localizer.Token(109) + ":",
                ((int) Cost + (bonus?.StartingCost ?? 0)) * (1f - bonus?.CostBonus ?? 0), 99);
            Cursor.Y += Fonts.Arial12Bold.LineSpacing + 2;

            if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useProportionalUpkeep)
            {
                Upkeep = GetMaintCostShipyardProportional(this.ActiveHull, Cost, EmpireManager.Player);
            }
            else
            {
                Upkeep = GetMaintCostShipyard(this.ActiveHull, Size, EmpireManager.Player);
            }

            this.DrawStat(ref Cursor, "Upkeep Cost:", -Upkeep, 175);
            Cursor.Y = Cursor.Y + (float) (Fonts.Arial12Bold.LineSpacing +
                                           2); //Gretman (so we can see how many total slots are on the ships)
            this.DrawStat(ref Cursor, "Ship UniverseRadius:", (float) ActiveHull.ModuleSlots.Length, 230);
            Cursor.Y = Cursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 10);

            this.DrawStatColor(ref Cursor, string.Concat(Localizer.Token(110), ":"), PowerCapacity, 100,
                Color.LightSkyBlue);
            Cursor.Y = Cursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 2);
            this.DrawStatColor(ref Cursor, string.Concat(Localizer.Token(111), ":"), (PowerFlow - PowerDraw), 101,
                Color.LightSkyBlue);

            //added by McShooterz: Allow Warp draw and after burner values be displayed in ship info
            float fDrawAtWarp = 0;
            if (WarpDraw != 0)
            {
                fDrawAtWarp = (PowerFlow - (WarpDraw / 2 * EmpireManager.Player.data.FTLPowerDrainModifier +
                                            (PowerDraw * EmpireManager.Player.data.FTLPowerDrainModifier)));
                if (WarpSpeed > 0)
                {
                    Cursor.Y = Cursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 2);
                    this.DrawStatColor(ref Cursor, string.Concat(Localizer.Token(112), ":"), fDrawAtWarp, 102,
                        Color.LightSkyBlue);
                }
            }
            else
            {
                fDrawAtWarp = (PowerFlow - PowerDraw * EmpireManager.Player.data.FTLPowerDrainModifier);
                if (WarpSpeed > 0)
                {
                    Cursor.Y = Cursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 2);
                    this.DrawStatColor(ref Cursor, string.Concat(Localizer.Token(112), ":"), fDrawAtWarp, 102,
                        Color.LightSkyBlue);
                }
            }


            float fWarpTime = ((-PowerCapacity / fDrawAtWarp) * 0.9f);
            string sWarpTime = fWarpTime.ToString("0.#");
            if (WarpSpeed > 0)
            {
                if (fDrawAtWarp < 0)
                {
                    Cursor.Y = Cursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 2);
                    this.DrawStatEnergy(ref Cursor, "FTL Time:", sWarpTime, 176);
                }
                else if (fWarpTime > 900)
                {
                    Cursor.Y = Cursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 2);
                    this.DrawStatEnergy(ref Cursor, "FTL Time:", "INF", 176);
                }
                else
                {
                    Cursor.Y = Cursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 2);
                    this.DrawStatEnergy(ref Cursor, "FTL Time:", "INF", 176);
                }
            }


            float powerconsumed = WeaponPowerNeeded - PowerFlow;
            float EnergyDuration = 0f;
            if (powerconsumed > 0)
            {
                EnergyDuration = WeaponPowerNeeded > 0 ? ((PowerCapacity) / powerconsumed) : 0;
                if ((EnergyDuration >= BeamLongestDuration) && bEnergyWeapons == true)
                {
                    Cursor.Y = Cursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 2);
                    this.DrawStatColor(ref Cursor, "Power Time:", EnergyDuration, 163, Color.LightSkyBlue);
                }
                else if (bEnergyWeapons == true)
                {
                    Cursor.Y = Cursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 2);
                    this.DrawStatEnergyBad(ref Cursor, "Power Time:", EnergyDuration.ToString("N1"), 163);
                }
            }
            else
            {
                if (bEnergyWeapons == true)
                {
                    Cursor.Y = Cursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 2);
                    this.DrawStatEnergy(ref Cursor, "Power Time:", "INF", 163);
                }
            }
            Cursor.Y = Cursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 10);
            this.DrawStatColor(ref Cursor, string.Concat(Localizer.Token(113), ":"), HitPoints, 103, Color.Goldenrod);
            //Added by McShooterz: draw total repair
            if (RepairRate > 0)
            {
                Cursor.Y = Cursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 2);
                this.DrawStatColor(ref Cursor, string.Concat(Localizer.Token(6013), ":"), RepairRate, 236,
                    Color.Goldenrod);
            }
            if (ShieldPower > 0)
            {
                Cursor.Y = Cursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 2);
                this.DrawStatColor(ref Cursor, string.Concat(Localizer.Token(114), ":"), ShieldPower, 104,
                    Color.Goldenrod);
            }
            if (EMPResist > 0)
            {
                Cursor.Y = Cursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 2);
                this.DrawStatColor(ref Cursor, string.Concat(Localizer.Token(6177), ":"), EMPResist, 220,
                    Color.Goldenrod);
            }
            if (TotalECM > 0)
            {
                Cursor.Y = Cursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 2);
                this.DrawStatColor(ref Cursor, string.Concat(Localizer.Token(6189), ":"), TotalECM, 234,
                    Color.Goldenrod, isPercent: true);
            }

            Cursor.Y = Cursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 10);


            // The Doctor: removed the mass display. It's a meaningless value to the player, and it takes up a valuable line in the limited space.
            //this.DrawStat(ref Cursor, string.Concat(Localizer.Token(115), ":"), (int)Mass, 79);
            //Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);

            #region HardcoreRule info

            if (GlobalStats.HardcoreRuleset)
            {
                string massstring = GetNumberString(Mass);
                string wmassstring = GetNumberString(WarpableMass);
                string warpmassstring = string.Concat(massstring, "/", wmassstring);
                if (Mass > WarpableMass)
                {
                    this.DrawStatBad(ref Cursor, "Warpable Mass:", warpmassstring, 153);
                }
                else
                {
                    this.DrawStat(ref Cursor, "Warpable Mass:", warpmassstring, 153);
                }
                Cursor.Y = Cursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 2);
                this.DrawRequirement(ref Cursor, "Warp Capable", Mass <= WarpableMass);
                Cursor.Y = Cursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 2);
                if (FTLCount > 0f)
                {
                    float speed = FTLSpeed / FTLCount;
                    this.DrawStat(ref Cursor, string.Concat(Localizer.Token(2170), ":"), speed, 135);
                    Cursor.Y = Cursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 2);
                }
            }

            #endregion

            else if (WarpSpeed <= 0f)
            {
                this.DrawStatColor(ref Cursor, string.Concat(Localizer.Token(2170), ":"), 0, 135, Color.DarkSeaGreen);
                Cursor.Y = Cursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 2);
            }
            else
            {
                this.DrawStatPropulsion(ref Cursor, string.Concat(Localizer.Token(2170), ":"), WarpString, 135);
                Cursor.Y = Cursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 2);
            }
            if (WarpSpeed > 0 && FTLSpoolTimer > 0)
            {
                this.DrawStatColor(ref Cursor, "FTL Spool:", FTLSpoolTimer, 177, Color.DarkSeaGreen);
                Cursor.Y = Cursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 2);
            }
            this.DrawStatColor(ref Cursor, string.Concat(Localizer.Token(116), ":"),
                (Speed * EmpireManager.Player.data.SubLightModifier *
                 (GlobalStats.ActiveMod != null && ResourceManager.HullBonuses.ContainsKey(this.ActiveHull.Hull)
                     ? 1f + bonus.SpeedBonus
                     : 1)), 105, Color.DarkSeaGreen);
            Cursor.Y = Cursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 2);
            //added by McShooterz: afterburn speed
            if (AfterSpeed != 0)
            {
                this.DrawStatColor(ref Cursor, "Afterburner Speed:", AfterSpeed, 105, Color.DarkSeaGreen);
                Cursor.Y = Cursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 2);
            }
            this.DrawStatColor(ref Cursor, string.Concat(Localizer.Token(117), ":"), Turn, 107, Color.DarkSeaGreen);
            Cursor.Y = Cursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 10);
            if (OrdnanceCap > 0)
            {
                this.DrawStatColor(ref Cursor, string.Concat(Localizer.Token(118), ":"), OrdnanceCap, 108,
                    Color.IndianRed);
            }
            if (OrdnanceRecoverd > 0)
            {
                Cursor.Y = Cursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 2);
                this.DrawStatColor(ref Cursor, "Ordnance Created / s:", OrdnanceRecoverd, 162, Color.IndianRed);
            }
            if (OrdnanceCap > 0)
            {
                float AmmoTime = 0f;
                if (OrdnanceUsed - OrdnanceRecoverd > 0)
                {
                    AmmoTime = OrdnanceCap / (OrdnanceUsed - OrdnanceRecoverd);
                    Cursor.Y = Cursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 2);
                    this.DrawStatColor(ref Cursor, "Ammo Time:", AmmoTime, 164, Color.IndianRed);
                }
                else
                {
                    Cursor.Y = Cursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 2);
                    this.DrawStatOrdnance(ref Cursor, "Ammo Time:", "INF", 164);
                }
            }
            if (TroopCount > 0)
            {
                Cursor.Y = Cursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 2);
                this.DrawStatColor(ref Cursor, string.Concat(Localizer.Token(6132), ":"), (float) TroopCount, 180,
                    Color.IndianRed);
            }

            Cursor.Y = Cursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 10);

            if (CargoSpace > 0)
            {
                this.DrawStat(ref Cursor, string.Concat(Localizer.Token(119), ":"),
                    (CargoSpace +
                     (GlobalStats.ActiveMod != null && GlobalStats.ActiveModInfo.useHullBonuses &&
                      ResourceManager.HullBonuses.ContainsKey(this.ActiveHull.Hull)
                         ? CargoSpace * bonus.CargoBonus
                         : 0)), 109);
                Cursor.Y = Cursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 2);
            }
            if (sensorRange != 0)
            {
                this.DrawStat(ref Cursor, string.Concat(Localizer.Token(6130), ":"),
                    ((sensorRange + sensorBonus) +
                     (GlobalStats.ActiveMod != null && GlobalStats.ActiveModInfo.useHullBonuses &&
                      ResourceManager.HullBonuses.ContainsKey(this.ActiveHull.Hull)
                         ? (sensorRange + sensorBonus) * bonus.SensorBonus
                         : 0)), 235);
                Cursor.Y = Cursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 2);
            }
            if (targets > 0)
            {
                this.DrawStat(ref Cursor, string.Concat(Localizer.Token(6188), ":"), ((targets + 1f)), 232);
                Cursor.Y = Cursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 2);
            }

            Cursor.Y = Cursor.Y + (float) (Fonts.Arial12Bold.LineSpacing);
            bool hasBridge = false;
            bool EmptySlots = true;
            foreach (SlotStruct slot in this.Slots)
            {
                if (slot.ModuleUID == null)
                    EmptySlots = false;

                if (slot.Module != null)
                {
                    Off += slot.Module.CalculateModuleOffense();
                    Def += slot.Module.CalculateModuleDefense((int) Size);
                }
                if (slot.ModuleUID == null || !ResourceManager.GetModuleTemplate(slot.ModuleUID).IsCommandModule)
                    continue;

                hasBridge = true;
            }
            strength = (Def > Off ? Off * 2 : Def + Off);
            if (strength > 0)
            {
                this.DrawStat(ref Cursor, string.Concat(Localizer.Token(6190), ":"), strength, 227);
                Cursor.Y = Cursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 2);
            }
            Vector2 CursorReq = new Vector2((float) (this.StatsSub.Menu.X - 180),
                (float) (this.ShipStats.Menu.Y + (Fonts.Arial12Bold.LineSpacing * 2) + 45));
            if (this.ActiveHull.Role != ShipData.RoleName.platform)
            {
                this.DrawRequirement(ref CursorReq, Localizer.Token(120), hasBridge);
                CursorReq.Y = CursorReq.Y + (float) (Fonts.Arial12Bold.LineSpacing + 2);
            }
            this.DrawRequirement(ref CursorReq, Localizer.Token(121), EmptySlots);
        }

        private void DrawHullBonus(ref Vector2 Cursor, string words, float stat)
        {
            ScreenManager.SpriteBatch.DrawString(Fonts.Verdana12,
                string.Concat((stat * 100f).ToString(), "% ", words), Cursor, Color.Orange);
        }

        public void DrawStatColor(ref Vector2 Cursor, string words, float stat, int Tooltip_ID, Color color
            , bool doGoodBadTint = true, bool isPercent = false)
        {
            SpriteFont font = Fonts.Arial12Bold;
            float amount = Spacing(120f);
            DrawString(Cursor, color, words, font);
            string numbers = "0.0";
            numbers = isPercent ? stat.ToString("P1") : GetNumberString(stat);
            if (stat == 0f) numbers = "0.0";
            Cursor = FontSpace(Cursor, amount, numbers, font);

            color = doGoodBadTint ? (stat > 0f ? Color.LightGreen : Color.LightPink) : Color.White;
            DrawString(Cursor, color, numbers, font);

            Cursor = FontBackSpace(Cursor, amount, numbers, font);

            CheckToolTip(Tooltip_ID, Cursor, words, numbers, font, MousePos);
        }

        public void DrawStat(ref Vector2 Cursor, string words, float stat, int Tooltip_ID, bool doGoodBadTint = true
            , bool isPercent = false)
        {
            DrawStatColor(ref Cursor, words, stat, Tooltip_ID, Color.White, doGoodBadTint, isPercent);
        }

        public void DrawStat(ref Vector2 Cursor, string words, string stat, int Tooltip_ID, Color nameColor,
            Color statColor, float spacing = 165f)
        {
            SpriteFont font = Fonts.Arial12Bold;
            float amount = Spacing(spacing);
            Color color = nameColor;
            DrawString(Cursor, color, words, font);
            Cursor = FontSpace(Cursor, amount, words, font);
            color = statColor;
            DrawString(Cursor, color, stat, font);
            Cursor = FontBackSpace(Cursor, amount, stat, font);
            CheckToolTip(Tooltip_ID, Cursor, words, stat, font, MousePos);
        }

        private void DrawStatEnergy(ref Vector2 Cursor, string words, string stat, int Tooltip_ID)
        {
            DrawStat(ref Cursor, words, stat, Tooltip_ID, Color.LightSkyBlue, Color.LightGreen);
        }

        private void DrawStatPropulsion(ref Vector2 Cursor, string words, string stat, int Tooltip_ID)
        {
            DrawStat(ref Cursor, words, stat, Tooltip_ID, Color.DarkSeaGreen, Color.LightGreen);
        }

        private void DrawStatOrdnance(ref Vector2 Cursor, string words, string stat, int Tooltip_ID)
        {
            DrawStat(ref Cursor, words, stat, Tooltip_ID, Color.White, Color.LightGreen);
        }

        private void DrawStatBad(ref Vector2 Cursor, string words, string stat, int Tooltip_ID)
        {
            DrawStat(ref Cursor, words, stat, Tooltip_ID, Color.White, Color.LightPink, 165);
        }

        private void DrawStat(ref Vector2 Cursor, string words, string stat, int Tooltip_ID)
        {
            DrawStat(ref Cursor, words, stat, Tooltip_ID, Color.White, Color.LightGreen, 165);
        }

        private void DrawStatEnergyBad(ref Vector2 Cursor, string words, string stat, int Tooltip_ID)
        {
            DrawStat(ref Cursor, words, stat, Tooltip_ID, Color.LightSkyBlue, Color.LightPink, 165);
        }

        private void DrawUI(GameTime gameTime)
        {
            this.EmpireUI.Draw(ScreenManager.SpriteBatch);
            this.DrawShipInfoPanel();

            //Defaults based on hull types
            //Freighter hull type defaults to Civilian behaviour when the hull is selected, player has to actively opt to change classification to disable flee/freighter behaviour
            if (this.ActiveHull.Role == ShipData.RoleName.freighter && this.Fml)
            {
                this.CategoryList.ActiveIndex = 1;
                this.Fml = false;
            }
            //Scout hull type defaults to Recon behaviour. Not really important, as the 'Recon' tag is going to supplant the notion of having 'Fighter' class hulls automatically be scouts, but it makes things easier when working with scout hulls without existing categorisation.
            else if (this.ActiveHull.Role == ShipData.RoleName.scout && this.Fml)
            {
                this.CategoryList.ActiveIndex = 2;
                this.Fml = false;
            }
            //All other hulls default to unclassified.
            else if (this.Fml)
            {
                this.CategoryList.ActiveIndex = 0;
                this.Fml = false;
            }

            //Loads the Category from the ShipDesign XML of the ship being loaded, and loads this OVER the hull type default, very importantly.
            if (Fmlevenmore && CategoryList.SetActiveEntry(LoadCategory.ToString()))
            {
                Fmlevenmore = false;
            }

            this.CategoryList.Draw(ScreenManager.SpriteBatch);
            this.CarrierOnlyBox.Draw(ScreenManager.SpriteBatch);
            string classifTitle = "Behaviour Presets";
            this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial14Bold, classifTitle, ClassifCursor, Color.Orange);
            float transitionOffset = (float) Math.Pow((double) TransitionPosition, 2);
            Rectangle r = this.BlackBar;
            if (ScreenState == ScreenState.TransitionOn ||
                ScreenState == ScreenState.TransitionOff)
            {
                r.Y = r.Y + (int) (transitionOffset * 50f);
            }
            ScreenManager.SpriteBatch.FillRectangle(r, Color.Black);
            r = this.BottomSep;
            if (ScreenState == ScreenState.TransitionOn ||
                ScreenState == ScreenState.TransitionOff)
            {
                r.Y = r.Y + (int) (transitionOffset * 50f);
            }
            ScreenManager.SpriteBatch.FillRectangle(r, new Color(77, 55, 25));
            r = this.SearchBar;
            if (ScreenState == ScreenState.TransitionOn ||
                ScreenState == ScreenState.TransitionOff)
            {
                r.Y = r.Y + (int) (transitionOffset * 50f);
            }
            ScreenManager.SpriteBatch.FillRectangle(r, new Color(54, 54, 54));
            if (Fonts.Arial20Bold.MeasureString(this.ActiveHull.Name).X <= (float) (this.SearchBar.Width - 5))
            {
                Vector2 Cursor = new Vector2((float) (this.SearchBar.X + 3),
                    (float) (r.Y + 14 - Fonts.Arial20Bold.LineSpacing / 2));
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, this.ActiveHull.Name, Cursor, Color.White);
            }
            else
            {
                Vector2 Cursor = new Vector2((float) (this.SearchBar.X + 3),
                    (float) (r.Y + 14 - Fonts.Arial12Bold.LineSpacing / 2));
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, this.ActiveHull.Name, Cursor, Color.White);
            }
            r = this.SaveButton.Rect;
            if (ScreenState == ScreenState.TransitionOn ||
                ScreenState == ScreenState.TransitionOff)
            {
                r.Y = r.Y + (int) (transitionOffset * 50f);
            }
            this.SaveButton.Draw(ScreenManager.SpriteBatch, r);
            r = this.LoadButton.Rect;
            if (ScreenState == ScreenState.TransitionOn ||
                ScreenState == ScreenState.TransitionOff)
            {
                r.Y = r.Y + (int) (transitionOffset * 50f);
            }
            this.LoadButton.Draw(ScreenManager.SpriteBatch, r);
            r = this.ToggleOverlayButton.Rect;
            if (ScreenState == ScreenState.TransitionOn ||
                ScreenState == ScreenState.TransitionOff)
            {
                r.Y = r.Y + (int) (transitionOffset * 50f);
            }
            this.ToggleOverlayButton.Draw(ScreenManager.SpriteBatch, r);
            this.DrawModuleSelection();
            this.DrawHullSelection();
 
            foreach (ToggleButton button in this.CombatStatusButtons)
            {
                button.Draw(ScreenManager);
            }
            if (IsActive)
            {
                ToolTip.Draw(ScreenManager.SpriteBatch);
            }
        }
    }
}